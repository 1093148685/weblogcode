using System.Text;
using Weblog.Core.Service.AI.Core;

namespace Weblog.Core.Service.AI.Providers;

public interface IAiProvider
{
    string Name { get; }
    string DisplayName { get; }
    AiProviderType Type { get; }
    List<string> Models { get; }
    bool SupportsStreaming { get; }
    bool SupportsFunctionCalling { get; }
    string? DefaultModel { get; }

    Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default);
}

/// <summary>支持 Embedding 向量化的 Provider 扩展接口</summary>
public interface IEmbeddingProvider
{
    /// <summary>对单条文本生成向量</summary>
    Task<float[]> EmbedAsync(string text, string apiKey, string? model = null, CancellationToken ct = default);

    /// <summary>批量生成向量（按 20 条/批自动分批）</summary>
    Task<List<float[]>> EmbedBatchAsync(List<string> texts, string apiKey, string? model = null, CancellationToken ct = default);
}

public abstract class BaseAiProvider : IAiProvider
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    public abstract AiProviderType Type { get; }
    public abstract List<string> Models { get; }
    public abstract bool SupportsStreaming { get; }
    public abstract bool SupportsFunctionCalling { get; }
    public abstract string? DefaultModel { get; }

    public abstract Task<AiChatResponse> ChatAsync(AiChatRequest request, string apiKey, CancellationToken ct = default);
    public abstract IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, string apiKey, CancellationToken ct = default);
    public abstract Task<bool> TestConnectionAsync(string apiUrl, string apiKey, CancellationToken ct = default);

    protected async Task<string> PostAsync(HttpClient client, string url, string json, CancellationToken ct)
    {
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"API request failed: {response.StatusCode} - {responseBody}");
        }
        
        return responseBody;
    }

    protected async Task<string> PostStreamAsync(HttpClient client, string url, string json, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);
        var result = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") == true)
            {
                var data = line[6..];
                if (data == "[DONE]")
                    break;
                result.Add(data);
            }
        }

        return string.Join("", result);
    }
}