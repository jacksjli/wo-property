using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WO.Property.TicketService.Tenant;

namespace WO.Property.TicketService.Tests;

public class TenantDbFactoryTests
{
    private TenantDbFactory CreateFactory()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4",
            ["ConnectionStrings:TenantDb"] = "Server=127.0.0.1;Port=3306;Database={db_name};User=root;Password=;CharSet=utf8mb4"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = new Mock<ILogger<TenantDbFactory>>().Object;
        return new TenantDbFactory(config, logger);
    }

    [Fact]
    public void SetCurrentTenantCode_SetsAndGetsSameTenantCode()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_a");
        Assert.Equal("tenant_a", factory.GetCurrentTenantCode());
    }

    [Fact]
    public void GetCurrentTenantCode_NoTenantSet_ReturnsNull()
    {
        var factory = CreateFactory();
        Assert.Null(factory.GetCurrentTenantCode());
    }

    [Fact]
    public void Clear_AfterSetting()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_b");
        factory.Clear();
        Assert.Null(factory.GetCurrentTenantCode());
    }

    [Fact]
    public void GetTenantConnectionString_KnownTenants_ReturnsValid()
    {
        var factory = CreateFactory();
        var connStr = factory.GetTenantConnectionString("tenant_a");
        Assert.Contains("tenant_a", connStr);
        Assert.Contains("Server=127.0.0.1", connStr);
    }

    [Fact]
    public void GetTenantConnectionString_UnknownTenant_ReturnsValidFormat()
    {
        var factory = CreateFactory();
        // GetTenantConnectionString accepts any tenant and replaces DB name
        // No validation exists - it replaces the database in the connection string
        var connStr = factory.GetTenantConnectionString("unknown_tenant");
        Assert.Contains("unknown_tenant", connStr);
    }

    [Fact]
    public void SetAndClear_TwoTenants()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_a");
        Assert.Equal("tenant_a", factory.GetCurrentTenantCode());
        factory.Clear();
        Assert.Null(factory.GetCurrentTenantCode());
        factory.SetCurrentTenantCode("tenant_b");
        Assert.Equal("tenant_b", factory.GetCurrentTenantCode());
    }
}