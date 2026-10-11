using ProjectSift.Application.Models;
using ProjectSift.Domain.Models;

namespace ProjectSift.Service.BackgroundServices;

public interface IBatchSubmissionService
{
    Task<BatchResult> QueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default);
}
