using ProjectSift.Application.Providers;
using ProjectSift.Domain.Constants;
using Xunit;

namespace ProjectSift.Application.Tests;

public class HealthStatusProviderTests
{
    [Fact]
    public void GetStatus_ShouldReturnHealthyStatus()
    {
        var provider = new HealthStatusProvider();

        var result = provider.GetStatus();

        Assert.Equal(HealthStatusTexts.Healthy, result.Status);
    }
}
