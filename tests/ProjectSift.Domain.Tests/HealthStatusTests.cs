using ProjectSift.Domain.Constants;
using ProjectSift.Domain.Models;
using Xunit;

namespace ProjectSift.Domain.Tests;

public class HealthStatusTests
{
    [Fact]
    public void Healthy_ShouldHaveExpectedStatusText()
    {
        Assert.Equal(HealthStatusTexts.Healthy, HealthStatus.Healthy.Status);
    }
}
