using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace WO.Property.ExpressService.Middleware;

/// <summary>
/// 租户路由中间件
/// 优先级：X-Project header > JWT project_code
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

    public async Task InvokeAsync(HttpContext context, WO.Property.ExpressService.Tenant.ITenantDbFactory tenantDbFactory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 跳过匿名接口
        if (IsAnonymousEndpoint(path))
        {
            await _next(context);
            return;
        }

        string? tenantCode = null;

        // 优先使用 X-Project header
        var xProjectHeader = context.Request.Headers["X-Project"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xProjectHeader))
        {
            tenantCode = xProjectHeader;
            _logger.LogDebug("Tenant from X-Project header: {TenantCode}", tenantCode);
        }

        // 备选：从 JWT 提取
        if (string.IsNullOrEmpty(tenantCode))
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                tenantCode = ExtractTenantCodeFromJwt(token);
            }
        }

        // 默认租户
        if (string.IsNullOrEmpty(tenantCode))
        {
            tenantCode = "wo_property";
        }

        // 映射到数据库名：X-Project 直接用，JWT project_code 加前缀
        var dbName = tenantCode.StartsWith("project_") ? tenantCode : $"project_{tenantCode}";
        tenantDbFactory.SetCurrentTenantCode(dbName);

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
