using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;
using Xunit;

namespace ProjectSift.Domain.Tests;

public class BatchContractsTests
{
    [Fact]
    public void BatchResult_ShouldConstructWithValidValues()
    {
        var payload = new PayloadResult(0, [
            new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful, [
                new PropertyPath("id"),
                new PropertyPath("customer.id")
            ])
        ]);

        var result = new BatchResult(DomainTestConstants.BatchId, BatchStatus.Processing, [payload]);

        Assert.Equal(DomainTestConstants.BatchId, result.BatchId);
        Assert.Equal(BatchStatus.Processing, result.Status);
        Assert.Single(result.Payloads);
    }

    [Fact]
    public void DocumentResult_ShouldRepresentSuccessfulRecoveredAndFailedOutcomes()
    {
        var successful = new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful);
        var recovered = new DocumentResult(1, DocumentType.Object, DocumentStatus.Recovered);
        var failed = new DocumentResult(2, DocumentType.Unknown, DocumentStatus.Failed);

        Assert.Equal(DocumentStatus.Successful, successful.Status);
        Assert.Equal(DocumentStatus.Recovered, recovered.Status);
        Assert.Equal(DocumentStatus.Failed, failed.Status);
    }

    [Fact]
    public void DocumentResult_ShouldRepresentObjectArrayAndUnknownTypes()
    {
        var objectDocument = new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful);
        var arrayDocument = new DocumentResult(1, DocumentType.Array, DocumentStatus.Successful);
        var unknownDocument = new DocumentResult(2, DocumentType.Unknown, DocumentStatus.Failed);

        Assert.Equal(DocumentType.Object, objectDocument.Type);
        Assert.Equal(DocumentType.Array, arrayDocument.Type);
        Assert.Equal(DocumentType.Unknown, unknownDocument.Type);
    }

    [Fact]
    public void DocumentResult_ShouldPreserveOrderedPropertyPathsAndDiagnostics()
    {
        var firstPath = new PropertyPath("id");
        var secondPath = new PropertyPath("items[].sku");
        var firstDiagnostic = new ExtractionDiagnostic("DOC001", "first issue", 12);
        var secondDiagnostic = new ExtractionDiagnostic("DOC002", "second issue", 15);

        var result = new DocumentResult(
            3,
            DocumentType.Object,
            DocumentStatus.Recovered,
            [firstPath, secondPath],
            [firstDiagnostic, secondDiagnostic]);

        Assert.Equal("id", result.PropertyPaths[0].Value);
        Assert.Equal("items[].sku", result.PropertyPaths[1].Value);
        Assert.Equal("DOC001", result.Diagnostics[0].Code);
        Assert.Equal("DOC002", result.Diagnostics[1].Code);
    }

    [Fact]
    public void PayloadResult_ShouldKeepSeparateDocumentResultsWhenPathMatches()
    {
        var docA = new DocumentResult(
            0,
            DocumentType.Object,
            DocumentStatus.Successful,
            [new PropertyPath("items[].id")]);

        var docB = new DocumentResult(
            1,
            DocumentType.Object,
            DocumentStatus.Successful,
            [new PropertyPath("items[].id")]);

        var payload = new PayloadResult(4, [docA, docB]);

        Assert.Equal(2, payload.Documents.Count);
        Assert.Equal(0, payload.Documents[0].DocumentIndex);
        Assert.Equal(1, payload.Documents[1].DocumentIndex);
    }

    [Fact]
    public void PayloadResult_ShouldAllowPayloadLevelDiagnosticsWhenNoDocumentIsDiscovered()
    {
        var diagnostic = new ExtractionDiagnostic(DomainTestConstants.PayloadNoDocumentCode, "No document boundaries discovered.");

        var payload = new PayloadResult(2, diagnostics: [diagnostic]);

        Assert.Empty(payload.Documents);
        Assert.Single(payload.Diagnostics);
        Assert.Equal(DomainTestConstants.PayloadNoDocumentCode, payload.Diagnostics[0].Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BatchResult_ShouldRejectInvalidBatchId(string invalidBatchId)
    {
        Assert.Throws<ArgumentException>(() => new BatchResult(invalidBatchId, BatchStatus.Processing));
    }

    [Fact]
    public void PayloadResult_ShouldRejectNegativePayloadIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadResult(-1));
    }

    [Fact]
    public void DocumentResult_ShouldRejectNegativeDocumentIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentResult(-1, DocumentType.Object, DocumentStatus.Successful));
    }
}
