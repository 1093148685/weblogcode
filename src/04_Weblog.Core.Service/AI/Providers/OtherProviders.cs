using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Weblog.Core.Service.AI.Core;

namespace Weblog.Core.Service.AI.Providers;

public class AzureOpenAiProvider : BaseAiProvider
{
    public override string Name => "azure";
    public override string DisplayName => "Azure OpenAI";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new() { "gpt-4", "gpt-4-turbo", "gpt-4o", "gpt-4o-mini", "gpt-35-turbo" };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => true;
    public override string? DefaultModel => "gpt-4o";

    /// <summary>
    /// 从 Config 或 ApiUrl 中解析出 Azure 资源和部署信息
    /// ApiUrl 格式: https://{resource}.openai.azure.com
    /// </summary>
    private static string BuildUrl(string apiUrl, string model)
    {
        var baseUrl = string.IsNullOrEmpty(apiUrl)
            ? "https://your-resource.openai.azure.com"
            : apiUrl.TrimEnd('/');

        return $"{baseUrl}/openai/deployments/{model}/chat/completions?api-version=2024-02-01";
    }

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var json = JsonSerializer.Serialize(payload);
        // 从 Provider 的 Config 中读取 ApiUrl
        var url = BuildUrl("", request.Model);

        var responseBody = await PostAsync(client, url, json, ct);

        var doc = JsonDocument.Parse(responseBody);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var content = choice.GetProperty("message").GetProperty("content").GetString() ?? "";

        var response = new AiChatResponse { Content = content, Model = request.Model };

        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt)) response.UsageInput = pt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var ct2)) response.UsageOutput = ct2.GetInt32();
        }

        return response;
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = true
        };

        var json = JsonSerializer.Serialize(payload);
        var url = BuildUrl("", request.Model);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("api-key", apiKey);
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") != true) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            string? content = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentProp))
                        content = contentProp.GetString();
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            var payload = new { model = DefaultModel, messages = new[] { new { role = "user", content = "Hi" } }, max_tokens = 5 };
            var url = string.IsNullOrEmpty(apiUrl)
                ? BuildUrl("", DefaultModel!)
                : $"{apiUrl.TrimEnd('/')}/openai/deployments/{DefaultModel}/chat/completions?api-version=2024-02-01";
            var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}

public class GeminiProvider : BaseAiProvider
{
    public override string Name => "gemini";
    public override string DisplayName => "Google Gemini";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new() { "gemini-2.0-flash", "gemini-1.5-pro", "gemini-1.5-flash", "gemini-pro" };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => false;
    public override string? DefaultModel => "gemini-2.0-flash";

    private static object[] BuildContents(List<AiChatMessage> messages)
    {
        // Gemini 不支持 system role，需要把 system 消息合并到第一条 user 消息中
        var systemParts = messages.Where(m => m.Role == "system").Select(m => m.Content).ToList();
        var nonSystemMessages = messages.Where(m => m.Role != "system").ToList();

        var contents = new List<object>();
        for (int i = 0; i < nonSystemMessages.Count; i++)
        {
            var m = nonSystemMessages[i];
            var role = m.Role == "assistant" ? "model" : "user";
            var text = m.Content;

            // 把 system 消息注入到第一条 user 消息
            if (i == 0 && role == "user" && systemParts.Count > 0)
            {
                text = string.Join("\n\n", systemParts) + "\n\n" + text;
            }

            contents.Add(new { role, parts = new[] { new { text } } });
        }

        if (contents.Count == 0)
        {
            contents.Add(new { role = "user", parts = new[] { new { text = "Hello" } } });
        }

        return contents.ToArray();
    }

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        var contents = BuildContents(request.Messages);

        var payload = new
        {
            contents,
            generationConfig = new { temperature = request.Temperature, maxOutputTokens = request.MaxTokens ?? 4096 }
        };

        var json = JsonSerializer.Serialize(payload);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{request.Model}:generateContent?key={apiKey}";

        var responseBody = await PostAsync(client, url, json, ct);

        var doc = JsonDocument.Parse(responseBody);
        var content = "";
        var usageInput = 0;
        var usageOutput = 0;

