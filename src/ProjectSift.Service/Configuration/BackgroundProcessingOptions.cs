using ProjectSift.Domain.Validation;

namespace ProjectSift.Service.Configuration;

public sealed class BackgroundProcessingOptions
{
    public const int DefaultQueueCapacity = 256;

    private int _queueCapacity = DefaultQueueCapacity;

    public int QueueCapacity
    {
        get => _queueCapacity;
        set => _queueCapacity = Guard.Positive(value, nameof(QueueCapacity));
    }
}
