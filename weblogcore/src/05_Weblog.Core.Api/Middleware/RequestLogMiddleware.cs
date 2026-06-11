namespace Weblog.Core.Api.Middleware;

public class RequestLogMiddleware
{
    private readonly ILogger<RequestLogMiddleware> _logger;
    private readonly RequestDelegate _next;

    public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var request = context.Request;
        var traceId = context.TraceIdentifier;

        _logger.LogInformation(
            "Request started {TraceId} {Method} {Path}{QueryString} ContentLength={ContentLength} ContentType={ContentType} RemoteIp={RemoteIp}",
            traceId,
            request.Method,
            request.Path,
            request.QueryString,
            request.ContentLength,
            request.ContentType,
            context.Connection.RemoteIpAddress);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            _logger.LogError(
                ex,
                "Request failed {TraceId} {Method} {Path} StatusCode={StatusCode} ElapsedMs={ElapsedMs:F2}",
                traceId,
                request.Method,
                request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            _logger.LogInformation(
                "Request finished {TraceId} {Method} {Path} StatusCode={StatusCode} ElapsedMs={ElapsedMs:F2}",
                traceId,
                request.Method,
                request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds);
        }
    }
}
