using System.Text;
using System.Text.Json;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Plugins;

[AiPlugin("article_summary", "文章摘要", "使用 AI 自动生成文章摘要，支持长度、截断字数自定义", "content", "openai", "claude", "deepseek", "zhipu")]
public class ArticleSummaryPlugin : BaseAiPlugin
{
    public override string PluginId => "article_summary";
    public override string Name => "文章摘要";
    public override string Description => "使用 AI 自动生成文章摘要，支持长度、截断字数自定义";
    public override string Category => "content";
    public override string Version => "1.1.0";
    public override string Author => "Weblog";
    public override List<string> RequiredProviders => new() { "openai", "claude", "deepseek", "azure", "gemini", "zhipu", "qianfan", "minimax" };

    private const string SystemPromptTemplate = @"你是一个专业的文章摘要助手。请根据用户提供的文章内容，生成一个简洁的中文摘要。

要求：
1. 摘要长度控制在 {summaryLength} 字之间
2. 包含文章的核心观点和关键信息
3. 语言简洁明了，便于快速阅读
4. 如果文章内容不足以生成摘要，请返回'内容不足，无法生成摘要'";

    private const string UserPrompt = @"请为以下文章生成摘要：

{content}";

    public override async Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request)
    {
        var context = Context ?? throw new InvalidOperationException("Plugin not initialized");

        var content = request.Parameters.GetValueOrDefault("content")?.ToString();
        var resolvedModel = await ResolveModelAsync(request);

        // 从 config 注入的参数中读取，支持管理员在插件市场配置
        var maxContentLength = GetIntParam(request, "maxContentLength") ?? 8000;
        var summaryLength = request.Parameters.GetValueOrDefault("summaryLength")?.ToString() ?? "200-300";

        if (string.IsNullOrEmpty(content))
        {
            return new AiPluginResult { Success = false, Error = "文章内容不能为空" };
        }

        if (content.Length > maxContentLength)
        {
            content = content.Substring(0, maxContentLength) + "...";
        }

        var systemPrompt = SystemPromptTemplate.Replace("{summaryLength}", summaryLength);

        var chatRequest = new AiChatRequest
        {
            Model = resolvedModel,
            Messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = UserPrompt.Replace("{content}", content) }
            },
            Temperature = 0.7,
            MaxTokens = 1000
        };

        try
        {
            var (provider, apiKey, error) = await context.ProviderSelector.SelectAsync(
                preferredProvider: null,
                type: AiProviderType.Chat
            );

            if (provider == null || apiKey == null)
                return new AiPluginResult { Success = false, Error = error ?? "No available provider" };

            var response = await provider.ChatAsync(chatRequest, apiKey);
            context.ProviderSelector.RecordSuccess(provider.Name, apiKey);

            return new AiPluginResult
            {
                Success = true,
                Data = response.Content,
                Metadata = new Dictionary<string, object>
                {
                    { "model", response.Model },
                    { "usage", response.UsageInput + response.UsageOutput }
                }
            };
        }
        catch (Exception ex)
        {
            return new AiPluginResult { Success = false, Error = ex.Message };
        }
    }

    private static int? GetIntParam(AiPluginExecuteRequest request, string key)
    {
        if (!request.Parameters.TryGetValue(key, out var value)) return null;
        if (value is int i) return i;
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
            return je.GetInt32();
        if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        return null;
    }

    public override async Task<AiPluginMetadata> GetMetadataAsync()
    {
        return new AiPluginMetadata
        {
            PluginId = PluginId,
            Name = Name,
            Description = Description,
            Category = Category,
            RequiredProviders = RequiredProviders,
            IsEnabled = true,
            Version = Version,
            Author = Author,
            Config = new Dictionary<string, object>
            {
                { "maxContentLength", 8000 },
                { "summaryLength", "200-300" }
            }
        };
    }
}

[AiPlugin("chat_assistant", "AI 对话助手", "与 AI 进行实时多轮对话，支持系统提示词与使用配额", "assistant", "openai", "claude", "deepseek", "zhipu")]
public class ChatAssistantPlugin : BaseAiPlugin
{
    public override string PluginId => "chat_assistant";
    public override string Name => "AI 对话助手";
    public override string Description => "与 AI 进行实时多轮对话，支持系统提示词与使用配额";
    public override string Category => "assistant";
    public override string Version => "1.2.0";
    public override string Author => "Weblog";
    public override List<string> RequiredProviders => new() { "openai", "claude", "deepseek", "azure", "gemini", "zhipu", "qianfan", "minimax" };

