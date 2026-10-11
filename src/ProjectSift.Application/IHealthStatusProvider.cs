using ProjectSift.Domain;

namespace ProjectSift.Application;

public interface IHealthStatusProvider
{
    HealthStatus GetStatus();
}
