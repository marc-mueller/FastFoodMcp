# FastFood MCP

A compact, company-aware MCP server for developers.

**Focus:** Error troubleshooting, service dependency awareness, and feature flags — powered by three local JSON files for tutorials and demos (easily swappable for real backends later).

## Overview

FastFood MCP is a demonstration Model Context Protocol (MCP) server written in C# (.NET 9) using the official MCP SDK. It showcases **both HTTP and stdio transport implementations** to demonstrate different deployment options. The server provides AI assistants with structured access to:

- **Error Troubleshooting**: Explain error codes, search errors, and get fix suggestions
- **Service Dependencies**: Query service metadata, dependencies, endpoints, and owners
- **Feature Flags**: List, query, and check flag status across environments

This server serves as a **tutorial and training implementation** showing how to build custom MCP servers with different transport protocols for your projects.

## Features

✅ **10 MCP Tools** across three domains  
✅ **Dual Transport Support** - HTTP (SSE) and stdio implementations  
✅ **Shared Core Library** - Reusable tools and infrastructure  
✅ **Hot-Reload** - JSON data files reload automatically on changes  
✅ **Fuzzy Matching** - Helpful suggestions when lookups fail  
✅ **.NET 9** - Modern, high-performance framework  
✅ **Deterministic** - Predictable, sorted outputs  
✅ **Production-Ready Patterns** - Logging, error handling, DI  
✅ **Docker Support** - Containerized stdio server option

## Architecture

The solution is organized into three main projects:

```
FastFoodMcp/
├── src/
│   ├── FastFoodMcp/                    # Shared core library
│   │   ├── FastFoodMcp.csproj
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── Infra/
│   │   │   ├── JsonStore.cs            # Hot-reload JSON file store
│   │   │   └── FuzzyMatcher.cs         # Levenshtein distance matching
│   │   ├── Models/
│   │   │   ├── ErrorModels.cs          # Error DTOs
│   │   │   ├── ServiceModels.cs        # Service DTOs
│   │   │   └── FlagModels.cs           # Flag DTOs
│   │   └── Tools/
│   │       ├── ErrorTools.cs           # 3 error tools
│   │       ├── ServiceTools.cs         # 4 service tools
│   │       └── FlagTools.cs            # 3 flag tools
│   ├── FastFoodMcpHttp/                # HTTP transport server
│   │   ├── FastFoodMcpHttp.csproj
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── FastFoodMcpStdio/               # Stdio transport server
│       ├── FastFoodMcpStdio.csproj
│       └── Program.cs
├── data/
│   ├── errors.json                     # Error catalog
│   ├── system.json                     # Services & owners
│   └── flags.json                      # Feature flags
└── tests/
    ├── FastFoodMcp.UnitTests/
    └── FastFoodMcp.IntegrationTests/
```

### Project Descriptions

- **FastFoodMcp**: Shared library containing all MCP tools, models, and infrastructure. Used by both transport implementations.
- **FastFoodMcpHttp**: ASP.NET Core server providing HTTP transport with Server-Sent Events (SSE).
- **FastFoodMcpStdio**: Console application providing stdio transport for direct process communication.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Any OS (Windows, macOS, Linux)

## Quick Start

### Option 1: HTTP Transport Server

The HTTP transport server runs as a web service and communicates via HTTP with Server-Sent Events (SSE).

#### 1. Navigate to the HTTP project

```bash
cd src/FastFoodMcpHttp
```

#### 2. Run the server

```bash
dotnet run
```

The server will start on `http://localhost:5000` by default.

You should see:

```
Starting FastFood MCP Server (HTTP) at http://localhost:5000
MCP endpoint: http://localhost:5000/mcp
Health check: http://localhost:5000/health
Press Ctrl+C to stop the server
```

