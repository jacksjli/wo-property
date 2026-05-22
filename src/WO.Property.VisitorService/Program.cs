using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.VisitorService.Data;
using WO.Property.VisitorService.Middleware;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5513");

// JWT 配置
var jwtIssuer = "wo-property-unified-auth";
var jwtAudience = "wo-property-services";
var jwtSecretKey = JwtHelper.GetSecretKey();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Phase 1 多租户组件
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.VisitorService.Tenant.ITenantDbFactory, WO.Property.VisitorService.Tenant.TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<WO.Property.VisitorService.Data.TenantDbContext>>(sp =>
    new WO.Property.VisitorService.Data.TenantDbContextFactory(
        sp.GetRequiredService<WO.Property.VisitorService.Tenant.ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<WO.Property.VisitorService.Data.TenantDbContext>>()
    ));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Phase 1 租户路由中间件
app.UseMiddleware<TenantRoutingMiddleware>();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "VisitorService",
    mode = "Phase 1 multi-tenant",
    timestamp = DateTime.UtcNow
}));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Visitor Service");
Console.WriteLine("  Port: 5513");
Console.WriteLine("  Mode: Phase 1 multi-tenant");
Console.WriteLine("===========================================");

app.Run();