using Microsoft.Extensions.Hosting;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Exceptions;
using ProjectSift.Domain.Models;
using ProjectSift.Service.Constants;

namespace ProjectSift.Service.BackgroundServices;

public sealed class BatchProcessingBackgroundService(
    IBatchWorkQueue queue,
    IBatchProcessor batchProcessor,
    IBatchStore batchStore,
    ILogger<BatchProcessingBackgroundService> logger) : BackgroundService
{
    private const int MaxConflictRetries = 3;

    private readonly IBatchWorkQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    private readonly IBatchProcessor _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
    private readonly IBatchStore _batchStore = batchStore ?? throw new ArgumentNullException(nameof(batchStore));
    private readonly ILogger<BatchProcessingBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
        {
            await ProcessQueuedBatchAsync(request, stoppingToken);
        }
    }

    private async Task ProcessQueuedBatchAsync(BatchProcessingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var processed = await _batchProcessor.ProcessAsync(request, cancellationToken);
            await PersistCompletedBatchAsync(request.BatchId, processed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PayloadProcessingException exception)
        {
            await PersistFailedBatchAsync(request, exception.GetType().Name, cancellationToken);
            _logger.LogWarning(exception,
                "Batch processing handled payload failure for batch {BatchId}. FailureCategory: {FailureCategory}",
                request.BatchId,
                exception.GetType().Name);
        }
        catch (Exception exception)
        {
            await PersistFailedBatchAsync(request, exception.GetType().Name, cancellationToken);
            _logger.LogError(exception,
                "Batch processing failed for batch {BatchId}. FailureCategory: {FailureCategory}",
                request.BatchId,
                exception.GetType().Name);
        }
    }

    private async Task PersistCompletedBatchAsync(string batchId, BatchResult processedResult, CancellationToken cancellationToken)
    {
        await PersistWithRetriesAsync(
            batchId,
            current => BuildCompletedResult(current, processedResult),
            onMissing: () => _logger.LogWarning(
                "Batch {BatchId} could not be updated because it no longer exists.",
                batchId),
            onRetryExhausted: () => throw new ConflictException(ServiceConstants.BatchProcessing.ConflictRetryExhaustedMessage),
            cancellationToken);
    }

    private async Task PersistFailedBatchAsync(BatchProcessingRequest request, string failureCategory, CancellationToken cancellationToken)
    {
        var diagnosticsPayloads = CreateFailurePayloads(request, failureCategory);
        await PersistWithRetriesAsync(
            request.BatchId,
            current => BuildFailedResult(current, request.BatchId, diagnosticsPayloads),
            onMissing: static () => { },
            onRetryExhausted: () => _logger.LogWarning(
                "Batch {BatchId} could not be marked completed after conflict retries. FailureCategory: {FailureCategory}",
                request.BatchId,
                failureCategory),
            cancellationToken);
    }

    private async Task PersistWithRetriesAsync(
        string batchId,
        Func<BatchResult, BatchResult?> buildResult,
        Action onMissing,
        Action onRetryExhausted,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConflictRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = await _batchStore.GetByIdAsync(batchId, cancellationToken);
            if (current is null)
            {
                onMissing();
                return;
            }

            var update = buildResult(current);
            if (update is null)
            {
                return;
            }

            try
            {
                _ = await _batchStore.UpdateAsync(update, cancellationToken);
                return;
            }
            catch (ConflictException) when (attempt < MaxConflictRetries)
            {
            }
        }
        onRetryExhausted();
    }

    private static BatchResult? BuildCompletedResult(BatchResult current, BatchResult processedResult)
    {
        if (current.Status == BatchStatus.Completed)
        {
            return null;
        }

        return new BatchResult(
            current.BatchId,
            BatchStatus.Completed,
            processedResult.Payloads,
            current.CreatedUtc,
            processedResult.CompletedUtc ?? DateTimeOffset.UtcNow,
            current.Etag);
    }

    private static BatchResult? BuildFailedResult(BatchResult current, string batchId, IReadOnlyList<PayloadResult> payloads)
    {
        if (current.Status == BatchStatus.Completed)
        {
            return null;
        }

        return new BatchResult(
            batchId,
            BatchStatus.Completed,
            payloads,
            current.CreatedUtc,
            DateTimeOffset.UtcNow,
            current.Etag);
    }

    private static IReadOnlyList<PayloadResult> CreateFailurePayloads(BatchProcessingRequest request, string failureCategory)
    {
        return request.Payloads
            .Select(payload => new PayloadResult(payload.PayloadIndex, diagnostics:
            [
                new ExtractionDiagnostic(
                    ServiceConstants.BatchProcessing.UnexpectedProcessingFailureCode,
                    ServiceConstants.BatchProcessing.UnexpectedProcessingFailureMessage(failureCategory))
            ]))
            .ToArray();
    }
}
