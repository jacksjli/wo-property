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
    options.UseNpgsql("Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!"));

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

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DeviceDbContext>();
    dbContext.Database.EnsureCreated();
    
    // 添加初始数据（如果不存在）
    if (!dbContext.DeviceCategories.Any())
    {
        dbContext.DeviceCategories.AddRange(
            new DeviceCategory { Name = "空调系统", Code = "AC", Description = "中央空调、分体空调等" },
            new DeviceCategory { Name = "电气设备", Code = "ELEC", Description = "配电箱、照明、插座等" },
            new DeviceCategory { Name = "电梯设备", Code = "ELEV", Description = "客梯、货梯、扶梯等" },
            new DeviceCategory { Name = "给排水系统", Code = "PLUMB", Description = "水泵、水管、水箱等" },
            new DeviceCategory { Name = "消防系统", Code = "FIRE", Description = "灭火器、喷淋、报警等" },
            new DeviceCategory { Name = "安防系统", Code = "SEC", Description = "监控、门禁、对讲等" },
            new DeviceCategory { Name = "网络设备", Code = "NET", Description = "路由器、交换机、AP等" },
            new DeviceCategory { Name = "办公设备", Code = "OFFICE", Description = "打印机、复印机、电脑等" }
        );
        await dbContext.SaveChangesAsync();
    }
    
    if (!dbContext.Locations.Any())
    {
        dbContext.Locations.AddRange(
            new Location { Name = "A栋", Type = "Building", ParentId = null },
            new Location { Name = "B栋", Type = "Building", ParentId = null },
            new Location { Name = "1楼", Type = "Floor", ParentId = 1 },
            new Location { Name = "2楼", Type = "Floor", ParentId = 1 },
            new Location { Name = "101办公室", Type = "Room", ParentId = 3 },
            new Location { Name = "201会议室", Type = "Room", ParentId = 4 }
        );
        await dbContext.SaveChangesAsync();
    }
    
    if (!dbContext.Devices.Any())
    {
        dbContext.Devices.AddRange(
            new Device
            {
                Code = "AC-001",
                Name = "中央空调主机",
                Model = "格力GMV-280W",
                SerialNumber = "SN-AC-2024-001",
                CategoryId = 1,
                LocationId = 5,
                PurchaseDate = new DateTime(2024, 1, 15),
                WarrantyEndDate = new DateTime(2027, 1, 15),
                Status = "Active",
                CurrentStatus = "Normal",
                Notes = "机房主要制冷设备"
            },
            new Device
            {
                Code = "ELEC-001",
                Name = "主配电箱",
                Model = "ABB S260",
                SerialNumber = "SN-ELEC-2024-001",
                CategoryId = 2,
                LocationId = 5,
                PurchaseDate = new DateTime(2024, 2, 10),
                WarrantyEndDate = new DateTime(2027, 2, 10),
                Status = "Active",
                CurrentStatus = "Normal",
                Notes = "主要电力分配设备"
            },
            new Device
            {
                Code = "OFFICE-001",
                Name = "彩色激光打印机",
                Model = "HP Color LaserJet Pro",
                SerialNumber = "SN-OFFICE-2024-001",
                CategoryId = 8,
                LocationId = 6,
                PurchaseDate = new DateTime(2024, 3, 5),
                WarrantyEndDate = new DateTime(2026, 3, 5),
                Status = "Active",
                CurrentStatus = "Normal",
                Notes = "行政部主要打印设备"
            }
        );
        await dbContext.SaveChangesAsync();
    }
    
    if (!dbContext.MaintenanceRecords.Any())
    {
        dbContext.MaintenanceRecords.Add(
            new MaintenanceRecord
            {
                DeviceId = 1,
                MaintenanceType = "季度例行维护",
                MaintenanceDate = DateTime.UtcNow.AddDays(-30),
                Description = "清洁滤网，检查制冷剂压力",
                Technician = "张师傅",
                Cost = 500.00m,
                Hours = 2,
                Notes = "设备运行正常",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            }
        );
        await dbContext.SaveChangesAsync();
    }
}

// 输出服务信息
Console.WriteLine("=== 设备管理服务（统一认证版）===");
Console.WriteLine($"服务地址: http://localhost:5007");
Console.WriteLine($"JWT配置: Issuer={issuer}, Audience={audience}");
Console.WriteLine("=== 服务已启动 ===");

app.Run("http://0.0.0.0:5007");

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
