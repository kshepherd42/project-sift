using ProjectSift.Application.Models;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Abstractions;

public interface IBatchProcessor
{
    Task<BatchResult> ProcessAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default);
}
