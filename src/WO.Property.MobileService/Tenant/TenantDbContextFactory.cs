using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WO.Property.MobileService.Data;

namespace WO.Property.MobileService.Tenant;

/// <summary>
/// 租户数据库工厂实现
/// </summary>
public class TenantDbContextFactory : ITenantDbFactory, IDbContextFactory<TenantDbContext>
{
    private string? _currentTenantCode;
    private readonly string _defaultConnectionString;
    private readonly string _tenantDbConnectionStringTemplate;
    private readonly ILogger<TenantDbContextFactory> _logger;

    public TenantDbContextFactory(IConfiguration configuration, ILogger<TenantDbContextFactory> logger)
    {
        _defaultConnectionString = configuration["ConnectionStrings:Default"]!;
        _tenantDbConnectionStringTemplate = configuration["ConnectionStrings:TenantDb"]!;
        _logger = logger;
    }

    public string GetTenantConnectionString()
    {
        if (string.IsNullOrEmpty(_currentTenantCode))
            return _defaultConnectionString;
        
        return _tenantDbConnectionStringTemplate.Replace("{db_name}", $"wo_tenant_{_currentTenantCode}");
    }

    public TenantDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(
            GetTenantConnectionString(), 
            ServerVersion.AutoDetect(GetTenantConnectionString()));
        return new TenantDbContext(optionsBuilder.Options);
    }

    // ITenantDbFactory 实现
    public void SetCurrentTenantCode(string? tenantCode)
    {
        _logger.LogDebug("Setting tenant code: {TenantCode}", tenantCode ?? "null");
        _currentTenantCode = tenantCode;
    }

    public string? GetCurrentTenantCode() => _currentTenantCode;

    public void Clear()
    {
        _logger.LogDebug("Clearing tenant context");
        _currentTenantCode = null;
    }
}