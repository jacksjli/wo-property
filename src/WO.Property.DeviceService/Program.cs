using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

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

// 添加数据库上下文
builder.Services.AddDbContext<DeviceDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});

// 使用统一JWT配置
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var issuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var audience = jwtSettings["Audience"] ?? "wo-property-services";

// 配置JWT认证（使用统一配置）
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

var app = builder.Build();

// 配置中间件
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// 健康检查端点
app.MapGet("/health", () =>
{
    return Results.Json(new
    {
        status = "healthy",
        service = "WO设备管理服务（统一认证）",
        version = "4.0.1",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        features = new[] { "设备台账管理", "维护记录跟踪", "设备统计报表", "统一JWT认证" },
        authConfig = new
        {
            type = "统一认证",
            issuer = issuer,
            audience = audience
        }
    });
});

// 设备管理API（需要认证）
app.MapGet("/api/devices", [Authorize] async (DeviceDbContext db, [FromQuery] int? categoryId, [FromQuery] string? status) =>
{
    var query = db.Devices.AsQueryable();
    
    if (categoryId.HasValue)
    {
        query = query.Where(d => d.CategoryId == categoryId.Value);
    }
    
    if (!string.IsNullOrEmpty(status))
    {
        query = query.Where(d => d.Status == status);
    }
    
    var devices = await query.ToListAsync();
    return Results.Ok(new { devices });
});

app.MapGet("/api/devices/{id}", [Authorize] async (DeviceDbContext db, int id) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound(new { message = "设备不存在" });
    
    return Results.Ok(new { device });
});

app.MapPost("/api/devices", [Authorize(Roles = "Administrator,Technician")] async (DeviceDbContext db, CreateDeviceRequest request) =>
{
    var device = new Device
    {
        Code = request.Code,
        Name = request.Name,
        Model = request.Model,
        SerialNumber = request.SerialNumber,
        CategoryId = request.CategoryId,
        LocationId = request.LocationId,
        PurchaseDate = request.PurchaseDate,
        WarrantyEndDate = request.WarrantyEndDate,
        Status = request.Status,
        CurrentStatus = request.CurrentStatus,
        Notes = request.Notes
    };
    
    db.Devices.Add(device);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/devices/{device.Id}", new { device });
});

app.MapPut("/api/devices/{id}", [Authorize(Roles = "Administrator,Technician")] async (DeviceDbContext db, int id, UpdateDeviceRequest request) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound(new { message = "设备不存在" });
    
    device.Name = request.Name;
    device.Model = request.Model;
    device.SerialNumber = request.SerialNumber;
    device.CategoryId = request.CategoryId;
    device.LocationId = request.LocationId;
    device.PurchaseDate = request.PurchaseDate;
    device.WarrantyEndDate = request.WarrantyEndDate;
    device.Status = request.Status;
    device.CurrentStatus = request.CurrentStatus;
    device.Notes = request.Notes;
    device.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    
    return Results.Ok(new { device });
});

// 设备分类API（公开，无需认证）
app.MapGet("/api/device-categories", async (DeviceDbContext db) =>
{
    var categories = await db.DeviceCategories.ToListAsync();
    return Results.Ok(new { categories });
});

// 位置信息API（公开，无需认证）
app.MapGet("/api/locations", async (DeviceDbContext db) =>
{
    var locations = await db.Locations.ToListAsync();
    return Results.Ok(new { locations });
});

// 维护记录API（需要认证）
app.MapPost("/api/devices/{id}/maintenance", [Authorize(Roles = "Administrator,Technician")] async (DeviceDbContext db, int id, CreateMaintenanceRecordRequest request) =>
{
    var device = await db.Devices.FindAsync(id);
    if (device == null)
        return Results.NotFound(new { message = "设备不存在" });
    
    var maintenanceRecord = new MaintenanceRecord
    {
        DeviceId = id,
        MaintenanceType = request.MaintenanceType,
        MaintenanceDate = request.MaintenanceDate,
        Description = request.Description,
        Technician = request.Technician,
        Cost = request.Cost,
        Hours = request.Hours,
        Notes = request.Notes,
        CreatedAt = DateTime.UtcNow
    };
    
    db.MaintenanceRecords.Add(maintenanceRecord);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/devices/{id}/maintenance/{maintenanceRecord.Id}", new { maintenanceRecord });
});

app.MapGet("/api/devices/{id}/maintenance-history", [Authorize] async (DeviceDbContext db, int id) =>
{
    var history = await db.MaintenanceRecords
        .Where(m => m.DeviceId == id)
        .OrderByDescending(m => m.MaintenanceDate)
        .ToListAsync();
    
    return Results.Ok(new { history });
});

// 设备统计API（需要认证）
app.MapGet("/api/devices/statistics", [Authorize] async (DeviceDbContext db) =>
{
    var totalDevices = await db.Devices.CountAsync();
    var activeDevices = await db.Devices.CountAsync(d => d.Status == "Active");
    var maintenanceDevices = await db.Devices.CountAsync(d => d.Status == "Maintenance");
    var scrappedDevices = await db.Devices.CountAsync(d => d.Status == "Scrapped");
    
    var categoryDistribution = await db.Devices
        .GroupBy(d => d.CategoryId)
        .Select(g => new
        {
            categoryId = g.Key,
            count = g.Count()
        })
        .ToListAsync();
    
    var recentMaintenance = await db.MaintenanceRecords
        .OrderByDescending(m => m.MaintenanceDate)
        .Take(5)
        .ToListAsync();
    
    return Results.Ok(new
    {
        statistics = new
        {
            totalDevices,
            activeDevices,
            maintenanceDevices,
            scrappedDevices
        },
        categoryDistribution,
        recentMaintenance
    });
});


// 输出服务信息
Console.WriteLine("=== 设备管理服务（统一认证版）===");
Console.WriteLine($"服务地址: http://localhost:5007");
Console.WriteLine($"JWT配置: Issuer={issuer}, Audience={audience}");
Console.WriteLine("=== 服务已启动 ===");

app.Run();

// 数据库上下文
public class DeviceDbContext : DbContext
{
    public DeviceDbContext(DbContextOptions<DeviceDbContext> options) : base(options) { }
    
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceCategory> DeviceCategories { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
}

// 实体类
public class Device
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Inactive, Maintenance, Scrapped
    public string CurrentStatus { get; set; } = string.Empty; // Normal, Warning, Fault
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class DeviceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Building, Floor, Room
    public int? ParentId { get; set; }
}

public class MaintenanceRecord
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTime MaintenanceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Hours { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 请求模型
public class CreateDeviceRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    [Required]
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string Status { get; set; } = "Active";
    public string CurrentStatus { get; set; } = "Normal";
    public string? Notes { get; set; }
}

public class UpdateDeviceRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    [Required]
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string Status { get; set; } = "Active";
    public string CurrentStatus { get; set; } = "Normal";
    public string? Notes { get; set; }
}

public class CreateMaintenanceRecordRequest
{
    [Required]
    public string MaintenanceType { get; set; } = string.Empty;
    [Required]
    public DateTime MaintenanceDate { get; set; }
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string Technician { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Hours { get; set; }
    public string? Notes { get; set; }
}
