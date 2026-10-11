using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectSift.Service.Configuration;

namespace ProjectSift.Service.BackgroundServices;

public static class BackgroundProcessingDependencyInjection
{
    public static IServiceCollection AddProjectSiftBackgroundProcessing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IBatchWorkQueue>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BackgroundProcessingOptions>>().Value;
            return new BoundedBatchWorkQueue(options.QueueCapacity);
        });
        services.AddSingleton<IBatchSubmissionService, BatchSubmissionService>();
        services.AddHostedService<BatchProcessingBackgroundService>();

        return services;
    }
}
