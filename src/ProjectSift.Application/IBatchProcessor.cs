using ProjectSift.Domain;

namespace ProjectSift.Application;

public interface IBatchProcessor
{
    Task<BatchResult> ProcessAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default);
}
