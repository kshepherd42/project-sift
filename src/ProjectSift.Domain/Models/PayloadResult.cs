using ProjectSift.Domain.Validation;

namespace ProjectSift.Domain.Models;

public sealed record PayloadResult
{
    public PayloadResult(
        int payloadIndex,
        IEnumerable<DocumentResult>? documents = null,
        IEnumerable<ExtractionDiagnostic>? diagnostics = null,
        string? payloadName = null)
    {
        PayloadIndex = Guard.NonNegative(payloadIndex, nameof(payloadIndex));
        Documents = documents?.ToArray() ?? [];
        Diagnostics = diagnostics?.ToArray() ?? [];
        PayloadName = string.IsNullOrWhiteSpace(payloadName) ? null : payloadName;
    }
    public int PayloadIndex { get; }
    public string? PayloadName { get; }
    public IReadOnlyList<DocumentResult> Documents { get; }
    public IReadOnlyList<ExtractionDiagnostic> Diagnostics { get; }
}
