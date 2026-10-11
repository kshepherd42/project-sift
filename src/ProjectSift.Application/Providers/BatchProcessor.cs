using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class BatchProcessor(BinaryJsonPayloadScanner payloadScanner) : IBatchProcessor
{
    private readonly BinaryJsonPayloadScanner _payloadScanner = payloadScanner ?? throw new ArgumentNullException(nameof(payloadScanner));

    public async Task<BatchResult> ProcessAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var createdUtc = DateTimeOffset.UtcNow;
        var payloadResults = new List<PayloadResult>(request.Payloads.Count);

        foreach (var payload in request.Payloads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                payloadResults.Add(await _payloadScanner.ScanAsync(payload, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (IsNonFatal(exception))
            {
                payloadResults.Add(CreatePayloadExceptionResult(payload.PayloadIndex, exception.GetType().Name));
            }
        }

        return new BatchResult(
            request.BatchId,
            BatchStatus.Completed,
            payloadResults,
            createdUtc,
            DateTimeOffset.UtcNow);
    }

    private static PayloadResult CreatePayloadExceptionResult(int payloadIndex, string exceptionType)
    {
        var diagnostic = new ExtractionDiagnostic(
            BatchProcessingDiagnosticCodes.PayloadProcessingException,
            BatchProcessingMessages.PayloadProcessingException(exceptionType));

        return new PayloadResult(payloadIndex, diagnostics: [diagnostic]);
    }

    private static bool IsNonFatal(Exception exception)
        => exception is not (
            OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException
            or CannotUnloadAppDomainException);
}
