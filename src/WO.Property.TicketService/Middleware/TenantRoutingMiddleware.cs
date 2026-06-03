using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.TicketService.Middleware;

/// <summary>
/// 租户路由中间件
/// 从 JWT 提取 tenant_code → 注入 TenantDbFactory
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

    public async Task InvokeAsync(HttpContext context, WO.Property.TicketService.Tenant.ITenantDbFactory tenantDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 跳过匿名接口
        if (IsAnonymousEndpoint(path))
        {
            // 匿名接口也使用默认租户
            tenantDbFactory.SetCurrentTenantCode("wo_property");
            _logger.LogDebug("Anonymous endpoint, using default tenant");
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            // 无认证 → 走默认租户（仅限匿名接口/health等）
            _logger.LogDebug("No auth header, using default tenant");
            tenantDbFactory.SetCurrentTenantCode("wo_property");
            await _next(context);
            return;
        }

        // 有认证请求：X-Project header 必须存在
        var tenantCode = context.Request.Headers["X-Project"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogWarning("Authenticated request without X-Project header — rejected");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Missing X-Project header. Authenticated requests must specify the project."
            });
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

    private string? ExtractTenantCodeFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            // 优先检查 project_code（Phase 1+），兼容 tenant_code（Phase 0）
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "project_code")?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
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