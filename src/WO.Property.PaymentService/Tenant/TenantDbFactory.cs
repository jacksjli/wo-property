using System.Collections.Concurrent;

namespace WO.Property.PaymentService.Tenant;

/// <summary>
/// 租户数据库工厂实现
/// 使用 TenantConfigLoader 从 config/tenant-mapping.json 读取配置
/// </summary>
public class TenantDbFactory : ITenantDbFactory
{
    private readonly TenantConfigLoader _configLoader;
    private readonly ILogger<TenantDbFactory> _logger;
    private static readonly AsyncLocal<string?> _currentTenantCode = new();

    public TenantDbFactory(TenantConfigLoader configLoader, ILogger<TenantDbFactory> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    public string? GetCurrentTenantCode() => _currentTenantCode.Value;

    public void SetCurrentTenantCode(string tenantCode)
    {
        // 验证租户是否存在
        if (!_configLoader.TenantExists(tenantCode))
        {
            _logger.LogWarning("租户 [{TenantCode}] 不存在于配置中", tenantCode);
            throw new InvalidOperationException($"租户 [{tenantCode}] 不存在");
        }

        _currentTenantCode.Value = tenantCode;
        _logger.LogDebug("TenantContext set: {TenantCode}", tenantCode);
    }

    public void Clear()
    {
        _currentTenantCode.Value = null;
    }

    public string GetTenantConnectionString(string tenantCode)
    {
        var connStr = _configLoader.BuildConnectionString(tenantCode);
        _logger.LogDebug("Generated connection string for tenant {TenantCode}", tenantCode);
        return connStr;
    }
}