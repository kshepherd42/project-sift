using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectSift.Application.Models;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Domain.Models;
using ProjectSift.Service.BackgroundServices;
using ProjectSift.Service.Configuration;
using ProjectSift.Service.Constants;
using ProjectSift.Service.Controllers.v1.Models;
using ProjectSift.Service.Exceptions;
using ProjectSift.Service.Validation;

namespace ProjectSift.Service.Controllers.v1;

[ApiController]
[Route(ServiceConstants.Routes.BatchesV1)]
public sealed class BatchesController(
    IBatchSubmissionService batchSubmissionService,
    IBatchStore batchStore,
    BatchUploadValidator uploadValidator,
    IOptions<UploadLimitsOptions> options,
    ILogger<BatchesController> logger) : ControllerBase
{
    private readonly IBatchSubmissionService _batchSubmissionService = batchSubmissionService ?? throw new ArgumentNullException(nameof(batchSubmissionService));
    private readonly IBatchStore _batchStore = batchStore ?? throw new ArgumentNullException(nameof(batchStore));
    private readonly BatchUploadValidator _uploadValidator = uploadValidator ?? throw new ArgumentNullException(nameof(uploadValidator));
    private readonly UploadLimitsOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<BatchesController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost]
    [ProducesResponseType(typeof(BatchAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SubmitAsync([FromForm] List<IFormFile>? files, CancellationToken cancellationToken)
    {
        _uploadValidator.Validate(files);

        var payloads = new List<PayloadInput>(files!.Count);
        for (var payloadIndex = 0; payloadIndex < files.Count; payloadIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            payloads.Add(await CreatePayloadAsync(files[payloadIndex], payloadIndex, cancellationToken));
        }

        var batchId = Guid.NewGuid().ToString();
        _ = await _batchSubmissionService.QueueAsync(new BatchProcessingRequest(batchId, payloads), cancellationToken);

        var resourceUrl = Url.ActionLink(nameof(GetByIdAsync), values: new { batchId })
            ?? $"/{ServiceConstants.Routes.BatchesV1}/{batchId}";

        _logger.LogInformation("Batch {BatchId} accepted with {PayloadCount} payloads.", batchId, payloads.Count);

        Response.Headers.Location = resourceUrl;
        return Accepted(resourceUrl, new BatchAcceptedResponse(batchId, resourceUrl));
    }

    [HttpGet("{batchId}")]
    [ProducesResponseType(typeof(BatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BatchResponse>> GetByIdAsync(string batchId, CancellationToken cancellationToken)
    {
        var result = await _batchStore.GetByIdAsync(batchId, cancellationToken);
        if (result is null)
        {
            throw new BatchNotFoundException(ServiceMessages.BatchNotFound);
        }

        return Ok(ToResponse(result));
    }

    private async Task<PayloadInput> CreatePayloadAsync(IFormFile file, int payloadIndex, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream((int)Math.Min(file.Length, _options.MaxFileBytes));
        await stream.CopyToAsync(buffer, cancellationToken);
        return new PayloadInput(payloadIndex, buffer.ToArray());
    }

    private static BatchResponse ToResponse(BatchResult result)
    {
        return new BatchResponse(
            result.BatchId,
            result.Status,
            result.Payloads
                .Select(payload => new PayloadResponse(
                    payload.PayloadIndex,
                    payload.Documents
                        .Select(document => new DocumentResponse(
                            document.DocumentIndex,
                            document.Type,
                            document.Status,
                            document.PropertyPaths.Select(path => new PropertyPathResponse(path.Value)).ToArray(),
                            document.Diagnostics.Select(diagnostic => new DiagnosticResponse(diagnostic.Code, diagnostic.Message, diagnostic.Position)).ToArray()))
                        .ToArray(),
                    payload.Diagnostics.Select(diagnostic => new DiagnosticResponse(diagnostic.Code, diagnostic.Message, diagnostic.Position)).ToArray()))
                .ToArray(),
            result.CreatedUtc,
            result.CompletedUtc);
    }
}
