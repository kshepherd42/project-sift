using System.Text;
using ProjectSift.Application.Constants;
using ProjectSift.Application.Models;
using ProjectSift.Application.Providers;
using ProjectSift.Domain.Enums;
using Xunit;

namespace ProjectSift.Application.Tests;

public class BinaryJsonPayloadScannerTests
{
    [Fact]
    public async Task ScanAsync_PayloadA_ShouldExtractOnlyOrderId()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payload = new PayloadInput(0, HexToBytes(
            "DE AD BE EF 01 00 00 12 9C 41 F3 07 7B 22 6F 72" +
            " 64 65 72 49 64 22 3A 22 41 31 42 32 2D 39 46 22" +
            " 7D 00 00 FF FF C8 33"));

        var result = await scanner.ScanAsync(payload);

        var document = Assert.Single(result.Documents);
        Assert.Equal(DocumentStatus.Successful, document.Status);
        Assert.Equal(DocumentType.Object, document.Type);
        var path = Assert.Single(document.PropertyPaths);
        Assert.Equal("orderId", path.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task ScanAsync_PayloadB_ShouldExtractNestedPaths()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payload = new PayloadInput(1, HexToBytes(
            "CA FE BA BE 02 00 00 3B 11 E0 7B 22 73 6B 75 22" +
            " 3A 22 58 2D 31 30 30 22 2C 22 71 74 79 22 3A 35" +
            " 2C 22 77 61 72 65 68 6F 75 73 65 22 3A 7B 22 7A" +
            " 6F 6E 65 22 3A 22 43 34 22 7D 7D 0D 0A 00 00 7A" +
            " 7A"));

        var result = await scanner.ScanAsync(payload);

        var document = Assert.Single(result.Documents);
        Assert.Equal(["qty", "sku", "warehouse.zone"], document.PropertyPaths.Select(path => path.Value).ToArray());
    }

    [Fact]
    public async Task ScanAsync_PayloadC_ShouldIgnoreDelimitersInsideString()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payload = new PayloadInput(2, HexToBytes(
            "17 03 03 00 7B 42 42 00 E1 7B 22 6E 6F 74 65 22" +
            " 3A 22 75 73 65 20 7B 20 77 69 74 68 20 63 61 72" +
            " 65 22 2C 22 73 74 61 74 75 73 22 3A 22 6F 6B 22" +
            " 7D 00 04 D2 00"));

        var result = await scanner.ScanAsync(payload);

