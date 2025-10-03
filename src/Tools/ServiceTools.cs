using System.ComponentModel;
using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FastFoodMcp.Tools;

/// <summary>
/// MCP tools for service dependency awareness.
/// </summary>
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
    [McpServerTool, Description("Fetch a service's metadata")]
    public GetServiceResponse GetService(GetServiceRequest request)
    {
        _logger.LogInformation("GetService called for: {Name}", request.Name);

        var services = _systemStore.Data.Services;
        var serviceName = request.Name.ToLowerInvariant();

        // Try exact match
        if (services.TryGetValue(serviceName, out var service))
        {
            return MapToResponse(serviceName, service);
        }

        // Try case-insensitive
        var entry = services.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, request.Name, StringComparison.OrdinalIgnoreCase));

        if (entry.Value != null)
        {
            return MapToResponse(entry.Key, entry.Value);
        }

        // Not found - provide suggestions
        var suggestions = FuzzyMatcher.FindTopMatches(
            request.Name,
            services.Keys,
            k => k,
            topN: 3
        );

        var suggestionText = suggestions.Any()
            ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
            : "";

        throw new McpException($"Service '{request.Name}' not found.{suggestionText}"
        , McpErrorCode.InvalidRequest);
    }

    /// <summary>
    /// Lists service dependencies (inbound or outbound).
    /// </summary>
    [McpServerTool, Description("List a service's inbound/outbound dependencies")]
    public List<DependencyItem> ListDependencies(ListDependenciesRequest request)
    {
        _logger.LogInformation("ListDependencies called for: {Name}, direction: {Direction}", 
            request.Name, request.Direction);

        var services = _systemStore.Data.Services;
        var serviceName = request.Name.ToLowerInvariant();

        // Verify service exists
        if (!services.ContainsKey(serviceName))
        {
            var entry = services.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, request.Name, StringComparison.OrdinalIgnoreCase));
            
            if (entry.Value == null)
            {
                var suggestions = FuzzyMatcher.FindTopMatches(
                    request.Name,
                    services.Keys,
                    k => k,
                    topN: 3
                );

                var suggestionText = suggestions.Any()
                    ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
                    : "";

                throw new McpException($"Service '{request.Name}' not found.{suggestionText}"
                , McpErrorCode.InvalidRequest);
            }
            serviceName = entry.Key;
        }

        if (request.Direction.Equals("outbound", StringComparison.OrdinalIgnoreCase))
        {
            // Return services this service depends on
            var deps = services[serviceName].DependsOn
                .Select(name => new DependencyItem { Name = name })
                .OrderBy(d => d.Name)
                .ToList();
            return deps;
        }
        else if (request.Direction.Equals("inbound", StringComparison.OrdinalIgnoreCase))
        {
            // Return services that depend on this service
            var deps = services
                .Where(kvp => kvp.Value.DependsOn.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
                .Select(kvp => new DependencyItem { Name = kvp.Key })
                .OrderBy(d => d.Name)
                .ToList();
            return deps;
        }
        else
        {
            throw new McpException($"Invalid direction '{request.Direction}'. Must be 'inbound' or 'outbound'."
            , McpErrorCode.InvalidParams);
        }
    }

    /// <summary>
    /// Finds API endpoints for a service.
    /// </summary>
    [McpServerTool, Description("List API endpoints for a service")]
    public List<EndpointResult> FindEndpoint(FindEndpointRequest request)
    {
        _logger.LogInformation("FindEndpoint called for: {Name}, path: {Path}", 
            request.Name, request.Path);

        var services = _systemStore.Data.Services;
        var serviceName = request.Name.ToLowerInvariant();

        // Find service
        if (!services.TryGetValue(serviceName, out var service))
        {
            var entry = services.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, request.Name, StringComparison.OrdinalIgnoreCase));

            if (entry.Value == null)
            {
                var suggestions = FuzzyMatcher.FindTopMatches(
                    request.Name,
                    services.Keys,
                    k => k,
                    topN: 3
                );

                var suggestionText = suggestions.Any()
                    ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
                    : "";

                throw new McpException($"Service '{request.Name}' not found.{suggestionText}"
                , McpErrorCode.InvalidRequest);
            }
            service = entry.Value;
        }

        var endpoints = service.Api;

        // Filter by path if provided
        if (!string.IsNullOrWhiteSpace(request.Path))
        {
            endpoints = endpoints
                .Where(e => e.Path.Contains(request.Path, StringComparison.OrdinalIgnoreCase))
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
    [McpServerTool, Description("Get owning team and contact info for a service")]
    public ServiceOwnerResponse ServiceOwner(ServiceOwnerRequest request)
    {
        _logger.LogInformation("ServiceOwner called for: {Name}", request.Name);

        var data = _systemStore.Data;
        var serviceName = request.Name.ToLowerInvariant();

        // Find service
        if (!data.Services.TryGetValue(serviceName, out var service))
        {
            var entry = data.Services.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, request.Name, StringComparison.OrdinalIgnoreCase));

            if (entry.Value == null)
            {
                var suggestions = FuzzyMatcher.FindTopMatches(
                    request.Name,
                    data.Services.Keys,
                    k => k,
                    topN: 3
                );

                var suggestionText = suggestions.Any()
                    ? $" Did you mean: {string.Join(", ", suggestions.Select(s => s.Item))}?"
                    : "";

                throw new McpException($"Service '{request.Name}' not found.{suggestionText}"
                , McpErrorCode.InvalidRequest);
            }
            service = entry.Value;
        }

        // Get first owner
        if (service.Owners.Count == 0)
        {
            throw new McpException($"Service '{request.Name}' has no owners defined."
            , McpErrorCode.InvalidRequest);
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

        // Owner info not found, return basic info
        return new ServiceOwnerResponse
        {
            Team = ownerKey,
            Slack = null,
            Pager = null,
            Runbook = null
        };
    }

    private static GetServiceResponse MapToResponse(string name, ServiceEntry service)
    {
        return new GetServiceResponse
        {
            Name = name,
            Description = service.Description,
            Owners = service.Owners,
            Repo = service.Repo,
            Language = service.Language,
            DependsOn = service.DependsOn,
            Api = service.Api
        };
    }
}
