namespace ProjectSift.Service.Exceptions;

public sealed class UploadValidationException : Exception
{
    public UploadValidationException(string message)
        : base(message)
    {
    }
}
