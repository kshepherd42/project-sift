namespace ProjectSift.Application;

public sealed record BatchProcessingRequest
{
    public BatchProcessingRequest(string batchId, IEnumerable<PayloadInput> payloads)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("Batch ID cannot be null, empty, or whitespace.", nameof(batchId));
        }

        ArgumentNullException.ThrowIfNull(payloads);

        BatchId = batchId;
        Payloads = payloads.ToArray();
    }

    public string BatchId { get; }

    public IReadOnlyList<PayloadInput> Payloads { get; }
}
