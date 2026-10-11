using ProjectSift.Domain.Constants;

namespace ProjectSift.Domain.Models;

public sealed record HealthStatus(string Status)
{
    public static HealthStatus Healthy { get; } = new(HealthStatusTexts.Healthy);
}
