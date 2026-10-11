using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class BinaryJsonPayloadScanner
{
    private readonly IDocumentExtractor _documentExtractor;
    private readonly BinaryJsonExtractionOptions _options;

    public BinaryJsonPayloadScanner(IDocumentExtractor? documentExtractor = null, BinaryJsonExtractionOptions? options = null)
    {
        _options = options ?? new BinaryJsonExtractionOptions();
        _documentExtractor = documentExtractor ?? new BinaryJsonDocumentExtractor(_options);
    }

    public async Task<PayloadResult> ScanAsync(PayloadInput payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var documents = new List<DocumentResult>();
        var content = payload.Content.ToArray();
        var documentIndex = 0;

        for (var position = 0; position < content.Length; position++)
        {
            var current = content[position];
            if (!IsOpeningDelimiter(current))
            {
                continue;
            }

            var boundary = ScanBoundary(content, position);
            switch (boundary.Status)
            {
                case BoundaryScanStatus.Complete:
                {
                    var candidateLength = boundary.EndPosition - position + 1;
                    var candidateMemory = new ReadOnlyMemory<byte>(content, position, candidateLength);
                    var candidate = new DocumentCandidate(payload.PayloadIndex, documentIndex, candidateMemory);
                    var result = await SafeExtractAsync(candidate, position, cancellationToken);
                    documents.Add(result);
                    documentIndex++;

                    // Deterministic resynchronization rule:
                    // - successful candidate: consume the full boundary to avoid nested duplicate discovery
                    // - failed candidate: continue scanning within the same range so inner independent candidates
                    //   can still be discovered when binary framing introduces false-positive outer boundaries
                    if (result.Status is not DocumentStatus.Failed)
                    {
                        position = boundary.EndPosition;
                    }

                    break;
                }

                case BoundaryScanStatus.RecoverableIncomplete:
                {
                    var originalLength = content.Length - position;
                    var repaired = new byte[originalLength + boundary.RecoveryClosers.Count];
                    Array.Copy(content, position, repaired, 0, originalLength);

                    for (var closerIndex = 0; closerIndex < boundary.RecoveryClosers.Count; closerIndex++)
                    {
                        repaired[originalLength + closerIndex] = boundary.RecoveryClosers[closerIndex];
                    }

                    var candidate = new DocumentCandidate(payload.PayloadIndex, documentIndex, repaired);
                    var extracted = await SafeExtractAsync(candidate, position, cancellationToken);
                    var repairedDiagnostic = new ExtractionDiagnostic(
                        ExtractionDiagnosticCodes.CandidateIncompleteRecovered,
                        "Candidate ended at payload boundary and was deterministically repaired by appending missing closing delimiters.",
                        content.Length - 1);

                    var recoveredStatus = extracted.Status is DocumentStatus.Successful
                        ? DocumentStatus.Recovered
                        : extracted.Status;

                    var recoveredDiagnostics = extracted.Diagnostics.Concat([repairedDiagnostic]);
                    documents.Add(new DocumentResult(
                        documentIndex,
                        extracted.Type,
                        recoveredStatus,
                        extracted.PropertyPaths,
                        recoveredDiagnostics));

                    documentIndex++;

                    if (recoveredStatus is not DocumentStatus.Failed)
                    {
                        position = content.Length;
                    }

                    break;
                }

                case BoundaryScanStatus.TooDeep:
                {
                    var diagnostic = new ExtractionDiagnostic(
                        ExtractionDiagnosticCodes.CandidateDepthLimitExceeded,
                        $"Candidate exceeded the configured nesting limit of {_options.MaxNestingDepth}.",
                        boundary.ErrorPosition);

                    documents.Add(new DocumentResult(
                        documentIndex,
                        current == (byte)'{' ? DocumentType.Object : DocumentType.Array,
                        DocumentStatus.Failed,
                        diagnostics: [diagnostic]));

                    documentIndex++;
                    break;
                }

                case BoundaryScanStatus.TooLarge:
                {
                    var diagnostic = new ExtractionDiagnostic(
                        ExtractionDiagnosticCodes.CandidateTooLarge,
                        $"Candidate exceeded the configured size limit of {_options.MaxCandidateBytes} bytes.",
                        boundary.ErrorPosition);

                    documents.Add(new DocumentResult(
                        documentIndex,
                        current == (byte)'{' ? DocumentType.Object : DocumentType.Array,
                        DocumentStatus.Failed,
                        diagnostics: [diagnostic]));

                    documentIndex++;
                    break;
                }

                case BoundaryScanStatus.MismatchedBoundary:
                {
                    var diagnostic = new ExtractionDiagnostic(
                        ExtractionDiagnosticCodes.CandidateBoundaryMismatch,
                        "Candidate contains mismatched JSON delimiters.",
                        boundary.ErrorPosition);

                    documents.Add(new DocumentResult(
                        documentIndex,
                        current == (byte)'{' ? DocumentType.Object : DocumentType.Array,
                        DocumentStatus.Failed,
                        diagnostics: [diagnostic]));

                    documentIndex++;
                    break;
                }

                case BoundaryScanStatus.AmbiguousIncomplete:
                {
                    var diagnostic = new ExtractionDiagnostic(
                        ExtractionDiagnosticCodes.CandidateIncompleteAmbiguous,
                        "Candidate ended in an ambiguous state (for example inside a JSON string) and was not repaired.",
                        content.Length == 0 ? null : content.Length - 1);

                    documents.Add(new DocumentResult(
                        documentIndex,
                        current == (byte)'{' ? DocumentType.Object : DocumentType.Array,
                        DocumentStatus.Failed,
                        diagnostics: [diagnostic]));

                    documentIndex++;
                    position = content.Length;
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unexpected boundary status: {boundary.Status}.");
            }
        }

        var payloadDiagnostics = documents.Count == 0
            ? new[]
            {
                new ExtractionDiagnostic(
                    ExtractionDiagnosticCodes.NoCandidatesDiscovered,
                    "No top-level JSON object or array candidate was discovered in the payload.")
            }
            : Array.Empty<ExtractionDiagnostic>();

        return new PayloadResult(payload.PayloadIndex, documents, payloadDiagnostics);
    }

    private async Task<DocumentResult> SafeExtractAsync(DocumentCandidate candidate, int candidateStartPosition, CancellationToken cancellationToken)
    {
        try
        {
            return await _documentExtractor.ExtractAsync(candidate, cancellationToken);
        }
        catch (Exception exception)
        {
            var extractionDiagnostic = new ExtractionDiagnostic(
                ExtractionDiagnosticCodes.CandidateExtractionException,
                $"Candidate extraction failed with an unexpected exception: {exception.GetType().Name}.",
                candidateStartPosition);

            return new DocumentResult(
                candidate.DocumentIndex,
                DocumentType.Unknown,
                DocumentStatus.Failed,
                diagnostics: [extractionDiagnostic]);
        }
    }

    private BoundaryScanResult ScanBoundary(ReadOnlySpan<byte> content, int startPosition)
    {
        Span<byte> delimiters = stackalloc byte[_options.MaxNestingDepth];
        var depth = 0;
        var insideString = false;
        var escaped = false;

        for (var position = startPosition; position < content.Length; position++)
        {
            var current = content[position];

            if ((position - startPosition + 1) > _options.MaxCandidateBytes)
            {
                return BoundaryScanResult.TooLarge(position);
            }

            if (insideString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == (byte)'\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == (byte)'"')
                {
                    insideString = false;
                }

                continue;
            }

            if (current == (byte)'"')
            {
                insideString = true;
                continue;
            }

            if (current is (byte)'{' or (byte)'[')
            {
                if (depth == _options.MaxNestingDepth)
                {
                    return BoundaryScanResult.TooDeep(position);
                }

                delimiters[depth] = current;
                depth++;
                continue;
            }

            if (current is (byte)'}' or (byte)']')
            {
                if (depth == 0)
                {
                    return BoundaryScanResult.Mismatched(position);
                }

                var open = delimiters[depth - 1];
                if (!IsMatchingDelimiter(open, current))
                {
                    return BoundaryScanResult.Mismatched(position);
                }

                depth--;
                if (depth == 0)
                {
                    return BoundaryScanResult.Complete(position);
                }
            }
        }

        if (depth == 0)
        {
            return BoundaryScanResult.Ambiguous();
        }

        if (insideString || escaped)
        {
            return BoundaryScanResult.Ambiguous();
        }

        var recoveryClosers = new List<byte>(depth);
        for (var index = depth - 1; index >= 0; index--)
        {
            recoveryClosers.Add(delimiters[index] == (byte)'{' ? (byte)'}' : (byte)']');
        }

        return BoundaryScanResult.Recoverable(recoveryClosers);
    }

    private static bool IsOpeningDelimiter(byte value) => value is (byte)'{' or (byte)'[';

    private static bool IsMatchingDelimiter(byte open, byte close)
        => (open == (byte)'{' && close == (byte)'}')
        || (open == (byte)'[' && close == (byte)']');

    private enum BoundaryScanStatus
    {
        Complete,
        RecoverableIncomplete,
        AmbiguousIncomplete,
        MismatchedBoundary,
        TooDeep,
        TooLarge
    }

    private sealed record BoundaryScanResult(BoundaryScanStatus Status, int EndPosition, int? ErrorPosition, IReadOnlyList<byte> RecoveryClosers)
    {
        public static BoundaryScanResult Complete(int endPosition)
            => new(BoundaryScanStatus.Complete, endPosition, null, []);

        public static BoundaryScanResult Recoverable(IReadOnlyList<byte> recoveryClosers)
            => new(BoundaryScanStatus.RecoverableIncomplete, -1, null, recoveryClosers);

        public static BoundaryScanResult Ambiguous()
            => new(BoundaryScanStatus.AmbiguousIncomplete, -1, null, []);

        public static BoundaryScanResult Mismatched(int position)
            => new(BoundaryScanStatus.MismatchedBoundary, -1, position, []);

        public static BoundaryScanResult TooDeep(int position)
            => new(BoundaryScanStatus.TooDeep, -1, position, []);

        public static BoundaryScanResult TooLarge(int position)
            => new(BoundaryScanStatus.TooLarge, -1, position, []);
    }
}
