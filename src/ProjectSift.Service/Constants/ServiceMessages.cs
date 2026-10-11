namespace ProjectSift.Service.Constants;

public static class ServiceMessages
{
    public const string MissingFileCollection = "The upload request must include at least one file.";
    public const string EmptyFileCollection = "The upload request must include at least one file.";
    public const string EmptyFile = "Uploaded file is empty.";
    public const string InvalidFileMetadata = "Uploaded file metadata is invalid.";
    public const string FileCountLimitExceeded = "Uploaded file count exceeds configured limit.";
    public const string FileSizeLimitExceeded = "Uploaded file size exceeds configured limit.";
    public const string RequestSizeLimitExceeded = "Total upload size exceeds configured limit.";
    public const string BatchNotFound = "Batch was not found.";
}
