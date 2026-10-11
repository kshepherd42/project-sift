using ProjectSift.Domain.Exceptions;
using ProjectSift.Domain.Models;
using ProjectSift.Infrastructure.Constants;

namespace ProjectSift.Infrastructure.Persistence.InMemory;

public sealed class InMemoryBatchDatabase
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, StoredBatchRecord> _records = new(StringComparer.Ordinal);

    public BatchResult Create(BatchResult batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_syncRoot)
        {
            if (_records.ContainsKey(batch.BatchId))
            {
                throw new ConflictException(InfrastructureMessages.BatchAlreadyExists);
            }

            var record = StoredBatchRecord.FromBatch(batch);
            _records[batch.BatchId] = record;
            return record.ToBatchResult();
        }
    }

    public BatchResult Update(BatchResult batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_syncRoot)
        {
            if (!_records.TryGetValue(batch.BatchId, out var current))
            {
                throw new KeyNotFoundException(InfrastructureMessages.BatchNotFound);
            }

            if (current.Etag != batch.Etag)
            {
                throw new ConflictException(InfrastructureMessages.EtagMismatch);
            }

            var nextBatch = new BatchResult(
                batch.BatchId,
                batch.Status,
                batch.Payloads,
                batch.CreatedUtc,
                batch.CompletedUtc,
                current.Etag + 1);

            var next = StoredBatchRecord.FromBatch(nextBatch);
            _records[batch.BatchId] = next;
            return next.ToBatchResult();
        }
    }

    public BatchResult? GetById(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        lock (_syncRoot)
        {
            return _records.TryGetValue(batchId, out var record)
                ? record.ToBatchResult()
                : null;
        }
    }

    public bool DeleteById(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        lock (_syncRoot)
        {
            return _records.Remove(batchId);
        }
    }

    private sealed record StoredBatchRecord(
        string BatchId,
        BatchResult Batch,
        long Etag)
    {
        public static StoredBatchRecord FromBatch(BatchResult batch)
            => new(
                batch.BatchId,
                new BatchResult(batch.BatchId, batch.Status, batch.Payloads, batch.CreatedUtc, batch.CompletedUtc, batch.Etag),
                batch.Etag);

        public BatchResult ToBatchResult()
            => new(Batch.BatchId, Batch.Status, Batch.Payloads, Batch.CreatedUtc, Batch.CompletedUtc, Etag);
    }
}