#### 3. Test the health endpoint

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{"status":"healthy","server":"fastfood-mcp","version":"0.1.0","timestamp":"2025-10-03T08:09:09.027218Z"}
```

### Option 2: Stdio Transport Server

The stdio transport server runs as a console application and communicates via standard input/output streams.

#### 1. Navigate to the stdio project

```bash
cd src/FastFoodMcpStdio
```

#### 2. Run the server

```bash
dotnet run
```

The server will start and wait for JSON-RPC messages on stdin.

### Option 3: Docker Container (Stdio)

You can run the stdio server in a Docker container.

#### 1. Build the container image

```bash
dotnet publish src/FastFoodMcpStdio/FastFoodMcpStdio.csproj /t:PublishContainer
```

This creates a Docker image named `fastfoodmcp` using the settings in the `.csproj` file.

#### 2. Run the container

```bash
docker run -i --rm fastfoodmcp
```

The container runs the stdio server and communicates via stdin/stdout.

## MCP Tools

### Error Troubleshooting (3 tools)

#### 1. `explain_error`
Resolves an error code to human explanation + steps.

**Request:**
```json
{
  "code": "E2145",
  "service": "gateway"
}
```

**Response:**
```json
{
  "code": "E2145",
  "title": "JWT token expired",
  "severity": "medium",
  "services": ["gateway", "usersvc"],
  "likelyCauses": ["Client clock skew", "Short token TTL"],
  "recommendedSteps": [
    "Check client NTP sync",
    "Increase auth leeway to 90s",
    "Rotate signing key if older than 90 days"
  ],
  "references": [
    {
      "label": "Auth Runbook",
      "url": "https://docs/runbooks/auth#jwt-expired"
    }
  ]
}
```

#### 2. `search_errors`
Fuzzy search by message snippet or keyword.

**Request:**
```json
{
  "query": "timeout",
  "limit": 10
}
```

**Response:**
```json
[
  {
    "code": "P5001",
    "title": "Payments: gateway timeout",
    "severity": "high"
  }
]
```

#### 3. `suggest_fix`
Returns curated fix steps for an error code.

**Request:**
```json
{
  "code": "P5001"
}
```

**Response:**
```json
[
  { "step": "Check upstream health dashboard" },
  { "step": "Temporarily raise timeout to 5s" },
  { "step": "Verify circuit breaker thresholds" }
]
```

### Service Dependency Awareness (4 tools)

#### 4. `get_service`
Fetch service metadata.

**Request:**
```json
{
  "name": "usersvc"
}
```

**Response:**
```json
{
  "name": "usersvc",
  "description": "User profiles and auth glue",
  "owners": ["@identity-team"],
  "repo": "github.com/acme/usersvc",
  "language": "csharp",
  "dependsOn": ["db.users", "emailsvc"],
  "api": [
    { "method": "GET", "path": "/users/{id}", "auth": "JWT" },
    { "method": "POST", "path": "/users", "auth": "JWT" }
  ]
}
```

#### 5. `list_dependencies`
List inbound/outbound dependencies.

**Request:**
```json
{
  "name": "checkout",
  "direction": "outbound",
  "depth": 1
}
```

**Response:**
```json
[
  { "name": "paymentsvc" },
  { "name": "usersvc" }
]
```

#### 6. `find_endpoint`
List API endpoints for a service.

**Request:**
```json
{
  "name": "usersvc",
  "path": "/users"
}
```

**Response:**
```json
[
  { "method": "GET", "path": "/users/{id}", "auth": "JWT", "examples": [] },
  { "method": "POST", "path": "/users", "auth": "JWT", "examples": [] }
]
```

#### 7. `service_owner`
Get owning team and contact info.

**Request:**
```json
{
  "name": "paymentsvc"
}
```

**Response:**
```json
{
  "team": "@payments-core",
  "slack": "#payments",
  "runbook": "https://docs/runbooks/payments"
}
```

### Feature Flags (3 tools)

#### 8. `list_flags`
List feature flags (optionally scoped to a service).

**Request:**
```json
{
  "service": "webapp"
}
```

**Response:**
```json
[
  {
    "key": "checkout.newAddressForm",
    "service": "webapp",
    "type": "boolean"
  }
]
```

#### 9. `get_flag`
Get full flag definition.

**Request:**
```json
{
  "key": "checkout.newAddressForm"
}
```

**Response:**
```json
{
  "key": "checkout.newAddressForm",
  "service": "webapp",
  "type": "boolean",
  "default": false,
  "owners": ["@web-checkout"],
  "description": "New React form for address entry",
  "environments": {
    "dev": true,
    "staging": true,
    "prod": false
  }
}
```

#### 10. `flag_status`
Resolve flag value in an environment.

**Request:**
```json
{
  "key": "checkout.newAddressForm",
  "environment": "prod"
}
```

**Response:**
```json
{
  "key": "checkout.newAddressForm",
  "environment": "prod",
  "value": false
}
```

## Using with MCP Clients

MCP clients can connect to FastFood MCP using different transport methods. Here are configuration examples for popular clients.

### VS Code with GitHub Copilot

Add to your `.vscode/mcp.json` in your workspace root:

#### HTTP Transport

```json
{
  "mcpServers": {
    "fastfoodhttp": {
      "url": "http://localhost:5000/mcp",
      "type": "http"
    }
  }
}
```

#### Stdio Transport (Direct Command)

```json
{
  "mcpServers": {
    "fastfoodstdio": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/marc/Repos/Demo/FastFoodMcp/src/FastFoodMcpStdio/FastFoodMcpStdio.csproj"
      ]
    }
  }
}
```

#### Stdio Transport (Docker)

```json
{
  "mcpServers": {
    "fastfoodstdiodocker": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "fastfoodmcp"
      ]
    }
  }
}
```

**Note:** Update the project path in the stdio configuration to match your local installation path.

### Choosing a Transport

- **HTTP**: Best for remote servers, web deployments, or when you need RESTful access
- **Stdio**: Best for local tools, CLI integration, or when running alongside the client process
- **Docker**: Best for isolated environments, reproducible setups, or when distribution is important

## Data Files

The server loads data from JSON files in the `data/` directory:

### errors.json
Contains error codes with explanations, severity, affected services, causes, fix steps, and references.

### system.json
Contains service definitions with dependencies, APIs, owners, and contact information.

### flags.json
Contains feature flags with types, variants, environment-specific values, and metadata.

All files support **hot-reload** — edit them while the server is running, and changes are automatically picked up.

## Configuration

### HTTP Server Configuration

Edit `src/FastFoodMcpHttp/appsettings.json`:

#### Changing the Port

```json
{
  "Urls": "http://localhost:8080"
}
```

Or use environment variable:

```bash
ASPNETCORE_URLS=http://localhost:8080 dotnet run
```

#### Logging

Adjust logging levels:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ModelContextProtocol": "Debug"
    }
  }
}
```

