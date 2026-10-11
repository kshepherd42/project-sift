namespace ProjectSift.Domain;

public sealed record PayloadResult
{
    public PayloadResult(
        int payloadIndex,
        IEnumerable<DocumentResult>? documents = null,
        IEnumerable<ExtractionDiagnostic>? diagnostics = null)
    {
        if (payloadIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadIndex), "Payload index cannot be negative.");
        }

        PayloadIndex = payloadIndex;
        Documents = documents?.ToArray() ?? [];
        Diagnostics = diagnostics?.ToArray() ?? [];
    }

    public int PayloadIndex { get; }

    public IReadOnlyList<DocumentResult> Documents { get; }

    public IReadOnlyList<ExtractionDiagnostic> Diagnostics { get; }
}
