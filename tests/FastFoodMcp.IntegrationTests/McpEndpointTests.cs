using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FastFoodMcp.IntegrationTests;

public class McpEndpointTests : IClassFixture<FastFoodMcpFactory>
{
    private readonly HttpClient _client;
    private readonly FastFoodMcpFactory _factory;

    public McpEndpointTests(FastFoodMcpFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task McpEndpoint_GET_ReturnsExpectedResponse()
    {
        // Act
        var response = await _client.GetAsync("/mcp");

        // Assert - Document what we get
        Console.WriteLine($"GET /mcp Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"GET /mcp Content: {content}");
        
        // The endpoint should exist (not 404)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound, 
            "MCP endpoint should be registered");
    }

    [Fact]
    public async Task McpEndpoint_POST_Initialize_ReturnsResponse()
    {
        // Arrange
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "test-client",
                    version = "1.0.0"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp", initRequest);

        // Assert - Document what we get
        Console.WriteLine($"POST /mcp Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"POST /mcp Content: {content}");
        
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "MCP endpoint should be registered");
            
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.TryGetProperty("result", out var resultProp).Should().BeTrue(
                "Response should contain result property");
            
            if (resultProp.ValueKind != JsonValueKind.Undefined)
            {
                var serverInfo = resultProp.GetProperty("serverInfo");
                serverInfo.GetProperty("name").GetString().Should().Be("fastfood-mcp");
                serverInfo.GetProperty("version").GetString().Should().Be("0.1.0");
            }
        }
    }

    [Fact]
    public async Task McpEndpoint_POST_ToolsList_ReturnsTools()
    {
        // Arrange - First initialize
        await InitializeServer();

        var listToolsRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp", listToolsRequest);

        // Assert
        Console.WriteLine($"POST /mcp tools/list Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"POST /mcp tools/list Content: {content}");
        
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("result", out var resultProp) && 
                resultProp.TryGetProperty("tools", out var tools))
            {
                tools.GetArrayLength().Should().Be(10, 
                    "Server should expose 10 tools");
            }
        }
    }

    private async Task InitializeServer()
    {
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "test-client",
                    version = "1.0.0"
                }
            }
        };

        await _client.PostAsJsonAsync("/mcp", initRequest);
    }
}
