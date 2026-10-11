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
    IOptions<UploadLimitsOptions> uploadOptions,
    ILogger<BatchesController> logger) : ControllerBase
{
    private readonly IBatchSubmissionService _batchSubmissionService = batchSubmissionService ?? throw new ArgumentNullException(nameof(batchSubmissionService));
    private readonly IBatchStore _batchStore = batchStore ?? throw new ArgumentNullException(nameof(batchStore));
    private readonly BatchUploadValidator _uploadValidator = uploadValidator ?? throw new ArgumentNullException(nameof(uploadValidator));
    private readonly UploadLimitsOptions _uploadOptions = uploadOptions?.Value ?? throw new ArgumentNullException(nameof(uploadOptions));
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
        try
        {
            if (file.Length > _uploadOptions.MaxFileBytes)
            {
                throw new UploadValidationException(ServiceMessages.FileSizeLimitExceededAt(payloadIndex, file.FileName));
            }

            await using var stream = file.OpenReadStream();

            var length = checked((int)file.Length);
            var bytes = GC.AllocateUninitializedArray<byte>(length);

            var readTotal = 0;
            while (readTotal < length)
            {
                var bytesRead = await stream.ReadAsync(bytes.AsMemory(readTotal), cancellationToken);
                if (bytesRead == 0)
                {
                    throw new UploadValidationException(ServiceMessages.FileReadFailedAt(payloadIndex, file.FileName));
                }

                readTotal += bytesRead;
            }

            return new PayloadInput(payloadIndex, bytes, file.FileName);
        }
        catch (IOException)
        {
            throw new UploadValidationException(ServiceMessages.FileReadFailedAt(payloadIndex, file.FileName));
        }
        catch (InvalidDataException)
        {
            throw new UploadValidationException(ServiceMessages.FileReadFailedAt(payloadIndex, file.FileName));
        }
    }

    private static BatchResponse ToResponse(BatchResult result)
    {
        return new BatchResponse(
            result.BatchId,
            result.Status,
            result.Payloads
                .Select(payload => new PayloadResponse(
                    payload.PayloadIndex,
                    payload.PayloadName,
                    payload.Documents
                        .Select(document => new DocumentResponse(
                            document.DocumentIndex,
                            document.DocumentName,
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
