using MySqlConnector;
using Microsoft.EntityFrameworkCore;

namespace WO.Property.PaymentService.Data;

public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public TenantDbContextFactory(
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
        if (string.IsNullOrEmpty(tenantCode))
            throw new InvalidOperationException("Tenant code not set in AsyncLocal context");

        var connectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new TenantDbContext(options, _tenantDbFactory, _logger);
    }

    public string GetTenantConnectionString(string tenantCode)
    {
        return _tenantDbFactory.GetTenantConnectionString(tenantCode);
    }
}