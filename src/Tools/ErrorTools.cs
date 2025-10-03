using System.ComponentModel;
using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FastFoodMcp.Tools;

/// <summary>
/// MCP tool to explain an internal error code.
/// </summary>
[McpServerToolType]
public class ErrorTools
{
    private readonly JsonStore<Dictionary<string, ErrorEntry>> _errorStore;
    private readonly ILogger<ErrorTools> _logger;

    public ErrorTools(JsonStore<Dictionary<string, ErrorEntry>> errorStore, ILogger<ErrorTools> logger)
    {
        _errorStore = errorStore ?? throw new ArgumentNullException(nameof(errorStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Explains an internal error code with causes, fix steps, and references.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Explain an internal error code and suggest steps")]
    public ExplainErrorResponse ExplainError(
        [Description("The error code to explain (e.g., 'ERR001')")] string code)
    {
        _logger.LogInformation("ExplainError called for code: {Code}", code);

        var errors = _errorStore.Data;
        var codeUpper = code.ToUpperInvariant();

        // Try exact match
        if (errors.TryGetValue(codeUpper, out var entry))
        {
            return new ExplainErrorResponse
            {
                Code = codeUpper,
                Title = entry.Title,
                Services = entry.Services,
                Severity = entry.Severity,
                LikelyCauses = entry.Causes,
                RecommendedSteps = entry.Fix,
                References = entry.Links
            };
        }

        // Try case-insensitive match
        var entryMatch = errors.FirstOrDefault(kvp => 
            string.Equals(kvp.Key, code, StringComparison.OrdinalIgnoreCase));

        if (entryMatch.Value != null)
        {
            return new ExplainErrorResponse
            {
                Code = entryMatch.Key,
                Title = entryMatch.Value.Title,
                Services = entryMatch.Value.Services,
                Severity = entryMatch.Value.Severity,
                LikelyCauses = entryMatch.Value.Causes,
                RecommendedSteps = entryMatch.Value.Fix,
                References = entryMatch.Value.Links
            };
        }

        // Not found - provide suggestions
        var suggestions = FuzzyMatcher.FindTopMatches(
            code,
            errors.Keys,
            k => k,
            topN: 3
        );

        var suggestionText = suggestions.Any()
            ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
            : "";

        return new ExplainErrorResponse
        {
            Code = code,
            Title = $"Error code '{code}' not found.{suggestionText}",
            Services = new List<string>(),
            Severity = "unknown",
            LikelyCauses = new List<string> { "The error code does not exist in the catalog." },
            RecommendedSteps = suggestions.Any() 
                ? new List<string> { $"Try one of these codes: {string.Join(", ", suggestions.Select(s => s.Item))}" }
                : new List<string> { "Check the error code spelling or search the catalog." },
            References = new List<ErrorLink>()
        };
    }

    /// <summary>
    /// Searches the error catalog by keyword or message pattern.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Search error catalog by keyword")]
    public List<ErrorSearchResult> SearchErrors(
        [Description("Keyword or text to search for in error codes, titles, and messages")] string query,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int limit = 10)
    {
        _logger.LogInformation("SearchErrors called with query: {Query}", query);

        var errors = _errorStore.Data;
        var queryLower = query.ToLowerInvariant();
        var limitCapped = Math.Min(limit, 50); // Cap at 50

        var results = errors
            .Where(kvp =>
            {
                var code = kvp.Key.ToLowerInvariant();
                var entry = kvp.Value;
                
                // Search in code, title, message patterns
                return code.Contains(queryLower) ||
                       entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       entry.MessagePatterns.Any(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                       FuzzyMatcher.FuzzyContains(entry.Title, query, 0.5);
            })
            .Take(limitCapped)
            .Select(kvp => new ErrorSearchResult
            {
                Code = kvp.Key,
                Title = kvp.Value.Title,
                Severity = kvp.Value.Severity
            })
            .OrderBy(r => r.Code)
            .ToList();

        _logger.LogInformation("SearchErrors found {Count} results", results.Count);
        return results;
    }

    /// <summary>
    /// Returns curated fix steps for an error code.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Get fix steps for an error code")]
    public List<string> SuggestFix(
        [Description("The error code to get fix steps for")] string code)
    {
        _logger.LogInformation("SuggestFix called for code: {Code}", code);

        var errors = _errorStore.Data;
        var codeUpper = code.ToUpperInvariant();

        // Try exact match
        if (errors.TryGetValue(codeUpper, out var entry))
        {
            return entry.Fix;
        }

        // Try case-insensitive
        var entryMatch = errors.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, code, StringComparison.OrdinalIgnoreCase));

        if (entryMatch.Value != null)
        {
            return entryMatch.Value.Fix;
        }

        // Not found
        var suggestions = FuzzyMatcher.FindTopMatches(
            code,
            errors.Keys,
            k => k,
            topN: 3
        );

        var suggestionText = suggestions.Any()
            ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
            : "";

        return new List<string> 
        { 
            $"Error code '{code}' not found.{suggestionText}"
        };
    }
}
