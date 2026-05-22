using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WO.Property.MobileService.Tenant;
using WO.Property.MobileService.Data;

namespace WO.Property.MobileService.Tests;

public class TenantDbFactoryTests
{
    private TenantDbContextFactory CreateFactory()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4",
            ["ConnectionStrings:TenantDb"] = "Server=127.0.0.1;Port=3306;Database={db_name};User=root;Password=;CharSet=utf8mb4"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = new Mock<ILogger<TenantDbContextFactory>>().Object;
        return new TenantDbContextFactory(config, logger);
    }

    [Fact]
    public void SetCurrentTenantCode_SetsAndGetsSameTenantCode()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_a");
        Assert.Equal("tenant_a", factory.GetCurrentTenantCode());
    }

    [Fact]
    public void SetCurrentTenantCode_OverwritesPreviousTenantCode()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_a");
        factory.SetCurrentTenantCode("tenant_b");
        Assert.Equal("tenant_b", factory.GetCurrentTenantCode());
    }

    [Fact]
    public void GetCurrentTenantCode_WhenNotSet_ReturnsNull()
    {
        var factory = CreateFactory();
        Assert.Null(factory.GetCurrentTenantCode());
    }

    [Fact]
    public void Clear_AfterSetting_ClearsTenantCode()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_a");
        factory.Clear();
        Assert.Null(factory.GetCurrentTenantCode());
    }
}