    public override async Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request)
    {
        var context = Context ?? throw new InvalidOperationException("Plugin not initialized");

        var messages = request.Parameters.GetValueOrDefault("messages") as List<Dictionary<string, string>>;
        var resolvedModel = await ResolveModelAsync(request);
        var stream = request.Parameters.GetValueOrDefault("stream") as bool? ?? false;
        var temperature = GetParameter<double>(request, "temperature") ?? 0.7;
        var maxTokens = GetParameter<int>(request, "maxTokens") ?? 4096;
        var systemPrompt = request.Parameters.GetValueOrDefault("systemPrompt")?.ToString();

        if (messages == null || messages.Count == 0)
        {
            return new AiPluginResult
            {
                Success = false,
                Error = "消息列表不能为空"
            };
        }

        var chatMessages = messages.Select(m => new AiChatMessage
        {
            Role = m.GetValueOrDefault("role", "user"),
            Content = m.GetValueOrDefault("content", "")
        }).ToList();

        if (!string.IsNullOrEmpty(systemPrompt) && chatMessages.First().Role != "system")
        {
            chatMessages.Insert(0, new AiChatMessage { Role = "system", Content = systemPrompt });
        }

        var chatRequest = new AiChatRequest
        {
            Model = resolvedModel,
            Messages = chatMessages,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        try
        {
            var (provider, apiKey, error) = await context.ProviderSelector.SelectAsync(
                preferredProvider: null,
                type: AiProviderType.Chat
            );

            if (provider == null || apiKey == null)
            {
                return new AiPluginResult
                {
                    Success = false,
                    Error = error ?? "No available provider"
                };
            }

            if (stream)
            {
                return new AiPluginResult
                {
                    Success = true,
                    Metadata = new Dictionary<string, object>
                    {
                        { "stream", true },
                        { "provider", provider.Name },
                        { "model", resolvedModel }
                    }
                };
            }

            var response = await provider.ChatAsync(chatRequest, apiKey);

            context.ProviderSelector.RecordSuccess(provider.Name, apiKey);

            return new AiPluginResult
            {
                Success = true,
                Data = response.Content,
                Metadata = new Dictionary<string, object>
                {
                    { "model", response.Model },
                    { "provider", provider.Name },
                    { "usage", response.UsageInput + response.UsageOutput }
                }
            };
        }
        catch (Exception ex)
        {
            return new AiPluginResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private T? GetParameter<T>(AiPluginExecuteRequest request, string key) where T : struct
    {
        if (request.Parameters.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;
            
            try
            {
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    if (typeof(T) == typeof(double))
                        return (T)(object)jsonElement.GetDouble();
                    if (typeof(T) == typeof(int))
                        return (T)(object)jsonElement.GetInt32();
                    if (typeof(T) == typeof(float))
                        return (T)(object)jsonElement.GetSingle();
                    if (typeof(T) == typeof(bool))
                        return (T)(object)jsonElement.GetBoolean();
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return null;
            }
        }
        return null;
    }
}

[AiPlugin("editor_assistant", "编辑器助手", "在文章编辑器中辅助写作：续写、改写、润色、扩写、缩写", "editor", "openai", "claude", "deepseek", "zhipu")]
public class EditorAssistantPlugin : BaseAiPlugin
{
    public override string PluginId => "editor_assistant";
    public override string Name => "编辑器助手";
    public override string Description => "在文章编辑器中辅助写作：续写、改写、润色、扩写、缩写";
    public override string Category => "editor";
    public override string Version => "1.0.0";
    public override string Author => "Weblog";
    public override List<string> RequiredProviders => new() { "openai", "claude", "deepseek", "zhipu" };

    private const string SystemPrompt = @"你是一个专业的写作助手。用户正在编辑文章，请根据用户的请求提供帮助。

支持的功能：
1. 续写：根据当前内容继续写作
2. 改写：改善现有内容的表达
3. 润色：优化语言和格式
4. 扩写：扩展内容使其更丰富
5. 缩写：精简内容保留核心

请根据用户的具体请求进行创作，保持风格一致性。";

    public override async Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request)
    {
        var context = Context ?? throw new InvalidOperationException("Plugin not initialized");

        var currentContent = request.Parameters.GetValueOrDefault("currentContent")?.ToString();
        var instruction = request.Parameters.GetValueOrDefault("instruction")?.ToString();
        var resolvedModel = await ResolveModelAsync(request);

        if (string.IsNullOrEmpty(currentContent))
        {
            return new AiPluginResult
            {
                Success = false,
                Error = "当前内容不能为空"
            };
        }

        if (string.IsNullOrEmpty(instruction))
        {
            instruction = "请续写以下内容：";
        }

        var chatRequest = new AiChatRequest
        {
            Model = resolvedModel,
            Messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = SystemPrompt },
                new() { Role = "user", Content = $"{instruction}\n\n当前内容：\n{currentContent}" }
            },
            Temperature = 0.8,
            MaxTokens = 2000
        };

        try
        {
            var (provider, apiKey, error) = await context.ProviderSelector.SelectAsync();

            if (provider == null || apiKey == null)
            {
                return new AiPluginResult
                {
                    Success = false,
                    Error = error ?? "No available provider"
                };
            }

            var response = await provider.ChatAsync(chatRequest, apiKey);

            context.ProviderSelector.RecordSuccess(provider.Name, apiKey);

            return new AiPluginResult
            {
                Success = true,
                Data = response.Content,
                Metadata = new Dictionary<string, object>
                {
                    { "model", response.Model }
                }
            };
        }
        catch (Exception ex)
        {
            return new AiPluginResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

[AiPlugin("tag_recommend", "AI 标签推荐", "根据文章内容自动推荐合适的标签，支持现有标签匹配", "content", "openai", "claude", "deepseek", "zhipu")]
public class TagRecommendPlugin : BaseAiPlugin
{
    public override string PluginId => "tag_recommend";
    public override string Name => "AI 标签推荐";
    public override string Description => "根据文章内容自动推荐合适的标签，支持现有标签匹配";
    public override string Category => "content";
    public override string Version => "1.0.0";
    public override string Author => "Weblog";
    public override List<string> RequiredProviders => new() { "openai", "claude", "deepseek", "azure", "gemini", "zhipu", "qianfan", "minimax" };

    private const string SystemPromptTemplate = @"你是一个专业的文章标签推荐助手。请根据文章内容推荐合适的标签。

要求：
1. 推荐 {tagCount} 个最相关的标签
2. 标签要简洁、精准，每个标签 2-6 个字
3. 优先从已有标签中匹配：{existingTags}
4. 如果已有标签不够匹配，可以推荐新标签
5. 只返回标签列表，用英文逗号分隔，不要返回其他内容

示例输出：Vue.js,前端开发,组件化,TypeScript";

    public override async Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request)
    {
        var context = Context ?? throw new InvalidOperationException("Plugin not initialized");

        // 测试模式：使用测试内容验证 Provider 连通性
        var isTest = request.Parameters.GetValueOrDefault("test") is bool t && t;

        var content = request.Parameters.GetValueOrDefault("content")?.ToString();
        var resolvedModel = await ResolveModelAsync(request);
        var existingTags = request.Parameters.GetValueOrDefault("existingTags")?.ToString() ?? "";
        var tagCount = GetIntParam(request, "tagCount") ?? 5;
        var maxContentLength = GetIntParam(request, "maxContentLength") ?? 5000;

        if (isTest)
        {
            content = "Vue.js 是一个渐进式 JavaScript 框架，用于构建用户界面。本文介绍了 Vue 3 Composition API 的核心特性以及如何使用 TypeScript 进行类型安全的开发。";
            tagCount = 3;
        }

        if (string.IsNullOrEmpty(content))
            return new AiPluginResult { Success = false, Error = "文章内容不能为空" };

        if (content.Length > maxContentLength)
            content = content.Substring(0, maxContentLength) + "...";

        var systemPrompt = SystemPromptTemplate
            .Replace("{tagCount}", tagCount.ToString())
            .Replace("{existingTags}", string.IsNullOrEmpty(existingTags) ? "（暂无已有标签）" : existingTags);

        var chatRequest = new AiChatRequest
        {
            Model = resolvedModel,
            Messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = $"请为以下文章推荐标签：\n\n{content}" }
            },
            Temperature = 0.5,
            MaxTokens = 500
        };

        try
        {
            var (provider, apiKey, error) = await context.ProviderSelector.SelectAsync(
                preferredProvider: null, type: AiProviderType.Chat);

            if (provider == null || apiKey == null)
                return new AiPluginResult { Success = false, Error = error ?? "No available provider" };

            var response = await provider.ChatAsync(chatRequest, apiKey);
            context.ProviderSelector.RecordSuccess(provider.Name, apiKey);

            return new AiPluginResult
            {
                Success = true,
                Data = response.Content,
                Metadata = new Dictionary<string, object>
                {
                    { "model", response.Model },
                    { "usage", response.UsageInput + response.UsageOutput }
                }
            };
        }
        catch (Exception ex)
        {
            return new AiPluginResult { Success = false, Error = ex.Message };
        }
    }

    private static int? GetIntParam(AiPluginExecuteRequest request, string key)
    {
        if (!request.Parameters.TryGetValue(key, out var value)) return null;
        if (value is int i) return i;
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
            return je.GetInt32();
        if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        return null;
    }

    public override async Task<AiPluginMetadata> GetMetadataAsync()
    {
        return new AiPluginMetadata
        {
            PluginId = PluginId,
            Name = Name,
            Description = Description,
            Category = Category,
            RequiredProviders = RequiredProviders,
            IsEnabled = true,
            Version = Version,
            Author = Author,
            Config = new Dictionary<string, object>
            {
                { "tagCount", 5 },
                { "maxContentLength", 5000 }
            }
        };
    }
}

