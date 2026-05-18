using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.RenovationService.Data;
using WO.Property.RenovationService.Middleware;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5521");

// ─── Phase 1 多租户组件注册 ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.RenovationService.Tenant.ITenantDbFactory, WO.Property.RenovationService.Tenant.TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<WO.Property.RenovationService.Tenant.ITenantDbFactory>(),
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
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
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
    service = "RenovationService",
    timestamp = DateTime.UtcNow,
    mode = "Phase 1 multi-tenant"
}));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Renovation Service");
Console.WriteLine("  Port: 5521 (Phase 1 multi-tenant)");
Console.WriteLine("===========================================");

app.Run();