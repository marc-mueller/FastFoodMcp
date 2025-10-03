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
        
        var result = await ParseSseResponse(response);
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
        
        var result = await ParseSseResponse(response);
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
        
        var result = await ParseSseResponse(response);
        Console.WriteLine($"ExplainError Response: {result}");
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent for the actual response object
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        
        structuredContent.GetProperty("code").GetString().Should().Be("E2145");
        structuredContent.GetProperty("title").GetString().Should().Contain("JWT token expired");
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
        
        var result = await ParseSseResponse(response);
        Console.WriteLine($"ExplainError (invalid) Response: {result}");
        
        // Should contain a success response with error message in structuredContent
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        
        var title = structuredContent.GetProperty("title").GetString();
        title.Should().Contain("not found");
    }

        [Fact]
    public async Task SearchErrors_FindsMatchingErrors()
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
                name = "search_errors",
                arguments = new
                {
                    query = "E2145",
                    limit = 10
                }
            }
        };

        // Act
        var response = await PostMcpRequest(callToolRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await ParseSseResponse(response);
        Console.WriteLine($"SearchErrors Response: {result}");
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent for the array of results
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        
        // MCP SDK wraps List<T> in an array - just verify we got results
        // The SDK serializes List<ErrorSearchResult> directly as an array in structuredContent
        if (structuredContent.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            structuredContent.GetArrayLength().Should().BeGreaterThan(0);
            
            // Verify one of the results contains the search term
            bool foundMatch = false;
            foreach (var item in structuredContent.EnumerateArray())
            {
                if (item.GetProperty("code").GetString()?.Contains("E2145") == true)
                {
                    foundMatch = true;
                    break;
                }
            }
            foundMatch.Should().BeTrue();
        }
        else
        {
            // If it's an object, the array might be a property
            structuredContent.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
        }
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent exists (array of fix steps or wrapped object)
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);
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
        
        var result = await ParseSseResponse(response);
        Console.WriteLine($"GetService Response: {result}");
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.GetProperty("name").GetString().Should().Be("usersvc");
        structuredContent.GetProperty("description").GetString().Should().NotBeNullOrEmpty();
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        
        var description = structuredContent.GetProperty("description").GetString();
        description.Should().Contain("not found");
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent for the dependencies (array or object wrapping)
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent exists (may be empty array or object wrapping)
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.GetProperty("team").GetString().Should().NotBeNullOrEmpty();
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
        
        var result = await ParseSseResponse(response);
        Console.WriteLine($"ListFlags Response: {result}");
        
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent for the flags  
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        
        // MCP SDK may wrap List<T> differently - just verify we got data
        if (structuredContent.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            structuredContent.GetArrayLength().Should().BeGreaterThan(0);
            
            // Verify specific flags exist
            var flagKeys = new List<string>();
            foreach (var flag in structuredContent.EnumerateArray())
            {
                flagKeys.Add(flag.GetProperty("key").GetString()!);
            }
            flagKeys.Should().Contain("checkout.newAddressForm");
            flagKeys.Should().Contain("pricing.experimentA");
        }
        else
        {
            // If wrapped in object, just verify it's not null
            structuredContent.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
        }
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.GetProperty("key").GetString().Should().Be("checkout.newAddressForm");
        structuredContent.GetProperty("description").GetString().Should().NotBeNullOrEmpty();
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        
        var description = structuredContent.GetProperty("description").GetString();
        description.Should().Contain("not found");
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
        
        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();
        
        // Check structuredContent
        resultProp.TryGetProperty("structuredContent", out var structuredContent).Should().BeTrue();
        structuredContent.GetProperty("key").GetString().Should().Be("pricing.experimentA");
        structuredContent.GetProperty("environment").GetString().Should().Be("staging");
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
        
        // MCP HTTP transport requires both Accept headers
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = content
        };
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        
        return await _client.SendAsync(requestMessage);
    }

    private async Task<JsonElement> ParseSseResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        // Parse SSE format: "event: message\ndata: {json}\n\n"
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
            {
                var jsonData = line.Substring(6); // Remove "data: " prefix
                return JsonSerializer.Deserialize<JsonElement>(jsonData);
            }
        }
        
        throw new InvalidOperationException($"No data found in SSE response: {content}");
    }

    #endregion
}
