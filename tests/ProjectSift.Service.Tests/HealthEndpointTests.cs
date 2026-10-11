using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProjectSift.Domain;
using ProjectSift.Service;
using Xunit;

namespace ProjectSift.Service.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200AndExpectedPayload()
    {
        var response = await _client.GetAsync(ServiceConstants.Routes.Health);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(payload);
        Assert.True(payload.TryGetValue(ServiceConstants.ResponseFields.Status, out var status));
        Assert.Equal(HealthStatusTexts.Healthy, status);
    }
}
