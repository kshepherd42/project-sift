namespace ProjectSift.Service.Constants;

public static class ServiceConstants
{
    public static class Cors
    {
        public const string ViteDevServerPolicyName = "ViteDevServer";
        public const string ViteDevServerOrigin = "http://localhost:5173";
    }

    public static class Routes
    {
        public const string Health = "/api/health";
    }

    public static class ResponseFields
    {
        public const string Status = "status";
    }

    public static class BatchProcessing
    {
        public const string ConfigurationSection = "BatchProcessing";
        public const string QueueFullMessage = "Batch queue is full.";
        public const string ConflictRetryExhaustedMessage = "Batch update conflicts exceeded retry limit.";
        public const string UnexpectedProcessingFailureCode = "BATCH_PROCESSING_FAILED";

        public static string UnexpectedProcessingFailureMessage(string failureCategory)
            => $"Batch processing failed with category: {failureCategory}.";
    }
}
