using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/comment-admin")]
public class CommentAdminController : ControllerBase
{
    private readonly ICommentAdminService _commentAdminService;
    private readonly ICommentService _commentService;

    public CommentAdminController(ICommentAdminService commentAdminService, ICommentService commentService)
    {
        _commentAdminService = commentAdminService;
        _commentService = commentService;
    }

    private async Task<(bool isAdmin, string? error)> CheckAdminAuth()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return (false, "未登录");

        if (token.StartsWith("Bearer "))
            token = token.Substring(7);

        var admin = await _commentAdminService.GetByTokenAsync(token);
        if (admin == null)
            return (false, "登录已过期");

        return (true, null);
    }

    [HttpPost("login")]
    public async Task<Result<CommentAdminDto>> Login([FromBody] CommentAdminLoginRequest request)
    {
        try
        {
            var result = await _commentAdminService.LoginAsync(request);
            return Result<CommentAdminDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<CommentAdminDto>.Fail(ex.Message);
        }
    }

    [HttpPost("verify")]
    public async Task<Result<bool>> Verify()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return Result<bool>.Ok(false);

        if (token.StartsWith("Bearer "))
            token = token.Substring(7);

        var result = await _commentAdminService.VerifyTokenAsync(token);
        return Result<bool>.Ok(result);
    }

    [HttpPost("logout")]
    public async Task<Result<bool>> Logout()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return Result<bool>.Ok(true);

        if (token.StartsWith("Bearer "))
            token = token.Substring(7);

        await _commentAdminService.LogoutAsync(token);
        return Result<bool>.Ok(true);
    }

    [HttpGet("info")]
    public async Task<Result<CommentAdminDto>> GetInfo()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return Result<CommentAdminDto>.Fail("未登录");

        if (token.StartsWith("Bearer "))
            token = token.Substring(7);

        var result = await _commentAdminService.GetByTokenAsync(token);
        if (result == null)
            return Result<CommentAdminDto>.Fail("登录已过期");

        return Result<CommentAdminDto>.Ok(result);
    }

    [HttpPost("reset-secret")]
    public async Task<Result<bool>> ResetSecret([FromBody] ResetSecretRequest request)
    {
        var (isAdmin, error) = await CheckAdminAuth();
        if (!isAdmin)
            return Result<bool>.Fail(error ?? "无权操作");

        try
        {
            var result = await _commentService.ResetSecretAsync(request);
            return Result<bool>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
}
