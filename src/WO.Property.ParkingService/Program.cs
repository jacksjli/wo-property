using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.ParkingService.Data;
using WO.Property.ParkingService.Middleware;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

ServiceRunner.ConfigurePort(builder, "ParkingService", 5525);

// 添加连接字符串配置（供 TenantDbFactory 使用）
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4",
    ["ConnectionStrings:CenterDb"] = "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4"
});

// ─── Phase 1 多租户组件注册 ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.ParkingService.Tenant.ITenantDbFactory, WO.Property.ParkingService.Tenant.TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<WO.Property.ParkingService.Tenant.ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<TenantDbContext>>()
    ));

// ─── JWT 认证 ───
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

builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175"
        ).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ─── Phase 1 租户路由中间件 ───
app.UseMiddleware<TenantRoutingMiddleware>();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "ParkingService",
    timestamp = DateTime.UtcNow,
    mode = "Phase 1 multi-tenant"
}));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Parking Service");
Console.WriteLine("  Port: 5525 (Phase 1 multi-tenant)");
Console.WriteLine("===========================================");

app.Run();