        var successfulDocument = Assert.Single(result.Documents, document => document.Status == DocumentStatus.Successful);
        Assert.Equal(["note", "status"], successfulDocument.PropertyPaths.Select(path => path.Value).ToArray());
    }

    [Fact]
    public async Task ScanAsync_ShouldExtractStandaloneObjectAndArray()
    {
        var scanner = new BinaryJsonPayloadScanner();

        var objectResult = await scanner.ScanAsync(new PayloadInput(3, Encoding.UTF8.GetBytes("{\"a\":1,\"b\":{\"c\":2}}")));
        var arrayResult = await scanner.ScanAsync(new PayloadInput(4, Encoding.UTF8.GetBytes("[{\"id\":1},{\"id\":2}]")));

        Assert.Equal(["a", "b.c"], objectResult.Documents.Single().PropertyPaths.Select(path => path.Value));
        Assert.Equal(["[]", "[].id"], arrayResult.Documents.Single().PropertyPaths.Select(path => path.Value));
        Assert.Equal(DocumentType.Array, arrayResult.Documents.Single().Type);
    }

    [Fact]
    public async Task ScanAsync_ShouldHandleMultipleDocumentsWithNoise()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payloadBytes = Encoding.UTF8.GetBytes("AA{\"id\":1}ZZ[{\"sku\":\"X\"}]YY");

        var result = await scanner.ScanAsync(new PayloadInput(5, payloadBytes));

        Assert.Equal(2, result.Documents.Count);
        Assert.Equal(["id"], result.Documents[0].PropertyPaths.Select(path => path.Value));
        Assert.Equal(["[]", "[].sku"], result.Documents[1].PropertyPaths.Select(path => path.Value));
    }

    [Fact]
    public async Task ScanAsync_ShouldDeduplicateAndPreserveCasing()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payload = new PayloadInput(6, Encoding.UTF8.GetBytes("{\"Id\":1,\"id\":2,\"items\":[{\"Id\":1},{\"Id\":2}]}"));

        var result = await scanner.ScanAsync(payload);

        var paths = result.Documents.Single().PropertyPaths.Select(path => path.Value).ToArray();
        Assert.Equal(["Id", "id", "items[]", "items[].Id"], paths);
    }

    [Fact]
    public async Task ScanAsync_ShouldEscapeReservedPropertyNames()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payload = new PayloadInput(7, Encoding.UTF8.GetBytes("{\"a.b\":1,\"x[y]\":2,\"o\\\"k\":3,\"plain\":4}"));

        var result = await scanner.ScanAsync(payload);

        var paths = result.Documents.Single().PropertyPaths.Select(path => path.Value).ToArray();
        Assert.Equal(["['a.b']", "['o\"k']", "['x[y]']", "plain"], paths);
    }

    [Fact]
    public async Task ScanAsync_WhenNoJsonCandidate_ShouldReturnPayloadLevelDiagnostic()
    {
        var scanner = new BinaryJsonPayloadScanner();

        var result = await scanner.ScanAsync(new PayloadInput(8, HexToBytes("01 02 03 FF 80 7F")));

        Assert.Empty(result.Documents);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ExtractionDiagnosticCodes.NoCandidatesDiscovered, diagnostic.Code);
    }

    [Fact]
    public async Task ScanAsync_WhenJsonContainsInvalidUtf8_ShouldContinueScanningLaterCandidates()
    {
        var scanner = new BinaryJsonPayloadScanner();
        var payload = HexToBytes("7B 22 61 22 3A 22 C3 22 7D 99 7B 22 62 22 3A 31 7D");

        var result = await scanner.ScanAsync(new PayloadInput(9, payload));

        Assert.True(result.Documents.Count >= 2);
        Assert.Contains(result.Documents, document =>
            document.Status == DocumentStatus.Successful
            && document.PropertyPaths.Select(path => path.Value).SequenceEqual(["b"]));
    }

    [Fact]
    public async Task ScanAsync_WhenBoundaryIsMismatched_ShouldReturnFailedDocument()
    {
        var scanner = new BinaryJsonPayloadScanner();

        var result = await scanner.ScanAsync(new PayloadInput(10, Encoding.UTF8.GetBytes("abc{\"a\":[1,2}\"tail\"")));

        Assert.Contains(result.Documents, document =>
            document.Status == DocumentStatus.Failed
            && document.Diagnostics.Any(diagnostic => diagnostic.Code == ExtractionDiagnosticCodes.CandidateBoundaryMismatch));
    }

    [Fact]
    public async Task ScanAsync_WhenCandidateEndsAtPayloadBoundaryWithoutAmbiguity_ShouldRecoverDeterministically()
    {
        var scanner = new BinaryJsonPayloadScanner();

        var result = await scanner.ScanAsync(new PayloadInput(11, Encoding.UTF8.GetBytes("xx{\"root\":{\"id\":1")));

        var document = Assert.Single(result.Documents);
        Assert.Equal(DocumentStatus.Recovered, document.Status);
        Assert.Equal("root.id", document.PropertyPaths.Single().Value);
        Assert.Contains(document.Diagnostics, diagnostic => diagnostic.Code == ExtractionDiagnosticCodes.CandidateIncompleteRecovered);
    }

    [Fact]
    public async Task ScanAsync_WhenCandidateEndsInsideString_ShouldFailWithoutRecovery()
    {
        var scanner = new BinaryJsonPayloadScanner();

        var result = await scanner.ScanAsync(new PayloadInput(12, Encoding.UTF8.GetBytes("xx{\"id\":\"abc")));

        var document = Assert.Single(result.Documents);
        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(ExtractionDiagnosticCodes.CandidateIncompleteAmbiguous, document.Diagnostics.Single().Code);
    }

    [Fact]
    public async Task ScanAsync_WhenDepthLimitExceeded_ShouldReturnFailedDocument()
    {
        var options = new BinaryJsonExtractionOptions(maxNestingDepth: 3);
        var scanner = new BinaryJsonPayloadScanner(options: options);

        var result = await scanner.ScanAsync(new PayloadInput(13, Encoding.UTF8.GetBytes("{\"a\":{\"b\":{\"c\":{\"d\":1}}}}")));

        Assert.Contains(result.Documents, document =>
            document.Status == DocumentStatus.Failed
            && document.Diagnostics.Any(diagnostic => diagnostic.Code == ExtractionDiagnosticCodes.CandidateDepthLimitExceeded));
    }

    [Fact]
    public async Task ScanAsync_WhenSizeLimitExceeded_ShouldReturnFailedDocument()
    {
        var options = new BinaryJsonExtractionOptions(maxCandidateBytes: 12);
        var scanner = new BinaryJsonPayloadScanner(options: options);

        var result = await scanner.ScanAsync(new PayloadInput(14, Encoding.UTF8.GetBytes("{\"veryLong\":123456}")));

        var document = Assert.Single(result.Documents);
        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(ExtractionDiagnosticCodes.CandidateTooLarge, document.Diagnostics.Single().Code);
    }

    [Fact]
    public async Task ScanAsync_WhenPathLimitExceeded_ShouldReturnFailedDocument()
    {
        var options = new BinaryJsonExtractionOptions(maxPathsPerDocument: 2);
        var scanner = new BinaryJsonPayloadScanner(options: options);

        var result = await scanner.ScanAsync(new PayloadInput(15, Encoding.UTF8.GetBytes("{\"a\":1,\"b\":2,\"c\":3}")));

        var document = Assert.Single(result.Documents);
        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(ExtractionDiagnosticCodes.CandidatePathLimitExceeded, document.Diagnostics.Single().Code);
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new List<byte>();
        var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            bytes.Add(Convert.ToByte(part, 16));
        }

        return bytes.ToArray();
    }
}
