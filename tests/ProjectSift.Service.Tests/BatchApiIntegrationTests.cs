using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Enums;
using ProjectSift.Service.BackgroundServices;
using ProjectSift.Service.Constants;
using Xunit;

namespace ProjectSift.Service.Tests;

public sealed class BatchApiIntegrationTests
{
    [Fact]
    public async Task UploadingOneValidPayload_ShouldReturnAccepted()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("one.json", "{\"customer\":{\"id\":1}}"));
        var response = await client.PostAsync(ServiceConstants.Routes.BatchesV1, content);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var accepted = await response.Content.ReadFromJsonAsync<BatchAcceptedResponse>();
        Assert.NotNull(accepted);
        Assert.False(string.IsNullOrWhiteSpace(accepted.BatchId));
        Assert.False(string.IsNullOrWhiteSpace(accepted.ResourceUrl));
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task UploadingMultipleFiles_ShouldCreateSeparatePayloads()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(
            ("first.json", "{\"a\":1}"),
            ("second.json", "{\"b\":2}"));

        var accepted = await PostAcceptedAsync(client, content);
        var completed = await WaitForBatchCompletedAsync(client, accepted.BatchId);

        Assert.Equal(2, completed.Payloads.Count);
        Assert.Contains(completed.Payloads, payload => payload.PayloadIndex == 0);
        Assert.Contains(completed.Payloads, payload => payload.PayloadIndex == 1);
    }

    [Fact]
    public async Task AcceptedResponse_ShouldContainUsableBatchIdAndResourceUrl()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("one.json", "{\"ok\":true}"));
        var response = await client.PostAsync(ServiceConstants.Routes.BatchesV1, content);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var accepted = await response.Content.ReadFromJsonAsync<BatchAcceptedResponse>();
        Assert.NotNull(accepted);

        var byLocation = await client.GetAsync(accepted.ResourceUrl);
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);
    }

    [Fact]
    public async Task RetrievingRegisteredBatch_ShouldReturnOk()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("one.json", "{\"ok\":true}"));
        var accepted = await PostAcceptedAsync(client, content);

        var response = await client.GetAsync($"/{ServiceConstants.Routes.BatchesV1}/{accepted.BatchId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var batch = await response.Content.ReadFromJsonAsync<BatchResponse>();
        Assert.NotNull(batch);
        Assert.Equal(accepted.BatchId, batch.BatchId);
    }

    [Fact]
    public async Task RetrievingUnknownBatch_ShouldReturnNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/{ServiceConstants.Routes.BatchesV1}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_ShouldBeAcceptedAndReportedInExtractionResults()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("malformed.json", "{\"id\":1,"));
        var accepted = await PostAcceptedAsync(client, content);
        var completed = await WaitForBatchCompletedAsync(client, accepted.BatchId);

        var payload = Assert.Single(completed.Payloads);
        Assert.Contains(
            payload.Documents.SelectMany(document => document.Diagnostics),
            diagnostic => diagnostic.Code == "CANDIDATE_INVALID_JSON");
    }

    [Fact]
    public async Task EmptyUploadAndMissingFileCollection_ShouldReturnBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var missingCollection = new MultipartFormDataContent();
        var missingResponse = await client.PostAsync(ServiceConstants.Routes.BatchesV1, missingCollection);
        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);

        using var emptyFileCollection = new MultipartFormDataContent();
        emptyFileCollection.Add(new ByteArrayContent([]), "files", "empty.json");
        var emptyResponse = await client.PostAsync(ServiceConstants.Routes.BatchesV1, emptyFileCollection);
        Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);
    }

    [Fact]
    public async Task OversizedRequestsFilesAndExcessiveCount_ShouldReturnBadRequest()
    {
        await using var factory = new UploadLimitTestFactory();
        using var client = factory.CreateClient();

        using var oversizedFile = CreateMultipart(("big.json", "123456789"));
        var oversizedFileResponse = await client.PostAsync(ServiceConstants.Routes.BatchesV1, oversizedFile);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedFileResponse.StatusCode);

        using var oversizedRequest = CreateMultipart(
            ("one.json", "123456"),
            ("two.json", "123456"));
        var oversizedRequestResponse = await client.PostAsync(ServiceConstants.Routes.BatchesV1, oversizedRequest);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedRequestResponse.StatusCode);

        using var excessiveCount = CreateMultipart(
            ("1.json", "{}"),
            ("2.json", "{}"),
            ("3.json", "{}"));
        var excessiveCountResponse = await client.PostAsync(ServiceConstants.Routes.BatchesV1, excessiveCount);
        Assert.Equal(HttpStatusCode.BadRequest, excessiveCountResponse.StatusCode);
    }

    [Fact]
    public async Task QueueSaturation_ShouldReturnServiceUnavailable_AndNotLeaveProcessingBatch()
    {
        await using var factory = new QueueRejectingFactory();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("one.json", "{\"ok\":true}"));
        var response = await client.PostAsync(ServiceConstants.Routes.BatchesV1, content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ServiceConstants.ErrorCodes.QueueUnavailable, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.BatchId));

        var retrieval = await client.GetAsync($"/{ServiceConstants.Routes.BatchesV1}/{error.BatchId}");
        Assert.Equal(HttpStatusCode.OK, retrieval.StatusCode);

        var batch = await retrieval.Content.ReadFromJsonAsync<BatchResponse>();
        Assert.NotNull(batch);
        Assert.NotEqual(BatchStatus.Processing, batch.Status);
    }

    [Fact]
    public async Task AcceptedBatch_ShouldEventuallyExposeCompletedResults()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("one.json", "{\"customer\":{\"id\":123}}"));
        var accepted = await PostAcceptedAsync(client, content);

        var completed = await WaitForBatchCompletedAsync(client, accepted.BatchId);

        Assert.Equal(BatchStatus.Completed, completed.Status);
        Assert.Single(completed.Payloads);
        Assert.NotEmpty(completed.Payloads[0].Documents);
    }

    [Fact]
    public async Task MixedSuccessfulAndFailedDocuments_ShouldPreservePartialResultsAndDiagnostics()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("mixed.bin", "{}{\"unterminated\":"));
        var accepted = await PostAcceptedAsync(client, content);

        var completed = await WaitForBatchCompletedAsync(client, accepted.BatchId);
        var payload = Assert.Single(completed.Payloads);

        Assert.Contains(payload.Documents, document => document.Status == DocumentStatus.Successful);
        Assert.Contains(payload.Documents, document => document.Status == DocumentStatus.Failed);
        Assert.Contains(payload.Documents.SelectMany(document => document.Diagnostics), diagnostic => !string.IsNullOrWhiteSpace(diagnostic.Code));
    }

    [Fact]
    public async Task CompletedResponses_ShouldNotExposeRawPayloadBytesOrJsonValues()
    {
        const string secretValue = "ULTRA_SECRET_VALUE";

        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var content = CreateMultipart(("secret.json", $"{{\"token\":\"{secretValue}\"}}"));
        var accepted = await PostAcceptedAsync(client, content);

        _ = await WaitForBatchCompletedAsync(client, accepted.BatchId);
        var response = await client.GetAsync($"/{ServiceConstants.Routes.BatchesV1}/{accepted.BatchId}");

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(secretValue, body, StringComparison.Ordinal);
        Assert.DoesNotContain("content", body, StringComparison.OrdinalIgnoreCase);
    }

    private static MultipartFormDataContent CreateMultipart(params (string fileName, string content)[] files)
    {
        var multipart = new MultipartFormDataContent();

        foreach (var file in files)
        {
            multipart.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(file.content)), "files", file.fileName);
        }

        return multipart;
    }

    private static async Task<BatchAcceptedResponse> PostAcceptedAsync(HttpClient client, MultipartFormDataContent content)
    {
        var response = await client.PostAsync(ServiceConstants.Routes.BatchesV1, content);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var accepted = await response.Content.ReadFromJsonAsync<BatchAcceptedResponse>();
        Assert.NotNull(accepted);
        return accepted;
    }

    private static async Task<BatchResponse> WaitForBatchCompletedAsync(HttpClient client, string batchId)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(5);

        while (DateTimeOffset.UtcNow < timeout)
        {
            var response = await client.GetAsync($"/{ServiceConstants.Routes.BatchesV1}/{batchId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var current = await response.Content.ReadFromJsonAsync<BatchResponse>();
            Assert.NotNull(current);

            if (current.Status == BatchStatus.Completed)
            {
                return current;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"Batch {batchId} did not complete in time.");
    }

    private sealed class UploadLimitTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UploadLimits:MaxRequestBytes"] = "10",
                    ["UploadLimits:MaxFileBytes"] = "8",
                    ["UploadLimits:MaxFileCount"] = "2"
                });
            });
        }
    }

    private sealed class QueueRejectingFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBatchWorkQueue>();
                services.AddSingleton<IBatchWorkQueue, RejectingQueue>();
            });
        }
    }

    private sealed class RejectingQueue : IBatchWorkQueue
    {
        public ValueTask<bool> TryEnqueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(false);
        }

        public IAsyncEnumerable<BatchProcessingRequest> ReadAllAsync(CancellationToken cancellationToken)
        {
            return AsyncEnumerable.Empty<BatchProcessingRequest>();
        }
    }

    private sealed record BatchAcceptedResponse(string BatchId, string ResourceUrl);
    private sealed record ApiErrorResponse(string Code, string Message, string? BatchId);
    private sealed record BatchResponse(string BatchId, BatchStatus Status, IReadOnlyList<PayloadResponse> Payloads);
    private sealed record PayloadResponse(int PayloadIndex, IReadOnlyList<DocumentResponse> Documents);
    private sealed record DocumentResponse(DocumentStatus Status, IReadOnlyList<DiagnosticResponse> Diagnostics);
    private sealed record DiagnosticResponse(string Code, string Message, int? Position);
}
