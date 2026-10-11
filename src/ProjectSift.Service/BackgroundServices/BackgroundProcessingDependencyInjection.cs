using Microsoft.Extensions.DependencyInjection;

namespace ProjectSift.Service.BackgroundServices;

public static class BackgroundProcessingDependencyInjection
{
    public static IServiceCollection AddProjectSiftBackgroundProcessing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var queueCapacity = 256;
        services.AddSingleton<IBatchWorkQueue>(_ => new BoundedBatchWorkQueue(queueCapacity));
        services.AddSingleton<IBatchSubmissionService, BatchSubmissionService>();
        services.AddHostedService<BatchProcessingBackgroundService>();

        return services;
    }
}
