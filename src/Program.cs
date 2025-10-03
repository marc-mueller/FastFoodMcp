using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using FastFoodMcp.Tools;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Get data file paths
var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
var errorsPath = Path.Combine(dataPath, "errors.json");
var systemPath = Path.Combine(dataPath, "system.json");
var flagsPath = Path.Combine(dataPath, "flags.json");

// Register JSON stores as singletons with hot-reload capability
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<JsonStore<Dictionary<string, ErrorEntry>>>>();
    return new JsonStore<Dictionary<string, ErrorEntry>>(errorsPath, logger);
});

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<JsonStore<SystemData>>>();
    return new JsonStore<SystemData>(systemPath, logger);
});

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<JsonStore<FlagsData>>>();
    return new JsonStore<FlagsData>(flagsPath, logger);
});

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

// Map MCP endpoints with explicit route
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
