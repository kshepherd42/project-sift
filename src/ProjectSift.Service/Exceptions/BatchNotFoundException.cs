namespace ProjectSift.Service.Exceptions;

public sealed class BatchNotFoundException : Exception
{
    public BatchNotFoundException(string message)
        : base(message)
    {
    }
}
