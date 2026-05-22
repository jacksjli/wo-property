using System.Security.Claims;
using WO.Property.ProjectTrackingService.Tenant;

namespace WO.Property.ProjectTrackingService.Middleware;

public class TenantRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRoutingMiddleware> _logger;

    public TenantRoutingMiddleware(RequestDelegate next, ILogger<TenantRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantDbFactory tenantDbFactory)
    {
        // 优先使用 X-Project header
        var projectCode = context.Request.Headers["X-Project"].FirstOrDefault();

        // 备选：从 JWT token 中获取
        if (string.IsNullOrEmpty(projectCode))
        {
            projectCode = context.User?.Claims?.FirstOrDefault(c => c.Type == "project_code")?.Value;
        }

        // 备选：从 query string
        if (string.IsNullOrEmpty(projectCode))
        {
            projectCode = context.Request.Query["projectCode"].FirstOrDefault();
        }

        // 默认项目
        if (string.IsNullOrEmpty(projectCode))
        {
            projectCode = "wo_property";
        }

        _logger.LogDebug("TenantRouting: Using project {ProjectCode}", projectCode);

        // 将 tenant info 存入 HttpContext items
        context.Items["TenantId"] = projectCode;
        context.Items["TenantDbFactory"] = tenantDbFactory;

        await _next(context);
    }
}

public static class TenantRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantRouting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantRoutingMiddleware>();
    }
}
