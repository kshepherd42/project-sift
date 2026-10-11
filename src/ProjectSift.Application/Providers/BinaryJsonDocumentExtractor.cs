using System.Text.Json;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class BinaryJsonDocumentExtractor : IDocumentExtractor
{
    private readonly BinaryJsonExtractionOptions _options;
    private readonly IPropertyPathCollector _propertyPathCollector;

    public BinaryJsonDocumentExtractor(BinaryJsonExtractionOptions? options = null, IPropertyPathCollector? propertyPathCollector = null)
    {
        _options = options ?? new BinaryJsonExtractionOptions();
        _propertyPathCollector = propertyPathCollector ?? new JsonPropertyPathCollector(_options, new PropertyPathFormatter());
    }

    public Task<DocumentResult> ExtractAsync(DocumentCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        cancellationToken.ThrowIfCancellationRequested();

        if (TryCreateEarlyFailureResult(candidate, out var earlyFailure))
        {
            return Task.FromResult(earlyFailure);
        }

        try
        {
            using var document = ParseCandidateDocument(candidate.Content);
            return Task.FromResult(ExtractFromParsedDocument(candidate, document.RootElement, cancellationToken));
        }
        catch (JsonException jsonException)
        {
            return Task.FromResult(CreateInvalidJsonFailureResult(candidate, jsonException));
        }
    }

    private bool TryCreateEarlyFailureResult(DocumentCandidate candidate, out DocumentResult result)
    {
        if (candidate.Content.IsEmpty)
        {
            result = CreateFailedResult(
                candidate.DocumentIndex,
                DocumentType.Unknown,
                ExtractionDiagnosticCodes.CandidateEmpty,
                ExtractionMessages.CandidateContentEmpty,
                0);

            return true;
        }

        if (candidate.Content.Length > _options.MaxCandidateBytes)
        {
            result = CreateFailedResult(
                candidate.DocumentIndex,
                InferDocumentType(candidate.Content.Span),
                ExtractionDiagnosticCodes.CandidateTooLarge,
                ExtractionMessages.CandidateSizeLimitExceeded(_options.MaxCandidateBytes),
                0);

            return true;
        }

        result = null!;
        return false;
    }

    private JsonDocument ParseCandidateDocument(ReadOnlyMemory<byte> content)
        => JsonDocument.Parse(content, new JsonDocumentOptions
        {
            MaxDepth = _options.MaxNestingDepth
        });

    private DocumentResult ExtractFromParsedDocument(
        DocumentCandidate candidate,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        var rootType = ToDocumentType(root.ValueKind);
        if (rootType is DocumentType.Unknown)
        {
            return CreateFailedResult(
                candidate.DocumentIndex,
                DocumentType.Unknown,
                ExtractionDiagnosticCodes.CandidateUnsupportedRoot,
                ExtractionMessages.CandidateUnsupportedRoot,
                null);
        }

        var collectionResult = _propertyPathCollector.Collect(root, cancellationToken);
        if (!collectionResult.Succeeded)
        {
            return CreateFailedResult(
                candidate.DocumentIndex,
                rootType,
                ExtractionDiagnosticCodes.CandidatePathLimitExceeded,
                ExtractionMessages.CandidatePathLimitExceeded(_options.MaxPathsPerDocument),
                null);
        }

        return CreateSuccessfulResult(candidate.DocumentIndex, rootType, collectionResult.Paths);
    }

    private static DocumentResult CreateSuccessfulResult(int documentIndex, DocumentType rootType, IReadOnlyCollection<string> paths)
    {
        var orderedPaths = paths
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new PropertyPath(path))
            .ToArray();

        return new DocumentResult(
            documentIndex,
            rootType,
            DocumentStatus.Successful,
            orderedPaths);
    }

    private static DocumentResult CreateFailedResult(
        int documentIndex,
        DocumentType type,
        string diagnosticCode,
        string diagnosticMessage,
        int? position)
    {
        var diagnostic = new ExtractionDiagnostic(diagnosticCode, diagnosticMessage, position);
        return new DocumentResult(
            documentIndex,
            type,
            DocumentStatus.Failed,
            diagnostics: [diagnostic]);
    }

    private static DocumentResult CreateInvalidJsonFailureResult(DocumentCandidate candidate, JsonException jsonException)
    {
        int? position = jsonException.BytePositionInLine >= 0 && jsonException.BytePositionInLine <= int.MaxValue
            ? (int)jsonException.BytePositionInLine
            : null;

        return CreateFailedResult(
            candidate.DocumentIndex,
            InferDocumentType(candidate.Content.Span),
            ExtractionDiagnosticCodes.CandidateInvalidJson,
            ExtractionMessages.CandidateInvalidJson,
            position);
    }

    private static DocumentType InferDocumentType(ReadOnlySpan<byte> content)
    {
        foreach (var value in content)
        {
            if (value is <= 0x20)
            {
                continue;
            }

            return value switch
            {
                (byte)'{' => DocumentType.Object,
                (byte)'[' => DocumentType.Array,
                _ => DocumentType.Unknown
            };
        }

        return DocumentType.Unknown;
    }

    private static DocumentType ToDocumentType(JsonValueKind valueKind) => valueKind switch
    {
        JsonValueKind.Object => DocumentType.Object,
        JsonValueKind.Array => DocumentType.Array,
        _ => DocumentType.Unknown
    };

}
