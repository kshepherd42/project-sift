using ProjectSift.Domain.Validation;

namespace ProjectSift.Domain.Models;

public sealed record PayloadResult
{
    public PayloadResult(
        int payloadIndex,
        IEnumerable<DocumentResult>? documents = null,
        IEnumerable<ExtractionDiagnostic>? diagnostics = null)
    {
        PayloadIndex = Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        Documents = documents?.ToArray() ?? [];
        Diagnostics = diagnostics?.ToArray() ?? [];
    }
    public int PayloadIndex { get; }
    public IReadOnlyList<DocumentResult> Documents { get; }
    public IReadOnlyList<ExtractionDiagnostic> Diagnostics { get; }
}
