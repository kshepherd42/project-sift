using ProjectSift.Application.Abstractions;
using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Providers;

public sealed class HealthStatusProvider : IHealthStatusProvider
{
    public HealthStatus GetStatus() => HealthStatus.Healthy;
}
