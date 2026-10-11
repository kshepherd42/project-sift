using ProjectSift.Application.Models;

namespace ProjectSift.Service.BackgroundServices;

public interface IBatchWorkQueue
{
    ValueTask<bool> TryEnqueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BatchProcessingRequest> ReadAllAsync(CancellationToken cancellationToken);
}
