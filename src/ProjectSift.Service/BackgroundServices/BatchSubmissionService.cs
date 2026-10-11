using ProjectSift.Application.Models;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;
using ProjectSift.Service.Constants;

namespace ProjectSift.Service.BackgroundServices;

public sealed class BatchSubmissionService(
    IBatchStore batchStore,
    IBatchWorkQueue queue,
    ILogger<BatchSubmissionService> logger) : IBatchSubmissionService
{
    private readonly IBatchStore _batchStore = batchStore ?? throw new ArgumentNullException(nameof(batchStore));
    private readonly IBatchWorkQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    private readonly ILogger<BatchSubmissionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<BatchResult> QueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var created = await _batchStore.CreateAsync(request.BatchId, cancellationToken);

        bool queued;
        try
        {
            queued = await _queue.TryEnqueueAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkSubmissionCancelledAsync(request, created);
            throw;
        }

        if (queued)
        {
            return created;
        }

        try
        {
            _ = await _batchStore.UpdateAsync(
                new BatchResult(
                    created.BatchId,
                    BatchStatus.Completed,
                    request.Payloads.Select(payload => new PayloadResult(payload.PayloadIndex, diagnostics:
                    [
                        new ExtractionDiagnostic(
                            ServiceConstants.BatchProcessing.QueueRejectedDiagnosticCode,
                            ServiceConstants.BatchProcessing.QueueFullMessage)
                    ], payloadName: payload.PayloadName)),
                    created.CreatedUtc,
                    DateTimeOffset.UtcNow,
                    created.Etag),
                cancellationToken);
        }
        finally
        {
            PayloadMemoryReleaser.ClearPayloadBuffers(request.Payloads);
        }

        _logger.LogWarning(
            "Batch submission rejected because queue is full. BatchId: {BatchId}. DiagnosticCode: {DiagnosticCode}",
            created.BatchId,
            ServiceConstants.BatchProcessing.QueueRejectedDiagnosticCode);

        throw new BatchQueueFullException(ServiceConstants.BatchProcessing.QueueFullMessage, created.BatchId);
    }

    private async Task MarkSubmissionCancelledAsync(BatchProcessingRequest request, BatchResult created)
    {
        try
        {
            _ = await _batchStore.UpdateAsync(
                new BatchResult(
                    created.BatchId,
                    BatchStatus.Completed,
                    request.Payloads.Select(payload => new PayloadResult(payload.PayloadIndex, diagnostics:
                    [
                        new ExtractionDiagnostic(
                            ServiceConstants.BatchProcessing.ProcessingCancelledDiagnosticCode,
                            ServiceConstants.BatchProcessing.ProcessingCancelledMessage(nameof(OperationCanceledException)))
                    ], payloadName: payload.PayloadName)),
                    created.CreatedUtc,
                    DateTimeOffset.UtcNow,
                    created.Etag),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Batch submission cancellation could not persist completion. BatchId: {BatchId}. FailureCategory: {FailureCategory}. DiagnosticCode: {DiagnosticCode}",
                created.BatchId,
                exception.GetType().Name,
                ServiceConstants.BatchProcessing.ProcessingCancelledDiagnosticCode);
        }
        finally
        {
            PayloadMemoryReleaser.ClearPayloadBuffers(request.Payloads);
        }
    }
}
