using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.PaymentService.Middleware;

/// <summary>
/// 租户/项目路由中间件
/// 优先级：X-Project Header > JWT tenant_code > JWT project_code > 默认租户
/// </summary>
public class TenantRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRoutingMiddleware> _logger;

    // 不需要认证的路径
    private static readonly string[] AnonymousPaths = new[]
    {
        "/health",
        "/swagger",
        "/api/auth/login",
        "/api/auth/register"
    };

    public TenantRoutingMiddleware(RequestDelegate next, ILogger<TenantRoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, WO.Property.PaymentService.Tenant.ITenantDbFactory tenantDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 跳过匿名接口
        if (IsAnonymousEndpoint(path))
        {
            await _next(context);
            return;
        }

        string? tenantCode = null;

        // 1. 优先从 X-Project Header 获取（单租户多项目架构）
        var xProject = context.Request.Headers["X-Project"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xProject))
        {
            tenantCode = xProject;
            _logger.LogDebug("Project routing via X-Project: {ProjectCode}", tenantCode);
        }

        // 2. 否则从 JWT 获取
        if (string.IsNullOrEmpty(tenantCode))
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                tenantCode = ExtractTenantCodeFromJwt(token) ?? ExtractProjectCodeFromJwt(token);

                if (!string.IsNullOrEmpty(tenantCode))
                {
                    _logger.LogDebug("Project routing via JWT: {ProjectCode}", tenantCode);
                }
            }
        }

        // 3. 如果都没有，使用默认租户（兼容旧系统）
        if (string.IsNullOrEmpty(tenantCode))
        {
            tenantCode = "wo_property";
            _logger.LogDebug("Using default tenant: {TenantCode}", tenantCode);
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

    private string? ExtractTenantCodeFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_code")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JWT token for tenant_code");
            return null;
        }
    }

    private string? ExtractProjectCodeFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "project_code")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAnonymousEndpoint(string path)
    {
        foreach (var p in AnonymousPaths)
        {
            if (path.Contains(p.ToLower())) return true;
        }
        return false;
    }
}