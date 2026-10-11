using System.Text;
using System.Text.Json;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class BinaryJsonDocumentExtractor(BinaryJsonExtractionOptions? options = null) : IDocumentExtractor
{
    private readonly BinaryJsonExtractionOptions _options = options ?? new BinaryJsonExtractionOptions();

    public Task<DocumentResult> ExtractAsync(DocumentCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.Content.IsEmpty)
        {
            var emptyDiagnostic = new ExtractionDiagnostic(
                ExtractionDiagnosticCodes.CandidateEmpty,
                "Candidate content is empty.",
                0);

            return Task.FromResult(new DocumentResult(
                candidate.DocumentIndex,
                DocumentType.Unknown,
                DocumentStatus.Failed,
                diagnostics: [emptyDiagnostic]));
        }

        if (candidate.Content.Length > _options.MaxCandidateBytes)
        {
            var sizeDiagnostic = new ExtractionDiagnostic(
                ExtractionDiagnosticCodes.CandidateTooLarge,
                $"Candidate exceeds the configured size limit of {_options.MaxCandidateBytes} bytes.",
                0);

            return Task.FromResult(new DocumentResult(
                candidate.DocumentIndex,
                InferDocumentType(candidate.Content.Span),
                DocumentStatus.Failed,
                diagnostics: [sizeDiagnostic]));
        }

        try
        {
            using var document = JsonDocument.Parse(candidate.Content, new JsonDocumentOptions
            {
                MaxDepth = _options.MaxNestingDepth
            });

            var rootType = ToDocumentType(document.RootElement.ValueKind);
            if (rootType is DocumentType.Unknown)
            {
                var unsupportedRoot = new ExtractionDiagnostic(
                    ExtractionDiagnosticCodes.CandidateUnsupportedRoot,
                    "Candidate root is neither a JSON object nor array.");

                return Task.FromResult(new DocumentResult(
                    candidate.DocumentIndex,
                    DocumentType.Unknown,
                    DocumentStatus.Failed,
                    diagnostics: [unsupportedRoot]));
            }

            var paths = new HashSet<string>(StringComparer.Ordinal);
            if (!CollectPaths(document.RootElement, string.Empty, 0, paths))
            {
                var pathLimitDiagnostic = new ExtractionDiagnostic(
                    ExtractionDiagnosticCodes.CandidatePathLimitExceeded,
                    $"Candidate exceeded the configured path limit of {_options.MaxPathsPerDocument}.");

                return Task.FromResult(new DocumentResult(
                    candidate.DocumentIndex,
                    rootType,
                    DocumentStatus.Failed,
                    diagnostics: [pathLimitDiagnostic]));
            }

            var orderedPaths = paths
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new PropertyPath(path))
                .ToArray();

            return Task.FromResult(new DocumentResult(
                candidate.DocumentIndex,
                rootType,
                DocumentStatus.Successful,
                orderedPaths));
        }
        catch (JsonException jsonException)
        {
            var invalidJson = new ExtractionDiagnostic(
                ExtractionDiagnosticCodes.CandidateInvalidJson,
                "Candidate is not valid JSON.",
                jsonException.BytePositionInLine >= 0 && jsonException.BytePositionInLine <= int.MaxValue
                    ? (int)jsonException.BytePositionInLine
                    : null);

            return Task.FromResult(new DocumentResult(
                candidate.DocumentIndex,
                InferDocumentType(candidate.Content.Span),
                DocumentStatus.Failed,
                diagnostics: [invalidJson]));
        }
    }

    private bool CollectPaths(JsonElement node, string currentPath, int depth, HashSet<string> paths)
    {
        if (depth > _options.MaxNestingDepth)
        {
            return false;
        }

        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in node.EnumerateObject())
                {
                    var propertyPath = AppendPropertySegment(currentPath, property.Name);
                    if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        if (!CollectPaths(property.Value, propertyPath, depth + 1, paths))
                        {
                            return false;
                        }

                        continue;
                    }

                    paths.Add(propertyPath);
                    if (paths.Count > _options.MaxPathsPerDocument)
                    {
                        return false;
                    }
                }

                return true;

            case JsonValueKind.Array:
                var arrayPath = AppendArraySegment(currentPath);
                paths.Add(arrayPath);
                if (paths.Count > _options.MaxPathsPerDocument)
                {
                    return false;
                }

                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        if (!CollectPaths(item, arrayPath, depth + 1, paths))
                        {
                            return false;
                        }
                    }
                }

                return true;

            default:
                if (!string.IsNullOrEmpty(currentPath))
                {
                    paths.Add(currentPath);
                    if (paths.Count > _options.MaxPathsPerDocument)
                    {
                        return false;
                    }
                }

                return true;
        }
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

    private static string AppendPropertySegment(string parentPath, string segment)
    {
        var escaped = IsSimplePropertyName(segment)
            ? segment
            : ToBracketQuotedSegment(segment);

        if (string.IsNullOrEmpty(parentPath))
        {
            return escaped;
        }

        if (escaped.StartsWith("[", StringComparison.Ordinal))
        {
            return string.Concat(parentPath, escaped);
        }

        return string.Concat(parentPath, ".", escaped);
    }

    private static string AppendArraySegment(string parentPath) => string.IsNullOrEmpty(parentPath)
        ? "[]"
        : string.Concat(parentPath, "[]");

    private static bool IsSimplePropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (!(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!(char.IsLetterOrDigit(character) || character == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string ToBracketQuotedSegment(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        builder.Append("['");

        foreach (var character in name)
        {
            if (character is '\\' or '\'')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        builder.Append("']");
        return builder.ToString();
    }
}
