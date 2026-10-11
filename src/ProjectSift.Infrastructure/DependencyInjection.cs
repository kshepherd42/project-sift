using Microsoft.Extensions.DependencyInjection;
using ProjectSift.Domain.Abstractions.Infrastructure;
using ProjectSift.Infrastructure.Persistence.InMemory;

namespace ProjectSift.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectSiftInfrastructure(
        this IServiceCollection services,
        Action<InMemoryBatchStoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new InMemoryBatchStoreOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<InMemoryBatchDatabase>();
        services.AddSingleton<IBatchStore, InMemoryBatchStore>();

        return services;
    }
}
