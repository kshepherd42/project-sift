using ProjectSift.Domain.Models;

namespace ProjectSift.Application.Abstractions;

public interface IHealthStatusProvider
{
    HealthStatus GetStatus();
}
