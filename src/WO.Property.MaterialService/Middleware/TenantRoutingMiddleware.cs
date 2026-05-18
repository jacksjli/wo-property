using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace WO.Property.MaterialService.Middleware;

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

    public async Task InvokeAsync(HttpContext context, WO.Property.MaterialService.Tenant.ITenantDbFactory tenantDbFactory)
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
        
        // 直接解析 JWT payload (bypass claims mapping which strips non-standard claims)
        var tenantCode = ExtractTenantCodeFromJwt(token);

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogWarning("JWT does not contain tenant_code claim");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid token: missing tenant information"
            });
            return;
        }

        _logger.LogWarning("[INSPECTION MW] extracted tenantCode: {TC}, setting...", tenantCode ?? "NULL");
        tenantDbFactory.SetCurrentTenantCode(tenantCode);

        try
        {
            _logger.LogWarning("[INSPECTION MW] after SetCurrent, GetCurrent: {TC}", tenantDbFactory.GetCurrentTenantCode() ?? "NULL");
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
            // JWT payload is the second segment (index 1)
            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            // Convert Base64URL to Base64
            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding if needed
            var pad = payload.Length % 4;
            if (pad > 0) payload += new string('=', 4 - pad);

            var payloadBytes = Convert.FromBase64String(payload);
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            // Parse JSON to find tenant_code
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("tenant_code", out var tcProp))
                return tcProp.GetString();
            
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