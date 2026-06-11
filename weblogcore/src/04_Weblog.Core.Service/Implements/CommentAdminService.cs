using Weblog.Core.Common.Helpers;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class CommentAdminService : ICommentAdminService
{
    private readonly DbContext _dbContext;

    public CommentAdminService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CommentAdminDto> LoginAsync(CommentAdminLoginRequest request)
    {
        var admin = await _dbContext.Db.Queryable<CommentAdmin>()
            .Where(it => it.Username == request.Username)
            .FirstAsync();

        if (admin == null)
            throw new Exception("用户名或密码错误");

        var passwordHash = AesUtil.HashSha256(request.Password);
        if (admin.PasswordHash != passwordHash)
            throw new Exception("用户名或密码错误");

        var token = Guid.NewGuid().ToString("N");
        var expireTime = DateTime.Now.AddDays(7);

        admin.Token = token;
        admin.TokenExpireTime = expireTime;
        admin.UpdatedAt = DateTime.Now;
        await _dbContext.Db.Updateable(admin).ExecuteCommandAsync();

        return new CommentAdminDto
        {
            Id = admin.Id,
            Username = admin.Username,
            Nickname = admin.Nickname,
            Token = token,
            TokenExpireTime = expireTime
        };
    }

    public async Task<bool> VerifyTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        var admin = await _dbContext.Db.Queryable<CommentAdmin>()
            .Where(it => it.Token == token && it.TokenExpireTime > DateTime.Now)
            .FirstAsync();

        return admin != null;
    }

    public async Task<bool> LogoutAsync(string token)
    {
        var admin = await _dbContext.Db.Queryable<CommentAdmin>()
            .Where(it => it.Token == token)
            .FirstAsync();

        if (admin == null)
            return false;

        admin.Token = null;
        admin.TokenExpireTime = null;
        admin.UpdatedAt = DateTime.Now;
        await _dbContext.Db.Updateable(admin).ExecuteCommandAsync();
        return true;
    }

    public async Task<CommentAdminDto?> GetByTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var admin = await _dbContext.Db.Queryable<CommentAdmin>()
            .Where(it => it.Token == token && it.TokenExpireTime > DateTime.Now)
            .FirstAsync();

        if (admin == null)
            return null;

        return new CommentAdminDto
        {
            Id = admin.Id,
            Username = admin.Username,
            Nickname = admin.Nickname,
            Token = admin.Token,
            TokenExpireTime = admin.TokenExpireTime
        };
    }
}
