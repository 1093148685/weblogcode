using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface ICommentAdminService
{
    Task<CommentAdminDto> LoginAsync(CommentAdminLoginRequest request);
    Task<bool> VerifyTokenAsync(string token);
    Task<bool> LogoutAsync(string token);
    Task<CommentAdminDto?> GetByTokenAsync(string token);
}