#### Session Timeout

Configure idle timeout in `Program.cs`:

```csharp
.WithHttpTransport(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
})
```

### Stdio Server Configuration

The stdio server uses environment variables for configuration:

```bash
# Set log level
export Logging__LogLevel__Default=Debug

# Run the server
dotnet run --project src/FastFoodMcpStdio/FastFoodMcpStdio.csproj
```

### Docker Container Settings

Container configuration is defined in `src/FastFoodMcpStdio/FastFoodMcpStdio.csproj`:

```xml
<PropertyGroup>
  <ContainerImageName>fastfoodmcp</ContainerImageName>
  <ContainerImageTag>latest</ContainerImageTag>
</PropertyGroup>
```

## Development

### Project Structure

The solution uses a shared library pattern:

- **FastFoodMcp**: Contains all tools, models, and infrastructure
- **FastFoodMcpHttp**: Thin wrapper providing HTTP transport
- **FastFoodMcpStdio**: Thin wrapper providing stdio transport

Both transport projects reference the shared library and configure the appropriate MCP transport.

### Adding New Tools

1. Create request/response models in `src/FastFoodMcp/Models/`
2. Add tool method to appropriate class in `src/FastFoodMcp/Tools/`
3. Decorate with `[McpServerTool(UseStructuredContent = true)]` and `[Description]`
4. The tool is automatically discovered and registered in both transports

Example:

