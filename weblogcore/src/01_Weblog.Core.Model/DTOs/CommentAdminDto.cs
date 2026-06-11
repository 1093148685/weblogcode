namespace Weblog.Core.Model.DTOs;

public class CommentAdminLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CommentAdminDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Token { get; set; }
    public DateTime? TokenExpireTime { get; set; }
}

public class VerifySecretRequest
{
    public long CommentId { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string Captcha { get; set; } = string.Empty;
    public string CaptchaId { get; set; } = string.Empty;
}

public class CaptchaResponse
{
    public string CaptchaId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
}

public class SecretContentResponse
{
    public string Content { get; set; } = string.Empty;
}

public class ResetSecretRequest
{
    public long CommentId { get; set; }
    public string? NewSecretContent { get; set; }
    public string? NewSecretKey { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
