using System.Text.Json.Serialization;

namespace FastFoodMcp.Models;

/// <summary>
/// Root container for system data.
/// </summary>
public class SystemData
{
    [JsonPropertyName("services")]
    public Dictionary<string, ServiceEntry> Services { get; set; } = new();

    [JsonPropertyName("owners")]
    public Dictionary<string, OwnerInfo> Owners { get; set; } = new();
}

/// <summary>
/// Represents a service entry.
/// </summary>
public class ServiceEntry
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("owners")]
    public List<string> Owners { get; set; } = new();

    [JsonPropertyName("repo")]
    public string? Repo { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("dependsOn")]
    public List<string> DependsOn { get; set; } = new();

    [JsonPropertyName("api")]
    public List<ApiEndpoint> Api { get; set; } = new();
}

/// <summary>
/// Represents an API endpoint.
/// </summary>
public class ApiEndpoint
{
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("auth")]
    public string? Auth { get; set; }
}

/// <summary>
/// Represents team/owner information.
/// </summary>
public class OwnerInfo
{
    [JsonPropertyName("team")]
    public required string Team { get; set; }

    [JsonPropertyName("slack")]
    public string? Slack { get; set; }

    [JsonPropertyName("pager")]
    public string? Pager { get; set; }

    [JsonPropertyName("runbook")]
    public string? Runbook { get; set; }
}

/// <summary>
/// Request to get service information.
/// </summary>
public class GetServiceRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Response with service information.
/// </summary>
public class GetServiceResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("owners")]
    public List<string> Owners { get; set; } = new();

    [JsonPropertyName("repo")]
    public string? Repo { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("dependsOn")]
    public List<string> DependsOn { get; set; } = new();

    [JsonPropertyName("api")]
    public List<ApiEndpoint> Api { get; set; } = new();
}

/// <summary>
/// Request to list service dependencies.
/// </summary>
public class ListDependenciesRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "outbound";

    [JsonPropertyName("depth")]
    public int Depth { get; set; } = 1;
}

/// <summary>
/// Represents a dependency item.
/// </summary>
public class DependencyItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Request to find endpoints.
/// </summary>
public class FindEndpointRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

/// <summary>
/// Represents an endpoint result.
/// </summary>
public class EndpointResult
{
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("auth")]
    public string? Auth { get; set; }

    [JsonPropertyName("examples")]
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// Request to get service owner.
/// </summary>
public class ServiceOwnerRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Response with owner information.
/// </summary>
public class ServiceOwnerResponse
{
    [JsonPropertyName("team")]
    public required string Team { get; set; }

    [JsonPropertyName("slack")]
    public string? Slack { get; set; }

    [JsonPropertyName("pager")]
    public string? Pager { get; set; }

    [JsonPropertyName("runbook")]
    public string? Runbook { get; set; }
}
