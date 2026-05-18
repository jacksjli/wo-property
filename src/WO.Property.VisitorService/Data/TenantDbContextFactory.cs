using MySqlConnector;
using Microsoft.EntityFrameworkCore;

namespace WO.Property.VisitorService.Data;

public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public TenantDbContextFactory(Tenant.ITenantDbFactory tenantDbFactory, ILogger<TenantDbContext> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
        if (string.IsNullOrEmpty(tenantCode))
            throw new InvalidOperationException("Tenant code not set");
        var connStr = $"Server=127.0.0.1;Port=3306;Database={tenantCode};User=root;Password=;CharSet=utf8mb4;";
        var serverVersion = ServerVersion.AutoDetect(connStr);
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connStr, serverVersion)
            .Options;
        return new TenantDbContext(options, _tenantDbFactory, _logger);
    }
}