using Microsoft.EntityFrameworkCore;
using WO.Property.ProjectTrackingService.Tenant;

namespace WO.Property.ProjectTrackingService.Data;

public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>, ITenantDbFactory
{
    private readonly string _connectionString;
    private readonly ILogger<TenantDbContextFactory> _logger;

    public TenantDbContextFactory(
        ILogger<TenantDbContextFactory> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("Default")
            ?? "Server=localhost;Port=3306;Database=wo_property;User=root;Password=;";
    }

    public string GetTenantConnectionString()
    {
        return _connectionString;
    }

    public TenantDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString))
            .Options;
        return new TenantDbContext(options);
    }

    public Task<TenantDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}
