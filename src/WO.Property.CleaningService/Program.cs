using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.CleaningService.Data;
using WO.Property.CleaningService.Models;
using WO.Property.Shared.Configuration;
using WO.Property.CleaningService.Tenant;
using WO.Property.CleaningService.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5516");

builder.Services.AddDbContext<CleaningDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});

// JWT 配置
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var jwtIssuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var jwtAudience = jwtSettings["Audience"] ?? "wo-property-services";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
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
builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy => {
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5173" };
    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));

// Phase 1: 多租户连接字符串配置（必须在 builder.Build() 之前）
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

// Phase 1: 租户服务注册
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<TenantDbContextFactory>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantRoutingMiddleware>();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "CleaningService", timestamp = DateTime.UtcNow }));

Console.WriteLine("========================================");
Console.WriteLine("  CleaningService running on :5516 (Phase 1 multi-tenant)");
Console.WriteLine("========================================");

app.Run();