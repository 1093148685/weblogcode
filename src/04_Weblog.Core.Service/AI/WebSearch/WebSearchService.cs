using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Weblog.Core.Service.AI.WebSearch;

public interface IWebSearchService
{
    Task<List<WebSearchResult>> SearchAsync(string query, int topK = 5, CancellationToken ct = default);
    Task<List<WebSearchResult>> SearchAsync(string query, WebSearchOptions options, CancellationToken ct = default);
}

public class WebSearchOptions
{
    public int TopK { get; set; } = 5;
    public string? TavilyApiKey { get; set; }
    public bool EnableFreeFallback { get; set; } = true;
}

public class WebSearchResult
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string SourceType { get; set; } = "web";
    public bool IsAuthoritative { get; set; }
    public float Relevance { get; set; }
    public float ContentQuality { get; set; }
    public bool HasUsableContent { get; set; }
}

public class WebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebSearchService> _logger;
    private readonly IConfiguration _configuration;

    public WebSearchService(HttpClient httpClient, ILogger<WebSearchService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 WeblogCoreAiSearch/2.0");
    }

    public async Task<List<WebSearchResult>> SearchAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        return await SearchAsync(query, new WebSearchOptions { TopK = topK }, ct);
    }

    public async Task<List<WebSearchResult>> SearchAsync(string query, WebSearchOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<WebSearchResult>();

        var topK = Math.Clamp(options.TopK, 1, 8);

        var weatherResults = await SearchWeatherAsync(query, ct);
        if (weatherResults.Count > 0)
        {
            return await FinalizeResultsAsync(query, Reindex(weatherResults), enrichPages: false, ct);
        }

        var packageResults = await SearchKnownPackageRegistryAsync(query, ct);
        if (packageResults.Count > 0 && IsPackageVersionQuestion(query))
        {
            return await FinalizeResultsAsync(query, Reindex(packageResults), enrichPages: false, ct);
        }

        var searchQuery = BuildSearchQuery(query);

        var configuredResults = await SearchConfiguredProviderAsync(searchQuery, Math.Max(1, topK - packageResults.Count), options, ct);
        if (configuredResults.Count > 0)
        {
            return await FinalizeResultsAsync(searchQuery, MergeResults(packageResults, configuredResults), enrichPages: true, ct);
        }

        if (!options.EnableFreeFallback)
        {
            return await FinalizeResultsAsync(searchQuery, Reindex(packageResults), enrichPages: false, ct);
        }

        if (IsNewsQuestion(query))
        {
            try
            {
                var newsResults = await SearchBingNewsRssAsync(searchQuery, Math.Max(1, topK - packageResults.Count), ct);
                if (newsResults.Count > 0) return await FinalizeResultsAsync(searchQuery, MergeResults(packageResults, newsResults), enrichPages: true, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bing News RSS search failed, trying regular web fallback");
            }
        }

        try
        {
            var bingResults = await SearchBingRssAsync(searchQuery, Math.Max(1, topK - packageResults.Count), ct);
            if (bingResults.Count > 0) return await FinalizeResultsAsync(searchQuery, MergeResults(packageResults, bingResults), enrichPages: true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bing RSS search failed, trying DuckDuckGo html fallback");
        }

        try
        {
            var duckResults = await SearchDuckDuckGoAsync(searchQuery, Math.Max(1, topK - packageResults.Count), ct);
            return await FinalizeResultsAsync(searchQuery, MergeResults(packageResults, duckResults), enrichPages: true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DuckDuckGo search failed");
            return await FinalizeResultsAsync(searchQuery, Reindex(packageResults), enrichPages: false, ct);
        }
    }

    private async Task<List<WebSearchResult>> SearchConfiguredProviderAsync(string query, int topK, WebSearchOptions options, CancellationToken ct)
    {
        var tavilyApiKey = !string.IsNullOrWhiteSpace(options.TavilyApiKey)
            ? options.TavilyApiKey
            : _configuration["WebSearch:TavilyApiKey"];
        if (!string.IsNullOrWhiteSpace(tavilyApiKey))
        {
            var tavilyResults = await SearchTavilyAsync(query, topK, tavilyApiKey, ct);
            if (tavilyResults.Count > 0) return tavilyResults;
        }

        var unSearchBaseUrl = _configuration["WebSearch:UnSearchBaseUrl"];
        if (!string.IsNullOrWhiteSpace(unSearchBaseUrl))
        {
            var unSearchResults = await SearchUnSearchAsync(query, topK, unSearchBaseUrl, _configuration["WebSearch:UnSearchApiKey"], ct);
            if (unSearchResults.Count > 0) return unSearchResults;
        }

        var searxngBaseUrl = _configuration["WebSearch:SearXngBaseUrl"];
        if (!string.IsNullOrWhiteSpace(searxngBaseUrl))
        {
            var searxngResults = await SearchSearXngAsync(query, topK, searxngBaseUrl, ct);
            if (searxngResults.Count > 0) return searxngResults;
        }

        return new List<WebSearchResult>();
    }

    private async Task<List<WebSearchResult>> SearchTavilyAsync(string query, int topK, string apiKey, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                api_key = apiKey,
                query,
                search_depth = "advanced",
                max_results = topK,
                include_answer = false,
                include_raw_content = false
            };

            using var response = await _httpClient.PostAsJsonAsync("https://api.tavily.com/search", payload, ct);
            if (!response.IsSuccessStatusCode) return new List<WebSearchResult>();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return new List<WebSearchResult>();
            }

            return Reindex(results.EnumerateArray()
                .Select(item => new WebSearchResult
                {
                    Title = GetJsonString(item, "title"),
                    Url = GetJsonString(item, "url"),
                    Snippet = FirstJsonString(item, "content", "snippet", "description"),
                    SourceType = "tavily",
                    Relevance = GetJsonFloat(item, "score")
                })
                .Where(IsValidResult)
                .Take(topK));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tavily search failed");
            return new List<WebSearchResult>();
        }
    }

    private async Task<List<WebSearchResult>> SearchUnSearchAsync(string query, int topK, string baseUrl, string? apiKey, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, CombineUrl(baseUrl, "/v1/search"));
            request.Content = JsonContent.Create(new { query, limit = topK });
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new List<WebSearchResult>();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var results = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.TryGetProperty("results", out var r) ? r
                : doc.RootElement.TryGetProperty("data", out var d) ? d
                : default;

            if (results.ValueKind != JsonValueKind.Array) return new List<WebSearchResult>();

            return Reindex(results.EnumerateArray()
                .Select(item => new WebSearchResult
                {
                    Title = FirstJsonString(item, "title", "name"),
                    Url = FirstJsonString(item, "url", "link"),
                    Snippet = FirstJsonString(item, "content", "snippet", "description", "text"),
                    SourceType = "unsearch"
                })
                .Where(IsValidResult)
                .Take(topK));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UnSearch search failed");
            return new List<WebSearchResult>();
        }
    }

    private async Task<List<WebSearchResult>> SearchSearXngAsync(string query, int topK, string baseUrl, CancellationToken ct)
    {
        try
        {
            var url = $"{CombineUrl(baseUrl, "/search")}?q={Uri.EscapeDataString(query)}&format=json&language=zh-CN&safesearch=1";
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return new List<WebSearchResult>();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return new List<WebSearchResult>();
            }

            return Reindex(results.EnumerateArray()
                .Where(item =>
                {
                    var category = GetJsonString(item, "category");
                    return !category.Contains("images", StringComparison.OrdinalIgnoreCase);
                })
                .Select(item => new WebSearchResult
                {
                    Title = GetJsonString(item, "title"),
                    Url = GetJsonString(item, "url"),
                    Snippet = FirstJsonString(item, "content", "snippet"),
                    SourceType = "searxng",
                    IsAuthoritative = true
                })
                .Where(IsValidResult)
                .Take(topK));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SearXNG search failed");
            return new List<WebSearchResult>();
        }
    }

    private async Task<List<WebSearchResult>> SearchKnownPackageRegistryAsync(string query, CancellationToken ct)
    {
        var normalized = query.ToLowerInvariant();
        var results = new List<WebSearchResult>();

        if (normalized.Contains("fastapi"))
        {
            var result = await SearchPypiPackageAsync("fastapi", ct);
            if (result != null) results.Add(result);
        }

        if (normalized.Contains("pydantic"))
        {
            var result = await SearchPypiPackageAsync("pydantic", ct);
            if (result != null) results.Add(result);
        }

        if (normalized.Contains("django"))
        {
            var result = await SearchPypiPackageAsync("Django", ct);
            if (result != null) results.Add(result);
        }

        if (normalized.Contains("vue"))
        {
            var result = await SearchNpmPackageAsync("vue", ct);
            if (result != null) results.Add(result);
        }

        if (normalized.Contains("vite"))
        {
            var result = await SearchNpmPackageAsync("vite", ct);
            if (result != null) results.Add(result);
        }

        return results.Take(3).ToList();
    }

    private async Task<WebSearchResult?> SearchPypiPackageAsync(string packageName, CancellationToken ct)
    {
        try
        {
            var json = await _httpClient.GetStringAsync($"https://pypi.org/pypi/{packageName}/json", ct);
            using var doc = JsonDocument.Parse(json);
            var info = doc.RootElement.GetProperty("info");
            var version = info.GetProperty("version").GetString() ?? "";
            var summary = info.TryGetProperty("summary", out var summaryElement) ? summaryElement.GetString() : "";

            return new WebSearchResult
            {
                Title = $"{packageName} 最新版本：{version} · PyPI",
                Url = $"https://pypi.org/project/{packageName}/",
                Snippet = $"{packageName} 在 PyPI 上的当前最新版本是 {version}。{summary}",
                SourceType = "package",
                IsAuthoritative = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PyPI package lookup failed for {PackageName}", packageName);
            return null;
        }
    }

    private async Task<WebSearchResult?> SearchNpmPackageAsync(string packageName, CancellationToken ct)
    {
        try
        {
            var json = await _httpClient.GetStringAsync($"https://registry.npmjs.org/{packageName}/latest", ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var version = root.GetProperty("version").GetString() ?? "";
            var description = root.TryGetProperty("description", out var descElement) ? descElement.GetString() : "";

            return new WebSearchResult
            {
                Title = $"{packageName} 最新版本：{version} · npm",
                Url = $"https://www.npmjs.com/package/{packageName}",
                Snippet = $"{packageName} 在 npm 上的 latest 版本是 {version}。{description}",
                SourceType = "package",
                IsAuthoritative = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "npm package lookup failed for {PackageName}", packageName);
            return null;
        }
    }

    private async Task<List<WebSearchResult>> SearchWeatherAsync(string query, CancellationToken ct)
    {
        if (!Regex.IsMatch(query, "天气|气温|温度|下雨|weather", RegexOptions.IgnoreCase))
        {
            return new List<WebSearchResult>();
        }

        var location = DetectWeatherLocation(query);
        if (string.IsNullOrWhiteSpace(location.QueryName))
        {
            return new List<WebSearchResult>();
        }

        try
        {
            var json = await _httpClient.GetStringAsync($"https://wttr.in/{Uri.EscapeDataString(location.QueryName)}?format=j1", ct);
            using var doc = JsonDocument.Parse(json);
            var current = doc.RootElement.GetProperty("current_condition")[0];
            var temp = current.GetProperty("temp_C").GetString();
            var feelsLike = current.GetProperty("FeelsLikeC").GetString();
            var humidity = current.GetProperty("humidity").GetString();
            var wind = current.GetProperty("windspeedKmph").GetString();
            var desc = current.GetProperty("weatherDesc")[0].GetProperty("value").GetString();
            var localTime = current.TryGetProperty("localObsDateTime", out var timeElement) ? timeElement.GetString() : "";

            return new List<WebSearchResult>
            {
                new()
                {
                    Title = $"{location.DisplayName} 当前天气 · wttr.in",
                    Url = $"https://wttr.in/{Uri.EscapeDataString(location.QueryName)}",
                    Snippet = $"{location.DisplayName} 当前天气：{TranslateWeather(desc)}，气温 {temp}°C，体感 {feelsLike}°C，湿度 {humidity}%，风速 {wind} km/h。观测时间：{localTime}。",
                    SourceType = "weather",
                    IsAuthoritative = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weather lookup failed for {Location}", location.QueryName);
            return new List<WebSearchResult>();
        }
    }

    private async Task<List<WebSearchResult>> SearchBingRssAsync(string query, int topK, CancellationToken ct)
    {
        var url = $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}&format=rss";
        var xml = await _httpClient.GetStringAsync(url, ct);
        var doc = XDocument.Parse(xml);

        return Reindex(doc.Descendants("item")
            .Take(topK)
            .Select(item => new WebSearchResult
            {
                Title = CleanText(item.Element("title")?.Value),
                Url = item.Element("link")?.Value?.Trim() ?? string.Empty,
                Snippet = CleanText(item.Element("description")?.Value),
                SourceType = "bing"
            })
            .Where(IsValidResult));
    }

    private async Task<List<WebSearchResult>> SearchBingNewsRssAsync(string query, int topK, CancellationToken ct)
    {
        var url = $"https://www.bing.com/news/search?q={Uri.EscapeDataString(query)}&format=rss&mkt=zh-CN";
        var xml = await _httpClient.GetStringAsync(url, ct);
        var doc = XDocument.Parse(xml);

        return Reindex(doc.Descendants("item")
            .Take(topK)
            .Select(item => new WebSearchResult
            {
                Title = CleanText(item.Element("title")?.Value),
                Url = item.Element("link")?.Value?.Trim() ?? string.Empty,
                Snippet = CleanText(item.Element("description")?.Value),
                SourceType = "bing-news"
            })
            .Where(IsValidResult));
    }

    private async Task<List<WebSearchResult>> SearchDuckDuckGoAsync(string query, int topK, CancellationToken ct)
    {
        var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
        var html = await _httpClient.GetStringAsync(url, ct);
        var results = new List<WebSearchResult>();

        var blocks = Regex.Matches(html, "<div class=\"result[\\s\\S]*?</div>\\s*</div>", RegexOptions.IgnoreCase);
        foreach (Match block in blocks)
        {
            if (results.Count >= topK) break;

            var titleMatch = Regex.Match(block.Value, "<a[^>]*class=\"result__a\"[^>]*href=\"(?<url>[^\"]+)\"[^>]*>(?<title>[\\s\\S]*?)</a>", RegexOptions.IgnoreCase);
            if (!titleMatch.Success) continue;

            var snippetMatch = Regex.Match(block.Value, "<a[^>]*class=\"result__snippet\"[^>]*>(?<snippet>[\\s\\S]*?)</a>", RegexOptions.IgnoreCase);
            var rawUrl = WebUtility.HtmlDecode(titleMatch.Groups["url"].Value);
            results.Add(new WebSearchResult
            {
                Title = CleanText(titleMatch.Groups["title"].Value),
                Url = NormalizeDuckDuckGoUrl(rawUrl),
                Snippet = CleanText(snippetMatch.Groups["snippet"].Value),
                SourceType = "duckduckgo"
            });
        }

        return Reindex(results.Where(IsValidResult));
    }

    private static (string QueryName, string DisplayName) DetectWeatherLocation(string query)
    {
        var known = new Dictionary<string, (string QueryName, string DisplayName)>(StringComparer.OrdinalIgnoreCase)
        {
            ["东京"] = ("Tokyo", "东京"),
            ["東京"] = ("Tokyo", "东京"),
            ["tokyo"] = ("Tokyo", "东京"),
            ["北京"] = ("Beijing", "北京"),
            ["上海"] = ("Shanghai", "上海"),
            ["广州"] = ("Guangzhou", "广州"),
            ["深圳"] = ("Shenzhen", "深圳"),
            ["杭州"] = ("Hangzhou", "杭州"),
            ["成都"] = ("Chengdu", "成都"),
            ["重庆"] = ("Chongqing", "重庆"),
            ["武汉"] = ("Wuhan", "武汉"),
            ["南京"] = ("Nanjing", "南京"),
            ["香港"] = ("Hong Kong", "香港"),
            ["纽约"] = ("New York", "纽约"),
            ["伦敦"] = ("London", "伦敦"),
            ["巴黎"] = ("Paris", "巴黎"),
            ["首尔"] = ("Seoul", "首尔"),
            ["大阪"] = ("Osaka", "大阪")
        };

        foreach (var item in known)
        {
            if (query.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        var match = Regex.Match(query, @"(?:今天|现在|当前)?\s*([\u4e00-\u9fa5A-Za-z\s]{2,30})\s*(?:的)?\s*(?:天气|气温|温度|weather)", RegexOptions.IgnoreCase);
        if (!match.Success) return ("", "");

        var raw = match.Groups[1].Value.Trim();
        raw = Regex.Replace(raw, "今天|现在|当前|查询|请问|帮我|看看", "").Trim();
        return string.IsNullOrWhiteSpace(raw) ? ("", "") : (raw, raw);
    }

    private static string TranslateWeather(string? weather)
    {
        if (string.IsNullOrWhiteSpace(weather)) return "天气状况未知";
        var lower = weather.ToLowerInvariant();
        if (lower.Contains("partly cloudy")) return "局部多云";
        if (lower.Contains("cloudy") || lower.Contains("overcast")) return "多云";
        if (lower.Contains("sunny") || lower.Contains("clear")) return "晴";
        if (lower.Contains("rain")) return "有雨";
        if (lower.Contains("snow")) return "有雪";
        if (lower.Contains("mist") || lower.Contains("fog")) return "有雾";
        return weather;
    }

    private static List<WebSearchResult> MergeResults(List<WebSearchResult> primary, List<WebSearchResult> secondary)
    {
        return Reindex(primary.Concat(secondary)
            .Where(IsValidResult)
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()));
    }

    private static List<WebSearchResult> Reindex(IEnumerable<WebSearchResult> results)
    {
        return results
            .Select((item, index) =>
            {
                item.Index = index + 1;
                return item;
            })
            .ToList();
    }

    private async Task<List<WebSearchResult>> FinalizeResultsAsync(
        string query,
        List<WebSearchResult> results,
        bool enrichPages,
        CancellationToken ct)
    {
        var ranked = ApplyRelevance(query, Reindex(results));
        if (enrichPages)
        {
            await EnrichPageContentAsync(query, ranked, ct);
            ranked = ApplyRelevance(query, ranked);
        }

        return Reindex(ranked
            .OrderByDescending(item => item.IsAuthoritative)
            .ThenByDescending(item => item.HasUsableContent)
            .ThenByDescending(item => item.ContentQuality)
            .ThenByDescending(item => item.Relevance));
    }

    private static List<WebSearchResult> ApplyRelevance(string query, List<WebSearchResult> results)
    {
        foreach (var result in results)
        {
            if (result.IsAuthoritative)
            {
                result.Relevance = 1f;
                result.ContentQuality = 1f;
                result.HasUsableContent = true;
                continue;
            }

            var keywordRelevance = CalculateRelevance(query, $"{result.Title} {result.Snippet}");
            result.Relevance = Math.Clamp(Math.Max(result.Relevance, keywordRelevance), 0f, 1f);
            result.ContentQuality = Math.Clamp(Math.Max(result.ContentQuality, CalculateContentQuality(query, result.Snippet)), 0f, 1f);
            result.HasUsableContent = result.ContentQuality >= 0.18f && CleanText(result.Snippet).Length >= 80;
        }

        return results;
    }

    private async Task EnrichPageContentAsync(string query, List<WebSearchResult> results, CancellationToken ct)
    {
        foreach (var result in results
            .Where(item => !item.IsAuthoritative && IsHttpUrl(item.Url))
            .Take(5))
        {
            try
            {
                var pageText = await FetchReadablePageTextAsync(result.Url, ct);
                if (string.IsNullOrWhiteSpace(pageText)) continue;

                var merged = MergeSnippet(result.Snippet, pageText);
                result.Snippet = Truncate(merged, 900);
                result.ContentQuality = Math.Max(result.ContentQuality, CalculateContentQuality(query, result.Snippet));
                result.HasUsableContent = result.ContentQuality >= 0.18f && result.Snippet.Length >= 80;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to enrich web search result {Url}", result.Url);
            }
        }
    }

    private async Task<string> FetchReadablePageTextAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) return string.Empty;

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (!mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)
            && !mediaType.Contains("text", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var html = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        if (html.Length > 180_000) html = html[..180_000];

        return ExtractReadableText(html);
    }

    private static string ExtractReadableText(string html)
    {
        var candidates = new List<string>();
        candidates.Add(GetMetaContent(html, "description"));
        candidates.Add(GetMetaContent(html, "og:description"));

        foreach (Match match in Regex.Matches(html, "<article[\\s\\S]*?</article>", RegexOptions.IgnoreCase))
        {
            candidates.Add(CleanText(match.Value));
        }

        foreach (Match match in Regex.Matches(html, "<p[^>]*>(?<text>[\\s\\S]*?)</p>", RegexOptions.IgnoreCase).Take(12))
        {
            candidates.Add(CleanText(match.Groups["text"].Value));
        }

        return Truncate(string.Join(" ", candidates.Where(item => item.Length >= 20)), 1200);
    }

    private static string GetMetaContent(string html, string name)
    {
        var pattern = $"<meta[^>]+(?:name|property)=[\"']{Regex.Escape(name)}[\"'][^>]+content=[\"'](?<content>.*?)[\"'][^>]*>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            pattern = $"<meta[^>]+content=[\"'](?<content>.*?)[\"'][^>]+(?:name|property)=[\"']{Regex.Escape(name)}[\"'][^>]*>";
            match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        }

        return match.Success ? CleanText(match.Groups["content"].Value) : string.Empty;
    }

    private static string MergeSnippet(string existing, string pageText)
    {
        var cleanExisting = CleanText(existing);
        var cleanPage = CleanText(pageText);
        if (string.IsNullOrWhiteSpace(cleanExisting)) return cleanPage;
        if (cleanPage.Contains(cleanExisting, StringComparison.OrdinalIgnoreCase)) return cleanPage;
        return $"{cleanExisting} {cleanPage}";
    }

    private static float CalculateContentQuality(string query, string text)
    {
        var cleaned = CleanText(text);
        if (cleaned.Length < 40) return 0f;

        var relevance = CalculateRelevance(query, cleaned);
        var lengthScore = cleaned.Length switch
        {
            >= 260 => 0.45f,
            >= 140 => 0.30f,
            >= 80 => 0.18f,
            _ => 0.05f
        };

        return Math.Clamp(relevance * 0.7f + lengthScore, 0f, 1f);
    }

    private static float CalculateRelevance(string query, string text)
    {
        var queryTokens = ExtractTokens(query).ToList();
        if (queryTokens.Count == 0) return 0.2f;

        var target = text.ToLowerInvariant();
        var hits = queryTokens.Count(token => target.Contains(token, StringComparison.OrdinalIgnoreCase));
        return Math.Clamp((float)hits / queryTokens.Count, 0f, 1f);
    }

    private static IEnumerable<string> ExtractTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;

        foreach (Match match in Regex.Matches(value.ToLowerInvariant(), @"[a-z0-9\.\+#]{2,}|[\u4e00-\u9fa5]{2,}"))
        {
            var token = match.Value.Trim();
            if (token is "今天" or "今日" or "现在" or "当前" or "最新" or "最近" or "什么" or "如何" or "怎么")
            {
                continue;
            }
            yield return token;
        }
    }

    private static bool IsPackageVersionQuestion(string query)
    {
        return Regex.IsMatch(query, "最新版本|最新版|版本号|latest\\s+version|current\\s+version|version", RegexOptions.IgnoreCase);
    }

    private static bool IsNewsQuestion(string query)
    {
        return Regex.IsMatch(query, "新闻|热点|资讯|今日|今天|最新|趋势|动态|news|hot|trend|trending", RegexOptions.IgnoreCase);
    }

    private static string BuildSearchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return query;

        if (Regex.IsMatch(query, "AI|人工智能|大模型|AIGC", RegexOptions.IgnoreCase)
            && IsNewsQuestion(query))
        {
            return $"{query} 人工智能 大模型 今日新闻 最新进展";
        }

        return query;
    }

    private static bool IsValidResult(WebSearchResult item)
    {
        return !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url);
    }

    private static bool IsHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string NormalizeDuckDuckGoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (!url.Contains("uddg=", StringComparison.OrdinalIgnoreCase)) return url;

        var match = Regex.Match(url, @"[?&]uddg=(?<url>[^&]+)", RegexOptions.IgnoreCase);
        return match.Success ? Uri.UnescapeDataString(match.Groups["url"].Value) : url;
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var withoutTags = Regex.Replace(value, "<.*?>", " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private static string GetJsonString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(propertyName, out var property)
               && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static float GetJsonFloat(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return 0f;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetSingle(out var number))
        {
            return Math.Clamp(number, 0f, 1f);
        }

        if (property.ValueKind == JsonValueKind.String && float.TryParse(property.GetString(), out var parsed))
        {
            return Math.Clamp(parsed, 0f, 1f);
        }

        return 0f;
    }

    private static string FirstJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetJsonString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return string.Empty;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
