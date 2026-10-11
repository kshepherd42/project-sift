using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Validation;

namespace ProjectSift.Domain.Models;

public sealed record BatchResult
{
    public BatchResult(
        string batchId,
        BatchStatus status,
        IEnumerable<PayloadResult>? payloads = null,
        DateTimeOffset? createdUtc = null,
        DateTimeOffset? completedUtc = null,
        long etag = 0)
    {
        BatchId = Guard.NotNullOrWhiteSpace(batchId, nameof(batchId));
        Status = status;
        Payloads = payloads?.ToArray() ?? [];
        CreatedUtc = createdUtc ?? DateTimeOffset.UtcNow;
        CompletedUtc = completedUtc;
        Etag = Guard.NonNegative(etag, nameof(etag));
    }
    public string BatchId { get; }
    public BatchStatus Status { get; }
    public IReadOnlyList<PayloadResult> Payloads { get; }
    public DateTimeOffset CreatedUtc { get; }
    public DateTimeOffset? CompletedUtc { get; }
    public long Etag { get; }
}
