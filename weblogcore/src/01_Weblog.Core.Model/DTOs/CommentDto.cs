using System.Text.Json.Serialization;

namespace Weblog.Core.Model.DTOs;

public class CommentDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Nickname { get; set; }
    public string? Mail { get; set; }
    public string? Website { get; set; }
    public string? RouterUrl { get; set; }
    public long? ReplyCommentId { get; set; }
    public long? ParentCommentId { get; set; }
    public int Status { get; set; }
    public string? Reason { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    [JsonPropertyName("childComments")]
    public List<CommentDto>? Children { get; set; }
    public string? ReplyNickname { get; set; }
    public string? Message { get; set; }
    public string? IpAddress { get; set; }
    public string? IpLocation { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public int FlowerCount { get; set; }
    public bool HasCurrentUserFlower { get; set; }
    public string? Images { get; set; }
    public bool IsSecret { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsReset { get; set; }
    public bool IsAdmin { get; set; }
}

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Nickname { get; set; }
    public string? Mail { get; set; }
    public string? Website { get; set; }
    public string? RouterUrl { get; set; }
    public long? ReplyCommentId { get; set; }
    public long? ParentCommentId { get; set; }
    public string? ReplyNickname { get; set; }
    public string? IpAddress { get; set; }
    public string? IpLocation { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? Images { get; set; }
    public bool IsSecret { get; set; }
    public string? SecretContent { get; set; }
    public string? SecretKey { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateCommentRequest
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Reason { get; set; }
}

public class QQUserInfoDto
{
    public string? Nickname { get; set; }
    public string? Avatar { get; set; }
    public string? Mail { get; set; }
}

public class CommentListResultDto
{
    public int Total { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
}

public class CommentPageRequest : PageRequest
{
    public string? Nickname { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public int? Status { get; set; }
    public bool? IsSecret { get; set; }
}

public class FlowerRequest
{
    public long CommentId { get; set; }
    public string UserKey { get; set; } = string.Empty;
    public string? Fingerprint { get; set; }
}

public class FlowerStatusRequest
{
    public List<long> CommentIds { get; set; } = new();
    public string UserKey { get; set; } = string.Empty;
    public string? Fingerprint { get; set; }
}
