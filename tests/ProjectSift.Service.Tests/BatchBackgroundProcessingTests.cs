using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;
using ProjectSift.Service.BackgroundServices;
using ProjectSift.Service.Constants;
using ProjectSift.Infrastructure.Persistence.InMemory;
using Xunit;

namespace ProjectSift.Service.Tests;

public sealed class BatchBackgroundProcessingTests
{
    [Fact]
    public async Task QueuedBatch_ShouldEventuallyReachCompleted()
    {
        var batchId = Guid.NewGuid().ToString();
        var processor = new FakeBatchProcessor();
        processor.SetResult(batchId, request =>
        {
            var payload = request.Payloads.Single();
            return new BatchResult(
                request.BatchId,
                BatchStatus.Completed,
                [new PayloadResult(payload.PayloadIndex, [new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful)])],
                completedUtc: DateTimeOffset.UtcNow);
        });

        await using var factory = new TestFactory(processor);
        using var scope = factory.Services.CreateScope();
        var submission = scope.ServiceProvider.GetRequiredService<IBatchSubmissionService>();
        var store = scope.ServiceProvider.GetRequiredService<IBatchStore>();

        _ = await submission.QueueAsync(new BatchProcessingRequest(
            batchId,
            [new PayloadInput(0, new byte[] { 0x7B, 0x7D })]));

        var completed = await WaitForCompletedAsync(store, batchId);

