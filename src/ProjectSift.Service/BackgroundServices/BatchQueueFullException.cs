namespace ProjectSift.Service.BackgroundServices;

public sealed class BatchQueueFullException : Exception
{
    public BatchQueueFullException(string message)
        : base(message)
    {
    }
}
