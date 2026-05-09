using YARP.Gateway.RateLimiting;

namespace YARP.Gateway.Middleware;

/// <summary>
/// 项目隔离中间件
/// - 从请求头或 JWT 中提取 ProjectCode
/// - 注入 X-Project-Code 下游 Header
/// - 实施基于路由的限流
/// </summary>
public class ProjectIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProjectIsolationMiddleware> _logger;
    private readonly IRateLimiter _rateLimiter;

    public ProjectIsolationMiddleware(
        RequestDelegate next,
        ILogger<ProjectIsolationMiddleware> logger,
        IRateLimiter rateLimiter)
    {
        _next = next;
        _logger = logger;
        _rateLimiter = rateLimiter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // 跳过健康检查和 Swagger
        if (path.StartsWith("/health") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // 1. 提取 ProjectCode（优先从 Header，fallback 到 JWT claim）
        var projectCode = ExtractProjectCode(context);

        // 2. 注入下游 Header（下游服务无需自行解析）
        if (!string.IsNullOrEmpty(projectCode))
        {
            context.Request.Headers["X-Project-Code"] = projectCode;
            context.Items["ProjectCode"] = projectCode;
        }

        // 3. 限流检查
        var clientId = GetClientId(context); // IP + UserId 组合
        var routePath = DetermineRateLimitTier(context);

        if (!_rateLimiter.AllowRequest(clientId, routePath))
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId} on route {Route}",
                clientId, routePath);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "请求过于频繁，请稍后再试",
                retryAfter = 60
            });
            return;
        }

        await _next(context);
    }

    private static string? ExtractProjectCode(HttpContext context)
    {
        // 优先从显式 Header 提取
        if (context.Request.Headers.TryGetValue("X-Project-Code", out var header))
            return header.FirstOrDefault();

        // 从 JWT claim 提取（如果有认证）
        var projectClaim = context.User?.FindFirst("ProjectCode")
            ?? context.User?.FindFirst("project_code");
        return projectClaim?.Value;
    }

    private static string GetClientId(HttpContext context)
    {
        // 优先使用登录用户 ID
        var userId = context.User?.FindFirst("sub")
            ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId?.Value))
            return $"user:{userId.Value}";

        // Fallback 到客户端 IP
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    private static string DetermineRateLimitTier(HttpContext context)
    {
        // 根据路由确定限流层级
        var path = context.Request.Path.Value?.ToLower() ?? "";

        return path switch
        {
            var p when p.Contains("/auth/") => "high",
            var p when p.Contains("/tickets") || p.Contains("/persons") || p.Contains("/master") => "medium",
            _ => "low"
        };
    }
}

public static class ProjectIsolationMiddlewareExtensions
{
    public static IApplicationBuilder UseProjectIsolation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ProjectIsolationMiddleware>();
    }
}
