namespace ProjectSift.Domain;

public sealed record HealthStatus(string Status)
{
    public static HealthStatus Healthy { get; } = new(HealthStatusTexts.Healthy);
}
