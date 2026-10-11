using System.Runtime.InteropServices;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Enums;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Exceptions;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class BinaryJsonPayloadScanner
{
    private readonly IBinaryJsonBoundaryScanner _boundaryScanner;
    private readonly IDocumentExtractor _documentExtractor;
    private readonly IScannerResultFactory _scannerResultFactory;

    public BinaryJsonPayloadScanner(
        IDocumentExtractor? documentExtractor = null,
        BinaryJsonExtractionOptions? options = null,
        IBinaryJsonBoundaryScanner? boundaryScanner = null,
        IScannerResultFactory? scannerResultFactory = null)
    {
        var extractionOptions = options ?? new BinaryJsonExtractionOptions();
        _documentExtractor = documentExtractor ?? new BinaryJsonDocumentExtractor(extractionOptions);
        _boundaryScanner = boundaryScanner ?? new BinaryJsonBoundaryScanner(extractionOptions);
        _scannerResultFactory = scannerResultFactory ?? new ScannerResultFactory(extractionOptions);
    }

    public async Task<PayloadResult> ScanAsync(PayloadInput payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        var documents = new List<DocumentResult>();
        var payloadBuffer = ResolvePayloadBuffer(payload);
        var documentIndex = 0;

        for (var position = 0; position < payloadBuffer.Length; position++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = GetByteAt(payloadBuffer, position);
            if (!IsOpeningDelimiter(current))
            {
                continue;
            }

            var boundary = _boundaryScanner.Scan(
                payloadBuffer.Content,
                payloadBuffer.Offset,
                payloadBuffer.Length,
                position,
                cancellationToken);

            switch (boundary.Status)
            {
                case BoundaryScanStatus.Complete:
                {
                    var completeResult = await ExtractCandidateAsync(
                        payload,
                        payloadBuffer,
                        position,
                        boundary.EndPosition,
                        documentIndex,
                        cancellationToken);

                    documents.Add(completeResult);
                    if (ShouldAdvanceToBoundaryEnd(completeResult.Status))
                    {
                        position = boundary.EndPosition;
                    }

                    documentIndex++;
                    break;
                }

                case BoundaryScanStatus.RecoverableIncomplete:
                {
                    var recovered = await ExtractRecoveredCandidateAsync(
                        payload,
                        payloadBuffer,
                        boundary,
                        position,
                        documentIndex,
                        cancellationToken);

                    documents.Add(recovered);
                    if (ShouldTerminateAfterRecovery(recovered.Status))
                    {
                        position = payloadBuffer.Length;
                    }

                    documentIndex++;
                    break;
                }

                case BoundaryScanStatus.TooDeep:
                case BoundaryScanStatus.TooLarge:
                case BoundaryScanStatus.MismatchedBoundary:
                case BoundaryScanStatus.AmbiguousIncomplete:
                {
                    documents.Add(CreateBoundaryFailure(current, boundary, documentIndex, payloadBuffer.Length));
                    documentIndex++;
                    if (boundary.Status is BoundaryScanStatus.AmbiguousIncomplete)
                    {
                        position = payloadBuffer.Length;
                    }

                    break;
                }

                default:
                    throw new PayloadProcessingException($"{ExtractionMessages.UnexpectedBoundaryStatus}: {boundary.Status}.");
            }
        }

        return _scannerResultFactory.CreatePayloadResult(payload.PayloadIndex, documents);
    }

    private async Task<DocumentResult> ExtractCandidateAsync(
        PayloadInput payload,
        PayloadBuffer payloadBuffer,
        int position,
        int endPosition,
        int documentIndex,
        CancellationToken cancellationToken)
    {
        var candidateLength = endPosition - position + 1;
        var candidateMemory = SlicePayload(payloadBuffer, position, candidateLength);
        var candidate = new DocumentCandidate(payload.PayloadIndex, documentIndex, candidateMemory);
        return await SafeExtractAsync(candidate, position, cancellationToken);
    }

    private async Task<DocumentResult> ExtractRecoveredCandidateAsync(
        PayloadInput payload,
        PayloadBuffer payloadBuffer,
        BoundaryScanResult boundary,
        int position,
        int documentIndex,
        CancellationToken cancellationToken)
    {
        var repairedCandidate = CreateRecoveredCandidate(payloadBuffer, position, boundary.RecoveryClosers);
        var candidate = new DocumentCandidate(payload.PayloadIndex, documentIndex, repairedCandidate);
        var extracted = await SafeExtractAsync(candidate, position, cancellationToken);
        return _scannerResultFactory.CreateRecoveredResult(documentIndex, extracted, payloadBuffer.Length - 1);
    }

    private static bool ShouldAdvanceToBoundaryEnd(DocumentStatus status)
        => status is not DocumentStatus.Failed;

    private static bool ShouldTerminateAfterRecovery(DocumentStatus status)
        => status is not DocumentStatus.Failed;

    private static PayloadBuffer ResolvePayloadBuffer(PayloadInput payload)
    {
        if (MemoryMarshal.TryGetArray(payload.Content, out var segment) && segment.Array is not null)
        {
            return new PayloadBuffer(segment.Array, segment.Offset, payload.Content.Length);
        }

        var copied = payload.Content.ToArray();
        return new PayloadBuffer(copied, 0, copied.Length);
    }

    private static byte GetByteAt(PayloadBuffer payloadBuffer, int relativePosition)
        => payloadBuffer.Content[payloadBuffer.Offset + relativePosition];

    private static ReadOnlyMemory<byte> SlicePayload(PayloadBuffer payloadBuffer, int start, int length)
        => new(payloadBuffer.Content, payloadBuffer.Offset + start, length);

    private static byte[] CreateRecoveredCandidate(PayloadBuffer payloadBuffer, int position, IReadOnlyList<byte> recoveryClosers)
    {
        var originalLength = payloadBuffer.Length - position;
        var repaired = new byte[originalLength + recoveryClosers.Count];
        Array.Copy(payloadBuffer.Content, payloadBuffer.Offset + position, repaired, 0, originalLength);

        for (var closerIndex = 0; closerIndex < recoveryClosers.Count; closerIndex++)
        {
            repaired[originalLength + closerIndex] = recoveryClosers[closerIndex];
        }

        return repaired;
    }

    private DocumentResult CreateBoundaryFailure(byte openingDelimiter, BoundaryScanResult boundary, int documentIndex, int payloadLength)
    {
        var position = boundary.Status is BoundaryScanStatus.AmbiguousIncomplete
            ? payloadLength == 0 ? null : payloadLength - 1
            : boundary.ErrorPosition;

        return _scannerResultFactory.CreateBoundaryFailure(documentIndex, openingDelimiter, boundary.Status, position);
    }

    private async Task<DocumentResult> SafeExtractAsync(DocumentCandidate candidate, int candidateStartPosition, CancellationToken cancellationToken)
    {
        try
        {
            return await _documentExtractor.ExtractAsync(candidate, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return _scannerResultFactory.CreateExtractionExceptionFailure(
                candidate.DocumentIndex,
                candidateStartPosition,
                exception.GetType().Name);
        }
    }

    private static bool IsOpeningDelimiter(byte value) => value is (byte)'{' or (byte)'[';

    private readonly record struct PayloadBuffer(byte[] Content, int Offset, int Length);
}
