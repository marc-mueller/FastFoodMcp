using FastFoodMcp.Extensions;
using FastFoodMcp.Tools;
using FastFoodMcpHttp.Options;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Json Stores as data sources for the MCP tools.
builder.Services.AddJsonStores();
builder.Services.Configure<ApiKeyOptions>(
    builder.Configuration.GetSection("FastFoodMcp:Auth:ApiKey"));

// Configure MCP Server with HTTP transport
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "fastfood-mcp",
        Version = "0.1.0"
    };
})
.WithHttpTransport()
.WithToolsFromAssembly(typeof(ErrorTools).Assembly);

var app = builder.Build();
var apiKeyOptionsAccessor = app.Services.GetRequiredService<IOptionsMonitor<ApiKeyOptions>>();

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/mcp"),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            var apiKeyOptions = apiKeyOptionsAccessor.CurrentValue;
            if (!apiKeyOptions.Enabled)
            {
                await next();
                return;
            }

            var apiKeyHeaderName = string.IsNullOrWhiteSpace(apiKeyOptions.HeaderName)
                ? "X-API-Key"
                : apiKeyOptions.HeaderName;

            if (string.IsNullOrWhiteSpace(apiKeyOptions.Key))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "API key auth enabled but no key configured."
                });
                return;
            }

            if (!context.Request.Headers.TryGetValue(apiKeyHeaderName, out var providedKey) ||
                !string.Equals(providedKey.ToString(), apiKeyOptions.Key, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Missing or invalid API key."
                });
                return;
            }

            await next();
        });
    });

// For production, replace this with AddAuthentication().AddOpenIdConnect(...) and RequireAuthorization().

// Map MCP endpoints (uses "/mcp" route)
app.MapMcp("/mcp");

// Add a health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    server = "fastfood-mcp",
    version = "0.1.0",
    timestamp = DateTime.UtcNow
}));

var serverUrl = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
Console.WriteLine($"Starting FastFood MCP Server at {serverUrl}");
Console.WriteLine($"MCP endpoint: {serverUrl}/mcp");
Console.WriteLine($"Health check: {serverUrl}/health");
if (apiKeyOptionsAccessor.CurrentValue.Enabled)
{
    var apiKeyHeaderName = string.IsNullOrWhiteSpace(apiKeyOptionsAccessor.CurrentValue.HeaderName)
        ? "X-API-Key"
        : apiKeyOptionsAccessor.CurrentValue.HeaderName;
    Console.WriteLine($"API key auth enabled (header: {apiKeyHeaderName})");
}
Console.WriteLine("Press Ctrl+C to stop the server");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
