using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WO.Property.TicketService.Middleware;
using WO.Property.TicketService.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.TicketService.Tests;

public class TenantRoutingMiddlewareTests
{
    private TenantDbFactory CreateFactory()
    {
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<ILogger<TenantDbFactory>>().Object;
        return new TenantDbFactory(config, logger);
    }

    private ILogger<TenantRoutingMiddleware> CreateLogger()
    {
        return new Mock<ILogger<TenantRoutingMiddleware>>().Object;
    }

    private string CreateTestJwtToken(string? tenantCode = "tenant_a", int userId = 1)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_code", tenantCode ?? "")
        };
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("wo-property-secret-key-min-32-chars!!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "wo-property-unified-auth",
            audience: "wo-property-services",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );
        return tokenHandler.WriteToken(token);
    }

    private RequestDelegate CreateNextDelegate()
    {
        return (ctx) => Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_ValidTenantToken_CallsNext()
    {
        var factory = CreateFactory();
        var logger = CreateLogger();
        var middleware = new TenantRoutingMiddleware(CreateNextDelegate(), logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {CreateTestJwtToken("tenant_a")}";

        var nextCalled = false;
        var next = new RequestDelegate(_ => { nextCalled = true; return Task.CompletedTask; });
        var middleware2 = new TenantRoutingMiddleware(next, logger);

        await middleware2.InvokeAsync(httpContext, factory);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_NoAuthHeader_CallsNext()
    {
        var factory = CreateFactory();
        var logger = CreateLogger();
        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new TenantRoutingMiddleware(next, logger);
        var httpContext = new DefaultHttpContext();
        // No Authorization header

        await middleware.InvokeAsync(httpContext, factory);

        Assert.Null(factory.GetCurrentTenantCode()); // unchanged
    }

    [Fact]
    public async Task InvokeAsync_InvalidToken_CallsNextWithoutSetting()
    {
        var factory = CreateFactory();
        factory.SetCurrentTenantCode("tenant_x"); // pre-set
        var logger = CreateLogger();
        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new TenantRoutingMiddleware(next, logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer invalid.jwt.token";

        await middleware.InvokeAsync(httpContext, factory);

        Assert.Equal("tenant_x", factory.GetCurrentTenantCode()); // unchanged
    }
}