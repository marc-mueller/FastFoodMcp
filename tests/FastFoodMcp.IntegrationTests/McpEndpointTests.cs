using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
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

    #region Basic Endpoint Tests

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
    public async Task McpEndpoint_POST_Initialize_ReturnsServerInfo()
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
        var response = await PostMcpRequest(initRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        var serverInfo = resultProp.GetProperty("serverInfo");
        serverInfo.GetProperty("name").GetString().Should().Be("fastfood-mcp");
        serverInfo.GetProperty("version").GetString().Should().Be("0.1.0");
    }

    [Fact]
    public async Task McpEndpoint_POST_ToolsList_ReturnsAllTools()
    {
        // Arrange
        await InitializeServer();

        var listToolsRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

        // Act
        var response = await PostMcpRequest(listToolsRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var tools = resultProp.GetProperty("tools");
        
        tools.GetArrayLength().Should().Be(10, "Server should expose 10 tools");
        
        var toolNames = new List<string>();
        foreach (var tool in tools.EnumerateArray())
        {
            toolNames.Add(tool.GetProperty("name").GetString()!);
        }

        toolNames.Should().Contain(new[]
        {
            "explain_error",
            "search_errors",
            "suggest_fix",
            "get_service",
            "list_dependencies",
            "find_endpoint",
            "service_owner",
            "list_flags",
            "get_flag",
            "flag_status"
        });
    }

    #endregion

    #region Error Tools Round-Trip Tests

    [Fact]
    public async Task ExplainError_WithValidCode_ReturnsErrorDetails()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 10,
            method = "tools/call",
            @params = new
            {
                name = "explain_error",
                arguments = new
                {
                    code = "E2145"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"ExplainError Response: {content}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        var contentArray = resultProp.GetProperty("content");
        contentArray.GetArrayLength().Should().BeGreaterThan(0);
        
        var text = contentArray[0].GetProperty("text").GetString();
        text.Should().Contain("E2145");
        text.Should().Contain("Database Connection Failed");
    }

    [Fact]
    public async Task ExplainError_WithInvalidCode_ReturnsError()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 11,
            method = "tools/call",
            @params = new
            {
                name = "explain_error",
                arguments = new
                {
                    code = "INVALID999"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Should contain an error response
        result.TryGetProperty("error", out var error).Should().BeTrue();
    }

    [Fact]
    public async Task SearchErrors_FindsMatchingErrors()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 12,
            method = "tools/call",
            @params = new
            {
                name = "search_errors",
                arguments = new
                {
                    query = "database"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var contentArray = resultProp.GetProperty("content");
        
        var text = contentArray[0].GetProperty("text").GetString();
        text.Should().Contain("E2145");
    }

    [Fact]
    public async Task SuggestFix_ReturnsFixSuggestions()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 13,
            method = "tools/call",
            @params = new
            {
                name = "suggest_fix",
                arguments = new
                {
                    code = "P5001"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var contentArray = resultProp.GetProperty("content");
        
        var text = contentArray[0].GetProperty("text").GetString();
        text.Should().Contain("P5001");
        text.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Service Tools Round-Trip Tests

    [Fact]
    public async Task GetService_WithValidName_ReturnsServiceDetails()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 20,
            method = "tools/call",
            @params = new
            {
                name = "get_service",
                arguments = new
                {
                    name = "usersvc"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"GetService Response: {content}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().Contain("usersvc");
        text.Should().Contain("User management service");
    }

    [Fact]
    public async Task GetService_WithInvalidName_ReturnsError()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 21,
            method = "tools/call",
            @params = new
            {
                name = "get_service",
                arguments = new
                {
                    name = "nonexistent-service"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("error", out var error).Should().BeTrue();
    }

    [Fact]
    public async Task ListDependencies_ReturnsServiceDependencies()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 22,
            method = "tools/call",
            @params = new
            {
                name = "list_dependencies",
                arguments = new
                {
                    name = "checkout",
                    depth = 2
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().Contain("checkout");
    }

    [Fact]
    public async Task FindEndpoint_WithQuery_FindsMatchingEndpoints()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 23,
            method = "tools/call",
            @params = new
            {
                name = "find_endpoint",
                arguments = new
                {
                    name = "create user"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ServiceOwner_ReturnsOwnershipInformation()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 24,
            method = "tools/call",
            @params = new
            {
                name = "service_owner",
                arguments = new
                {
                    name = "paymentsvc"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().Contain("paymentsvc");
    }

    #endregion

    #region Flag Tools Round-Trip Tests

    [Fact]
    public async Task ListFlags_ReturnsAllFlags()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 30,
            method = "tools/call",
            @params = new
            {
                name = "list_flags",
                arguments = new { }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"ListFlags Response: {content}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().Contain("checkout.newAddressForm");
        text.Should().Contain("pricing.experimentA");
    }

    [Fact]
    public async Task GetFlag_WithValidKey_ReturnsFlagDetails()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 31,
            method = "tools/call",
            @params = new
            {
                name = "get_flag",
                arguments = new
                {
                    key = "checkout.newAddressForm"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().Contain("checkout.newAddressForm");
        text.Should().Contain("New address validation form");
    }

    [Fact]
    public async Task GetFlag_WithInvalidKey_ReturnsError()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 32,
            method = "tools/call",
            @params = new
            {
                name = "get_flag",
                arguments = new
                {
                    key = "nonexistent.flag"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("error", out var error).Should().BeTrue();
    }

    [Fact]
    public async Task FlagStatus_ReturnsStatusInformation()
    {
        // Arrange
        await InitializeServer();

        var callToolRequest = new
        {
            jsonrpc = "2.0",
            id = 33,
            method = "tools/call",
            @params = new
            {
                name = "flag_status",
                arguments = new
                {
                    key = "pricing.experimentA",
                    environment = "staging"
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        var text = resultProp.GetProperty("content")[0].GetProperty("text").GetString();
        text.Should().Contain("pricing.experimentA");
    }

    #endregion

    #region Helper Methods

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

        await PostMcpRequest(initRequest);
    }

    private async Task<HttpResponseMessage> PostMcpRequest(object request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/mcp", content);
    }

    #endregion
}
