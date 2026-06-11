using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Weblog.Core.Service.AI.Core;

namespace Weblog.Core.Service.AI.Providers;

public class DeepSeekProvider : BaseAiProvider
{
    public override string Name => "deepseek";
    public override string DisplayName => "DeepSeek";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new() { "deepseek-chat", "deepseek-coder" };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => true;
    public override string? DefaultModel => "deepseek-chat";

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => FormatMessage(m)),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens > 0 ? request.MaxTokens : 4096,
            tools = request.Tools,
            stream = false
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var url = "https://api.deepseek.com/v1/chat/completions";

        _logger?.LogDebug("DeepSeek Request: {Payload}", json);

        var responseBody = await PostAsync(client, url, json, ct);
        
        _logger?.LogDebug("DeepSeek Response: {Response}", responseBody);
        
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

            // 检查是否有 tool_calls
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

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DeepSeek Response Parse Error");
            Console.WriteLine($"DeepSeek Response Parse Error: {ex.Message}");
            Console.WriteLine($"Response Body: {responseBody}");
            throw;
        }
    }

    private static object FormatMessage(AiChatMessage m) => m.Role switch
    {
        "tool" => new
        {
            role = m.Role,
            tool_call_id = m.ToolCallId,
            content = m.Content ?? ""
        },
        "assistant" when m.ToolCalls != null && m.ToolCalls.Count > 0 => new
        {
            role = m.Role,
            content = m.Content ?? "",
            tool_calls = m.ToolCalls.Select(tc => new
            {
                id = tc.Id,
                type = tc.Type,
                function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
            })
        },
        _ => new
        {
            role = m.Role,
            content = m.Content ?? ""
        }
    };

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => FormatMessage(m)),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens > 0 ? request.MaxTokens : 4096,
            tools = request.Tools,
            stream = true
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var url = "https://api.deepseek.com/v1/chat/completions";

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
            if (line?.StartsWith("data: ") == true)
            {
                var data = line[6..];
                if (data == "[DONE]")
                    yield break;

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
                catch
                {
                }
                if (!string.IsNullOrEmpty(parsedContent))
                {
                    yield return parsedContent;
                }
            }
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var payload = new { model = DefaultModel, messages = new[] { new { role = "user", content = "Hi" } }, max_tokens = 5 };
            
            var url = string.IsNullOrEmpty(apiUrl) ? "https://api.deepseek.com/v1/chat/completions" : $"{apiUrl}/chat/completions";
            var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"DeepSeek TestConnection Error: {response.StatusCode} - {errorBody}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeepSeek TestConnection Exception: {ex.Message}");
            return false;
        }
    }
    
    private ILogger<DeepSeekProvider>? _logger;
    public void SetLogger(ILogger<DeepSeekProvider> logger) => _logger = logger;

    // ── Embedding ─────────────────────────────────────────────────────────
    // DeepSeek Embedding API 兼容 OpenAI 格式

    public async Task<float[]> EmbedAsync(
        string text, string apiKey, string? model = null, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        var payload = new { model = model ?? "deepseek-embedding", input = text };
        var json = JsonSerializer.Serialize(payload);
        var responseBody = await PostAsync(client, "https://api.deepseek.com/v1/embeddings", json, ct);
        var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
            .EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }

    public async Task<List<float[]>> EmbedBatchAsync(
        List<string> texts, string apiKey, string? model = null, CancellationToken ct = default)
    {
        const int batchSize = 20;
        var result = new List<float[]>();
        for (var i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var payload = new { model = model ?? "deepseek-embedding", input = batch };
            var json = JsonSerializer.Serialize(payload);
            var responseBody = await PostAsync(client, "https://api.deepseek.com/v1/embeddings", json, ct);
            var doc = JsonDocument.Parse(responseBody);
            var ordered = doc.RootElement.GetProperty("data")
                .EnumerateArray()
                .OrderBy(d => d.GetProperty("index").GetInt32())
                .Select(d => d.GetProperty("embedding")
                    .EnumerateArray().Select(e => e.GetSingle()).ToArray())
                .ToList();
            result.AddRange(ordered);
        }
        return result;
    }
}