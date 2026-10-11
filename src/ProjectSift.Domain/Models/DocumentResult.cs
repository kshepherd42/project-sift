using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Validation;

namespace ProjectSift.Domain.Models;

public sealed record DocumentResult
{
    public DocumentResult(
        int documentIndex,
        DocumentType type,
        DocumentStatus status,
        IEnumerable<PropertyPath>? propertyPaths = null,
        IEnumerable<ExtractionDiagnostic>? diagnostics = null)
    {
        DocumentIndex = Guard.NonNegative(documentIndex, nameof(documentIndex));
        Type = type;
        Status = status;
        PropertyPaths = propertyPaths?.ToArray() ?? [];
        Diagnostics = diagnostics?.ToArray() ?? [];
    }
    public int DocumentIndex { get; }
    public DocumentType Type { get; }
    public DocumentStatus Status { get; }
    public IReadOnlyList<PropertyPath> PropertyPaths { get; }
    public IReadOnlyList<ExtractionDiagnostic> Diagnostics { get; }
}
