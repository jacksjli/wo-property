using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using MySqlConnector;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.Shared.Configuration;
using WO.Property.PersonService.Data;
using WO.Property.PersonService.Tenant;

var builder = WebApplication.CreateBuilder(args);

// === Serilog 日志配置 ===
var serviceName = "PersonService";
var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsPath);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
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

// Tenant 支持
builder.Services.AddSingleton<TenantConfigLoader>();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<ITenantDbFactory>(),
        sp.GetRequiredService<ILoggerFactory>()
    ));

// Configure Kestrel to listen on port 5018
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5018);
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Configure JWT Authentication
var jwtKey = JwtHelper.GetSecretKey();
var key = Encoding.UTF8.GetBytes(jwtKey);

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
        ValidIssuer = "wo-property-unified-auth",
        ValidAudience = "wo-property-services",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WO Property PersonService API",
        Version = "v1",
        Description = "WO 物业管理软件 - 统一人员中心 API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Initialize database using raw MySqlConnection
try
{
    await using var conn = new MySqlConnection(
        "Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=false;");
    await conn.OpenAsync();
    var initializer = new DbInitializer();
    await initializer.InitializeAsync(conn);
    Log.Information("Database initialized successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Database initialization error - service will continue but database operations may fail");
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PersonService API V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();
// X-Project 路由中间件
app.UseMiddleware<WO.Property.PersonService.Middleware.TenantRoutingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();