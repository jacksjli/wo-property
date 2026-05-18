using MySqlConnector;
using Microsoft.EntityFrameworkCore;

namespace WO.Property.KeyService.Data;

/// <summary>
/// 租户数据库工厂 - 动态切换租户数据库
/// </summary>
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

        var connectionString = BuildConnectionString(tenantCode);
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new TenantDbContext(options, _tenantDbFactory, _logger);
    }

    private string BuildConnectionString(string tenantCode)
    {
        return $"Server=127.0.0.1;Port=3306;Database={tenantCode};User=root;Password=;CharSet=utf8mb4;";
    }
}