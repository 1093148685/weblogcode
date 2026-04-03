using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Weblog.Core.Repository;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Plugins;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI;

public interface IAiKernel
{
    Task InitializeAsync();
    
    Task<(IAiProvider provider, string apiKey, string? error)> SelectProviderAsync(string? preferredProvider = null, AiProviderType type = AiProviderType.Chat);
    
    Task<AiChatResponse> ChatAsync(AiChatRequest request, string? preferredProvider = null);
    IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string? preferredProvider = null, CancellationToken ct = default);
    
    Task<AiPluginResult> ExecutePluginAsync(string pluginId, AiPluginExecuteRequest request);
    IEnumerable<AiPluginMetadata> GetAllPluginMetadata();
    
    Task<string> GenerateArticleSummaryAsync(long articleId, string content, string? model = null);
    Task<string> ChatWithAiAsync(List<AiChatMessage> messages, string? sessionId = null, string? model = null);
    Task<string> EditorAssistAsync(string currentContent, string instruction, string? model = null);
    Task<string> GenerateArticleAsync(string title, string? outline = null, string style = "技术", int wordCount = 800, string? model = null);
    Task<string> OptimizeSeoAsync(string title, string content, string? keywords = null, string? model = null);
    Task<string> ModerateContentAsync(string content, string? model = null);
}

public class AiKernel : IAiKernel
{
    private readonly ProviderRegistry _registry;
    private readonly AiProviderSelector _selector;
    private readonly PluginManager _pluginManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiKernel> _logger;
    private readonly IAiKeyEncryptionService _encryption;

    public AiKernel(
        ProviderRegistry registry,
        AiProviderSelector selector,
        PluginManager pluginManager,
        IServiceScopeFactory scopeFactory,
        IAiKeyEncryptionService encryption,
        ILogger<AiKernel> logger)
    {
        _registry = registry;
        _selector = selector;
        _pluginManager = pluginManager;
        _scopeFactory = scopeFactory;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing AI Kernel...");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        var context = new AiPluginContext
        {
            Services = scope.ServiceProvider,
            DbContext = dbContext,
            ProviderRegistry = _registry,
            ProviderSelector = _selector,
            EncryptionService = _encryption
        };

        var providers = await dbContext.AiProviderDb.ToListAsync();
        var configs = providers.Select(p => new AiProviderConfig
        {
            Id = p.Id,
            Name = p.Name,
            DisplayName = p.DisplayName,
            Type = Enum.TryParse<AiProviderType>(p.Type, true, out var type) ? type : AiProviderType.Chat,
            ApiUrl = p.ApiUrl,
            EncryptedApiKey = p.EncryptedApiKey,
            IsEnabled = p.IsEnabled,
            Priority = p.Priority,
            Config = p.Config
        }).ToList();

        _selector.InitializeKeyPools(configs);

        _pluginManager.RegisterAll(new IAiPlugin[]
        {
            new Plugins.ArticleSummaryPlugin(),
            new Plugins.ChatAssistantPlugin(),
            new Plugins.EditorAssistantPlugin(),
            new Plugins.TagRecommendPlugin(),
            new Plugins.TranslationPlugin(),
            new Plugins.ArticleWriterPlugin(),
            new Plugins.SeoOptimizerPlugin(),
            new Plugins.ContentModeratorPlugin()
        });

        await _pluginManager.InitializeAllAsync(context);

        _logger.LogInformation("AI Kernel initialized successfully");
    }

