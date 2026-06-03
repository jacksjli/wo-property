using Microsoft.EntityFrameworkCore;
using WO.Property.DispatchService.Tenant;
using Microsoft.Extensions.Logging;

namespace WO.Property.DispatchService.Data;

/// <summary>
/// 租户 DbContext 工厂
/// </summary>
public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DbContextOptions<TenantDbContext> _defaultOptions;

    public TenantDbContextFactory(
        ITenantDbFactory tenantDbFactory,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _tenantDbFactory = tenantDbFactory;
        _loggerFactory = loggerFactory;

        var defaultConnectionString = configuration.GetConnectionString("Default")
            ?? "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        _defaultOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(defaultConnectionString, serverVersion)
            .Options;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
        Console.WriteLine($"[TenantDbContextFactory] CreateDbContext called, tenantCode={tenantCode}");

        if (string.IsNullOrEmpty(tenantCode))
        {
            Console.WriteLine("No tenant context - using default wo_property database");
            tenantCode = "wo_property";
        }

        var tenantConnectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var dbOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(tenantConnectionString, serverVersion)
            .Options;

        var logger = _loggerFactory.CreateLogger<TenantDbContext>();
        return new TenantDbContext(dbOptions, _tenantDbFactory, logger);
    }
}