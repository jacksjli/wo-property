using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WO.Property.PersonService.Data;

public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILoggerFactory _loggerFactory;

    public TenantDbContextFactory(
        Tenant.ITenantDbFactory tenantDbFactory,
        ILoggerFactory loggerFactory)
    {
        _tenantDbFactory = tenantDbFactory;
        _loggerFactory = loggerFactory;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode()
            ?? throw new InvalidOperationException("Tenant code not set");

        var connectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

        var logger = _loggerFactory.CreateLogger<TenantDbContext>();
        return new TenantDbContext(optionsBuilder.Options, _tenantDbFactory, logger);
    }
}