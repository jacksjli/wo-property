using Microsoft.EntityFrameworkCore;
using WO.Property.TicketService.Tenant;

namespace WO.Property.TicketService.Data;

/// <summary>
/// 租户 DbContext 工厂
/// 每次请求创建新的 DbContext，根据租户上下文动态切换连接
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

        var centerConnectionString = configuration.GetConnectionString("CenterDb")
            ?? "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4";

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        _centerOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(centerConnectionString, serverVersion)
            .Options;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();
        Console.WriteLine($"[TenantDbContextFactory] CreateDbContext called, tenantCode={tenantCode}");
        _logger.LogDebug("CreateDbContext called, tenantCode={TenantCode}", tenantCode ?? "NULL");

        // 空租户代码时默认使用 wo_property（而非 center_db）
        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogWarning("No tenant context - using default wo_property database");
            tenantCode = "wo_property";
        }

        var tenantConnectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var dbOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(tenantConnectionString, serverVersion)
            .Options;

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantCode}", tenantCode);
        return new TenantDbContext(dbOptions, _tenantDbFactory, null!);
    }
}