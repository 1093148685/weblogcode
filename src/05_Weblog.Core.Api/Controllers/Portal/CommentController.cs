using Microsoft.AspNetCore.Mvc;
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
        // 获取真实 IP 地址
        var ipAddress = IpHelper.GetRealIpAddress(Request.Headers, HttpContext.Connection.RemoteIpAddress);
        
        // 获取 User-Agent 并解析
        var userAgent = Request.Headers.UserAgent.ToString();
        var (deviceType, browser, os) = IpHelper.ParseUserAgent(userAgent);
        
        // 解析 IP 属地
        var ipLocation = IpHelper.GetIpLocation(ipAddress);
        
        // 设置到请求中
        request.IpAddress = ipAddress;
        request.DeviceType = deviceType;
        request.Browser = browser;
        request.IpLocation = ipLocation;

        var result = await _commentService.CreateAsync(request);
        return Result<CommentDto>.Ok(result);
    }

    [HttpPost("qq/userInfo")]
    public async Task<Result<QQUserInfoDto>> GetQQUserInfo([FromBody] QQRequest request)
    {
        try
        {
            var qq = request.QQ;
            if (string.IsNullOrWhiteSpace(qq))
            {
                return Result<QQUserInfoDto>.Ok(new QQUserInfoDto());
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            // 使用稳定的 api.oioweb.cn API
            string? nickname = null;
            try
            {
                var apiUrl = $"https://api.oioweb.cn/api/qq/info?qq={qq}";
                var response = await httpClient.GetStringAsync(apiUrl);
                
                // 解析 JSON: {"code":1,"data":{"qq":"123456","name":"昵称","avatar":"url","mail":"123456@qq.com"}}
                if (response.Contains("\"code\":1") || response.Contains("\"code\":200"))
                {
                    // 尝试多种 name 字段模式
                    var patterns = new[] { "\"name\":\"", "\"nickname\":\"" };
                    foreach (var pattern in patterns)
                    {
                        var start = response.IndexOf(pattern);
                        if (start >= 0)
                        {
                            start += pattern.Length;
                            var end = response.IndexOf("\"", start);
                            if (end > start)
                            {
                                nickname = response.Substring(start, end - start);
                                if (!string.IsNullOrWhiteSpace(nickname) && nickname != "null" && nickname != qq)
                                    break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，继续使用备用方案
            }

            // 使用QQ官方头像API
            var avatar = $"https://q1.qlogo.cn/g?b=qq&nk={qq}&s=100";
            var mail = $"{qq}@qq.com";

            return Result<QQUserInfoDto>.Ok(new QQUserInfoDto
            {
                Nickname = nickname ?? qq, // 如果获取不到昵称，返回QQ号
                Avatar = avatar,
                Mail = mail
            });
        }
        catch
        {
            // 忽略错误，返回空
        }
        
        return Result<QQUserInfoDto>.Ok(new QQUserInfoDto());
    }

    [HttpPost("flower")]
    public async Task<Result<bool>> SendFlower([FromBody] FlowerRequest request)
    {
        // 获取用户标识（优先使用IP）
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
}

public class CommentListRequest
{
    public string? RouterUrl { get; set; }
}

public class QQRequest
{
    public string QQ { get; set; } = string.Empty;
}
