using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Weblog.Core.Common.Helpers;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/comment")]
public class CommentPortalController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentPortalController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost("list")]
    public async Task<Result<CommentListResultDto>> GetList([FromBody] CommentListRequest request)
    {
        var result = await _commentService.GetPortalListAsync(request.RouterUrl);
        return Result<CommentListResultDto>.Ok(new CommentListResultDto
        {
            Total = result.Count,
            Comments = result
        });
    }

    [HttpPost("publish")]
    public async Task<Result<CommentDto>> Create([FromBody] CreateCommentRequest request)
    {
        var ipAddress = IpHelper.GetRealIpAddress(Request.Headers, HttpContext.Connection.RemoteIpAddress);
        var userAgent = Request.Headers.UserAgent.ToString();
        var (deviceType, browser, _) = IpHelper.ParseUserAgent(userAgent);
        var ipLocation = IpHelper.GetIpLocation(ipAddress);

        request.IpAddress = ipAddress;
        request.DeviceType = deviceType;
        request.Browser = browser;
        request.IpLocation = ipLocation;
        request.Website = NormalizeWebsite(request.Website);

        if (IsQQNumber(request.Nickname))
        {
            var qqInfo = await ResolveQQUserInfoAsync(request.Nickname!);
            request.Nickname = string.IsNullOrWhiteSpace(qqInfo.Nickname) ? GenerateGuestName() : qqInfo.Nickname;
            request.Avatar = string.IsNullOrWhiteSpace(request.Avatar) ? qqInfo.Avatar : request.Avatar;
            request.Mail = string.IsNullOrWhiteSpace(request.Mail) ? qqInfo.Mail : request.Mail;
        }
        else if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            request.Nickname = GenerateGuestName();
        }

        var result = await _commentService.CreateAsync(request);
        return Result<CommentDto>.Ok(result);
    }

    [HttpPost("qq/userInfo")]
    public async Task<Result<QQUserInfoDto>> GetQQUserInfo([FromBody] QQRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.QQ))
            {
                return Result<QQUserInfoDto>.Ok(new QQUserInfoDto());
            }

            return Result<QQUserInfoDto>.Ok(await ResolveQQUserInfoAsync(request.QQ));
        }
        catch
        {
            return Result<QQUserInfoDto>.Ok(new QQUserInfoDto());
        }
    }

    [HttpPost("flower")]
    public async Task<Result<bool>> SendFlower([FromBody] FlowerRequest request)
    {
        var userKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? request.UserKey;
        var result = await _commentService.SendFlowerAsync(request.CommentId, userKey);
        return Result<bool>.Ok(result);
    }

    [HttpPost("unflower")]
    public async Task<Result<bool>> CancelFlower([FromBody] FlowerRequest request)
    {
        var userKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? request.UserKey;
        var result = await _commentService.CancelFlowerAsync(request.CommentId, userKey);
        return Result<bool>.Ok(result);
    }

    [HttpPost("flower/status")]
    public async Task<Result<Dictionary<long, bool>>> GetFlowerStatus([FromBody] FlowerStatusRequest request)
    {
        var userKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? request.UserKey;
        var result = await _commentService.GetFlowerStatusAsync(request.CommentIds, userKey);
        return Result<Dictionary<long, bool>>.Ok(result);
    }

    [HttpGet("captcha")]
    public async Task<Result<CaptchaResponse>> GetCaptcha()
    {
        var result = await _commentService.GetCaptchaAsync();
        return Result<CaptchaResponse>.Ok(result);
    }

    [HttpPost("verify-secret")]
    public async Task<Result<SecretContentResponse>> VerifySecret([FromBody] VerifySecretRequest request)
    {
        try
        {
            var result = await _commentService.VerifySecretAsync(request);
            return Result<SecretContentResponse>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<SecretContentResponse>.Fail(ex.Message);
        }
    }

    private static bool IsQQNumber(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
    }

    private static string GenerateGuestName()
    {
        var names = new[]
        {
            "路过的风", "山海访客", "星河旅人", "云边来客", "代码旅人",
            "清晨来信", "晚风同学", "蓝色便签", "小小宇宙", "温柔访客"
        };
        return $"{names[Random.Shared.Next(names.Length)]}{Random.Shared.Next(100, 999)}";
    }

    private static string? NormalizeWebsite(string? website)
    {
        if (string.IsNullOrWhiteSpace(website)) return null;
        var value = website.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }
        return $"https://{value}";
    }

    private static async Task<QQUserInfoDto> ResolveQQUserInfoAsync(string qq)
    {
        qq = qq.Trim();
        var nickname = await TryGetQQNicknameFromQzoneAsync(qq) ?? await TryGetQQNicknameFromFallbackAsync(qq);
        return new QQUserInfoDto
        {
            Nickname = string.IsNullOrWhiteSpace(nickname) || nickname == qq ? GenerateGuestName() : nickname,
            Avatar = $"https://q1.qlogo.cn/g?b=qq&nk={qq}&s=100",
            Mail = $"{qq}@qq.com"
        };
    }

    private static async Task<string?> TryGetQQNicknameFromQzoneAsync(string qq)
    {
        try
        {
            using var httpClient = CreateQQHttpClient();
            var bytes = await httpClient.GetByteArrayAsync($"https://r.qzone.qq.com/fcg-bin/cgi_get_portrait.fcg?uins={qq}");
            var response = DecodeQQResponse(bytes);
            var marker = $"\"{qq}\":[";
            var start = response.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;
            start = response.IndexOf('[', start) + 1;
            var end = response.IndexOf(']', start);
            if (end <= start) return null;
            using var doc = JsonDocument.Parse($"[{response.Substring(start, end - start)}]");
            var values = doc.RootElement.EnumerateArray().ToArray();
            return values.Length > 6 ? CleanQQNickname(values[6].GetString()) : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> TryGetQQNicknameFromFallbackAsync(string qq)
    {
        try
        {
            using var httpClient = CreateQQHttpClient();
            var response = await httpClient.GetStringAsync($"https://api.oioweb.cn/api/qq/info?qq={qq}");
            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.TryGetProperty("data", out var data)) return null;
            if (data.TryGetProperty("name", out var name)) return CleanQQNickname(name.GetString());
            if (data.TryGetProperty("nickname", out var nickname)) return CleanQQNickname(nickname.GetString());
        }
        catch
        {
        }
        return null;
    }

    private static HttpClient CreateQQHttpClient()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        return httpClient;
    }

    private static string DecodeQQResponse(byte[] bytes)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding("GB18030").GetString(bytes);
        }
        catch
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }

    private static string? CleanQQNickname(string? nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return null;
        var value = nickname.Trim();
        return value.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : value;
    }
}

public class CommentListRequest
{
    public string? RouterUrl { get; set; }
}

public class QQRequest
{
    public string QQ { get; set; } = string.Empty;
}
