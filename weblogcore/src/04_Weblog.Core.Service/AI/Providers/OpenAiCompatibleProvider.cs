using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.AI.Core;

namespace Weblog.Core.Service.AI.Providers;

public class OpenAiCompatibleProvider : BaseAiProvider
{
    private readonly AiProviderConfig _config;
    private readonly AiProviderAdvancedConfigDto _advancedConfig;

    public OpenAiCompatibleProvider(AiProviderConfig config)
    {
        _config = config;
        _advancedConfig = AiProviderConfigParser.Parse(config.Config);
    }

    public override string Name => _config.Name;
    public override string DisplayName => string.IsNullOrWhiteSpace(_config.DisplayName) ? _config.Name : _config.DisplayName;
    public override AiProviderType Type => _config.Type;
    public override List<string> Models => _advancedConfig.Models
        .Where(m => m.IsEnabled && !string.IsNullOrWhiteSpace(m.Id))
        .Select(m => m.Id)
        .ToList();
    public override bool SupportsStreaming => true;
    public override bool SupportsFunctionCalling => true;
    public override string? DefaultModel => _advancedConfig.Models.FirstOrDefault(m => m.IsEnabled && m.IsDefault)?.Id
        ?? _advancedConfig.Models.FirstOrDefault(m => m.IsEnabled)?.Id
        ?? "gpt-4o-mini";

    public override async Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        ApplyHeaders(client.DefaultRequestHeaders, apiKey);

        var json = JsonSerializer.Serialize(BuildPayload(request, stream: false));
        var responseBody = await PostAsync(client, BuildUrl(_advancedConfig.ChatPath), json, ct);

        return ParseResponse(responseBody, request.Model);
    }

    public override async IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        var json = JsonSerializer.Serialize(BuildPayload(request, stream: true));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl(_advancedConfig.ChatPath));
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        ApplyHeaders(httpRequest.Headers, apiKey);

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") != true)
                continue;

            var data = line[6..];
            if (data == "[DONE]")
                yield break;

            string? parsedContent = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
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
                yield return parsedContent;
        }
    }

    public override async Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            ApplyHeaders(client.DefaultRequestHeaders, apiKey);

            var payload = new
            {
                model = DefaultModel,
                messages = new[] { new { role = "user", content = "Hi" } },
                max_tokens = 5
            };

            var url = string.IsNullOrWhiteSpace(apiUrl)
                ? BuildUrl(_advancedConfig.ChatPath)
                : $"{apiUrl.TrimEnd('/')}{_advancedConfig.ChatPath}";

            var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private object BuildPayload(AiChatRequest request, bool stream)
    {
        var model = AiProviderConfigParser.ParseModelRoute(request.Model).ModelId;
        return new
        {
            model,
            messages = request.Messages.Select(m =>
            {
                if (m.ToolCalls != null && m.ToolCalls.Count > 0)
                {
                    return (object)new
                    {
                        role = m.Role,
                        content = m.Content,
                        tool_calls = m.ToolCalls.Select(tc => new
                        {
                            id = tc.Id,
                            type = tc.Type,
                            function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                        })
                    };
                }

                if (m.Role == "tool")
                    return (object)new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId ?? m.Name };

                return new { role = m.Role, content = m.Content };
            }),
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            tools = request.Tools,
            tool_choice = request.ToolChoice,
            stream
        };
    }

    private string BuildUrl(string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_config.ApiUrl)
            ? "https://api.openai.com/v1"
            : _config.ApiUrl.TrimEnd('/');
        return $"{baseUrl}{path}";
    }

    private void ApplyHeaders(HttpHeaders headers, string apiKey)
    {
        var customHeaders = _advancedConfig.Headers
            .Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Name))
            .ToList();

        var hasAuthHeader = customHeaders.Any(h =>
            h.Name.Equals("authorization", StringComparison.OrdinalIgnoreCase)
            || h.Name.Equals("api-key", StringComparison.OrdinalIgnoreCase)
            || h.Name.Equals("x-api-key", StringComparison.OrdinalIgnoreCase));

        if (!hasAuthHeader && !string.IsNullOrWhiteSpace(apiKey))
            headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

        foreach (var header in customHeaders)
        {
            var value = (header.Value ?? "")
                .Replace("{apiKey}", apiKey, StringComparison.OrdinalIgnoreCase)
                .Replace("${apiKey}", apiKey, StringComparison.OrdinalIgnoreCase)
                .Replace("{{apiKey}}", apiKey, StringComparison.OrdinalIgnoreCase);

            headers.Remove(header.Name);
            headers.TryAddWithoutValidation(header.Name, value);
        }
    }

    private static AiChatResponse ParseResponse(string responseBody, string requestModel)
    {
        using var doc = JsonDocument.Parse(responseBody);
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
            Model = requestModel,
            UsageInput = usageInput,
            UsageOutput = usageOutput,
            FinishReason = finishReason,
            ToolCallId = toolCallId,
            ToolName = toolName
        };
    }
}
