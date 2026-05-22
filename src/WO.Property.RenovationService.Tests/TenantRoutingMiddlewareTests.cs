using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WO.Property.RenovationService.Middleware;
using WO.Property.RenovationService.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.RenovationService.Tests;

public class TenantRoutingMiddlewareTests
{
    private TenantRoutingMiddleware CreateMiddleware(RequestDelegate next)
    {
        var logger = new Mock<ILogger<TenantRoutingMiddleware>>().Object;
        return new TenantRoutingMiddleware(next, logger);
    }

    private string CreateTestJwt(string tenantCode)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims: new [] { new Claim("tenant_code", tenantCode), new Claim("sub", "1") }
        );
        return handler.WriteToken(token);
    }

    [Fact]
    public async Task InvokeAsync_WithoutAuthorizationHeader_CallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        var mockFactory = new Mock<ITenantDbFactory>();
        
        await middleware.InvokeAsync(context, mockFactory.Object);
        
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithValidTenantCodeJwt_SetsTenant()
    {
        var mockFactory = new Mock<ITenantDbFactory>();
        var nextCalled = false;
        
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<TenantRoutingMiddleware>>().Object;
        var middleware = new TenantRoutingMiddleware(next, logger);
        
        var context = new DefaultHttpContext();
        var jwt = CreateTestJwt("tenant_a");
        context.Request.Headers["Authorization"] = "Bearer " + jwt;
        
        await middleware.InvokeAsync(context, mockFactory.Object);
        
        mockFactory.Verify(f => f.SetCurrentTenantCode("tenant_a"), Times.Once);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithTenantBJwt_SetsTenantB()
    {
        var mockFactory = new Mock<ITenantDbFactory>();
        var nextCalled = false;
        
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<TenantRoutingMiddleware>>().Object;
        var middleware = new TenantRoutingMiddleware(next, logger);
        
        var context = new DefaultHttpContext();
        var jwt = CreateTestJwt("tenant_b");
        context.Request.Headers["Authorization"] = "Bearer " + jwt;
        
        await middleware.InvokeAsync(context, mockFactory.Object);
        
        mockFactory.Verify(f => f.SetCurrentTenantCode("tenant_b"), Times.Once);
        Assert.True(nextCalled);
    }
}
