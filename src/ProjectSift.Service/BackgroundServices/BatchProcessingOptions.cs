using ProjectSift.Domain.Validation;
using ProjectSift.Service.Constants;

namespace ProjectSift.Service.BackgroundServices;

public sealed class BatchProcessingOptions
{
    public int QueueCapacity { get; set; } = 256;

    public void Validate()
    {
        Guard.Positive(QueueCapacity, nameof(QueueCapacity));
    }
}
