using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FastFoodMcp.IntegrationTests;

public class HealthEndpointTests : IClassFixture<FastFoodMcpFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(FastFoodMcpFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthData = JsonSerializer.Deserialize<JsonElement>(content);
        
        healthData.GetProperty("status").GetString().Should().Be("healthy");
        healthData.GetProperty("server").GetString().Should().Be("fastfood-mcp");
        healthData.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }
}
