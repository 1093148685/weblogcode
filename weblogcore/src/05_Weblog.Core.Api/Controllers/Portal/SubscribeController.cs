using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Helpers;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/subscribe")]
public class SubscribeController : ControllerBase
{
    private readonly ISubscribeService _subscribeService;

    public SubscribeController(ISubscribeService subscribeService)
    {
        _subscribeService = subscribeService;
    }

    [HttpPost]
    public async Task<Result<bool>> Subscribe([FromBody] SubscribeRequest request)
    {
        try
        {
            var ipAddress = IpHelper.GetRealIpAddress(Request.Headers, HttpContext.Connection.RemoteIpAddress);
            await _subscribeService.SubscribeAsync(request.Email, ipAddress);
            return Result<bool>.Ok(true, "订阅成功，请查收确认邮件");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
}
