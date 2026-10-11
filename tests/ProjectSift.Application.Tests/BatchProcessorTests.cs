using System.Text;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Enums;
using ProjectSift.Application.Models;
using ProjectSift.Application.Providers;
using ProjectSift.Domain.Enums;
using Xunit;

namespace ProjectSift.Application.Tests;

public sealed class BatchProcessorTests
{
    private const string BatchId = "batch-001";

    [Fact]
    public async Task ProcessAsync_WhenSinglePayloadContainsOneDocument_ShouldReturnCompletedBatchWithSuccessfulDocument()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(
            BatchId,
            [new PayloadInput(0, Encoding.UTF8.GetBytes("{\"orderId\":\"A1\"}"))]);

        var result = await processor.ProcessAsync(request);

        Assert.Equal(BatchStatus.Completed, result.Status);
        var payload = Assert.Single(result.Payloads);
        Assert.Equal(0, payload.PayloadIndex);
        Assert.Empty(payload.Diagnostics);

        var document = Assert.Single(payload.Documents);
        Assert.Equal(DocumentStatus.Successful, document.Status);
        Assert.Equal(["orderId"], document.PropertyPaths.Select(path => path.Value));
        Assert.True(result.CompletedUtc.HasValue);
    }

    [Fact]
    public async Task ProcessAsync_WhenMultiplePayloadsContainMultipleDocuments_ShouldAggregatePerPayloadWithoutMerging()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(
            BatchId,
            [
                new PayloadInput(2, Encoding.UTF8.GetBytes("xx{\"a\":1}yy[{\"b\":2}]")),
                new PayloadInput(5, Encoding.UTF8.GetBytes("{\"c\":3}{\"d\":4}"))
            ]);

        var result = await processor.ProcessAsync(request);

        Assert.Equal(2, result.Payloads.Count);
        Assert.Equal(2, result.Payloads[0].Documents.Count);
        Assert.Equal(2, result.Payloads[1].Documents.Count);
        Assert.Equal(2, result.Payloads[0].PayloadIndex);
        Assert.Equal(5, result.Payloads[1].PayloadIndex);
        Assert.Equal([0, 1], result.Payloads[0].Documents.Select(document => document.DocumentIndex));
        Assert.Equal([0, 1], result.Payloads[1].Documents.Select(document => document.DocumentIndex));
    }

    [Fact]
    public async Task ProcessAsync_WhenPayloadContainsSuccessfulAndFailedDocuments_ShouldPreservePartialSuccess()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(
            BatchId,
            [new PayloadInput(3, Encoding.UTF8.GetBytes("{\"ok\":1}{\"id\":\"abc"))]);

        var result = await processor.ProcessAsync(request);

        var payload = Assert.Single(result.Payloads);
        Assert.Equal(2, payload.Documents.Count);
        Assert.Contains(payload.Documents, document => document.Status == DocumentStatus.Successful && document.PropertyPaths.Select(path => path.Value).SequenceEqual(["ok"]));
        Assert.Contains(payload.Documents, document =>
            document.Status == DocumentStatus.Failed
            && document.Diagnostics.Any(diagnostic => diagnostic.Code == ExtractionDiagnosticCodes.CandidateIncompleteAmbiguous));
    }

    [Fact]
    public async Task ProcessAsync_WhenPayloadContainsNoDocuments_ShouldReturnNoCandidatesDiagnostic()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(
            BatchId,
            [new PayloadInput(1, new byte[] { 0x01, 0x02, 0x03, 0x04 })]);

        var result = await processor.ProcessAsync(request);

        var payload = Assert.Single(result.Payloads);
        Assert.Empty(payload.Documents);
        var diagnostic = Assert.Single(payload.Diagnostics);
        Assert.Equal(ExtractionDiagnosticCodes.NoCandidatesDiscovered, diagnostic.Code);
    }

    [Fact]
    public async Task ProcessAsync_WhenFirstPayloadFailsAndSecondSucceeds_ShouldContinueProcessing()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(
            BatchId,
            [
                new PayloadInput(8, Encoding.UTF8.GetBytes("xx{\"id\":\"abc")),
                new PayloadInput(9, Encoding.UTF8.GetBytes("{\"value\":1}"))
            ]);

        var result = await processor.ProcessAsync(request);

        Assert.Equal(2, result.Payloads.Count);
        Assert.Contains(result.Payloads[0].Documents, document => document.Status == DocumentStatus.Failed);
        Assert.Contains(result.Payloads[1].Documents, document => document.Status == DocumentStatus.Successful);
    }

    [Fact]
    public async Task ProcessAsync_ShouldPreservePayloadAssociationAndDocumentDiagnostics()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(
            BatchId,
            [new PayloadInput(42, Encoding.UTF8.GetBytes("xx{\"id\":\"abc"))]);

        var result = await processor.ProcessAsync(request);

        var payload = Assert.Single(result.Payloads);
        Assert.Equal(42, payload.PayloadIndex);

        var document = Assert.Single(payload.Documents);
        Assert.Equal(0, document.DocumentIndex);
        var diagnostic = Assert.Single(document.Diagnostics);
        Assert.Equal(ExtractionDiagnosticCodes.CandidateIncompleteAmbiguous, diagnostic.Code);
    }

    [Fact]
    public async Task ProcessAsync_WhenPayloadCollectionIsEmpty_ShouldReturnCompletedBatchWithNoPayloads()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(BatchId, []);

        var result = await processor.ProcessAsync(request);

        Assert.Equal(BatchStatus.Completed, result.Status);
        Assert.Empty(result.Payloads);
        Assert.True(result.CompletedUtc.HasValue);
    }

    [Fact]
    public async Task ProcessAsync_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        var processor = CreateProcessor();
        var request = new BatchProcessingRequest(BatchId, [new PayloadInput(0, Encoding.UTF8.GetBytes("{\"id\":1}"))]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ProcessAsync(request, cancellation.Token));
    }

    [Fact]
    public async Task ProcessAsync_WhenPayloadScannerThrowsUnexpectedException_ShouldRecordPayloadDiagnosticAndContinue()
    {
        var scanner = new BinaryJsonPayloadScanner(boundaryScanner: new ThrowOnMarkerBoundaryScanner());
        var processor = new BatchProcessor(scanner);
        var request = new BatchProcessingRequest(
            BatchId,
            [
                new PayloadInput(10, new byte[] { 0xAA, (byte)'{', (byte)'"', (byte)'a', (byte)'"', (byte)':', (byte)'1', (byte)'}' }),
                new PayloadInput(11, Encoding.UTF8.GetBytes("{\"ok\":true}"))
            ]);

        var result = await processor.ProcessAsync(request);

        Assert.Equal(BatchStatus.Completed, result.Status);
        Assert.Equal(2, result.Payloads.Count);

        var failedPayload = result.Payloads[0];
        Assert.Equal(10, failedPayload.PayloadIndex);
        Assert.Empty(failedPayload.Documents);
        var payloadDiagnostic = Assert.Single(failedPayload.Diagnostics);
        Assert.Equal(BatchProcessingDiagnosticCodes.PayloadProcessingException, payloadDiagnostic.Code);

        var successfulPayload = result.Payloads[1];
        var successfulDocument = Assert.Single(successfulPayload.Documents);
        Assert.Equal(DocumentStatus.Successful, successfulDocument.Status);
    }

    private static BatchProcessor CreateProcessor()
        => new(new BinaryJsonPayloadScanner());

    private sealed class ThrowOnMarkerBoundaryScanner : IBinaryJsonBoundaryScanner
    {
        private readonly BinaryJsonBoundaryScanner _inner = new(new BinaryJsonExtractionOptions());

        public BoundaryScanResult Scan(
            byte[] content,
            int offset,
            int contentLength,
            int startPosition,
            CancellationToken cancellationToken = default)
        {
            if (content[offset] == 0xAA)
            {
                throw new InvalidOperationException("Simulated boundary failure.");
            }

            return _inner.Scan(content, offset, contentLength, startPosition, cancellationToken);
        }
    }
}
