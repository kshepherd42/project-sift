using ProjectSift.Domain.Exceptions;
using ProjectSift.Domain.Models;
using ProjectSift.Infrastructure.Constants;
using ProjectSift.Infrastructure.Models;

namespace ProjectSift.Infrastructure.Persistence.InMemory;

public sealed class InMemoryBatchDatabase
{
    private readonly Dictionary<string, InfrastructureBatchResult> _records = new(StringComparer.Ordinal);
    private readonly Lock _sync = new();

    public BatchResult Create(BatchResult batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_sync)
        {
            if (_records.ContainsKey(batch.BatchId))
            {
                throw new ConflictException(InfrastructureMessages.BatchAlreadyExists);
            }

            var record = InfrastructureBatchResultMapper.ToInfrastructure(batch) with { Etag = 0 };
            _records[batch.BatchId] = record;
            return InfrastructureBatchResultMapper.ToDomain(record);
        }
    }

    public BatchResult Update(BatchResult batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_sync)
        {
            if (!_records.TryGetValue(batch.BatchId, out var current))
            {
                throw new KeyNotFoundException(InfrastructureMessages.BatchNotFound);
            }

            if (current.Etag != batch.Etag)
            {
                throw new ConflictException(InfrastructureMessages.EtagMismatch);
            }

            var next = InfrastructureBatchResultMapper.ToInfrastructure(batch) with
            {
                Etag = current.Etag + 1
            };

            _records[batch.BatchId] = next;
            return InfrastructureBatchResultMapper.ToDomain(next);
        }
    }

    public BatchResult? GetById(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        lock (_sync)
        {
            return _records.TryGetValue(batchId, out var record)
                ? InfrastructureBatchResultMapper.ToDomain(record)
                : null;
        }
    }

    public bool DeleteById(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        lock (_sync)
        {
            return _records.Remove(batchId);
        }
    }
}
