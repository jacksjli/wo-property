using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace WO.Property.TicketService.Tenant;

/// <summary>
/// 工单服务数据库上下文工厂实现
/// 每次 CreateDbContext() 创建新实例，从 AsyncLocal 读取当前租户
/// </summary>
public class TicketDbContextFactory : ITicketDbContextFactory
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TicketDbContextFactory> _logger;

    public TicketDbContextFactory(
        ITenantDbFactory tenantDbFactory,
        ILogger<TicketDbContextFactory> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    public AppDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode() ?? "wo_property";
        var connectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        
        _logger.LogDebug("Creating AppDbContext for tenant: {TenantCode}", tenantCode);
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        optionsBuilder.UseMySql(new MySqlConnection(connectionString), serverVersion);
        
        // Pass the tenantDbFactory so the context can read AsyncLocal
        return new AppDbContext(optionsBuilder.Options);
    }
}