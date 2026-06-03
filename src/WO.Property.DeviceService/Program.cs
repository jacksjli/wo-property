using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using WO.Property.DeviceService.Tenant;
using WO.Property.DeviceService.Middleware;
using WO.Property.DeviceService.Data;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

ServiceRunner.ConfigurePort(builder, "DeviceService", 5530);

// 连接字符串配置
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

// 添加 HttpClientFactory（用于调用其他服务如 TicketService）
builder.Services.AddHttpClient();

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
builder.Services.AddDbContext<DeviceDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(connectionString, serverVersion);
});

// JWT 配置 - 使用统一的 JwtHelper
var secretKey = JwtHelper.GetSecretKey();
var issuer = JwtHelper.GetIssuer();
var audience = JwtHelper.GetAudience();

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

// 添加控制器服务（支持 TenantDeviceController）
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddAuthorization();

// === Phase 2: 注册租户组件 ===
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>, TenantDbContextFactory>();

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
        service = "WO设备管理服务（多租户）",
        version = "5.0.0",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        features = new[] { "设备台账管理", "维护记录跟踪", "多租户隔离", "统一JWT认证" },
        authConfig = new
        {
            type = "统一认证",
            issuer = issuer,
            audience = audience
        }
    });
});

// 原有设备管理 API（需要认证）
app.MapGet("/api/devices", [Authorize] async (DeviceDbContext db, [FromQuery] int? categoryId, [FromQuery] string? status) =>
{
    var query = db.Devices.AsQueryable();

    if (categoryId.HasValue)
    {
        query = query.Where(d => d.DeviceTypeId == categoryId.Value);
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
        DeviceTypeId = request.DeviceTypeId,
        Location = request.Location,
        PurchaseDate = request.PurchaseDate,
        WarrantyEndDate = request.WarrantyEndDate,
        Status = request.Status,
        CurrentStatus = request.CurrentStatus,
        Remarks = request.Remarks
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
    device.DeviceTypeId = request.DeviceTypeId;
    device.Location = request.Location;
    device.PurchaseDate = request.PurchaseDate;
    device.WarrantyEndDate = request.WarrantyEndDate;
    device.Status = request.Status;
    device.CurrentStatus = request.CurrentStatus;
    device.Remarks = request.Remarks;
    device.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new { device });
});

// 设备分类 API（公开）
app.MapGet("/api/device-categories", async (DeviceDbContext db) =>
{
    var categories = await db.DeviceCategories.ToListAsync();
    return Results.Ok(new { categories });
});

// 位置信息 API（公开）
app.MapGet("/api/locations", async (DeviceDbContext db) =>
{
    var locations = await db.Locations.ToListAsync();
    return Results.Ok(new { locations });
});

// 维护记录 API
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
        Remarks = request.Remarks,
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

// 设备统计 API
app.MapGet("/api/devices/statistics", [Authorize] async (DeviceDbContext db) =>
{
    var totalDevices = await db.Devices.CountAsync();
    var activeDevices = await db.Devices.CountAsync(d => d.Status == "Active");
    var maintenanceDevices = await db.Devices.CountAsync(d => d.Status == "Maintenance");
    var scrappedDevices = await db.Devices.CountAsync(d => d.Status == "Scrapped");

    var categoryDistribution = await db.Devices
        .GroupBy(d => d.DeviceTypeId)
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
Console.WriteLine("=== 设备管理服务（多租户版 Phase 2）===");
Console.WriteLine($"服务地址: http://0.0.0.0:5530");
Console.WriteLine($"JWT配置: Issuer={issuer}, Audience={audience}");
Console.WriteLine("=== 服务已启动 ===");

app.Run();

// 数据库上下文
public class DeviceDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DeviceDbContext(DbContextOptions<DeviceDbContext> options) : base(options) { }

    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceCategory> DeviceCategories { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
}

// 实体类（与 TenantDbContext 中定义一致）
public class Device
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string Status { get; set; } = "Active";
    public string CurrentStatus { get; set; } = "Normal";
    public string? Remarks { get; set; }
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
    public string Type { get; set; } = "Building";
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
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 请求模型
public class CreateDeviceRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Status { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateDeviceRequest
{
    public string? Name { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? DeviceTypeId { get; set; }
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Status { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Remarks { get; set; }
}

public class CreateMaintenanceRecordRequest
{
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTime MaintenanceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Hours { get; set; }
    public string? Remarks { get; set; }
}