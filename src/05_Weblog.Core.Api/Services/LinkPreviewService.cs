using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Services;

public class LinkPreviewService : ILinkPreviewService
{
    private readonly DbContext _dbContext;
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkPreviewService> _logger;

    public LinkPreviewService(
        DbContext dbContext,
        IBlogSettingsService blogSettingsService,
        HttpClient httpClient,
        ILogger<LinkPreviewService> logger)
    {
        _dbContext = dbContext;
        _blogSettingsService = blogSettingsService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LinkPreviewDto?> GetPreviewAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!IsUrlAllowed(url))
        {
            _logger.LogInformation("URL不在白名单中: {Url}", url);
            return null;
        }

        var cached = await _dbContext.LinkPreviewCacheDb
            .Where(x => x.Url == url)
            .FirstAsync();

        if (cached != null)
        {
            _logger.LogInformation("从缓存获取链接预览: {Url}", url);
            return new LinkPreviewDto
            {
                Url = cached.Url,
                Title = cached.Title,
                Description = cached.Description,
                ImageUrl = cached.ImageUrl,
                FaviconUrl = cached.FaviconUrl,
                Domain = cached.Domain
            };
        }

        return await FetchAndCacheAsync(url);
    }

    public async Task<LinkPreviewDto?> FetchAndCacheAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!IsUrlAllowed(url))
        {
            _logger.LogInformation("URL不在白名单中: {Url}", url);
            return null;
        }

        try
        {
            var uri = new Uri(url);
            var domain = uri.Host;

            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");

            var response = await _httpClient.GetStringAsync(url);

            var title = ExtractMetaContent(response, "og:title") 
                        ?? ExtractMetaContent(response, "title")
                        ?? ExtractMetaContent(response, "twitter:title")
                        ?? domain;

            var description = ExtractMetaContent(response, "og:description")
                              ?? ExtractMetaContent(response, "description")
                              ?? ExtractMetaContent(response, "twitter:description");

            var imageUrl = ExtractMetaContent(response, "og:image")
                           ?? ExtractMetaContent(response, "twitter:image")
                           ?? ExtractMetaContent(response, "og:image:url");

            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
            {
                imageUrl = new Uri(new Uri(url), imageUrl).ToString();
            }

            var faviconUrl = await GetFaviconUrlAsync(uri, domain);

            var cache = new LinkPreviewCache
            {
                Url = url,
                Title = title,
                Description = description,
                ImageUrl = imageUrl,
                FaviconUrl = faviconUrl,
                Domain = domain,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            await _dbContext.Db.Insertable(cache).ExecuteCommandAsync();

            _logger.LogInformation("获取链接预览成功并已缓存: {Url}, Title: {Title}", url, title);

            return new LinkPreviewDto
            {
                Url = url,
                Title = title,
                Description = description,
                ImageUrl = imageUrl,
                FaviconUrl = faviconUrl,
                Domain = domain
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取链接预览失败: {Url}", url);
            return null;
        }
    }

    public bool IsUrlAllowed(string url)
    {
        try
        {
            var settings = _blogSettingsService.GetAsync().GetAwaiter().GetResult();
            
            if (settings == null)
            {
                _logger.LogWarning("Settings为null，拒绝URL: {Url}", url);
                return false;
            }
            
            if (!settings.IsLinkPreviewOpen)
            {
                _logger.LogInformation("链接预览功能未开启: {Url}", url);
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.LinkPreviewWhitelist))
            {
                _logger.LogInformation("白名单为空，不限制域名: {Url}", url);
                return true;
            }

            var uri = new Uri(url);
            var domain = uri.Host.ToLower();

            var whitelistLines = settings.LinkPreviewWhitelist
                .Split(new[] { '\r', '\n', '、', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            foreach (var line in whitelistLines)
            {
                var cleanLine = line;
                if (line.StartsWith("http://") || line.StartsWith("https://"))
                {
                    try
                    {
                        var uri2 = new Uri(line);
                        cleanLine = uri2.Host.ToLower();
                    }
                    catch
                    {
                        cleanLine = line.Replace("https://", "").Replace("http://", "").Split('/')[0];
                    }
                }

                if (cleanLine.StartsWith("*."))
                {
                    var pattern = cleanLine.Substring(2);
                    if (domain.EndsWith("." + pattern) || domain == pattern)
                    {
                        _logger.LogInformation("泛化匹配成功: {Domain} 匹配 {Pattern}", domain, pattern);
                        return true;
                    }
                }
                else if (domain == cleanLine || domain.EndsWith("." + cleanLine))
                {
                    _logger.LogInformation("精确匹配成功: {Domain} == {Line}", domain, cleanLine);
                    return true;
                }
            }

            _logger.LogInformation("URL不在白名单中: {Domain}", domain);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查URL白名单异常: {Url}", url);
            return false;
        }
    }

    public async Task ClearCacheAsync()
    {
        await _dbContext.Db.Deleteable<LinkPreviewCache>().ExecuteCommandAsync();
        _logger.LogInformation("已清除所有链接预览缓存");
    }

    private string? ExtractMetaContent(string html, string property)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        var patterns = new[]
        {
            $@"<meta[^>]*property\s*=\s*[""']?{Regex.Escape(property)}[""']?[^>]*content\s*=\s*[""']?([^""'>]+)[""']?",
            $@"<meta[^>]*content\s*=\s*[""']?([^""'>]+)[""']?[^>]*property\s*=\s*[""']?{Regex.Escape(property)}[""']?",
            $@"<meta[^>]*name\s*=\s*[""']?{Regex.Escape(property)}[""']?[^>]*content\s*=\s*[""']?([^""'>]+)[""']?",
            $@"<meta[^>]*content\s*=\s*[""']?([^""'>]+)[""']?[^>]*name\s*=\s*[""']?{Regex.Escape(property)}[""']?"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return HttpUtility.HtmlDecode(match.Groups[1].Value.Trim());
            }
        }

        return null;
    }

    private async Task<string?> GetFaviconUrlAsync(Uri baseUri, string domain)
    {
        var faviconUrls = new[]
        {
            $"https://{domain}/favicon.ico",
            $"http://{domain}/favicon.ico",
            $"https://{domain}/apple-touch-icon.png",
            $"https://www.{domain}/favicon.ico"
        };

        foreach (var faviconUrl in faviconUrls)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, faviconUrl);
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return faviconUrl;
                }
            }
            catch
            {
                continue;
            }
        }

        return $"https://{domain}/favicon.ico";
    }
}