using System.Text;
using System.Text.Json;
using Weblog.Core.Service.AI.Core;

namespace Weblog.Core.Service.AI.Providers;

public class ClaudeProvider : BaseAiProvider
{
    public override string Name => "claude";
    public override string DisplayName => "Anthropic Claude";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new()
    {
        "claude-sonnet-4-20250514", "claude-sonnet-4", "claude-3-5-sonnet-20241022",
        "claude-3-5-sonnet", "claude-3-opus-20240229", "claude-3-haiku-20240307",
        "claude-3-5-haiku-20241022"
    };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => true;
    public override string? DefaultModel => "claude-sonnet-4-20250514";

    /// <summary>
    /// 从消息列表中提取 system 消息（Claude API 要求 system 作为独立参数）
    /// </summary>
    private static (string? systemPrompt, List<object> messages) ExtractSystemAndMessages(List<AiChatMessage> allMessages)
    {
        string? systemPrompt = null;
        var messages = new List<object>();

        foreach (var m in allMessages)
        {
            if (m.Role == "system")
            {
                // Claude API: system 消息需要作为顶级参数，不能放在 messages 数组中
                systemPrompt = string.IsNullOrEmpty(systemPrompt)
                    ? m.Content
                    : systemPrompt + "\n\n" + m.Content;
            }
            else
            {
                // Claude 只支持 user 和 assistant 两种角色
                var role = m.Role == "assistant" ? "assistant" : "user";
                messages.Add(new { role, content = m.Content });
            }
        }

        // Claude 要求消息列表不能为空，且第一条必须是 user
        if (messages.Count == 0)
        {
            messages.Add(new { role = "user", content = "Hello" });
        }

        return (systemPrompt, messages);
    }

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var (systemPrompt, messages) = ExtractSystemAndMessages(request.Messages);

        var payloadDict = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["max_tokens"] = request.MaxTokens ?? 4096,
            ["temperature"] = request.Temperature
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            payloadDict["system"] = systemPrompt;
        }

        var json = JsonSerializer.Serialize(payloadDict);
        var url = "https://api.anthropic.com/v1/messages";

        var responseBody = await PostAsync(client, url, json, ct);

        var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var content = "";
        if (root.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
        {
            var firstBlock = contentArray[0];
            if (firstBlock.TryGetProperty("text", out var textProp))
                content = textProp.GetString() ?? "";
        }

        var usageInput = 0;
        var usageOutput = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("input_tokens", out var inputTokens))
                usageInput = inputTokens.GetInt32();
            if (usage.TryGetProperty("output_tokens", out var outputTokens))
                usageOutput = outputTokens.GetInt32();
        }

        var finishReason = "stop";
        if (root.TryGetProperty("stop_reason", out var stopReason))
            finishReason = stopReason.GetString() ?? "stop";

        return new AiChatResponse
        {
            Content = content,
            Model = request.Model,
            UsageInput = usageInput,
            UsageOutput = usageOutput,
            FinishReason = finishReason
        };
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        var (systemPrompt, messages) = ExtractSystemAndMessages(request.Messages);

        var payloadDict = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["max_tokens"] = request.MaxTokens ?? 4096,
            ["temperature"] = request.Temperature,
            ["stream"] = true
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            payloadDict["system"] = systemPrompt;
        }

        var json = JsonSerializer.Serialize(payloadDict);
        var url = "https://api.anthropic.com/v1/messages";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..];
            if (data == "[DONE]")
                yield break;

            string? resultText = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                // Claude SSE 事件类型
                if (root.TryGetProperty("type", out var typeProp))
                {
                    var eventType = typeProp.GetString();

                    switch (eventType)
                    {
                        case "content_block_delta":
                            if (root.TryGetProperty("delta", out var delta) &&
                                delta.TryGetProperty("text", out var textProp))
                            {
                                resultText = textProp.GetString();
                            }
                            break;

                        case "message_stop":
                            yield break;

                        case "error":
                            if (root.TryGetProperty("error", out var errorObj) &&
                                errorObj.TryGetProperty("message", out var errorMsg))
                            {
                                throw new Exception($"Claude API Error: {errorMsg.GetString()}");
                            }
                            break;
                    }
                }
                // 兼容旧格式
                else if (root.TryGetProperty("delta", out var legacyDelta) &&
                         legacyDelta.TryGetProperty("text", out var legacyText))
                {
                    resultText = legacyText.GetString();
                }
            }
            catch (JsonException)
            {
                // 忽略 JSON 解析错误
            }

            if (!string.IsNullOrEmpty(resultText))
            {
                yield return resultText;
            }
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var payload = new Dictionary<string, object>
            {
                ["model"] = DefaultModel!,
                ["messages"] = new[] { new { role = "user", content = "Hi" } },
                ["max_tokens"] = 5
            };
            var json = JsonSerializer.Serialize(payload);

            var url = string.IsNullOrEmpty(apiUrl)
                ? "https://api.anthropic.com/v1/messages"
                : $"{apiUrl.TrimEnd('/')}/v1/messages";

            var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
