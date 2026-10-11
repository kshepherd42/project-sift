using Microsoft.Extensions.DependencyInjection;
using ProjectSift.Application.Abstractions;
using ProjectSift.Application.Providers;

namespace ProjectSift.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectSiftApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<BinaryJsonPayloadScanner>();
        services.AddTransient<IBatchProcessor, BatchProcessor>();
        services.AddSingleton<IHealthStatusProvider, HealthStatusProvider>();

        return services;
    }
}
