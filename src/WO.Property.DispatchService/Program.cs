using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WO.Property.DispatchService.Data;
using WO.Property.DispatchService.Middleware;
using WO.Property.DispatchService.Tenant;
using WO.Property.DispatchService.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "WO-Property-Management-Unified-Secret-Key-2026-For-All-Services";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "wo-property-unified-auth",
            ValidAudience = "wo-property-services",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

// PersonService HttpClient
builder.Services.AddHttpClient("PersonService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5018");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Gateway HttpClient for event publishing
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// 注册租户工厂
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>, TenantDbContextFactory>();

// 注册超时监控服务
builder.Services.AddHostedService<DispatchTimeoutMonitor>();

// 配置连接字符串
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

var app = builder.Build();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "DispatchService", timestamp = DateTime.UtcNow }));

// 租户路由中间件
app.UseMiddleware<TenantRoutingMiddleware>();

// 路由
app.MapControllers();

// 端口
var port = 5241;
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Dispatch Service");
Console.WriteLine($"  Port: {port}");
Console.WriteLine("===========================================");

app.Run();