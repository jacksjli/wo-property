using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.CommunityService.Middleware;

/// <summary>
/// 租户路由中间件
/// 优先级：X-Project Header > JWT tenant_code > JWT project_code
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

    public async Task InvokeAsync(HttpContext context, WO.Property.CommunityService.Tenant.ITenantDbFactory tenantDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // 跳过匿名接口
        if (IsAnonymousEndpoint(path))
        {
            await _next(context);
            return;
        }

        var tenantCode = ExtractTenantCode(context);

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogWarning("Cannot determine tenant code from JWT or X-Project header");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid token: missing tenant information"
            });
            return;
        }

        tenantDbFactory.SetCurrentTenantCode(tenantCode);
        
        try
        {
            await _next(context);
        }
        finally
        {
            tenantDbFactory.Clear();
        }
    }

    private string? ExtractTenantCode(HttpContext context)
    {
        // 方式1: 优先从 X-Project header 获取 (Phase 1 单租户多项目)
        var xProject = context.Request.Headers["X-Project"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xProject))
        {
            _logger.LogDebug("Project routing via X-Project header: {ProjectCode}", xProject);
            return xProject;
        }

        // 方式2: 从 JWT claims 获取
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var tenantCode = ExtractTenantCodeFromJwt(token);
            if (!string.IsNullOrEmpty(tenantCode))
            {
                return tenantCode;
            }
        }

        return null;
    }

    private string? ExtractTenantCodeFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Try tenant_code first
            var tenantCode = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
            if (!string.IsNullOrEmpty(tenantCode))
            {
                return tenantCode;
            }
            
            // Try project_code
            var projectCode = jwtToken.Claims.FirstOrDefault(c => c.Type == "project_code")?.Value;
            if (!string.IsNullOrEmpty(projectCode))
            {
                return projectCode;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JWT token");
            return null;
        }
    }

    private static bool IsAnonymousEndpoint(string path)
    {
        return path.Contains("/health")
            || path.StartsWith("/swagger");
    }
}
