using ProjectSift.Domain.Models;

namespace ProjectSift.Domain.Abstractions.Infrastructure;

public interface IBatchStore
{
    Task<BatchResult> CreateAsync(string batchId, CancellationToken cancellationToken = default);
    Task<BatchResult> UpdateAsync(BatchResult batchResult, CancellationToken cancellationToken = default);
    Task<BatchResult?> GetByIdAsync(string batchId, CancellationToken cancellationToken = default);
}
