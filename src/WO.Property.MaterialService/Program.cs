using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using WO.Property.MaterialService.Tenant;
using WO.Property.MaterialService.Middleware;
using WO.Property.MaterialService.Data;
using WO.Property.MaterialService.Models;

var builder = WebApplication.CreateBuilder(args);

// 端口配置
builder.WebHost.UseUrls("http://0.0.0.0:5504");

// 连接字符串配置
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

// 添加服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 添加数据库上下文（原有）
builder.Services.AddDbContext<TenantDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(connectionString, serverVersion);
});

// JWT 配置
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "wo-property-jwt-secret-key-min-32-chars!";
var issuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var audience = jwtSettings["Audience"] ?? "wo-property-services";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 添加控制器服务（支持 TenantMaterialController）
builder.Services.AddControllers();

// 添加数据库上下文工厂（多租户）
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>, TenantDbContextFactory>();

// ─── Phase 2: 注册租户组件 ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();

var app = builder.Build();

// 配置中间件
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// === Phase 2: 租户路由中间件 ===
app.UseMiddleware<TenantRoutingMiddleware>();

// 映射控制器路由（必须放在所有中间件之后）
app.MapControllers();

// 健康检查端点
app.MapGet("/health", () =>
{
    return Results.Json(new
    {
        status = "healthy",
        service = "WO物料管理服务（多租户）",
        version = "5.0.0",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        features = new[] { "物料分类管理", "库存管理", "采购管理", "多租户隔离", "统一JWT认证" },
        authConfig = new
        {
            type = "统一认证",
            issuer = issuer,
            audience = audience
        }
    });
});

Console.WriteLine("=== Material Service Started ===");
Console.WriteLine($"JWT配置: Issuer={issuer}, Audience={audience}");

app.Run();
