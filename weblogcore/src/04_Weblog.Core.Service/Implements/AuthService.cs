using BCrypt.Net;
using Weblog.Core.Common.Extensions;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class AuthService : IAuthService
{
    private readonly DbContext _dbContext;
    private readonly string _jwtSecretKey;

    public AuthService(DbContext dbContext)
    {
        _dbContext = dbContext;
        _jwtSecretKey = "Weblog.Core.SecretKey.2024.WeBlog";
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _dbContext.SysUserDb.FirstAsync(it => it.Username == request.Username);
        if (user == null)
        {
            throw new Exception("用户名或密码错误");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            throw new Exception("用户名或密码错误");
        }

        var role = user.Role ?? "admin";
        var token = JwtHelper.CreateToken(user.Id, user.Username, role, _jwtSecretKey);

        return new LoginResponse
        {
            Token = token,
            User = new SysUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = role
            }
        };
    }

    public async Task<SysUserDto> GetCurrentUserAsync(long userId)
    {
        var user = await _dbContext.SysUserDb.FirstAsync(it => it.Id == userId);
        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        return new SysUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role ?? "admin"
        };
    }

    public async Task<bool> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var user = await _dbContext.SysUserDb.FirstAsync(it => it.Id == userId);
        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
        {
            throw new Exception("原密码错误");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        return await _dbContext.Db.Updateable(user).ExecuteCommandAsync() > 0;
    }
}
