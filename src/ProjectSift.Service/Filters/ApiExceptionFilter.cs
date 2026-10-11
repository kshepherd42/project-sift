using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectSift.Service.BackgroundServices;
using ProjectSift.Service.Constants;
using ProjectSift.Service.Controllers.v1.Models;
using ProjectSift.Service.Exceptions;

namespace ProjectSift.Service.Filters;

public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        switch (context.Exception)
        {
            case UploadValidationException exception:
                _logger.LogInformation(exception, "Upload request rejected.");
                context.Result = BuildResult(StatusCodes.Status400BadRequest, ServiceConstants.ErrorCodes.InvalidUpload, exception.Message);
                context.ExceptionHandled = true;
                break;

            case BatchNotFoundException exception:
                context.Result = BuildResult(StatusCodes.Status404NotFound, ServiceConstants.ErrorCodes.BatchNotFound, exception.Message);
                context.ExceptionHandled = true;
                break;

            case BatchQueueFullException exception:
                _logger.LogWarning("Batch submission rejected because queue is full. BatchId: {BatchId}", exception.BatchId);
                context.Result = BuildResult(StatusCodes.Status503ServiceUnavailable, ServiceConstants.ErrorCodes.QueueUnavailable, exception.Message, exception.BatchId);
                context.ExceptionHandled = true;
                break;

            default:
                _logger.LogError(context.Exception, "Unexpected API failure.");
                context.Result = BuildResult(StatusCodes.Status500InternalServerError, ServiceConstants.ErrorCodes.UnexpectedFailure, "An unexpected error occurred.");
                context.ExceptionHandled = true;
                break;
        }
    }

    private static ObjectResult BuildResult(int statusCode, string code, string message, string? batchId = null)
    {
        return new ObjectResult(new ApiErrorResponse(code, message, batchId))
        {
            StatusCode = statusCode
        };
    }
}
