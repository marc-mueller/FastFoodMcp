using System.ComponentModel;
using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FastFoodMcp.Tools;

/// <summary>
/// MCP tool to explain an internal error code.
/// </summary>
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
    [McpServerTool, Description("Explain an internal error code and suggest steps")]
    public ExplainErrorResponse ExplainError(ExplainErrorRequest request)
    {
        _logger.LogInformation("ExplainError called for code: {Code}", request.Code);

        var errors = _errorStore.Data;
        var codeUpper = request.Code.ToUpperInvariant();

        // Try exact match first
        if (errors.TryGetValue(codeUpper, out var errorEntry))
        {
            return new ExplainErrorResponse
            {
                Code = codeUpper,
                Title = errorEntry.Title,
                Severity = errorEntry.Severity,
                Services = errorEntry.Services,
                LikelyCauses = errorEntry.Causes,
                RecommendedSteps = errorEntry.Fix,
                References = errorEntry.Links
            };
        }

        // Try case-insensitive match
        var entry = errors.FirstOrDefault(kvp => 
            string.Equals(kvp.Key, request.Code, StringComparison.OrdinalIgnoreCase));
        
        if (entry.Value != null)
        {
            return new ExplainErrorResponse
            {
                Code = entry.Key,
                Title = entry.Value.Title,
                Severity = entry.Value.Severity,
                Services = entry.Value.Services,
                LikelyCauses = entry.Value.Causes,
                RecommendedSteps = entry.Value.Fix,
                References = entry.Value.Links
            };
        }

        // Not found - provide suggestions
        var suggestions = FuzzyMatcher.FindTopMatches(
            request.Code,
            errors.Keys,
            k => k,
            topN: 3
        );

        var suggestionText = suggestions.Any()
            ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
            : "";

        throw new McpException($"Error code '{request.Code}' not found.{suggestionText}"
        , McpErrorCode.InvalidRequest);
    }

    /// <summary>
    /// Searches the error catalog by keyword or message pattern.
    /// </summary>
    [McpServerTool, Description("Search error catalog by keyword")]
    public List<ErrorSearchResult> SearchErrors(SearchErrorsRequest request)
    {
        _logger.LogInformation("SearchErrors called with query: {Query}", request.Query);

        var errors = _errorStore.Data;
        var query = request.Query.ToLowerInvariant();
        var limit = Math.Min(request.Limit, 50); // Cap at 50

        var results = errors
            .Where(kvp =>
            {
                var code = kvp.Key.ToLowerInvariant();
                var entry = kvp.Value;
                
                // Search in code, title, message patterns
                return code.Contains(query) ||
                       entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       entry.MessagePatterns.Any(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                       FuzzyMatcher.FuzzyContains(entry.Title, query, 0.5);
            })
            .Take(limit)
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
    [McpServerTool, Description("Get fix steps for an error code")]
    public List<FixStep> SuggestFix(SuggestFixRequest request)
    {
        _logger.LogInformation("SuggestFix called for code: {Code}", request.Code);

        var errors = _errorStore.Data;
        var codeUpper = request.Code.ToUpperInvariant();

        // Try exact match
        if (errors.TryGetValue(codeUpper, out var errorEntry))
        {
            return errorEntry.Fix.Select(step => new FixStep { Step = step }).ToList();
        }

        // Try case-insensitive
        var entry = errors.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, request.Code, StringComparison.OrdinalIgnoreCase));

        if (entry.Value != null)
        {
            return entry.Value.Fix.Select(step => new FixStep { Step = step }).ToList();
        }

        // Not found
        var suggestions = FuzzyMatcher.FindTopMatches(
            request.Code,
            errors.Keys,
            k => k,
            topN: 3
        );

        var suggestionText = suggestions.Any()
            ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
            : "";

        throw new McpException($"Error code '{request.Code}' not found.{suggestionText}"
        , McpErrorCode.InvalidRequest);
    }
}
