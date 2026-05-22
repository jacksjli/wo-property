using System.Text;
using WO.Property.MobileService.Tenant;

namespace WO.Property.MobileService.Middleware;

/// <summary>
/// 租户路由中间件 - 从JWT解析tenant_code并设置租户上下文
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

    public async Task InvokeAsync(HttpContext context, ITenantDbFactory factory)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // 匿名端点不需要租户
        if (IsAnonymousEndpoint(path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            await _next(context);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        try
        {
            var tenantCode = ExtractTenantCodeFromJwt(token);
            if (!string.IsNullOrEmpty(tenantCode))
            {
                factory.SetCurrentTenantCode(tenantCode);
                _logger.LogInformation("Tenant context set: {TenantCode}", tenantCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract tenant code from JWT");
        }

        await _next(context);
    }

    private bool IsAnonymousEndpoint(string path)
    {
        return path == "/" || 
               path.StartsWith("/health") || 
               path.StartsWith("/swagger");
    }

    private string? ExtractTenantCodeFromJwt(string token)
    {
        // JWT格式: header.payload.signature
        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        var payload = parts[1];
        
        // Base64URL解码
        var payloadBytes = Base64UrlDecode(payload);
        var json = Encoding.UTF8.GetString(payloadBytes);
        
        // 简单的字符串匹配提取 tenant_code
        // {"tenant_code":"xxx", ...}
        var startIndex = json.IndexOf("\"tenant_code\"");
        if (startIndex < 0)
            return null;
        
        startIndex = json.IndexOf("\"", startIndex + 13);
        if (startIndex < 0)
            return null;
        
        var endIndex = json.IndexOf("\"", startIndex + 1);
        if (endIndex < 0)
            return null;
        
        return json.Substring(startIndex + 1, endIndex - startIndex - 1);
    }

    private byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        return Convert.FromBase64String(base64);
    }
}