using FastFoodMcp.Extensions;
using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using FastFoodMcp.Tools;
using FastFoodMcpBase.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Json Stores as data sources for the MCP tools.
builder.Services.AddJsonStores();

// Configure MCP Server with HTTP transport
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
    {
        Name = "fastfood-mcp",
        Version = "0.1.0"
    };
})
.WithHttpTransport(options =>
{
    // Configure session timeout (2 hours default is fine for demo)
    options.IdleTimeout = TimeSpan.FromHours(2);
})
.WithTools<ErrorTools>()
.WithTools<ServiceTools>()
.WithTools<FlagTools>();

var app = builder.Build();

// Configure HTTP request pipeline
app.UseHttpsRedirection();

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
Console.WriteLine("Press Ctrl+C to stop the server");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
