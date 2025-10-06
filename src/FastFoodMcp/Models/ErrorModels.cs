using System.Text.Json.Serialization;

namespace FastFoodMcp.Models;

/// <summary>
/// Represents an error entry in the error catalog.
/// </summary>
public class ErrorEntry
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("services")]
    public List<string> Services { get; set; } = new();

    [JsonPropertyName("severity")]
    public required string Severity { get; set; }

    [JsonPropertyName("messagePatterns")]
    public List<string> MessagePatterns { get; set; } = new();

    [JsonPropertyName("causes")]
    public List<string> Causes { get; set; } = new();

    [JsonPropertyName("fix")]
    public List<string> Fix { get; set; } = new();

    [JsonPropertyName("links")]
    public List<ErrorLink> Links { get; set; } = new();
}

/// <summary>
/// Represents a reference link for an error.
/// </summary>
public class ErrorLink
{
    [JsonPropertyName("label")]
    public required string Label { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

/// <summary>
/// Request to explain an error code.
/// </summary>
public class ExplainErrorRequest
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("service")]
    public string? Service { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }
}

/// <summary>
/// Response with error explanation.
/// </summary>
public class ExplainErrorResponse
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("severity")]
    public required string Severity { get; set; }

    [JsonPropertyName("services")]
    public List<string> Services { get; set; } = new();

    [JsonPropertyName("likelyCauses")]
    public List<string> LikelyCauses { get; set; } = new();

    [JsonPropertyName("recommendedSteps")]
    public List<string> RecommendedSteps { get; set; } = new();

    [JsonPropertyName("references")]
    public List<ErrorLink> References { get; set; } = new();
}

/// <summary>
/// Request to search for errors.
/// </summary>
public class SearchErrorsRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Represents a search result item.
/// </summary>
public class ErrorSearchResult
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("severity")]
    public required string Severity { get; set; }
}

/// <summary>
/// Request to get fix suggestions for an error.
/// </summary>
public class SuggestFixRequest
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }
}

/// <summary>
/// Represents a single fix step.
/// </summary>
public class FixStep
{
    [JsonPropertyName("step")]
    public required string Step { get; set; }
}
