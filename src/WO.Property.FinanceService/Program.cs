using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.FinanceService.Data;
using WO.Property.FinanceService.Middleware;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5009端口
builder.WebHost.UseUrls("http://0.0.0.0:5509");

// 添加数据库
builder.Services.AddDbContext<FinanceDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});

// JWT 配置 - 使用统一认证配置
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

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// ─── Phase 1 多租户组件注册 ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.FinanceService.Tenant.ITenantDbFactory, WO.Property.FinanceService.Tenant.TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<WO.Property.FinanceService.Data.TenantDbContext>>(sp =>
    new WO.Property.FinanceService.Data.TenantDbContextFactory(
        sp.GetRequiredService<WO.Property.FinanceService.Tenant.ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<WO.Property.FinanceService.Data.TenantDbContext>>()
    ));

// 添加APIExplorer用于开发
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 数据库初始化

app.UseAuthentication();
app.UseAuthorization();

// ─── Phase 1 租户路由中间件 ───
app.UseMiddleware<TenantRoutingMiddleware>();

app.MapControllers();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "FinanceService", mode = "Phase 1 multi-tenant", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Finance Service");
Console.WriteLine("  Port: 5509");
Console.WriteLine("===========================================");

app.Run();
