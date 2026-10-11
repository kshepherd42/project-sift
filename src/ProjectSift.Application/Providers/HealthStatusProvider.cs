using ProjectSift.Domain;

namespace ProjectSift.Application;

public sealed class HealthStatusProvider : IHealthStatusProvider
{
    public HealthStatus GetStatus() => HealthStatus.Healthy;
}
