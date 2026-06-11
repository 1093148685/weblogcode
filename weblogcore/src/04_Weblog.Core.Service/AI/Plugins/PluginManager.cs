using Microsoft.Extensions.Logging;
using SqlSugar;
using Weblog.Core.Repository;
using Weblog.Core.Model.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Weblog.Core.Service.AI.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, IAiPlugin> _plugins = new();
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PluginManager(ILogger<PluginManager> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public void Register(IAiPlugin plugin)
    {
        _plugins[plugin.PluginId] = plugin;
        _logger.LogInformation("Registered AI plugin: {PluginId} - {Name}", plugin.PluginId, plugin.Name);
    }

    public void RegisterAll(IEnumerable<IAiPlugin> plugins)
    {
        foreach (var plugin in plugins)
        {
            Register(plugin);
        }
    }

    public IAiPlugin? Get(string pluginId)
    {
        return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
    }

    public IEnumerable<IAiPlugin> GetAll()
    {
        return _plugins.Values;
    }

    public IEnumerable<AiPluginMetadata> GetAllMetadata()
    {
        var result = new List<AiPluginMetadata>();
        foreach (var plugin in _plugins.Values)
        {
            result.Add(new AiPluginMetadata
            {
                PluginId = plugin.PluginId,
                Name = plugin.Name,
                Description = plugin.Description,
                Category = plugin.Category,
                RequiredProviders = plugin.RequiredProviders,
                Version = plugin.Version,
                Author = plugin.Author
            });
        }
        return result;
    }

    public async Task InitializeAllAsync(AiPluginContext context)
    {
        foreach (var plugin in _plugins.Values)
        {
            try
            {
                await plugin.InitializeAsync(context);
                _logger.LogInformation("Initialized AI plugin: {PluginId}", plugin.PluginId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize plugin: {PluginId}", plugin.PluginId);
            }
        }
    }

    public async Task<AiPluginResult> ExecuteAsync(string pluginId, AiPluginExecuteRequest request)
    {
        var plugin = Get(pluginId);
        if (plugin == null)
        {
            return new AiPluginResult
            {
                Success = false,
                Error = $"Plugin not found: {pluginId}"
            };
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var allPlugins = await dbContext.AiPluginDb.ToListAsync();
        var dbPlugin = allPlugins.FirstOrDefault(p => p.PluginId == pluginId);
        
        if (dbPlugin != null && !dbPlugin.IsEnabled)
        {
            return new AiPluginResult
            {
                Success = false,
                Error = $"Plugin is disabled: {pluginId}"
            };
        }
        
        if (dbPlugin?.Config != null && !string.IsNullOrEmpty(dbPlugin.Config))
        {
            try
            {
                var pluginConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(dbPlugin.Config);
                if (pluginConfig != null)
                {
                    foreach (var kvp in pluginConfig)
                    {
                        if (!request.Parameters.ContainsKey(kvp.Key))
                        {
                            request.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse plugin config for: {PluginId}", pluginId);
            }
        }

        try
        {
            return await plugin.ExecuteAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin execution failed: {PluginId}", pluginId);
            return new AiPluginResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<List<IAiPlugin>> GetEnabledPluginsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var pluginConfigs = await dbContext.AiPluginDb.ToListAsync();
        var enabledIds = pluginConfigs.Where(p => p.IsEnabled).Select(p => p.PluginId).ToHashSet();

        return _plugins.Values
            .Where(p => enabledIds.Contains(p.PluginId) || !pluginConfigs.Any(c => c.PluginId == p.PluginId))
            .ToList();
    }
}