using ProjectSift.Application.Models;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;
using ProjectSift.Service.Constants;

namespace ProjectSift.Service.BackgroundServices;

public sealed class BatchSubmissionService(
    IBatchStore batchStore,
    IBatchWorkQueue queue) : IBatchSubmissionService
{
    private readonly IBatchStore _batchStore = batchStore ?? throw new ArgumentNullException(nameof(batchStore));
    private readonly IBatchWorkQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));

    public async Task<BatchResult> QueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var created = await _batchStore.CreateAsync(request.BatchId, cancellationToken);
        var queued = await _queue.TryEnqueueAsync(request, cancellationToken);

        if (queued)
        {
            return created;
        }

        _ = await _batchStore.UpdateAsync(
            new BatchResult(
                created.BatchId,
                BatchStatus.Completed,
                request.Payloads.Select(payload => new PayloadResult(payload.PayloadIndex, diagnostics:
                [
                    new ExtractionDiagnostic(
                        ServiceConstants.BatchProcessing.QueueRejectedDiagnosticCode,
                        ServiceConstants.BatchProcessing.QueueFullMessage)
                ])),
                created.CreatedUtc,
                DateTimeOffset.UtcNow,
                created.Etag),
            cancellationToken);

        throw new BatchQueueFullException(ServiceConstants.BatchProcessing.QueueFullMessage, created.BatchId);
    }
}
