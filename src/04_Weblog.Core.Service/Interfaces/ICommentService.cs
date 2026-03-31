using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface ICommentService
{
    Task<CommentDto> CreateAsync(CreateCommentRequest request);
    Task<CommentDto> UpdateAsync(UpdateCommentRequest request);
    Task<bool> DeleteAsync(long id);
    Task<bool> BatchDeleteAsync(List<long> ids);
    Task<CommentDto> GetByIdAsync(long id);
    Task<PageDto<CommentDto>> GetAdminPageAsync(CommentPageRequest request);
    Task<List<CommentDto>> GetPortalListAsync(string? routerUrl);
    Task<bool> ApproveAsync(long id);
    Task<bool> RejectAsync(long id, string reason);
    Task<bool> SendFlowerAsync(long commentId, string userKey);
    Task<bool> CancelFlowerAsync(long commentId, string userKey);
    Task<Dictionary<long, bool>> GetFlowerStatusAsync(List<long> commentIds, string userKey);
    Task<CaptchaResponse> GetCaptchaAsync();
    Task<SecretContentResponse> VerifySecretAsync(VerifySecretRequest request);
    Task<bool> ResetSecretAsync(ResetSecretRequest request);
}
