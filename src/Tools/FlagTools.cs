using System.ComponentModel;
using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FastFoodMcp.Tools;

/// <summary>
/// MCP tools for feature flag management.
/// </summary>
[McpServerToolType]
public class FlagTools
{
    private readonly JsonStore<FlagsData> _flagStore;
    private readonly ILogger<FlagTools> _logger;

    public FlagTools(JsonStore<FlagsData> flagStore, ILogger<FlagTools> logger)
    {
        _flagStore = flagStore ?? throw new ArgumentNullException(nameof(flagStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all feature flags, optionally filtered by service.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("List feature flags, optionally scoped to a service")]
    public List<FlagListItem> ListFlags(
        [Description("Optional service name to filter flags by")] string? service = null)
    {
        _logger.LogInformation("ListFlags called, service filter: {Service}", service);

        var flags = _flagStore.Data.Flags;

        if (!string.IsNullOrWhiteSpace(service))
        {
            flags = flags
                .Where(f => string.Equals(f.Service, service, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return flags
            .Select(f => new FlagListItem
            {
                Key = f.Key,
                Service = f.Service,
                Type = f.Type
            })
            .OrderBy(f => f.Key)
            .ToList();
    }

    /// <summary>
    /// Gets full details of a specific feature flag.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Get full flag definition")]
    public GetFlagResponse GetFlag(
        [Description("The unique key/identifier of the feature flag")] string key)
    {
        _logger.LogInformation("GetFlag called for: {Key}", key);

        var flags = _flagStore.Data.Flags;
        var flag = flags.FirstOrDefault(f =>
            string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

        if (flag == null)
        {
            // Provide suggestions
            var suggestions = FuzzyMatcher.FindTopMatches(
                key,
                flags,
                f => f.Key,
                topN: 3
            );

            var suggestionText = suggestions.Any()
                ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item.Key))}?"
                : "";

            throw new McpException($"Feature flag '{key}' not found.{suggestionText}"
            , McpErrorCode.InvalidRequest);
        }

        return new GetFlagResponse
        {
            Key = flag.Key,
            Service = flag.Service,
            Type = flag.Type,
            Default = flag.Default,
            Variants = flag.Variants,
            Owners = flag.Owners,
            Description = flag.Description,
            Environments = flag.Environments
        };
    }

    /// <summary>
    /// Resolves the value of a feature flag in a specific environment.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Resolve a flag's value in an environment")]
    public FlagStatusResponse FlagStatus(
        [Description("The unique key/identifier of the feature flag")] string key,
        [Description("The environment name (e.g., 'dev', 'staging', 'prod')")] string environment)
    {
        _logger.LogInformation("FlagStatus called for: {Key} in {Environment}", 
            key, environment);

        var flags = _flagStore.Data.Flags;
        var flag = flags.FirstOrDefault(f =>
            string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

        if (flag == null)
        {
            // Provide suggestions
            var suggestions = FuzzyMatcher.FindTopMatches(
                key,
                flags,
                f => f.Key,
                topN: 3
            );

            var suggestionText = suggestions.Any()
                ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item.Key))}?"
                : "";

            throw new McpException($"Feature flag '{key}' not found.{suggestionText}"
            , McpErrorCode.InvalidRequest);
        }

        // Normalize environment name
        var envKey = environment.ToLowerInvariant();
        
        // Try to get environment-specific value
        object? value = null;
        if (flag.Environments.TryGetValue(envKey, out var envValue))
        {
            value = envValue;
        }
        else
        {
            // Try case-insensitive match
            var envEntry = flag.Environments.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, environment, StringComparison.OrdinalIgnoreCase));

            if (envEntry.Key != null)
            {
                value = envEntry.Value;
            }
            else
            {
                // Environment not found, use default
                value = flag.Default;
                _logger.LogWarning("Environment '{Environment}' not found for flag '{Key}', using default value",
                    environment, key);
            }
        }

        return new FlagStatusResponse
        {
            Key = flag.Key,
            Environment = environment,
            Value = value
        };
    }
}
