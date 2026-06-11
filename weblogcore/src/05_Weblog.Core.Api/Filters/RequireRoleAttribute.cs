using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Weblog.Core.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public RequireRoleAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 从 JWT token 中获取角色
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;

        // 如果用户没有角色，默认设为 admin
        if (string.IsNullOrEmpty(roleClaim))
        {
            roleClaim = "visitor";
        }

        // 检查角色是否在允许的角色列表中
        foreach (var role in _allowedRoles)
        {
            if (role.Equals(roleClaim, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        // 角色不在允许列表中，返回 403
        context.Result = new ObjectResult(new { code = 403, message = "演示账号仅支持查询操作！" })
        {
            StatusCode = 403
        };
    }
}
