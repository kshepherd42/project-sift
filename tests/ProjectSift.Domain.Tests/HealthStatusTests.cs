using ProjectSift.Domain;
using Xunit;

namespace ProjectSift.Domain.Tests;

public class HealthStatusTests
{
    [Fact]
    public void Healthy_ShouldHaveExpectedStatusText()
    {
        Assert.Equal("Healthy", HealthStatus.Healthy.Status);
    }
}
