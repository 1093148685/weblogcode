using Microsoft.Extensions.Logging;
using Weblog.Core.Repository;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Plugins;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AiPluginAttribute : Attribute
{
    public string PluginId { get; }
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public List<string> RequiredProviders { get; }
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "Weblog";

    public AiPluginAttribute(string pluginId, string name, string description, string category = "general", params string[] requiredProviders)
    {
        PluginId = pluginId;
        Name = name;
        Description = description;
        Category = category;
        RequiredProviders = requiredProviders.ToList();
    }
}

public interface IAiPlugin
{
    string PluginId { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    List<string> RequiredProviders { get; }
    string Version { get; }
    string Author { get; }

    Task InitializeAsync(AiPluginContext context);
    Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request);
    Task<AiPluginMetadata> GetMetadataAsync();
}

public class AiPluginContext
{
    public IServiceProvider Services { get; set; } = null!;
    public DbContext DbContext { get; set; } = null!;
    public ProviderRegistry ProviderRegistry { get; set; } = null!;
    public AiProviderSelector ProviderSelector { get; set; } = null!;
    public IAiKeyEncryptionService EncryptionService { get; set; } = null!;
}

public class AiPluginExecuteRequest
{
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class AiPluginResult
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AiPluginMetadata
{
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> RequiredProviders { get; set; } = new();
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "Weblog";
}

public abstract class BaseAiPlugin : IAiPlugin
{
    public abstract string PluginId { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Category { get; }
    public abstract List<string> RequiredProviders { get; }
    public virtual string Version => "1.0.0";
    public virtual string Author => "Weblog";

    protected AiPluginContext? Context { get; private set; }

    public virtual Task InitializeAsync(AiPluginContext context)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public abstract Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request);

    /// <summary>
    /// 从数据库获取默认模型名称，优先使用已配置的模型，避免硬编码
    /// </summary>
    protected async Task<string> GetDefaultModelAsync()
    {
        if (Context?.DbContext == null) return string.Empty;

        try
        {
            var defaultModel = await Context.DbContext.Db.Queryable<Weblog.Core.Model.Entities.AiModel>()
                .Where(m => m.IsEnabled)
                .OrderByDescending(m => m.IsDefault)
                .FirstAsync();

            if (defaultModel != null && !string.IsNullOrEmpty(defaultModel.Model))
            {
                return defaultModel.Model;
            }
        }
        catch
        {
            // 查询失败时返回空，调用方应处理
        }

        return string.Empty;
    }

    /// <summary>
    /// 解析模型参数：优先使用请求中传入的model，其次使用数据库配置的默认模型
    /// </summary>
    protected async Task<string> ResolveModelAsync(AiPluginExecuteRequest request)
    {
        var model = request.Parameters.GetValueOrDefault("model")?.ToString();
        if (!string.IsNullOrEmpty(model)) return model;

        var defaultModel = await GetDefaultModelAsync();
        if (!string.IsNullOrEmpty(defaultModel)) return defaultModel;

        // 最终兜底，返回空字符串由 provider 自行决定
        return string.Empty;
    }

    public virtual Task<AiPluginMetadata> GetMetadataAsync()
    {
        return Task.FromResult(new AiPluginMetadata
        {
            PluginId = PluginId,
            Name = Name,
            Description = Description,
            Category = Category,
            RequiredProviders = RequiredProviders,
            IsEnabled = true,
            Version = Version,
            Author = Author
        });
    }
}