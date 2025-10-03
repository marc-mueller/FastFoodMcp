using System.ComponentModel;
using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FastFoodMcp.Tools;

/// <summary>
/// MCP tools for service dependency awareness.
/// </summary>
[McpServerToolType]
public class ServiceTools
{
    private readonly JsonStore<SystemData> _systemStore;
    private readonly ILogger<ServiceTools> _logger;

    public ServiceTools(JsonStore<SystemData> systemStore, ILogger<ServiceTools> logger)
    {
        _systemStore = systemStore ?? throw new ArgumentNullException(nameof(systemStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets detailed information about a service.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Fetch a service's metadata")]
    public GetServiceResponse GetService(
        [Description("The name of the service to retrieve information for")] string name)
    {
        _logger.LogInformation("GetService called for: {Name}", name);

        var services = _systemStore.Data.Services;
        var serviceName = name.ToLowerInvariant();

        // Try exact match
        if (services.TryGetValue(serviceName, out var service))
        {
            return MapToResponse(serviceName, service);
        }

        // Try case-insensitive
        var entry = services.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase));

        if (entry.Value != null)
        {
            return MapToResponse(entry.Key, entry.Value);
        }

        // Not found - provide suggestions
        var suggestions = FuzzyMatcher.FindTopMatches(
            name,
            services.Keys,
            k => k,
            topN: 3
        );

        var suggestionText = suggestions.Any()
            ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
            : "";

        return new GetServiceResponse
        {
            Name = name,
            Description = $"Service '{name}' not found.{suggestionText}",
            Owners = new List<string>(),
            Repo = null,
            Language = null,
            DependsOn = new List<string>(),
            Api = new List<ApiEndpoint>()
        };
    }

    /// <summary>
    /// Lists service dependencies (inbound or outbound).
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("List a service's inbound/outbound dependencies")]
    public List<DependencyItem> ListDependencies(
        [Description("The name of the service")] string name,
        [Description("Direction: 'inbound' (services that depend on this) or 'outbound' (services this depends on)")] string direction = "outbound")
    {
        _logger.LogInformation("ListDependencies called for: {Name}, direction: {Direction}", 
            name, direction);

        var services = _systemStore.Data.Services;
        var serviceName = name.ToLowerInvariant();

        // Verify service exists
        if (!services.ContainsKey(serviceName))
        {
            var entry = services.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase));
            
            if (entry.Value == null)
            {
                _logger.LogWarning("Service not found: {Name}", name);
                return new List<DependencyItem>();
            }
            serviceName = entry.Key;
        }

        if (direction.Equals("outbound", StringComparison.OrdinalIgnoreCase))
        {
            // Return services this service depends on
            return services[serviceName].DependsOn
                .Select(depName => new DependencyItem { Name = depName })
                .OrderBy(d => d.Name)
                .ToList();
        }
        else if (direction.Equals("inbound", StringComparison.OrdinalIgnoreCase))
        {
            // Return services that depend on this service
            return services
                .Where(kvp => kvp.Value.DependsOn.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
                .Select(kvp => new DependencyItem { Name = kvp.Key })
                .OrderBy(d => d.Name)
                .ToList();
        }
        else
        {
            throw new McpException($"Invalid direction '{direction}'. Must be 'inbound' or 'outbound'."
            , McpErrorCode.InvalidParams);
        }
    }

    /// <summary>
    /// Finds API endpoints for a service.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("List API endpoints for a service")]
    public List<EndpointResult> FindEndpoint(
        [Description("The name of the service")] string name,
        [Description("Optional path filter to search for (partial match)")] string? path = null)
    {
        _logger.LogInformation("FindEndpoint called for: {Name}, path: {Path}", 
            name, path);

        var services = _systemStore.Data.Services;
        var serviceName = name.ToLowerInvariant();

        // Find service
        if (!services.TryGetValue(serviceName, out var service))
        {
            var entry = services.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase));

            if (entry.Value == null)
            {
                _logger.LogWarning("Service not found: {Name}", name);
                return new List<EndpointResult>();
            }
            service = entry.Value;
        }

        var endpoints = service.Api;

        // Filter by path if provided
        if (!string.IsNullOrWhiteSpace(path))
        {
            endpoints = endpoints
                .Where(e => e.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return endpoints
            .Select(e => new EndpointResult
            {
                Method = e.Method,
                Path = e.Path,
                Auth = e.Auth,
                Examples = new List<string>() // Could be populated from examples if available
            })
            .OrderBy(e => e.Path)
            .ThenBy(e => e.Method)
            .ToList();
    }

    /// <summary>
    /// Gets owner/team information for a service.
    /// </summary>
    [McpServerTool(UseStructuredContent = true), Description("Get owning team and contact info for a service")]
    public ServiceOwnerResponse ServiceOwner(
        [Description("The name of the service")] string name)
    {
        _logger.LogInformation("ServiceOwner called for: {Name}", name);

        var data = _systemStore.Data;
        var serviceName = name.ToLowerInvariant();

        // Find service
        if (!data.Services.TryGetValue(serviceName, out var service))
        {
            var entry = data.Services.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase));

            if (entry.Value == null)
            {
                _logger.LogWarning("Service not found: {Name}", name);
                return new ServiceOwnerResponse
                {
                    Team = $"Service '{name}' not found.",
                    Slack = null,
                    Pager = null,
                    Runbook = null
                };
            }
            service = entry.Value;
        }

        // Get first owner
        if (service.Owners.Count == 0)
        {
            _logger.LogWarning("Service {Name} has no owners", name);
            return new ServiceOwnerResponse
            {
                Team = $"Service '{name}' has no owners defined.",
                Slack = null,
                Pager = null,
                Runbook = null
            };
        }

        var ownerKey = service.Owners[0];
        if (data.Owners.TryGetValue(ownerKey, out var ownerInfo))
        {
            return new ServiceOwnerResponse
            {
                Team = ownerInfo.Team,
                Slack = ownerInfo.Slack,
                Pager = ownerInfo.Pager,
                Runbook = ownerInfo.Runbook
            };
        }
        else
        {
            // Owner info not found, return basic info
            return new ServiceOwnerResponse
            {
                Team = ownerKey,
                Slack = null,
                Pager = null,
                Runbook = null
            };
        }
    }

    private static GetServiceResponse MapToResponse(string serviceName, ServiceEntry service)
    {
        return new GetServiceResponse
        {
            Name = serviceName,
            Description = service.Description,
            Owners = service.Owners,
            Repo = service.Repo,
            Language = service.Language,
            DependsOn = service.DependsOn,
            Api = service.Api
        };
    }
}
