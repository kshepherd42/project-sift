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

        if (files.Count > _options.MaxFileCount)
        {
            throw new UploadValidationException(ServiceMessages.FileCountLimitExceeded);
        }

        long totalBytes = 0;

        foreach (var file in files)
        {
            if (file is null)
            {
                throw new UploadValidationException(ServiceMessages.InvalidFileMetadata);
            }

            if (string.IsNullOrWhiteSpace(file.Name) || string.IsNullOrWhiteSpace(file.FileName))
            {
                throw new UploadValidationException(ServiceMessages.InvalidFileMetadata);
            }

            if (file.Length <= 0)
            {
                throw new UploadValidationException(ServiceMessages.EmptyFile);
            }

            if (file.Length > _options.MaxFileBytes)
            {
                throw new UploadValidationException(ServiceMessages.FileSizeLimitExceeded);
            }

            totalBytes += file.Length;
            if (totalBytes > _options.MaxRequestBytes)
            {
                throw new UploadValidationException(ServiceMessages.RequestSizeLimitExceeded);
            }
        }
    }
}