        Assert.Equal(BatchStatus.Completed, completed.Status);
        Assert.Single(completed.Payloads);
    }

    [Fact]
    public async Task QueuedBatch_ShouldPreservePartialFailures()
    {
        var batchId = Guid.NewGuid().ToString();
        var processor = new FakeBatchProcessor();
        processor.SetResult(batchId, request =>
            new BatchResult(
                request.BatchId,
                BatchStatus.Completed,
                [
                    new PayloadResult(2,
                    [
                        new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful),
                        new DocumentResult(1, DocumentType.Unknown, DocumentStatus.Failed, diagnostics: [new ExtractionDiagnostic("DOC_FAIL", "failed")])
                    ])
                ],
                completedUtc: DateTimeOffset.UtcNow));

        await using var factory = new TestFactory(processor);
        using var scope = factory.Services.CreateScope();
        var submission = scope.ServiceProvider.GetRequiredService<IBatchSubmissionService>();
        var store = scope.ServiceProvider.GetRequiredService<IBatchStore>();

        _ = await submission.QueueAsync(new BatchProcessingRequest(batchId, [new PayloadInput(2, new byte[] { 0x7B, 0x7D })]));

        var completed = await WaitForCompletedAsync(store, batchId);
        var payload = Assert.Single(completed.Payloads);
        Assert.Equal(2, payload.Documents.Count);
        Assert.Contains(payload.Documents, document => document.Status == DocumentStatus.Successful);
        Assert.Contains(payload.Documents, document => document.Status == DocumentStatus.Failed);
    }

    [Fact]
    public async Task FailingBatch_ShouldNotBlockSubsequentBatch()
    {
        var failedBatchId = Guid.NewGuid().ToString();
        var nextBatchId = Guid.NewGuid().ToString();
        var processor = new FakeBatchProcessor();
        processor.SetException(failedBatchId, new InvalidOperationException("forced"));
        processor.SetResult(nextBatchId, request =>
            new BatchResult(
                request.BatchId,
                BatchStatus.Completed,
                [new PayloadResult(1, [new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful)])],
                completedUtc: DateTimeOffset.UtcNow));

        await using var factory = new TestFactory(processor);
        using var scope = factory.Services.CreateScope();
        var submission = scope.ServiceProvider.GetRequiredService<IBatchSubmissionService>();
        var store = scope.ServiceProvider.GetRequiredService<IBatchStore>();

        _ = await submission.QueueAsync(new BatchProcessingRequest(failedBatchId, [new PayloadInput(0, new byte[] { 0x7B, 0x7D })]));
        _ = await submission.QueueAsync(new BatchProcessingRequest(nextBatchId, [new PayloadInput(1, new byte[] { 0x7B, 0x7D })]));

        var failed = await WaitForCompletedAsync(store, failedBatchId);
        var next = await WaitForCompletedAsync(store, nextBatchId);

        var failedPayload = Assert.Single(failed.Payloads);
        var diagnostic = Assert.Single(failedPayload.Diagnostics);
        Assert.Equal(ServiceConstants.BatchProcessing.UnexpectedProcessingFailureCode, diagnostic.Code);
        Assert.Equal(BatchStatus.Completed, next.Status);
    }

    [Fact]
    public async Task QueueFull_ShouldThrowException()
    {
        var batchId = Guid.NewGuid().ToString();
        var store = new InMemoryBatchStore(new InMemoryBatchDatabase());
        var submission = new BatchSubmissionService(store, new RejectingQueue(), NullLogger<BatchSubmissionService>.Instance);

        await Assert.ThrowsAsync<BatchQueueFullException>(() => submission.QueueAsync(
            new BatchProcessingRequest(batchId, [new PayloadInput(0, new byte[] { 0x7B, 0x7D })])));
    }

    [Fact]
    public async Task QueueAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        var batchId = Guid.NewGuid().ToString();
        var processor = new FakeBatchProcessor();
        await using var factory = new TestFactory(processor);
        using var scope = factory.Services.CreateScope();
        var submission = scope.ServiceProvider.GetRequiredService<IBatchSubmissionService>();

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => submission.QueueAsync(
            new BatchProcessingRequest(batchId, [new PayloadInput(0, new byte[] { 0x7B, 0x7D })]),
            cancellation.Token));
    }

    [Fact]
    public async Task QueueAsync_WhenCancelledDuringEnqueue_ShouldMarkBatchCompletedWithCancellationDiagnostic()
    {
        var batchId = Guid.NewGuid().ToString();
        var store = new InMemoryBatchStore(new InMemoryBatchDatabase());
        var submission = new BatchSubmissionService(store, new BlockingQueue(), NullLogger<BatchSubmissionService>.Instance);
        using var cancellation = new CancellationTokenSource();
        var payloadBytes = new byte[] { 0x7B, 0x7D };

        var queueTask = submission.QueueAsync(
            new BatchProcessingRequest(batchId, [new PayloadInput(0, payloadBytes)]),
            cancellation.Token);

        cancellation.CancelAfter(50);
        await Assert.ThrowsAsync<OperationCanceledException>(() => queueTask);

        var persisted = await store.GetByIdAsync(batchId);
        Assert.NotNull(persisted);
        Assert.Equal(BatchStatus.Completed, persisted.Status);

        var payload = Assert.Single(persisted.Payloads);
        var diagnostic = Assert.Single(payload.Diagnostics);
        Assert.Equal(ServiceConstants.BatchProcessing.ProcessingCancelledDiagnosticCode, diagnostic.Code);
        Assert.True(payloadBytes.All(value => value == 0));
    }

    private static async Task<BatchResult> WaitForCompletedAsync(IBatchStore store, string batchId)
    {
        var timeoutUtc = DateTimeOffset.UtcNow.AddSeconds(5);

        while (DateTimeOffset.UtcNow < timeoutUtc)
        {
            var current = await store.GetByIdAsync(batchId);
            if (current is not null && current.Status == BatchStatus.Completed)
            {
                return current;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"Batch {batchId} did not complete in time.");
    }

    private sealed class TestFactory(FakeBatchProcessor processor) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBatchProcessor>();
                services.AddSingleton<IBatchProcessor>(processor);
            });
        }
    }

    private sealed class FakeBatchProcessor : IBatchProcessor
    {
        private readonly ConcurrentDictionary<string, Func<BatchProcessingRequest, CancellationToken, Task<BatchResult>>> _handlers = new(StringComparer.Ordinal);

        public void SetResult(string batchId, Func<BatchProcessingRequest, BatchResult> factory)
        {
            _handlers[batchId] = (request, _) => Task.FromResult(factory(request));
        }

        public void SetAsyncResult(string batchId, Func<BatchProcessingRequest, Task<BatchResult>> factory)
        {
            _handlers[batchId] = (request, _) => factory(request);
        }

        public void SetException(string batchId, Exception exception)
        {
            _handlers[batchId] = (_, _) => Task.FromException<BatchResult>(exception);
        }

        public Task<BatchResult> ProcessAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
        {
            if (_handlers.TryGetValue(request.BatchId, out var handler))
            {
                return handler(request, cancellationToken);
            }

            return Task.FromResult(new BatchResult(
                request.BatchId,
                BatchStatus.Completed,
                [new PayloadResult(0)],
                completedUtc: DateTimeOffset.UtcNow));
        }
    }

    private sealed class RejectingQueue : IBatchWorkQueue
    {
        public ValueTask<bool> TryEnqueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return ValueTask.FromResult(false);
        }

        public async IAsyncEnumerable<BatchProcessingRequest> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class BlockingQueue : IBatchWorkQueue
    {
        public async ValueTask<bool> TryEnqueueAsync(BatchProcessingRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return false;
        }

        public IAsyncEnumerable<BatchProcessingRequest> ReadAllAsync(CancellationToken cancellationToken)
            => AsyncEnumerable.Empty<BatchProcessingRequest>();
    }
}
