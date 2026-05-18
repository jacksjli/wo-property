using MySqlConnector;
using Microsoft.EntityFrameworkCore;

namespace WO.Property.ExpressService.Data;

/// <summary>
/// 租户数据库工厂 - 动态切换租户数据库
/// </summary>
public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;
    private readonly DbContextOptions<TenantDbContext> _centerOptions;

    public TenantDbContextFactory(
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger,
        IConfiguration configuration)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;

        var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
        _centerOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
        if (string.IsNullOrEmpty(tenantCode))
            throw new InvalidOperationException("Tenant code not set in AsyncLocal context");

        var connectionString = BuildConnectionString(tenantCode);
        _logger.LogWarning("[EXPRESS DB] creating context for tenant: {TC}, conn: {Conn}", tenantCode, connectionString);

        // Build fresh options from scratch with the correct connection string
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;
        return new TenantDbContext(opts, _tenantDbFactory, _logger);
    }

    private string BuildConnectionString(string tenantCode)
    {
        return $"Server=127.0.0.1;Port=3306;Database={tenantCode};User=root;Password=;CharSet=utf8mb4;";
    }
}