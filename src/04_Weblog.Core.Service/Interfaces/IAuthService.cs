using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<SysUserDto> GetCurrentUserAsync(long userId);
    Task<bool> ChangePasswordAsync(long userId, ChangePasswordRequest request);
}
