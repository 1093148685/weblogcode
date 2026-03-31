using System.Text;
using System.Text.Json;
using Weblog.Core.Service.AI.Core;

namespace Weblog.Core.Service.AI.Providers;

public class OpenAiProvider : BaseAiProvider
{
    public override string Name => "openai";
    public override string DisplayName => "OpenAI";
    public override AiProviderType Type => AiProviderType.Chat;
    public override List<string> Models => new()
    {
        "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo"
    };
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => true;
    public override string? DefaultModel => "gpt-4o";

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m =>
            {
                if (m.ToolCalls != null && m.ToolCalls.Count > 0)
                    return (object)new { role = m.Role, content = m.Content, tool_calls = m.ToolCalls.Select(tc => new { id = tc.Id, type = tc.Type, function = new { name = tc.Function.Name, arguments = tc.Function.Arguments } }) };
                if (m.Role == "tool")
                    return (object)new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId ?? m.Name };
                return (object)new { role = m.Role, content = m.Content };
            }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            tools = request.Tools,
            tool_choice = request.ToolChoice
        };

        var json = JsonSerializer.Serialize(payload);
        var url = "https://api.openai.com/v1/chat/completions";

        var responseBody = await PostAsync(client, url, json, ct);
        
        var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var content = "";
        var usageInput = 0;
        var usageOutput = 0;
        string? finishReason = null;
        string? toolCallId = null;
        string? toolName = null;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("finish_reason", out var reasonProp))
                finishReason = reasonProp.GetString();

            if (firstChoice.TryGetProperty("message", out var message))
            {
                if (message.TryGetProperty("content", out var contentProp))
                    content = contentProp.GetString() ?? "";

                if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                {
                    var firstToolCall = toolCalls[0];
                    if (firstToolCall.TryGetProperty("id", out var idProp))
                        toolCallId = idProp.GetString();
                    if (firstToolCall.TryGetProperty("function", out var func))
                    {
                        if (func.TryGetProperty("name", out var nameProp))
                            toolName = nameProp.GetString();
                        // tool call arguments are in Content for downstream parsing
                        if (func.TryGetProperty("arguments", out var argsProp))
                            content = argsProp.GetString() ?? "";
                    }
                }
            }
        }

        if (root.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var inputTokens))
                usageInput = inputTokens.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var outputTokens))
                usageOutput = outputTokens.GetInt32();
        }

        return new AiChatResponse
        {
            Content = content,
            Model = request.Model,
            UsageInput = usageInput,
            UsageOutput = usageOutput,
            FinishReason = finishReason,
            ToolCallId = toolCallId,
            ToolName = toolName
        };
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m =>
            {
                if (m.ToolCalls != null && m.ToolCalls.Count > 0)
                    return (object)new { role = m.Role, content = m.Content, tool_calls = m.ToolCalls.Select(tc => new { id = tc.Id, type = tc.Type, function = new { name = tc.Function.Name, arguments = tc.Function.Arguments } }) };
                if (m.Role == "tool")
                    return (object)new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId ?? m.Name };
                return (object)new { role = m.Role, content = m.Content };
            }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = true
        };

        var json = JsonSerializer.Serialize(payload);
        var url = "https://api.openai.com/v1/chat/completions";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
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
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var payload = new { model = DefaultModel, messages = new[] { new { role = "user", content = "Hi" } }, max_tokens = 5 };
            var json = JsonSerializer.Serialize(payload);
            
            var url = string.IsNullOrEmpty(apiUrl) 
                ? "https://api.openai.com/v1/chat/completions" 
                : $"{apiUrl.TrimEnd('/')}/chat/completions";

            var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}