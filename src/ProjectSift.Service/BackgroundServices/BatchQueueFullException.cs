namespace ProjectSift.Service.BackgroundServices;

public sealed class BatchQueueFullException : Exception
{
    public string? BatchId { get; }

    public BatchQueueFullException(string message)
        : base(message)
    {
    }

    public BatchQueueFullException(string message, string? batchId)
        : base(message)
    {
        BatchId = batchId;
    }
}
