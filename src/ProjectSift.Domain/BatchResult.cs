namespace ProjectSift.Domain;

public sealed record BatchResult
{
    public BatchResult(
        string batchId,
        BatchStatus status,
        IEnumerable<PayloadResult>? payloads = null,
        DateTimeOffset? createdUtc = null,
        DateTimeOffset? completedUtc = null)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("Batch ID cannot be null, empty, or whitespace.", nameof(batchId));
        }

        BatchId = batchId;
        Status = status;
        Payloads = payloads?.ToArray() ?? [];
        CreatedUtc = createdUtc ?? DateTimeOffset.UtcNow;
        CompletedUtc = completedUtc;
    }

    public string BatchId { get; }

    public BatchStatus Status { get; }

    public IReadOnlyList<PayloadResult> Payloads { get; }

    public DateTimeOffset CreatedUtc { get; }

    public DateTimeOffset? CompletedUtc { get; }
}
