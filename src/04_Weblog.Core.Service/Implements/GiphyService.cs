using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class GiphyService : IGiphyService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GiphyService> _logger;
    private const string BaseUrl = "https://api.giphy.com/v1/gifs";

    public GiphyService(HttpClient httpClient, IConfiguration configuration, ILogger<GiphyService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GiphyApiKey"] ?? "dc6zaTOxFJmzC";
        _logger = logger;
    }

    public async Task<List<GiphyItemDto>> SearchAsync(string query, int limit = 20, int offset = 0)
    {
        try
        {
            var url = $"{BaseUrl}/search?api_key={_apiKey}&q={Uri.EscapeDataString(query)}&limit={limit}&offset={offset}&rating=g&lang=zh-CN";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return ParseGiphyResponse(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GIPHY search failed for query: {Query}", query);
            return new List<GiphyItemDto>();
        }
    }

    public async Task<List<GiphyItemDto>> TrendingAsync(int limit = 20, int offset = 0)
    {
        try
        {
            var url = $"{BaseUrl}/trending?api_key={_apiKey}&limit={limit}&offset={offset}&rating=g";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return ParseGiphyResponse(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GIPHY trending failed");
            return new List<GiphyItemDto>();
        }
    }

    private List<GiphyItemDto> ParseGiphyResponse(string json)
    {
        var result = new List<GiphyItemDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var data = root.GetProperty("data");

            foreach (var item in data.EnumerateArray())
            {
                var images = item.GetProperty("images");
                var original = images.GetProperty("original");
                var fixedHeight = images.GetProperty("fixed_height_small");

                result.Add(new GiphyItemDto
                {
                    GiphyId = item.GetProperty("id").GetString() ?? "",
                    Title = item.GetProperty("title").GetString() ?? "",
                    PreviewUrl = fixedHeight.TryGetProperty("url", out var previewUrl) 
                        ? previewUrl.GetString() ?? "" 
                        : original.GetProperty("url").GetString() ?? "",
                    OriginalUrl = original.GetProperty("url").GetString() ?? "",
                    Width = original.TryGetProperty("width", out var w) ? int.Parse(w.GetString() ?? "0") : 0,
                    Height = original.TryGetProperty("height", out var h) ? int.Parse(h.GetString() ?? "0") : 0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse GIPHY response");
        }

        return result;
    }
}