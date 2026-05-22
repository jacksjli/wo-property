using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.ComplaintService.Tenant;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// === Serilog 日志配置 ===
var serviceName = "ComplaintService";
var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsPath);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", serviceName)
    .Enrich.WithProperty("Application", "WO-Property")
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine))
    .WriteTo.File(
        new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine),
        Path.Combine(logsPath, $"{serviceName.ToLower()}-.log"),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 100 * 1024 * 1024,
        retainedFileCountLimit: 30,
        rollOnFileSizeLimit: true,
        shared: false,
        flushToDiskInterval: TimeSpan.FromSeconds(2))
    .CreateLogger();

builder.Host.UseSerilog();

// 配置端口
builder.WebHost.UseUrls("http://0.0.0.0:5011");

// Tenant 支持
builder.Services.AddSingleton<TenantConfigLoader>();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();

// 数据库连接字符串（支持 X-Project）
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;AllowUserVariables=true";

builder.Services.AddScoped<MySqlConnection>(sp => {
    var factory = sp.GetService<ITenantDbFactory>();
    if (factory != null)
    {
        try {
            var tenantCode = factory.GetCurrentTenantCode() ?? "wo_property";
            return new MySqlConnection(factory.GetTenantConnectionString(tenantCode));
        } catch { }
    }
    return new MySqlConnection(dbConnectionString);
});

// JWT 配置
var jwtIssuer = JwtHelper.GetIssuer();
var jwtAudience = JwtHelper.GetAudience();
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminPortal", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// 初始化数据库表
try
{
    using var connection = new MySqlConnection(dbConnectionString);
    await connection.OpenAsync();
    
    var initSql = @"
CREATE TABLE IF NOT EXISTS complaints (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    type VARCHAR(20) DEFAULT 'complaint',
    category VARCHAR(50),
    priority VARCHAR(20) DEFAULT 'medium',
    description TEXT,
    reporter_name VARCHAR(100),
    reporter_phone VARCHAR(20),
    reporter_room VARCHAR(50),
    status VARCHAR(20) DEFAULT 'pending',
    assigned_to BIGINT,
    assigned_to_name VARCHAR(100),
    result TEXT,
    response_at DATETIME,
    resolved_at DATETIME,
    source VARCHAR(20) DEFAULT 'phone',
    project_id INT DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (status),
    INDEX idx_type (type),
    INDEX idx_project (project_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

    using var cmd = new MySqlCommand(initSql, connection);
    await cmd.ExecuteNonQueryAsync();
    Log.Information("Complaints table initialized");
}
catch (Exception ex)
{
    Log.Warning(ex, "Table initialization failed, service will continue");
}

app.UseCors("AllowAdminPortal");

// X-Project 路由中间件
app.UseMiddleware<WO.Property.ComplaintService.Middleware.TenantRoutingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ComplaintService", timestamp = DateTime.UtcNow }));

Log.Information("===========================================");
Log.Information("  WO Property Complaint Service Started");
Log.Information("  Port: 5011");
Log.Information("  Database: MySQL (X-Project Enabled)");
Log.Information("===========================================");

app.Run();