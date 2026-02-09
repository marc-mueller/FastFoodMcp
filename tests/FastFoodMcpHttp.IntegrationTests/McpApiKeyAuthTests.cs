using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace FastFoodMcpHttp.IntegrationTests;

public class McpApiKeyAuthTests : IClassFixture<FastFoodMcpAuthFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _unauthenticatedClient;

    public McpApiKeyAuthTests(FastFoodMcpAuthFactory factory)
    {
        _unauthenticatedClient = factory.CreateClient();
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", FastFoodMcpAuthFactory.ApiKey);
    }

    [Fact]
    public async Task McpEndpoint_PostWithoutApiKey_ReturnsUnauthorized()
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

        var response = await PostMcpRequest(_unauthenticatedClient, initRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task McpEndpoint_PostWithApiKey_AllowsInitialize()
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

        var response = await PostMcpRequest(_client, initRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ParseSseResponse(response);
        result.TryGetProperty("result", out var resultProp).Should().BeTrue();

        var serverInfo = resultProp.GetProperty("serverInfo");
        serverInfo.GetProperty("name").GetString().Should().Be("fastfood-mcp");
        serverInfo.GetProperty("version").GetString().Should().Be("0.1.0");
    }

    private static async Task<HttpResponseMessage> PostMcpRequest(HttpClient client, object request)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = content
        };
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        return await client.SendAsync(requestMessage, cancellationToken);
    }

    private static async Task<JsonElement> ParseSseResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
            {
                var jsonData = line.Substring(6);
                return JsonSerializer.Deserialize<JsonElement>(jsonData);
            }
        }

        throw new InvalidOperationException($"No data found in SSE response: {content}");
    }
}
