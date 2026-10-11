namespace ProjectSift.Service.Constants;

public static class ServiceConstants
{
    public static class Configuration
    {
        public const string UploadLimitsSectionName = "UploadLimits";
        public const string ExtractionSectionName = "Extraction";
        public const string BackgroundProcessingSectionName = "BackgroundProcessing";
    }

    public static class Cors
    {
        public const string ViteDevServerPolicyName = "ViteDevServer";
        public const string ViteDevServerOrigin = "http://localhost:5173";
    }

    public static class Routes
    {
        public const string Health = "/api/health";
        public const string BatchesV1 = "v1/batches";
    }

    public static class ResponseFields
    {
        public const string Status = "status";
    }

    public static class ErrorCodes
    {
        public const string InvalidUpload = "INVALID_UPLOAD";
        public const string BatchNotFound = "BATCH_NOT_FOUND";
        public const string QueueUnavailable = "QUEUE_UNAVAILABLE";
        public const string UnexpectedFailure = "UNEXPECTED_FAILURE";
    }

    public static class BatchProcessing
    {
        public const string QueueFullMessage = "Batch queue is full.";
        public const string QueueRejectedDiagnosticCode = "BATCH_QUEUE_REJECTED";
        public const string ProcessingCancelledDiagnosticCode = "BATCH_PROCESSING_CANCELLED";
        public const string ConflictRetryExhaustedMessage = "Batch update conflicts exceeded retry limit.";
        public const string UnexpectedProcessingFailureCode = "BATCH_PROCESSING_FAILED";

        public static string UnexpectedProcessingFailureMessage(string failureCategory)
            => $"Batch processing failed with category: {failureCategory}.";

        public static string ProcessingCancelledMessage(string failureCategory)
            => $"Batch processing was cancelled with category: {failureCategory}.";
    }
}
