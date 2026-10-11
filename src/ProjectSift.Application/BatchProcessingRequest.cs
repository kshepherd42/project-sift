namespace ProjectSift.Application;

public sealed record BatchProcessingRequest
{
    public BatchProcessingRequest(string batchId, IEnumerable<PayloadInput> payloads)
    {
        BatchId = Domain.Guard.NotNullOrWhiteSpace(batchId, nameof(batchId));
        ArgumentNullException.ThrowIfNull(payloads);
        Payloads = payloads.ToArray();
    }
    public string BatchId { get; }
    public IReadOnlyList<PayloadInput> Payloads { get; }
}
