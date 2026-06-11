using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Plugins;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai/plugin")]
[ApiController]
[Authorize]
[RequireRole("admin")]
public class AiPluginController : ControllerBase
{
    private readonly PluginManager _pluginManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiPluginController> _logger;

    public AiPluginController(PluginManager pluginManager, IServiceProvider serviceProvider, ILogger<AiPluginController> logger)
    {
        _pluginManager = pluginManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<Result<List<AiPluginDto>>> GetList()
    {
        var db = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        
        var dbPlugins = await db.Queryable<AiPlugin>().ToListAsync();
        var metadataList = _pluginManager.GetAllMetadata();

        var result = metadataList.Select(m => {
            var dbPlugin = dbPlugins.FirstOrDefault(p => p.PluginId == m.PluginId);
            return new AiPluginDto
            {
                Id = dbPlugin?.Id ?? 0,
                PluginId = m.PluginId,
                Name = m.Name,
                Description = m.Description,
                Category = m.Category,
                RequiredProviders = m.RequiredProviders,
                IsEnabled = dbPlugin?.IsEnabled ?? true,
                Config = dbPlugin?.Config ?? "{}",
                Settings = dbPlugin?.Settings ?? "{}",
                Version = m.Version,
                Author = m.Author
            };
        }).ToList();

        return Result<List<AiPluginDto>>.Ok(result);
    }

    [HttpGet("{pluginId}")]
    public async Task<Result<AiPluginDto>> GetByPluginId(string pluginId)
    {
        var db = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        
        var metadata = _pluginManager.GetAllMetadata().FirstOrDefault(m => m.PluginId == pluginId);
        if (metadata == null)
            return Result<AiPluginDto>.Fail("Plugin not found");

        var dbPlugin = await db.Queryable<AiPlugin>().Where(p => p.PluginId == pluginId).FirstAsync();

        return Result<AiPluginDto>.Ok(new AiPluginDto
        {
            Id = dbPlugin?.Id ?? 0,
            PluginId = metadata.PluginId,
            Name = metadata.Name,
            Description = metadata.Description,
            Category = metadata.Category,
            RequiredProviders = metadata.RequiredProviders,
            IsEnabled = dbPlugin?.IsEnabled ?? true,
            Config = dbPlugin?.Config ?? "{}",
            Settings = dbPlugin?.Settings ?? "{}",
            Version = metadata.Version,
            Author = metadata.Author
        });
    }

    [HttpPut("{pluginId}")]
    public async Task<Result<AiPluginDto>> Update(string pluginId, [FromBody] UpdateAiPluginRequest request)
    {
        var db = _serviceProvider.GetRequiredService<ISqlSugarClient>();

        var metadata = _pluginManager.GetAllMetadata().FirstOrDefault(m => m.PluginId == pluginId);
        if (metadata == null)
            return Result<AiPluginDto>.Fail("Plugin not found");

        var dbPlugin = await db.Queryable<AiPlugin>().Where(p => p.PluginId == pluginId).FirstAsync();

        if (dbPlugin == null)
        {
            dbPlugin = new AiPlugin
            {
                PluginId = pluginId,
                Name = metadata.Name,
                Description = metadata.Description,
                IsEnabled = request.IsEnabled,
                Config = request.Config ?? "{}",
                Settings = request.Settings ?? "{}",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await db.Insertable(dbPlugin).ExecuteCommandAsync();
        }
        else
        {
            dbPlugin.IsEnabled = request.IsEnabled;
            dbPlugin.Config = request.Config ?? dbPlugin.Config;
            dbPlugin.Settings = request.Settings ?? dbPlugin.Settings;
            dbPlugin.UpdatedAt = DateTime.Now;
            await db.Updateable(dbPlugin).ExecuteCommandAsync();
        }

        return Result<AiPluginDto>.Ok(new AiPluginDto
        {
            Id = dbPlugin.Id,
            PluginId = pluginId,
            Name = metadata.Name,
            Description = metadata.Description,
            Category = metadata.Category,
            RequiredProviders = metadata.RequiredProviders,
            IsEnabled = dbPlugin.IsEnabled,
            Config = dbPlugin.Config,
            Settings = dbPlugin.Settings
        });
    }

    [HttpPost("{pluginId}/toggle")]
    public async Task<Result<AiPluginDto>> Toggle(string pluginId, [FromBody] TogglePluginRequest request)
    {
        var db = _serviceProvider.GetRequiredService<ISqlSugarClient>();

        var metadata = _pluginManager.GetAllMetadata().FirstOrDefault(m => m.PluginId == pluginId);
        if (metadata == null)
            return Result<AiPluginDto>.Fail("Plugin not found");

        var dbPlugin = await db.Queryable<AiPlugin>().Where(p => p.PluginId == pluginId).FirstAsync();

        if (dbPlugin == null)
        {
            dbPlugin = new AiPlugin
            {
                PluginId = pluginId,
                Name = metadata.Name,
                Description = metadata.Description,
                IsEnabled = request.IsEnabled,
                Config = "{}",
                Settings = "{}",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await db.Insertable(dbPlugin).ExecuteCommandAsync();
        }
        else
        {
            dbPlugin.IsEnabled = request.IsEnabled;
            dbPlugin.UpdatedAt = DateTime.Now;
            await db.Updateable(dbPlugin).ExecuteCommandAsync();
        }

        return Result<AiPluginDto>.Ok(new AiPluginDto
        {
            Id = dbPlugin.Id,
            PluginId = pluginId,
            Name = metadata.Name,
            Description = metadata.Description,
            Category = metadata.Category,
            RequiredProviders = metadata.RequiredProviders,
            IsEnabled = dbPlugin.IsEnabled
        });
    }

    [HttpGet("{pluginId}/metadata")]
    public Result<AiPluginMetadata> GetMetadata(string pluginId)
    {
        var plugin = _pluginManager.Get(pluginId);
        if (plugin == null)
            return Result<AiPluginMetadata>.Fail("Plugin not found");

        var metadata = _pluginManager.GetAllMetadata().FirstOrDefault(m => m.PluginId == pluginId);
        return Result<AiPluginMetadata>.Ok(metadata!);
    }

    [HttpPost("{pluginId}/test")]
    public async Task<Result<bool>> TestPlugin(string pluginId)
    {
        try
        {
            var result = await _pluginManager.ExecuteAsync(pluginId, new AiPluginExecuteRequest
            {
                Parameters = new Dictionary<string, object>
                {
                    { "test", true }
                }
            });

            return result.Success ? Result<bool>.Ok(true) : Result<bool>.Fail(result.Error ?? "Test failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin test failed: {PluginId}", pluginId);
            return Result<bool>.Fail(ex.Message);
        }
    }
}

public class AiPluginDto
{
    public long Id { get; set; }
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> RequiredProviders { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public string Config { get; set; } = "{}";
    public string Settings { get; set; } = "{}";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "Weblog";
}

public class UpdateAiPluginRequest
{
    public bool IsEnabled { get; set; } = true;
    public string? Config { get; set; }
    public string? Settings { get; set; }
}

public class TogglePluginRequest
{
    public bool IsEnabled { get; set; }
}