using System.Collections.Concurrent;
using MySqlConnector;

namespace WO.Property.DeviceService.Tenant;

/// <summary>
/// 租户数据库工厂（简化版 - 单租户单数据库）
/// </summary>
public class TenantDbFactory : ITenantDbFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantDbFactory> _logger;
    private static readonly AsyncLocal<string?> _currentTenantCode = new();

    public TenantDbFactory(IConfiguration configuration, ILogger<TenantDbFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetCurrentTenantCode() => _currentTenantCode.Value;

    public void SetCurrentTenantCode(string? tenantCode)
    {
        _currentTenantCode.Value = tenantCode;
        _logger.LogDebug("TenantContext set: {TenantCode}", tenantCode);
    }

    public void Clear()
    {
        _currentTenantCode.Value = null;
    }

    public string GetTenantConnectionString(string projectCode)
    {
        // 单租户模式，直接使用 wo_property 数据库
        var baseConnStr = _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Default connection string not configured");

        var result = System.Text.RegularExpressions.Regex.Replace(
            baseConnStr,
            @"Database\s*=\s*[^;]+",
            "Database=wo_property",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        _logger.LogDebug("Generated connection string for project {ProjectCode} -> database wo_property", projectCode);
        return result;
    }
}