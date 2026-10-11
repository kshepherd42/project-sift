namespace ProjectSift.Service.Constants;

public static class ServiceMessages
{
    public const string UploadValidationFailed = "Upload validation failed.";
    public const string MissingFileCollection = "The upload request must include at least one file.";
    public const string EmptyFileCollection = "The upload request must include at least one file.";
    public const string FileCountLimitExceeded = "Uploaded file count exceeds configured limit.";
    public const string BatchNotFound = "Batch was not found.";

    public static string EmptyFileAt(int payloadIndex, string fileName)
        => $"Uploaded file at payload index {payloadIndex} ('{fileName}') is empty.";

    public static string InvalidFileMetadataAt(int payloadIndex)
        => $"Uploaded file metadata is invalid at payload index {payloadIndex}.";

    public static string FileSizeLimitExceededAt(int payloadIndex, string fileName)
        => $"Uploaded file at payload index {payloadIndex} ('{fileName}') exceeds configured size limit.";

    public static string RequestSizeLimitExceededAt(int payloadIndex)
        => $"Total upload size exceeds configured limit after payload index {payloadIndex}.";

    public static string FileReadFailedAt(int payloadIndex, string fileName)
        => $"Uploaded file at payload index {payloadIndex} ('{fileName}') could not be read completely.";
}
