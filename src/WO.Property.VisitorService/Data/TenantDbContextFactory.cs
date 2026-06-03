using MySqlConnector;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using WO.Property.VisitorService.Tenant;

namespace WO.Property.VisitorService.Data;

public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;
    private readonly string _defaultConnectionString;

    public TenantDbContextFactory(
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
        _defaultConnectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;";
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();

        string connectionString;
        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogDebug("No tenant context - using default database");
            connectionString = _defaultConnectionString;
        }
        else
        {
            connectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
            _logger.LogDebug("Creating TenantDbContext for tenant {TenantCode}", tenantCode);
        }

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new TenantDbContext(options, _tenantDbFactory, _logger);
    }
}