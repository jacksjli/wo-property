using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.ProjectConfigService.Data;
using WO.Property.ProjectConfigService.Services;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// JWT 配置
// ============================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var issuer    = jwtSettings["Issuer"]      ?? "wo-property-unified-auth";
var audience  = jwtSettings["Audience"]    ?? "wo-property-services";

// 数据库配置
var dbSection = builder.Configuration.GetSection("Database");
var dbProvider = dbSection["Provider"] ?? "SQLite";

if (dbProvider == "PostgreSQL")
{
    var pg = dbSection.GetSection("PostgreSQL");
    var connStr = $"Host={pg["Host"]};Port={pg["Port"]};Database={pg["Database"]};Username={pg["Username"]};Password={pg["Password"]};";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connStr));
}
else if (dbProvider == "MySQL")
{
    var mysql = dbSection.GetSection("MySQL");
    var connStr = $"Server={mysql["Server"]};Port={mysql["Port"]};Database={mysql["Database"]};User={mysql["User"]};Password={mysql["Password"]};CharSet=utf8mb4;Pooling=false;";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));
}
else
{
    var dbPath = dbSection["SQLitePath"] ?? "Data Source=project_config.db";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(dbPath));
}

builder.Services.AddScoped<IProjectConfigService, ProjectConfigServiceImpl>();

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// 数据库初始化
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "ProjectConfigService",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
}));

app.Run();
