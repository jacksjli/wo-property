using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.DeliveryService.Data;
using WO.Property.DeliveryService.Models;
using WO.Property.DeliveryService.Tenant;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5017");

// MySQL 连接字符串管理 - 支持 X-Project 动态切换
builder.Services.AddSingleton<TenantConfigLoader>();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();

// DbContext Factory 模式 - 每次请求创建新实例
builder.Services.AddDbContextFactory<DeliveryDbContext>((sp, options) => {
    var factory = sp.GetRequiredService<ITenantDbFactory>();
    var tenantCode = factory.GetCurrentTenantCode() ?? "wo_property";
    var connStr = factory.GetTenantConnectionString(tenantCode);
    options.UseMySql(connStr, ServerVersion.Parse("8.0.35"));
});

var jwtIssuer = "wo-property-unified-auth";
var jwtAudience = "wo-property-services";
var jwtSecretKey = JwtHelper.GetSecretKey();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
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

// 允许前端开发服务器跨域
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

// X-Project 路由中间件
app.UseMiddleware<WO.Property.DeliveryService.Middleware.TenantRoutingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "DeliveryService",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    features = new[] { "外卖订单", "骑手管理", "取餐通知", "统计报表" }
}));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Delivery Service (外卖管理)");
Console.WriteLine("  Port: 5017");
Console.WriteLine("===========================================");


// ==================== 配送请求控制器 (直接查询 DeliveryRequests 表) ====================
app.MapGet("/api/delivery-requests", async (HttpContext context) =>
{
    try
    {
        var factory = context.RequestServices.GetRequiredService<ITenantDbFactory>();
        var tenantCode = context.Request.Headers["X-Project"].FirstOrDefault() ?? "wo_property";
        var connStr = factory.GetTenantConnectionString(tenantCode);
        
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();
        
        // 直接查询 DeliveryRequests 表
        await using var cmd = new MySqlCommand("SELECT * FROM DeliveryRequests ORDER BY CreatedAt DESC LIMIT 50", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var results = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }
        
        return Results.Ok(new { success = true, data = results });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = ex.Message });
    }
});

app.Run();
