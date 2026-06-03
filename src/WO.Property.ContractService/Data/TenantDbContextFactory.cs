using MySqlConnector;
using Microsoft.EntityFrameworkCore;

namespace WO.Property.ContractService.Data;

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
        _logger.LogWarning("[DEBUG] CreateDbContext called, tenantCode: {TC}", tenantCode ?? "NULL");
        if (string.IsNullOrEmpty(tenantCode))
            throw new InvalidOperationException("Tenant code not set");
        
        // 使用 TenantDbFactory 解析 project_code → actual database name
        var connStr = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        _logger.LogWarning("[DEBUG] Connection string: {CS}", connStr.Substring(0, Math.Min(50, connStr.Length)));
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(connStr, serverVersion)
            .Options;
        return new TenantDbContext(options, _tenantDbFactory, _logger);
    }
}