using System.Collections.Concurrent;

namespace WO.Property.CommunityService.Tenant;

/// <summary>
/// 租户数据库工厂实现
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

    public void SetCurrentTenantCode(string tenantCode)
    {
        _currentTenantCode.Value = tenantCode;
        _logger.LogDebug("TenantContext set: {TenantCode}", tenantCode);
    }

    public void Clear()
    {
        _currentTenantCode.Value = null;
    }

    public string GetTenantConnectionString(string tenantCode)
    {
        var baseConnStr = _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Default connection string not configured");

        // 项目数据库命名规范：project_{project_code}
        var databaseName = $"project_{tenantCode}";

        var result = System.Text.RegularExpressions.Regex.Replace(
            baseConnStr,
            @"Database\s*=\s*[^;]+",
            $"Database={databaseName}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        _logger.LogDebug("Generated connection string for tenant {TenantCode} -> database {Database}", tenantCode, databaseName);
        return result;
    }
}