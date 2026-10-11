using ProjectSift.Domain;

namespace ProjectSift.Application;

public interface IBatchStore
{
    Task<BatchResult> CreateAsync(string batchId, CancellationToken cancellationToken = default);

    Task<BatchResult> UpdateAsync(BatchResult batchResult, CancellationToken cancellationToken = default);

    Task<BatchResult?> GetByIdAsync(string batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetExpiredBatchIdsAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);
}
