using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ProjectSift.Service.Configuration;
using ProjectSift.Service.Constants;
using ProjectSift.Service.Exceptions;

namespace ProjectSift.Service.Validation;

public sealed class BatchUploadValidator(IOptions<UploadLimitsOptions> options)
{
    private readonly UploadLimitsOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public void Validate(IReadOnlyList<IFormFile>? files)
    {
        if (files is null)
        {
            throw new UploadValidationException(ServiceMessages.MissingFileCollection);
        }

        if (files.Count == 0)
        {
            throw new UploadValidationException(ServiceMessages.EmptyFileCollection);
        }

        var issues = new List<string>();

        if (files.Count > _options.MaxFileCount)
        {
            issues.Add(ServiceMessages.FileCountLimitExceeded);
        }

        long totalBytes = 0;
        var requestSizeExceeded = false;

        for (var payloadIndex = 0; payloadIndex < files.Count; payloadIndex++)
        {
            var file = files[payloadIndex];

            if (file is null)
            {
                issues.Add(ServiceMessages.InvalidFileMetadataAt(payloadIndex));
                continue;
            }

            if (string.IsNullOrWhiteSpace(file.Name) || string.IsNullOrWhiteSpace(file.FileName))
            {
                issues.Add(ServiceMessages.InvalidFileMetadataAt(payloadIndex));
            }

            if (file.Length <= 0)
            {
                issues.Add(ServiceMessages.EmptyFileAt(payloadIndex, file.FileName));
            }

            if (file.Length > _options.MaxFileBytes)
            {
                issues.Add(ServiceMessages.FileSizeLimitExceededAt(payloadIndex, file.FileName));
            }

            totalBytes += file.Length;
            if (!requestSizeExceeded && totalBytes > _options.MaxRequestBytes)
            {
                issues.Add(ServiceMessages.RequestSizeLimitExceededAt(payloadIndex));
                requestSizeExceeded = true;
            }
        }

        if (issues.Count > 0)
        {
            throw new UploadValidationException(issues);
        }
    }
}