    public async Task<(IAiProvider provider, string apiKey, string? error)> SelectProviderAsync(string? preferredProvider = null, AiProviderType type = AiProviderType.Chat)
    {
        return await _selector.SelectAsync(preferredProvider, type);
    }

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, string? preferredProvider = null)
    {
        var (provider, apiKey, error) = await SelectProviderAsync(preferredProvider);
        
        if (provider == null || apiKey == null)
        {
            throw new Exception(error ?? "No available provider");
        }

        var response = await provider.ChatAsync(request, apiKey);
        _selector.RecordSuccess(provider.Name, apiKey);
        
        return response;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string? preferredProvider = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var (provider, apiKey, error) = await SelectProviderAsync(preferredProvider);
        
        if (provider == null || apiKey == null)
        {
            throw new Exception(error ?? "No available provider");
        }

        await foreach (var chunk in provider.ChatStreamAsync(request, apiKey, ct))
        {
            yield return chunk;
        }

        _selector.RecordSuccess(provider.Name, apiKey);
    }

    public async Task<AiPluginResult> ExecutePluginAsync(string pluginId, AiPluginExecuteRequest request)
    {
        return await _pluginManager.ExecuteAsync(pluginId, request);
    }

    public IEnumerable<AiPluginMetadata> GetAllPluginMetadata()
    {
        return _pluginManager.GetAllMetadata();
    }

    public async Task<string> GenerateArticleSummaryAsync(long articleId, string content, string? model = null)
    {
        // 若未指定 model，用当前可用 Provider 的 DefaultModel
        if (string.IsNullOrEmpty(model))
        {
            var (p, _, _) = await SelectProviderAsync();
            model = p?.DefaultModel ?? "deepseek-chat";
        }
        var result = await ExecutePluginAsync("article_summary", new AiPluginExecuteRequest
        {
            Parameters = new Dictionary<string, object>
            {
                { "articleId", articleId },
                { "content", content },
                { "model", model }
            }
        });

        return result.Success ? result.Data ?? "" : throw new Exception(result.Error);
    }

    public async Task<string> ChatWithAiAsync(List<AiChatMessage> messages, string? sessionId = null, string? model = null)
    {
        if (string.IsNullOrEmpty(model))
        {
            var (p, _, _) = await SelectProviderAsync();
            model = p?.DefaultModel ?? "deepseek-chat";
        }
        var result = await ExecutePluginAsync("chat_assistant", new AiPluginExecuteRequest
        {
            SessionId = sessionId,
            Parameters = new Dictionary<string, object>
            {
                { "messages", messages.Select(m => new Dictionary<string, string> { { "role", m.Role }, { "content", m.Content } }).ToList() },
                { "model", model }
            }
        });

        return result.Success ? result.Data ?? "" : throw new Exception(result.Error);
    }

    public async Task<string> EditorAssistAsync(string currentContent, string instruction, string? model = null)
    {
        if (string.IsNullOrEmpty(model))
        {
            var (p, _, _) = await SelectProviderAsync();
            model = p?.DefaultModel ?? "deepseek-chat";
        }
        var result = await ExecutePluginAsync("editor_assistant", new AiPluginExecuteRequest
        {
            Parameters = new Dictionary<string, object>
            {
                { "currentContent", currentContent },
                { "instruction", instruction },
                { "model", model }
            }
        });

        return result.Success ? result.Data ?? "" : throw new Exception(result.Error);
    }

    public async Task<string> GenerateArticleAsync(string title, string? outline = null, string style = "技术", int wordCount = 800, string? model = null)
    {
        if (string.IsNullOrEmpty(model)) { var (p, _, _) = await SelectProviderAsync(); model = p?.DefaultModel ?? "deepseek-chat"; }
        var result = await ExecutePluginAsync("article_writer", new AiPluginExecuteRequest
        {
            Parameters = new Dictionary<string, object>
            {
                { "title", title },
                { "outline", outline ?? "" },
                { "style", style },
                { "wordCount", wordCount },
                { "model", model }
            }
        });
        return result.Success ? result.Data ?? "" : throw new Exception(result.Error);
    }

    public async Task<string> OptimizeSeoAsync(string title, string content, string? keywords = null, string? model = null)
    {
        if (string.IsNullOrEmpty(model)) { var (p, _, _) = await SelectProviderAsync(); model = p?.DefaultModel ?? "deepseek-chat"; }
        var result = await ExecutePluginAsync("seo_optimizer", new AiPluginExecuteRequest
        {
            Parameters = new Dictionary<string, object>
            {
                { "title", title },
                { "content", content },
                { "keywords", keywords ?? "" },
                { "model", model }
            }
        });
        return result.Success ? result.Data ?? "" : throw new Exception(result.Error);
    }

    public async Task<string> ModerateContentAsync(string content, string? model = null)
    {
        if (string.IsNullOrEmpty(model)) { var (p, _, _) = await SelectProviderAsync(); model = p?.DefaultModel ?? "deepseek-chat"; }
        var result = await ExecutePluginAsync("content_moderator", new AiPluginExecuteRequest
        {
            Parameters = new Dictionary<string, object>
            {
                { "content", content },
                { "model", model }
            }
        });
        return result.Success ? result.Data ?? "" : throw new Exception(result.Error);
    }
}