using System.Text;

namespace WO.Property.DeviceService.Middleware;

/// <summary>
/// 租户路由中间件
/// 从 JWT Base64URL 解码提取 tenant_code → 注入 TenantDbFactory
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

    public async Task InvokeAsync(HttpContext context, WO.Property.DeviceService.Tenant.ITenantDbFactory tenantDbFactory)
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

        // 优先使用 X-Project header（前端传递），其次 JWT 中的 project_code
        var tenantCode = context.Request.Headers["X-Project"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantCode))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            tenantCode = ExtractTenantCodeFromJwt(token);
        }

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

    /// <summary>
    /// 直接 Base64URL 解码 JWT payload，提取 tenant_code
    /// 不使用 JwtSecurityTokenHandler（会丢失非标准 claim）
    /// </summary>
    private string? ExtractTenantCodeFromJwt(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            // Base64URL decode
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));

            // 简单解析 JSON 提取 tenant_code
            // 格式: {"tenant_code":"xxx",...}
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tenant_code", out var tenantCodeElement))
            {
                return tenantCodeElement.GetString();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JWT token");
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }

    private static bool IsAnonymousEndpoint(string path)
    {
        return path.Contains("/health")
            || path.StartsWith("/swagger")
            || path == "/"
            || path.StartsWith("/api/device-categories")
            || path.StartsWith("/api/locations");
    }
}