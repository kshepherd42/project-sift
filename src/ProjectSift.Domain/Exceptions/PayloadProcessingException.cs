namespace ProjectSift.Domain.Exceptions;

public class PayloadProcessingException : Exception
{
    public PayloadProcessingException(string message)
        : base(message)
    {
    }

    public PayloadProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}