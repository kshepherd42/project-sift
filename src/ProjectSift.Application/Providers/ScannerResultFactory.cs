using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Enums;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class ScannerResultFactory(BinaryJsonExtractionOptions options) : IScannerResultFactory
{
    private readonly BinaryJsonExtractionOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public DocumentResult CreateBoundaryFailure(int documentIndex, byte openingDelimiter, BoundaryScanStatus status, int? position)
        => status switch
        {
            BoundaryScanStatus.TooDeep => CreateFailedDocumentResult(
                documentIndex,
                openingDelimiter,
                ExtractionDiagnosticCodes.CandidateDepthLimitExceeded,
                ExtractionMessages.CandidateNestingLimitExceeded(_options.MaxNestingDepth),
                position),
            BoundaryScanStatus.TooLarge => CreateFailedDocumentResult(
                documentIndex,
                openingDelimiter,
                ExtractionDiagnosticCodes.CandidateTooLarge,
                ExtractionMessages.CandidateSizeLimitExceeded(_options.MaxCandidateBytes),
                position),
            BoundaryScanStatus.MismatchedBoundary => CreateFailedDocumentResult(
                documentIndex,
                openingDelimiter,
                ExtractionDiagnosticCodes.CandidateBoundaryMismatch,
                ExtractionMessages.CandidateBoundaryMismatch,
                position),
            BoundaryScanStatus.AmbiguousIncomplete => CreateFailedDocumentResult(
                documentIndex,
                openingDelimiter,
                ExtractionDiagnosticCodes.CandidateIncompleteAmbiguous,
                ExtractionMessages.CandidateIncompleteAmbiguous,
                position),
            _ => throw new InvalidOperationException($"{ExtractionMessages.UnexpectedBoundaryStatus}: {status}.")
        };

    public DocumentResult CreateRecoveredResult(int documentIndex, DocumentResult extracted, int repairPosition)
    {
        var repairedDiagnostic = new ExtractionDiagnostic(
            ExtractionDiagnosticCodes.CandidateIncompleteRecovered,
            ExtractionMessages.CandidateIncompleteRecovered(),
            repairPosition);

        var recoveredStatus = extracted.Status is DocumentStatus.Successful
            ? DocumentStatus.Recovered
            : extracted.Status;

        return new DocumentResult(
            documentIndex,
            extracted.Type,
            recoveredStatus,
            extracted.PropertyPaths,
            extracted.Diagnostics.Concat([repairedDiagnostic]));
    }

    public DocumentResult CreateExtractionExceptionFailure(int documentIndex, int candidateStartPosition, string exceptionType)
    {
        var diagnostic = new ExtractionDiagnostic(
            ExtractionDiagnosticCodes.CandidateExtractionException,
            ExtractionMessages.CandidateExtractionException(exceptionType),
            candidateStartPosition);

        return new DocumentResult(
            documentIndex,
            DocumentType.Unknown,
            DocumentStatus.Failed,
            diagnostics: [diagnostic]);
    }

    public PayloadResult CreatePayloadResult(int payloadIndex, IReadOnlyCollection<DocumentResult> documents)
    {
        var payloadDiagnostics = documents.Count == 0
            ? new[]
            {
                new ExtractionDiagnostic(
                    ExtractionDiagnosticCodes.NoCandidatesDiscovered,
                    ExtractionMessages.NoCandidatesDiscovered)
            }
            : Array.Empty<ExtractionDiagnostic>();

        return new PayloadResult(payloadIndex, documents, payloadDiagnostics);
    }

    private static DocumentResult CreateFailedDocumentResult(
        int documentIndex,
        byte openingDelimiter,
        string diagnosticCode,
        string diagnosticMessage,
        int? position)
    {
        var diagnostic = new ExtractionDiagnostic(diagnosticCode, diagnosticMessage, position);
        return new DocumentResult(
            documentIndex,
            openingDelimiter == (byte)'{' ? DocumentType.Object : DocumentType.Array,
            DocumentStatus.Failed,
            diagnostics: [diagnostic]);
    }
}
