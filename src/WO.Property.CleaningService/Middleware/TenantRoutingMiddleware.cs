using System.Security.Claims;

namespace WO.Property.CleaningService.Middleware;

/// <summary>
/// 租户路由中间件
/// 优先使用 X-Project header，其次从 JWT 提取 tenant_code/project_code
/// </summary>
public class TenantRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRoutingMiddleware> _logger;

    public TenantRoutingMiddleware(RequestDelegate next, ILogger<TenantRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, WO.Property.CleaningService.Tenant.ITenantDbFactory tenantDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 跳过匿名接口
        if (IsAnonymousEndpoint(path))
        {
            await _next(context);
            return;
        }

        // 优先使用 X-Project header（前端传递），其次从 ClaimsPrincipal 获取
        var tenantCode = context.Request.Headers["X-Project"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantCode) && context.User?.Identity?.IsAuthenticated == true)
        {
            tenantCode = context.User.FindFirst("tenant_code")?.Value
                      ?? context.User.FindFirst("project_code")?.Value;
        }

        if (string.IsNullOrEmpty(tenantCode))
        {
            await _next(context);
            return;
        }

        tenantDbFactory.SetCurrentTenantCode(tenantCode);
        _logger.LogDebug("Tenant routing: {TenantCode}", tenantCode);

        try
        {
            await _next(context);
        }
        finally
        {
            tenantDbFactory.Clear();
        }
    }

    private static bool IsAnonymousEndpoint(string path)
    {
        return path.Contains("/health")
            || path.StartsWith("/swagger");
    }
}
