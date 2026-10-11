using ProjectSift.Domain.Enums;

namespace ProjectSift.Service.Controllers.v1.Models;

public sealed record BatchResponse(
    string BatchId,
    BatchStatus Status,
    IReadOnlyList<PayloadResponse> Payloads,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc);

public sealed record PayloadResponse(
    int PayloadIndex,
    string? PayloadName,
    IReadOnlyList<DocumentResponse> Documents,
    IReadOnlyList<DiagnosticResponse> Diagnostics);

public sealed record DocumentResponse(
    int DocumentIndex,
    string? DocumentName,
    DocumentType Type,
    DocumentStatus Status,
    IReadOnlyList<PropertyPathResponse> PropertyPaths,
    IReadOnlyList<DiagnosticResponse> Diagnostics);

public sealed record PropertyPathResponse(string Value);

public sealed record DiagnosticResponse(string Code, string Message, int? Position);
