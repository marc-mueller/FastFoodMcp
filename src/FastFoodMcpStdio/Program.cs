using FastFoodMcp.Extensions;
using FastFoodMcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddJsonStores()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ErrorTools>()
    .WithTools<ServiceTools>()
    .WithTools<FlagTools>();
await builder.Build().RunAsync();