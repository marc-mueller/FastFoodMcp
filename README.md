# FastFood MCP

A compact, company-aware MCP server for developers.

**Focus:** Error troubleshooting, service dependency awareness, and feature flags — powered by three local JSON files for tutorials and demos (easily swappable for real backends later).

## Overview

FastFood MCP is a demonstration HTTP-based Model Context Protocol (MCP) server written in C# (ASP.NET Core 9) using the official MCP SDK. It provides AI assistants with structured access to:

- **Error Troubleshooting**: Explain error codes, search errors, and get fix suggestions
- **Service Dependencies**: Query service metadata, dependencies, endpoints, and owners
- **Feature Flags**: List, query, and check flag status across environments

This server serves as a **tutorial and training implementation** showing how to build custom MCP servers for your projects.

## Features

✅ **10 MCP Tools** across three domains  
✅ **HTTP Transport** using Streamable HTTP (SSE compatible)  
✅ **Hot-Reload** - JSON data files reload automatically on changes  
✅ **Fuzzy Matching** - Helpful suggestions when lookups fail  
✅ **ASP.NET Core 9** - Modern, high-performance web framework  
✅ **Deterministic** - Predictable, sorted outputs  
✅ **Production-Ready Patterns** - Logging, error handling, DI

## Architecture

```
FastFoodMcp/
├── src/
│   ├── Program.cs              # ASP.NET Core app entry point
│   ├── FastFoodMcp.csproj      # Project file
│   ├── appsettings.json        # Configuration
│   ├── Infra/
│   │   ├── JsonStore.cs        # Hot-reload JSON file store
│   │   └── FuzzyMatcher.cs     # Levenshtein distance matching
│   ├── Models/
│   │   ├── ErrorModels.cs      # Error DTOs
│   │   ├── ServiceModels.cs    # Service DTOs
│   │   └── FlagModels.cs       # Flag DTOs
│   └── Tools/
│       ├── ErrorTools.cs       # 3 error tools
│       ├── ServiceTools.cs     # 4 service tools
│       └── FlagTools.cs        # 3 flag tools
└── data/
    ├── errors.json             # Error catalog
    ├── system.json             # Services & owners
    └── flags.json              # Feature flags
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Any OS (Windows, macOS, Linux)

## Quick Start

### 1. Clone and Navigate

```bash
git clone https://github.com/marc-mueller/FastFoodMcp.git
cd FastFoodMcp/src
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Run the Server

```bash
dotnet run
```

The server will start on `http://localhost:5000` by default.

You should see:

```
Starting FastFood MCP Server at http://localhost:5000
MCP endpoint: http://localhost:5000/mcp
Health check: http://localhost:5000/health
Press Ctrl+C to stop the server
```

### 4. Test the Server

Use curl to test the health endpoint:

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{"status":"healthy","server":"fastfood-mcp","version":"0.1.0","timestamp":"2025-10-03T08:09:09.027218Z"}
```

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

### Claude Desktop (Example Configuration)

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "fastfood": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

### VS Code with GitHub Copilot

The MCP server can be used with VS Code and GitHub Copilot Agent mode. The server exposes tools that help AI assistants:

- Resolve internal errors using `explain_error`
- Check service dependencies before generating code
- Verify feature flag status to avoid suggesting inactive features

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

### Changing the Port

Edit `appsettings.json`:

```json
{
  "Urls": "http://localhost:8080"
}
```

Or use environment variable:

```bash
ASPNETCORE_URLS=http://localhost:8080 dotnet run
```

### Logging

Adjust logging levels in `appsettings.json`:

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

### Session Timeout

Configure idle timeout in `Program.cs`:

```csharp
.WithHttpTransport(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
})
```

## Development

### Adding New Tools

1. Create request/response models in `Models/`
2. Add tool method to appropriate class in `Tools/`
3. Decorate with `[McpServerTool]` and `[Description]`
4. The tool is automatically discovered and registered

Example:

```csharp
[McpServerTool, Description("Your tool description")]
public YourResponse YourTool(YourRequest request)
{
    // Implementation
}
```

### Extending with Real Backends

The `JsonStore<T>` can easily be replaced with real data sources:

- **Errors**: Connect to Datadog, Loki, or incident management systems
- **Services**: Query Backstage, Terraform state, or OpenAPI specs
- **Flags**: Integrate with LaunchDarkly, Unleash, or similar platforms

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

### Error Handling

All tools throw `McpError` for consistent error responses:

```csharp
throw new McpError(
    McpErrorCode.InvalidRequest,
    $"Service '{request.Name}' not found. Did you mean: checkout, usersvc?"
);
```

## Testing

### Manual Testing with curl

Test the MCP initialization:

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {
        "name": "test-client",
        "version": "1.0"
      }
    }
  }'
```

List available tools:

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

Call a tool:

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "ExplainError",
      "arguments": {
        "code": "E2145"
      }
    }
  }'
```

## Production Considerations

For production use, consider:

1. **Authentication**: Add JWT bearer tokens or API keys
2. **Rate Limiting**: Prevent abuse with rate limits
3. **Caching**: Cache frequently accessed data
4. **Monitoring**: Add OpenTelemetry/Application Insights
5. **HTTPS**: Always use TLS in production
6. **Database**: Replace JSON files with proper database
7. **Scaling**: Deploy behind load balancer for high availability

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
- Share your use cases

## Support

For questions or issues:

- Open an issue on GitHub
- Check the MCP C# SDK documentation
- Review the MCP specification

---

**Happy Building! 🚀**

This MCP server demonstrates best practices for building AI-powered developer tools. Use it as a foundation for your own custom MCP servers!