[AiPlugin("translation", "AI 翻译", "将文章或选中内容翻译为目标语言，支持多语种", "content", "openai", "claude", "deepseek", "zhipu")]
public class TranslationPlugin : BaseAiPlugin
{
    public override string PluginId => "translation";
    public override string Name => "AI 翻译";
    public override string Description => "将文章或选中内容翻译为目标语言，支持多语种";
    public override string Category => "content";
    public override string Version => "1.0.0";
    public override string Author => "Weblog";
    public override List<string> RequiredProviders => new() { "openai", "claude", "deepseek", "azure", "gemini", "zhipu", "qianfan", "minimax" };

    private const string SystemPromptTemplate = @"你是一个专业的翻译助手。请将用户提供的内容翻译为 {targetLanguage}。

要求：
1. 翻译要准确、自然，符合目标语言的表达习惯
2. 保持原文的格式（如 Markdown 标记、段落分割）
3. 专业术语优先使用业界通用译法
4. 直接返回翻译结果，不要加任何说明或前缀";

    private static readonly Dictionary<string, string> LanguageMap = new()
    {
        { "en", "英语 (English)" },
        { "zh", "中文 (Chinese)" },
        { "ja", "日语 (Japanese)" },
        { "ko", "韩语 (Korean)" },
        { "fr", "法语 (French)" },
        { "de", "德语 (German)" },
        { "es", "西班牙语 (Spanish)" },
        { "ru", "俄语 (Russian)" }
    };

