using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WO.Property.DispatchService.Tenant;

namespace WO.Property.DispatchService.Middleware;

/// <summary>
/// 租户路由中间件
/// 从 JWT/X-Project Header 提取项目信息 → 注入 TenantDbFactory
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

    public async Task InvokeAsync(HttpContext context, ITenantDbFactory tenantDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 跳过匿名接口和健康检查
        if (IsAnonymousEndpoint(path))
        {
            await _next(context);
            return;
        }

        // 优先使用 X-Project header（前端传递），其次 JWT 中的 project_code
        var tenantCode = context.Request.Headers["X-Project"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(tenantCode))
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                tenantCode = ExtractTenantCodeFromJwt(token);
            }
        }

        // 默认使用 wo_property
        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogDebug("No tenant context - using default wo_property");
            tenantCode = "wo_property";
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

    private string? ExtractTenantCodeFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "project_code")?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAnonymousEndpoint(string path)
    {
        return path.Contains("/health")
            || path.StartsWith("/swagger");
    }
}