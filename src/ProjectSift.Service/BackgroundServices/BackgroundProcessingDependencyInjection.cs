using Microsoft.Extensions.DependencyInjection;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Infrastructure.Persistence.InMemory;

namespace ProjectSift.Service.BackgroundServices;

public static class BackgroundProcessingDependencyInjection
{
    public static IServiceCollection AddProjectSiftBackgroundProcessing(
        this IServiceCollection services,
        Action<BatchProcessingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BatchProcessingOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(new InMemoryBatchDatabase());
        services.AddSingleton<IBatchStore, InMemoryBatchStore>();

        services.AddSingleton(options);
        services.AddSingleton<IBatchWorkQueue>(_ => new BoundedBatchWorkQueue(options.QueueCapacity));
        services.AddSingleton<IBatchSubmissionService, BatchSubmissionService>();

        services.AddHostedService<BatchProcessingBackgroundService>();

        return services;
    }
}
