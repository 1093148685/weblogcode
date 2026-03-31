using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Weblog.Core.Common.Extensions;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<Result<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Result<LoginResponse>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Fail(ex.Message);
        }
    }

    [HttpPost("admin/user/info")]
    [Authorize]
    public async Task<Result<SysUserDto>> GetCurrentUser()
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _authService.GetCurrentUserAsync(userId);
        return Result<SysUserDto>.Ok(result);
    }

    [HttpPost("admin/password/update")]
    [Authorize]
    public async Task<Result> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.ChangePasswordAsync(userId, request);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