    public override async Task<AiPluginResult> ExecuteAsync(AiPluginExecuteRequest request)
    {
        var context = Context ?? throw new InvalidOperationException("Plugin not initialized");

        // 测试模式：使用测试内容验证 Provider 连通性
        var isTest = request.Parameters.GetValueOrDefault("test") is bool t && t;

        var content = request.Parameters.GetValueOrDefault("content")?.ToString();
        var resolvedModel = await ResolveModelAsync(request);
        var targetLang = request.Parameters.GetValueOrDefault("targetLanguage")?.ToString() ?? "en";
        var maxContentLength = GetIntParam(request, "maxContentLength") ?? 10000;

        if (isTest)
        {
            content = "你好，世界！这是一段用于测试翻译插件的文字。";
        }

        if (string.IsNullOrEmpty(content))
            return new AiPluginResult { Success = false, Error = "翻译内容不能为空" };

        if (content.Length > maxContentLength)
            content = content.Substring(0, maxContentLength) + "...";

        var targetLanguageName = LanguageMap.GetValueOrDefault(targetLang, targetLang);
        var systemPrompt = SystemPromptTemplate.Replace("{targetLanguage}", targetLanguageName);

        var chatRequest = new AiChatRequest
        {
            Model = resolvedModel,
            Messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = content }
            },
            Temperature = 0.3,
            MaxTokens = 4096
        };

        try
        {
            var (provider, apiKey, error) = await context.ProviderSelector.SelectAsync(
                preferredProvider: null, type: AiProviderType.Chat);

            if (provider == null || apiKey == null)
                return new AiPluginResult { Success = false, Error = error ?? "No available provider" };

            var response = await provider.ChatAsync(chatRequest, apiKey);
            context.ProviderSelector.RecordSuccess(provider.Name, apiKey);

            return new AiPluginResult
            {
                Success = true,
                Data = response.Content,
                Metadata = new Dictionary<string, object>
                {
                    { "model", response.Model },
                    { "targetLanguage", targetLang },
                    { "usage", response.UsageInput + response.UsageOutput }
                }
            };
        }
        catch (Exception ex)
        {
            return new AiPluginResult { Success = false, Error = ex.Message };
        }
    }

    private static int? GetIntParam(AiPluginExecuteRequest request, string key)
    {
        if (!request.Parameters.TryGetValue(key, out var value)) return null;
        if (value is int i) return i;
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
            return je.GetInt32();
        if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        return null;
    }

    public override async Task<AiPluginMetadata> GetMetadataAsync()
    {
        return new AiPluginMetadata
        {
            PluginId = PluginId,
            Name = Name,
            Description = Description,
            Category = Category,
            RequiredProviders = RequiredProviders,
            IsEnabled = true,
            Version = Version,
            Author = Author,
            Config = new Dictionary<string, object>
            {
                { "maxContentLength", 10000 },
                { "defaultTargetLanguage", "en" },
                { "supportedLanguages", "en,zh,ja,ko,fr,de,es,ru" }
            }
        };
    }
}