```csharp
[McpServerTool(UseStructuredContent = true), Description("Your tool description")]
public YourResponse YourTool(
    [Description("Parameter description")] string parameter)
{
    // Implementation
    return new YourResponse { /* ... */ };
}
```

### Error Handling Best Practices

**Important**: Use `McpException` only for protocol-level errors, not application-level errors.

❌ **Don't do this** (application-level error):
```csharp
if (!found)
{
    throw new McpException("Item not found", McpErrorCode.InvalidRequest);
}
```

✅ **Do this instead** (return error information in response):
```csharp
if (!found)
{
    return new YourResponse
    {
        Name = requestedName,
        Description = $"Item '{requestedName}' not found. Did you mean: {suggestions}?",
        // ... other fields with default/empty values
    };
}
```

This allows AI assistants to process and understand errors naturally.

### Extending with Real Backends

The `JsonStore<T>` can easily be replaced with real data sources:

- **Errors**: Connect to Datadog, Loki, or incident management systems
- **Services**: Query Backstage, Terraform state, or OpenAPI specs
- **Flags**: Integrate with LaunchDarkly, Unleash, or similar platforms

Simply replace the `JsonStore<T>` registration in `ServiceCollectionExtensions.cs` with your own data provider implementation.

## Key Design Patterns

### Hot-Reload Data Store

The `JsonStore<T>` class uses `FileSystemWatcher` for automatic reloading:

```csharp
public class JsonStore<T> where T : class
{
    // Thread-safe data access
    public T Data { get; }
    
    // Automatic file watching and reload
    private void OnFileChanged(object sender, FileSystemEventArgs e) { }
}
```

### Fuzzy Matching

When lookups fail, the server suggests alternatives using Levenshtein distance:

```csharp
var suggestions = FuzzyMatcher.FindTopMatches(
    userInput,
    availableItems,
    item => item.Key,
    topN: 3
);
```

## Testing

The solution includes comprehensive test coverage:

### Unit Tests

Located in `tests/FastFoodMcp.UnitTests/`:
- Infrastructure tests (JsonStore, FuzzyMatcher)
- Tool logic tests with mocked dependencies

Run unit tests:
```bash
dotnet test tests/FastFoodMcp.UnitTests
```

### Integration Tests

Located in `tests/FastFoodMcp.IntegrationTests/`:
- End-to-end HTTP transport tests
- MCP protocol compliance tests
- All 10 tools tested via HTTP API

Run integration tests:
```bash
dotnet test tests/FastFoodMcp.IntegrationTests
```

### Run All Tests

```bash
dotnet test
```

## Production Considerations

For production use, consider:

1. **Authentication**: Add JWT bearer tokens or API keys (HTTP) or secure stdio channel
2. **Rate Limiting**: Prevent abuse with rate limits (HTTP server)
3. **Caching**: Cache frequently accessed data
4. **Monitoring**: Add OpenTelemetry/Application Insights
5. **HTTPS**: Always use TLS in production (HTTP server)
6. **Database**: Replace JSON files with proper database
7. **Scaling**: Deploy HTTP server behind load balancer for high availability
8. **Container Security**: Use minimal base images and security scanning for Docker deployments

### Transport Selection for Production

- **HTTP**: Recommended for cloud deployments, microservices, and multi-tenant scenarios
- **Stdio**: Recommended for single-user CLI tools, local agents, and embedded scenarios
- **Docker**: Recommended for consistent deployments across different environments

## License

MIT License - See [LICENSE](LICENSE) file

## Resources

- [MCP Specification](https://modelcontextprotocol.io)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core)
- [.NET 9 Documentation](https://learn.microsoft.com/dotnet)

## Contributing

Contributions are welcome! This is a demo/tutorial project, so feel free to:

- Add more example tools
- Improve documentation
- Add unit tests
- Extend data models
- Add support for additional MCP transports
- Share your use cases

## Support

For questions or issues:

- Open an issue on GitHub
- Check the MCP C# SDK documentation
- Review the MCP specification

---

**Happy Building! 🚀**

This MCP server demonstrates best practices for building AI-powered developer tools with both HTTP and stdio transports. Use it as a foundation for your own custom MCP servers!
