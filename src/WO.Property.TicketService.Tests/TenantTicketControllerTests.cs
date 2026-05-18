using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WO.Property.TicketService.Controllers;
using WO.Property.TicketService.Data;
using WO.Property.TicketService.Tenant;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace WO.Property.TicketService.Tests;

public class TenantTicketControllerTests
{
    private TenantDbFactory CreateTenantDbFactory()
    {
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<ILogger<TenantDbFactory>>().Object;
        return new TenantDbFactory(config, logger);
    }

    private string CreateTestJwtToken(string tenantCode = "tenant_a", int userId = 1)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_code", tenantCode)
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

    private HttpContext CreateHttpContext(string? tenantCode = "tenant_a")
    {
        var httpContext = new DefaultHttpContext();
        if (tenantCode != null)
        {
            httpContext.Request.Headers["Authorization"] = $"Bearer {CreateTestJwtToken(tenantCode)}";
        }
        return httpContext;
    }

    [Fact]
    public void TenantCreateTicketRequest_DefaultValues()
    {
        var request = new TenantCreateTicketRequest
        {
            Title = "Test Ticket",
            ProjectId = 1
        };

        Assert.Equal("Test Ticket", request.Title);
        Assert.Equal(1, request.ProjectId);
        Assert.Null(request.Category);
        Assert.Null(request.Priority);
        Assert.Null(request.Description);
    }

    [Fact]
    public void TenantUpdateTicketRequest_OptionalFields()
    {
        var request = new TenantUpdateTicketRequest
        {
            Status = "Processing"
        };

        Assert.Null(request.Title);
        Assert.Equal("Processing", request.Status);
    }

    [Fact]
    public void CreateTestJwtToken_ContainsTenantCode()
    {
        var token = CreateTestJwtToken("tenant_b", userId: 5);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_code");
        Assert.NotNull(tenantClaim);
        Assert.Equal("tenant_b", tenantClaim.Value);
    }

    [Fact]
    public void CreateTestJwtToken_ContainsUserId()
    {
        var token = CreateTestJwtToken("tenant_a", userId: 42);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal("42", userIdClaim.Value);
    }
}