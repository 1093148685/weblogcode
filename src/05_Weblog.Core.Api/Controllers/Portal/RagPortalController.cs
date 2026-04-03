using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Service.AI.Rag;

namespace Weblog.Core.Api.Controllers.Portal;

/// <summary>前台公开知识库接口（无需登录）</summary>
[Route("api/portal/rag")]
[ApiController]
[AllowAnonymous]
public class RagPortalController : ControllerBase
{
    private readonly IRagService _rag;

    public RagPortalController(IRagService rag)
    {
        _rag = rag;
    }

    /// <summary>获取已启用的知识库列表（供前台聊天选择）</summary>
    [HttpGet("knowledge-bases")]
    public async Task<Result<List<PublicKbItem>>> GetPublicKbList()
    {
        var kbs = await _rag.GetAllKbsAsync();
        var items = kbs
            .Where(k => k.IsEnabled)
            .Select(k => new PublicKbItem
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description
            }).ToList();
        return Result<List<PublicKbItem>>.Ok(items);
    }
}

public class PublicKbItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
