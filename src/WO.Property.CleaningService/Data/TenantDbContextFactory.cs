using Microsoft.EntityFrameworkCore;
using WO.Property.CleaningService.Tenant;

namespace WO.Property.CleaningService.Data;

/// <summary>
/// 租户 DbContext 工厂
/// </summary>
public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContextFactory> _logger;
    private readonly DbContextOptions<TenantDbContext> _centerOptions;

    public TenantDbContextFactory(
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContextFactory> logger,
        IConfiguration configuration)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;

        var centerConnectionString = configuration.GetConnectionString("Default")
            ?? "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        _centerOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(centerConnectionString, serverVersion)
            .Options;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogDebug("No tenant context - using default database");
            var opts = new DbContextOptionsBuilder<TenantDbContext>(_centerOptions).Options;
            return new TenantDbContext(opts, _tenantDbFactory, null!);
        }

        var tenantConnectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var opts2 = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(tenantConnectionString, serverVersion)
            .Options;

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantCode}", tenantCode);
        return new TenantDbContext(opts2, _tenantDbFactory, null!);
    }
}