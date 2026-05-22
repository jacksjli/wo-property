using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using WO.Property.DeviceService.Tenant;

namespace WO.Property.DeviceService.Data;

/// <summary>
/// 租户数据库上下文工厂
/// </summary>
public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContextFactory> _logger;
    private readonly DbContextOptions<TenantDbContext> _defaultOptions;

    public TenantDbContextFactory(
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContextFactory> logger,
        IConfiguration configuration)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;

        var defaultConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Default connection string not configured");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        _defaultOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(defaultConnectionString, serverVersion)
            .Options;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogDebug("No tenant context - using default database");
            return new TenantDbContext(_defaultOptions, _tenantDbFactory, null!);
        }

        var tenantConnectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var tenantOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(tenantConnectionString, serverVersion)
            .Options;

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantCode}", tenantCode);
        return new TenantDbContext(tenantOptions, _tenantDbFactory, null!);
    }
}