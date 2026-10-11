using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Exceptions;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class BatchProcessor : IBatchProcessor
{
    private readonly BinaryJsonPayloadScanner _payloadScanner;

    public BatchProcessor(BinaryJsonPayloadScanner? payloadScanner = null)
    {
        _payloadScanner = payloadScanner ?? new BinaryJsonPayloadScanner();
    }

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (PayloadProcessingException exception)
            {
                payloadResults.Add(CreatePayloadExceptionResult(payload.PayloadIndex, payload.PayloadName, exception.GetType().Name));
            }
        }

        return new BatchResult(
            request.BatchId,
            BatchStatus.Completed,
            payloadResults,
            createdUtc,
            DateTimeOffset.UtcNow);
    }

    private static PayloadResult CreatePayloadExceptionResult(int payloadIndex, string? payloadName, string exceptionType)
    {
        var diagnostic = new ExtractionDiagnostic(
            BatchProcessingDiagnosticCodes.PayloadProcessingException,
            BatchProcessingMessages.PayloadProcessingException(exceptionType));

        return new PayloadResult(payloadIndex, diagnostics: [diagnostic], payloadName: payloadName);
    }
}
