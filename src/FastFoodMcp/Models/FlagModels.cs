using System.Text.Json.Serialization;

namespace FastFoodMcpBase.Models;

/// <summary>
/// Root container for feature flags data.
/// </summary>
public class FlagsData
{
    [JsonPropertyName("flags")]
    public List<FeatureFlag> Flags { get; set; } = new();
}

/// <summary>
/// Represents a feature flag.
/// </summary>
public class FeatureFlag
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("service")]
    public string? Service { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("variants")]
    public List<object>? Variants { get; set; }

    [JsonPropertyName("environments")]
    public Dictionary<string, object> Environments { get; set; } = new();

    [JsonPropertyName("owners")]
    public List<string> Owners { get; set; } = new();

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Request to list feature flags.
/// </summary>
public class ListFlagsRequest
{
    [JsonPropertyName("service")]
    public string? Service { get; set; }
}

/// <summary>
/// Represents a flag list item.
/// </summary>
public class FlagListItem
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("service")]
    public string? Service { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

/// <summary>
/// Request to get a specific flag.
/// </summary>
public class GetFlagRequest
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }
}

/// <summary>
/// Response with flag details.
/// </summary>
public class GetFlagResponse
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("service")]
    public string? Service { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("default")]
    public FlagValue? Default { get; set; }

    [JsonPropertyName("variants")]
    public List<object>? Variants { get; set; }

    [JsonPropertyName("owners")]
    public List<string> Owners { get; set; } = new();

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("environments")]
    public Dictionary<string, object> Environments { get; set; } = new();
}

/// <summary>
/// Request to get flag status in an environment.
/// </summary>
public class FlagStatusRequest
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("environment")]
    public required string Environment { get; set; }
}

/// <summary>
/// Response with flag status.
/// </summary>
public class FlagStatusResponse
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("environment")]
    public required string Environment { get; set; }

    [JsonPropertyName("value")]
    public FlagValue? Value { get; set; }
}

/// <summary>
/// Explicit representation of a flag value for MCP schema generation.
/// </summary>
public class FlagValue
{
    [JsonPropertyName("kind")]
    public required string Kind { get; set; }

    [JsonPropertyName("booleanValue")]
    public bool? BooleanValue { get; set; }

    [JsonPropertyName("stringValue")]
    public string? StringValue { get; set; }

    [JsonPropertyName("json")]
    public required string Json { get; set; }
}
