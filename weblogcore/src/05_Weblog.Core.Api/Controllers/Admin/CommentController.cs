using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/comment")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<CommentDto>>> GetPage([FromBody] CommentPageRequest request)
    {
        var result = await _commentService.GetAdminPageAsync(request);
        return Result<PageDto<CommentDto>>.Ok(result);
    }

    [HttpPost("delete")]
    [RequireRole("admin")]
    public async Task<Result> Delete([FromBody] IdRequest request)
    {
        var result = await _commentService.DeleteAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost("batchDelete")]
    [RequireRole("admin")]
    public async Task<Result> BatchDelete([FromBody] IdsRequest request)
    {
        var result = await _commentService.BatchDeleteAsync(request.Ids);
        return result ? Result.Ok() : Result.Fail("批量删除失败");
    }

    [HttpPost("examine")]
    [RequireRole("admin")]
    public async Task<Result> Examine([FromBody] ExamineRequest request)
    {
        if (request.Status == 2)
        {
            var result = await _commentService.ApproveAsync(request.Id);
            return result ? Result.Ok() : Result.Fail("审核失败");
        }
        else
        {
            var result = await _commentService.RejectAsync(request.Id, request.Reason ?? "");
            return result ? Result.Ok() : Result.Fail("审核失败");
        }
    }

    [HttpPost("secret/list")]
    [RequireRole("admin")]
    public async Task<Result<PageDto<CommentDto>>> GetSecretComments([FromBody] CommentPageRequest request)
    {
        request.IsSecret = true;
        var result = await _commentService.GetAdminPageAsync(request);
        return Result<PageDto<CommentDto>>.Ok(result);
    }

    [HttpPost("secret/reset")]
    [RequireRole("admin")]
    public async Task<Result<bool>> ResetSecret([FromBody] ResetSecretRequest request)
    {
        var result = await _commentService.ResetSecretAsync(request);
        return Result<bool>.Ok(result);
    }
}

public class ExamineRequest
{
    public long Id { get; set; }
    public int Status { get; set; }
    public string? Reason { get; set; }
}