        if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                contentObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
            {
                content = parts[0].GetProperty("text").GetString() ?? "";
            }
        }

        if (doc.RootElement.TryGetProperty("usageMetadata", out var usageMeta))
        {
            if (usageMeta.TryGetProperty("promptTokenCount", out var ptc)) usageInput = ptc.GetInt32();
            if (usageMeta.TryGetProperty("candidatesTokenCount", out var ctc)) usageOutput = ctc.GetInt32();
        }

        return new AiChatResponse { Content = content, Model = request.Model, UsageInput = usageInput, UsageOutput = usageOutput };
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.Timeout = TimeSpan.FromMinutes(5);

        var contents = BuildContents(request.Messages);

        var payload = new
        {
            contents,
            generationConfig = new { temperature = request.Temperature, maxOutputTokens = request.MaxTokens ?? 4096 }
        };

        var json = JsonSerializer.Serialize(payload);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{request.Model}:streamGenerateContent?alt=sse&key={apiKey}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") != true) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            string? text = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        text = parts[0].GetProperty("text").GetString();
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            var payload = new { contents = new[] { new { role = "user", parts = new[] { new { text = "Hi" } } } }, generationConfig = new { maxOutputTokens = 5 } };
            var response = await client.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{DefaultModel}:generateContent?key={apiKey}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}

public class ZhipuProvider : BaseAiProvider, IEmbeddingProvider
{
    public override string Name => "zhipu";
    public override string DisplayName => "智谱 AI (GLM)";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new() { "glm-4-plus", "glm-4", "glm-4-flash", "glm-3-turbo" };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => false;
    public override string? DefaultModel => "glm-4-flash";

    private const string EmbeddingUrl = "https://open.bigmodel.cn/api/paas/v4/embeddings";
    private const string DefaultEmbeddingModel = "embedding-3";

    public async Task<float[]> EmbedAsync(string text, string apiKey, string? model = null, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync(new List<string> { text }, apiKey, model, ct);
        return results[0];
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts, string apiKey, string? model = null, CancellationToken ct = default)
    {
        var embModel = string.IsNullOrWhiteSpace(model) ? DefaultEmbeddingModel : model;
        var results = new List<float[]>();

        // 智谱 embedding 每次只支持单条，逐条调用
        foreach (var text in texts)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var payload = new { model = embModel, input = text };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var responseBody = await PostAsync(client, EmbeddingUrl, json, ct);

            var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            var embedding = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
            results.Add(embedding);
        }

        return results;
    }

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens ?? 4096
        };

        var json = JsonSerializer.Serialize(payload);
        var url = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

        var responseBody = await PostAsync(client, url, json, ct);

        var doc = JsonDocument.Parse(responseBody);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var content = choice.GetProperty("message").GetProperty("content").GetString() ?? "";

        var response = new AiChatResponse { Content = content, Model = request.Model };

        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt)) response.UsageInput = pt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var ct2)) response.UsageOutput = ct2.GetInt32();
        }

        return response;
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens ?? 4096,
            stream = true
        };

        var json = JsonSerializer.Serialize(payload);
        var url = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") != true) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            string? parsedContent = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentProp))
                        parsedContent = contentProp.GetString();
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(parsedContent))
                yield return parsedContent;
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var payload = new { model = DefaultModel, messages = new[] { new { role = "user", content = "Hi" } }, max_tokens = 5 };
            var response = await client.PostAsync("https://open.bigmodel.cn/api/paas/v4/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}

public class QianfanProvider : BaseAiProvider
{
    public override string Name => "qianfan";
    public override string DisplayName => "百度千帆 (ERNIE)";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new() { "ernie-4.0-8k", "ernie-3.5-8k", "ernie-speed-8k", "ernie-speed-128k" };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => false;
    public override string? DefaultModel => "ernie-3.5-8k";

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // 千帆 V2 API 兼容 OpenAI 格式
        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_output_tokens = request.MaxTokens ?? 4096
        };

        var json = JsonSerializer.Serialize(payload);
        var url = "https://qianfan.baidubce.com/v2/chat/completions";

        var responseBody = await PostAsync(client, url, json, ct);

        var doc = JsonDocument.Parse(responseBody);

        // V2 API 返回 OpenAI 兼容格式
        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            var response = new AiChatResponse { Content = content, Model = request.Model };

            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt)) response.UsageInput = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct2)) response.UsageOutput = ct2.GetInt32();
            }

            return response;
        }

        // V1 旧格式兼容
        var resultContent = doc.RootElement.TryGetProperty("result", out var result) ? result.GetString() ?? "" : "";
        return new AiChatResponse { Content = resultContent, Model = request.Model };
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = request.Temperature,
            max_output_tokens = request.MaxTokens ?? 4096,
            stream = true
        };

        var json = JsonSerializer.Serialize(payload);
        var url = "https://qianfan.baidubce.com/v2/chat/completions";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") != true) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            string? parsedContent = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                // V2 格式
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentProp))
                        parsedContent = contentProp.GetString();
                }
                // V1 旧格式
                else if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    parsedContent = result.GetString();
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(parsedContent))
                yield return parsedContent;
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var payload = new { model = DefaultModel, messages = new[] { new { role = "user", content = "Hi" } }, max_output_tokens = 5 };
            var url = string.IsNullOrEmpty(apiUrl)
                ? "https://qianfan.baidubce.com/v2/chat/completions"
                : $"{apiUrl.TrimEnd('/')}/chat/completions";
            var response = await client.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}

