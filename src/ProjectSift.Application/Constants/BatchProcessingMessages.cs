namespace ProjectSift.Application.Constants;

public static class BatchProcessingMessages
{
    public static string PayloadProcessingException(string exceptionType)
        => $"Payload processing failed with an unexpected exception: {exceptionType}.";
}
