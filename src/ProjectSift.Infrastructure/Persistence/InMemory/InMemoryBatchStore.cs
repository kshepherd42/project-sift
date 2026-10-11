using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;
using ProjectSift.Domain.Validation;

namespace ProjectSift.Infrastructure.Persistence.InMemory;

public sealed class InMemoryBatchStore : IBatchStore
{
    private readonly InMemoryBatchDatabase _database;

    public InMemoryBatchStore(InMemoryBatchDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public Task<BatchResult> CreateAsync(string batchId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var created = _database.Create(new BatchResult(
            Guard.NotNullOrWhiteSpace(batchId, nameof(batchId)),
            BatchStatus.Processing,
            createdUtc: DateTimeOffset.UtcNow,
            etag: 0));

        return Task.FromResult(created);
    }

    public Task<BatchResult> UpdateAsync(BatchResult batchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batchResult);
        cancellationToken.ThrowIfCancellationRequested();

        var updated = _database.Update(batchResult);
        return Task.FromResult(updated);
    }

    public Task<BatchResult?> GetByIdAsync(string batchId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _database.GetById(Guard.NotNullOrWhiteSpace(batchId, nameof(batchId)));
        return Task.FromResult(result);
    }

}

