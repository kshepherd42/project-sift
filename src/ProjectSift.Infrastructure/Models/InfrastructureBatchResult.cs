using ProjectSift.Domain.Enums;
using ProjectSift.Domain.Models;

namespace ProjectSift.Infrastructure.Models;

internal sealed record InfrastructureBatchResult(
    string BatchId,
    BatchStatus Status,
    IReadOnlyList<PayloadResult> Payloads,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc,
    long Etag);