public class MiniMaxProvider : BaseAiProvider
{
    public override string Name => "minimax";
    public override string DisplayName => "MiniMax";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new() { "MiniMax-M2.7", "MiniMax-M2.5", "MiniMax-M1" };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => true;
    public override string? DefaultModel => "MiniMax-M2.7";

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = request.Messages.Select(m => FormatMessage(m)).ToList(),
            ["temperature"] = request.Temperature,
            ["max_tokens"] = request.MaxTokens > 0 ? request.MaxTokens : 4096
        };

        if (request.Tools != null && request.Tools.Count > 0)
        {
            payload["tools"] = request.Tools;
            payload["tool_choice"] = request.ToolChoice ?? "auto";
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var url = "https://api.minimaxi.com/v1/chat/completions";

        var responseBody = await PostAsync(client, url, json, ct);

        try
        {
            var doc = JsonDocument.Parse(responseBody);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            var response = new AiChatResponse
            {
                Content = message.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "",
                Model = request.Model,
                FinishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null
            };

            if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
            {
                var firstTool = toolCalls[0];
                response.ToolCallId = firstTool.TryGetProperty("id", out var tcId) ? tcId.GetString() : null;
                if (firstTool.TryGetProperty("function", out var func))
                {
                    response.ToolName = func.TryGetProperty("name", out var name) ? name.GetString() : null;
                    response.Content = func.TryGetProperty("arguments", out var args) ? args.GetString() ?? "{}" : "{}";
                }
            }

            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt)) response.UsageInput = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct2)) response.UsageOutput = ct2.GetInt32();
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MiniMax Response Parse Error: {ex.Message}");
            throw;
        }
    }

    private static object FormatMessage(AiChatMessage m) => m.Role switch
    {
        "tool" => new { role = m.Role, tool_call_id = m.ToolCallId, content = m.Content ?? "" },
        "assistant" when m.ToolCalls != null && m.ToolCalls.Count > 0 => new
        {
            role = m.Role,
            content = m.Content ?? "",
            tool_calls = m.ToolCalls.Select(tc => new { id = tc.Id, type = tc.Type, function = new { name = tc.Function.Name, arguments = tc.Function.Arguments } })
        },
        _ => new { role = m.Role, content = m.Content ?? "" }
    };

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        var payload = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = request.Messages.Select(m => FormatMessage(m)).ToList(),
            ["temperature"] = request.Temperature,
            ["max_tokens"] = request.MaxTokens > 0 ? request.MaxTokens : 4096,
            ["stream"] = true
        };

        if (request.Tools != null && request.Tools.Count > 0)
        {
            payload["tools"] = request.Tools;
            payload["tool_choice"] = request.ToolChoice ?? "auto";
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var url = "https://api.minimaxi.com/v1/chat/completions";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") != true) continue;

            var data = line[6..];
            if (data == "[DONE]") yield break;

            string? parsedContent = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var delta = choices[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentProp))
                        parsedContent = contentProp.GetString();
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(parsedContent))
                yield return parsedContent;
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var payload = new { model = DefaultModel, messages = new[] { new { role = "user", content = "Hi" } }, max_tokens = 10 };

            var response = await client.PostAsync("https://api.minimaxi.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);

            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
