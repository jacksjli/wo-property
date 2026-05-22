using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

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

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            await _next(context);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        // 优先使用 X-Project header（前端传递），其次 JWT 中的 project_code
        var tenantCode = context.Request.Headers["X-Project"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantCode))
        {
            tenantCode = ExtractTenantCodeFromJwt(token);
        }

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogWarning("JWT does not contain tenant/project code");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid token: missing tenant information"
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
            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            var pad = payload.Length % 4;
            if (pad > 0) payload += new string('=', 4 - pad);

            var payloadBytes = Convert.FromBase64String(payload);
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            
            // 优先尝试 tenant_code（旧格式），其次 project_code（新格式）
            if (doc.RootElement.TryGetProperty("tenant_code", out var tcProp))
                return tcProp.GetString();
            
            if (doc.RootElement.TryGetProperty("project_code", out var pcProp))
                return pcProp.GetString();
            
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
