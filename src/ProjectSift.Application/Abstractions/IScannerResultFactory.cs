using ProjectSift.Application.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Abstractions;

public interface IScannerResultFactory
{
    DocumentResult CreateBoundaryFailure(int documentIndex, byte openingDelimiter, BoundaryScanStatus status, int? position);
    DocumentResult CreateRecoveredResult(int documentIndex, DocumentResult extracted, int repairPosition);
    DocumentResult CreateExtractionExceptionFailure(int documentIndex, int candidateStartPosition, string exceptionType);
    PayloadResult CreatePayloadResult(int payloadIndex, IReadOnlyCollection<DocumentResult> documents);
}
