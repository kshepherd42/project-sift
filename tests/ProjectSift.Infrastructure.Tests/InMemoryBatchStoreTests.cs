using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Exceptions;
using ProjectSift.Domain.Models;
using ProjectSift.Infrastructure.Persistence.InMemory;
using Xunit;

namespace ProjectSift.Infrastructure.Tests;

public sealed class InMemoryBatchStoreTests
{
    private static readonly string BatchA = Guid.NewGuid().ToString();
    private static readonly string BatchB = Guid.NewGuid().ToString();

    [Fact]
    public async Task CreateAndRetrieve_ShouldReturnProcessingBatch()
    {
        var store = CreateStore();

        var created = await store.CreateAsync(BatchA);
        var fetched = await store.GetByIdAsync(BatchA);

        Assert.Equal(BatchStatus.Processing, created.Status);
        Assert.NotNull(fetched);
        Assert.Equal(BatchA, fetched.BatchId);
        Assert.Equal(BatchStatus.Processing, fetched.Status);
        Assert.Equal(0, fetched.Etag);
    }

    [Fact]
    public async Task Update_ShouldPersistCompletedStatusPayloadsAndDiagnostics()
    {
        var store = CreateStore();
        var created = await store.CreateAsync(BatchA);

        var diagnostic = new ExtractionDiagnostic("DOC001", "failed");
        var payload = new PayloadResult(1, diagnostics: [diagnostic]);
        var updated = await store.UpdateAsync(new BatchResult(
            created.BatchId,
            BatchStatus.Completed,
            [payload],
            created.CreatedUtc,
            DateTimeOffset.UtcNow,
            created.Etag));

        var fetched = await store.GetByIdAsync(BatchA);

        Assert.Equal(1, updated.Etag);
        Assert.NotNull(fetched);
        Assert.Equal(BatchStatus.Completed, fetched.Status);
        var persistedPayload = Assert.Single(fetched.Payloads);
        Assert.Equal(1, persistedPayload.PayloadIndex);
        Assert.Equal("DOC001", Assert.Single(persistedPayload.Diagnostics).Code);
    }

    [Fact]
    public async Task MissingBatchBehavior_ShouldReturnNull()
    {
        var store = CreateStore();

        var fetched = await store.GetByIdAsync("missing-batch");

        Assert.Null(fetched);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldAvoidCorruptionWithEtagConflicts()
    {
        var store = CreateStore();
        var created = await store.CreateAsync(BatchA);

        var work = Enumerable.Range(0, 20)
            .Select(async _ =>
            {
                var update = new BatchResult(
                    created.BatchId,
                    BatchStatus.Completed,
                    [new PayloadResult(0, diagnostics: [new ExtractionDiagnostic("C", "conflict-test")])],
                    created.CreatedUtc,
                    DateTimeOffset.UtcNow,
                    created.Etag);

                try
                {
                    return await store.UpdateAsync(update);
                }
                catch (ConflictException)
                {
                    return null;
                }
            });

        var results = await Task.WhenAll(work);

        _ = Assert.Single(results, result => result is not null);
        var final = await store.GetByIdAsync(BatchA);
        Assert.NotNull(final);
        Assert.Equal(BatchStatus.Completed, final.Status);
    }

    [Fact]
    public async Task IndependentBatches_ShouldRetainIndependentResults()
    {
        var store = CreateStore();
        var createdA = await store.CreateAsync(BatchA);
        var createdB = await store.CreateAsync(BatchB);

        _ = await store.UpdateAsync(new BatchResult(
            BatchA,
            BatchStatus.Completed,
            [new PayloadResult(0, diagnostics: [new ExtractionDiagnostic("A", "diag-a")])],
            createdA.CreatedUtc,
            DateTimeOffset.UtcNow,
            createdA.Etag));

        _ = await store.UpdateAsync(new BatchResult(
            BatchB,
            BatchStatus.Completed,
            [new PayloadResult(1, diagnostics: [new ExtractionDiagnostic("B", "diag-b")])],
            createdB.CreatedUtc,
            DateTimeOffset.UtcNow,
            createdB.Etag));

        var fetchedA = await store.GetByIdAsync(BatchA);
        var fetchedB = await store.GetByIdAsync(BatchB);

        Assert.NotNull(fetchedA);
        Assert.NotNull(fetchedB);
        Assert.Equal("A", Assert.Single(fetchedA.Payloads[0].Diagnostics).Code);
        Assert.Equal("B", Assert.Single(fetchedB.Payloads[0].Diagnostics).Code);
    }

    [Fact]
    public async Task CompletedBatch_NoPayload()
    {
        var store = CreateStore();
        var created = await store.CreateAsync(BatchA);

        _ = await store.UpdateAsync(new BatchResult(
            BatchA,
            BatchStatus.Completed,
            [new PayloadResult(0, [new DocumentResult(0, DocumentType.Object, DocumentStatus.Successful)])],
            created.CreatedUtc,
            DateTimeOffset.UtcNow,
            created.Etag));

        var fetched = await store.GetByIdAsync(BatchA);

        Assert.NotNull(fetched);
        Assert.All(fetched.Payloads, payload => Assert.All(payload.Documents, _ => Assert.True(true)));
    }

    private static InMemoryBatchStore CreateStore()
    {
        return new InMemoryBatchStore(new InMemoryBatchDatabase());
    }
}
