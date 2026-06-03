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
using WO.Property.NotificationService.Middleware;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;AllowUserVariables=true;UseAffectedRows=false";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});


// 添加JWT认证
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var issuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var audience = jwtSettings["Audience"] ?? "wo-property-services";

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
builder.Services.AddControllers();
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ─── Phase 1 多租户组件注册 ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.NotificationService.Tenant.ITenantDbFactory, WO.Property.NotificationService.Tenant.TenantDbFactory>();
builder.Services.AddScoped<IDbContextFactory<WO.Property.NotificationService.Data.TenantDbContext>>(sp =>
    new WO.Property.NotificationService.Data.TenantDbContextFactory(
        sp.GetRequiredService<WO.Property.NotificationService.Tenant.ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<WO.Property.NotificationService.Data.TenantDbContext>>()
    ));

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantRoutingMiddleware>();

// ─── Phase 1 租户路由中间件 ───
app.UseMiddleware<TenantRoutingMiddleware>();

app.MapControllers();

// 健康检查端点
app.MapGet("/health", () =>
{
    return Results.Json(new
    {
        status = "healthy",
        service = "NotificationService", mode = "Phase 1 multi-tenant",
        version = "1.0.0",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        features = new[] { "通知管理", "消息推送", "报表生成", "数据分析", "系统公告" }
    });
});

// 通知管理API
app.MapGet("/api/notifications", async (NotificationDbContext db, [FromQuery] int? userId, [FromQuery] bool? isRead) =>
{
    var query = db.Notifications.AsQueryable();
    
    if (userId.HasValue)
    {
        query = query.Where(n => n.UserId == userId.Value || n.UserId == 0); // 0表示全体用户
    }
    
    if (isRead.HasValue)
    {
        query = query.Where(n => n.IsRead == isRead.Value);
    }
    
    var notifications = await query
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();
    
    return Results.Ok(new { notifications });
});

app.MapGet("/api/notifications/{id}", async (NotificationDbContext db, int id) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "通知不存在" });
    
    return Results.Ok(new { notification });
});

app.MapPost("/api/notifications", async (NotificationDbContext db, CreateNotificationRequest request) =>
{
    var notification = new Notification
    {
        UserId = request.UserId,
        Title = request.Title,
        Content = request.Content,
        Type = request.Type,
        Priority = request.Priority,
        IsRead = false,
        RelatedEntityType = request.RelatedEntityType,
        RelatedEntityId = request.RelatedEntityId,
        CreatedAt = DateTime.UtcNow
    };
    
    db.Notifications.Add(notification);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/notifications/{notification.Id}", new { notification });
});

app.MapPut("/api/notifications/{id}/read", async (NotificationDbContext db, int id) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "通知不存在" });
    
    notification.IsRead = true;
    notification.ReadAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    
    return Results.Ok(new { notification });
});

app.MapPut("/api/notifications/read-all", async (NotificationDbContext db, [FromBody] MarkAllReadRequest request) =>
{
    var notifications = await db.Notifications
        .Where(n => n.UserId == request.UserId && !n.IsRead)
        .ToListAsync();
    
    foreach (var notification in notifications)
    {
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
    }
    
    await db.SaveChangesAsync();
    
    return Results.Ok(new { count = notifications.Count });
});

// 消息模板管理API
app.MapGet("/api/message-templates", async (NotificationDbContext db, [FromQuery] string? type) =>
{
    var query = db.MessageTemplates.AsQueryable();
    
    if (!string.IsNullOrEmpty(type))
    {
        query = query.Where(t => t.Type == type);
    }
    
    var templates = await query.ToListAsync();
    return Results.Ok(new { templates });
});

app.MapPost("/api/message-templates", async (NotificationDbContext db, CreateMessageTemplateRequest request) =>
{
    var template = new MessageTemplate
    {
        Name = request.Name,
        Type = request.Type,
        Subject = request.Subject,
        Content = request.Content,
        Variables = request.Variables,
        CreatedAt = DateTime.UtcNow
    };
    
    db.MessageTemplates.Add(template);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/message-templates/{template.Id}", new { template });
});

// 系统公告API
app.MapGet("/api/announcements", async (NotificationDbContext db, [FromQuery] bool? isActive) =>
{
    var query = db.Announcements.AsQueryable();
    
    if (isActive.HasValue)
    {
        query = query.Where(a => a.IsActive == isActive.Value);
    }
    
    var announcements = await query
        .OrderByDescending(a => a.IsTop)
        .ThenByDescending(a => a.CreatedAt)
        .ToListAsync();
    
    return Results.Ok(new { announcements });
});

app.MapPost("/api/announcements", async (NotificationDbContext db, CreateAnnouncementRequest request) =>
{
    var announcement = new Announcement
    {
        Title = request.Title,
        Content = request.Content,
        Type = request.Type,
        IsTop = request.IsTop,
        IsActive = true,
        Priority = request.Priority,
        StartDate = request.StartDate ?? DateTime.UtcNow,
        EndDate = request.EndDate,
        CreatedBy = request.CreatedBy,
        CreatedAt = DateTime.UtcNow
    };
    
    db.Announcements.Add(announcement);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/announcements/{announcement.Id}", new { announcement });
});

// 报表生成API
app.MapGet("/api/reports/tickets", async (NotificationDbContext db, [FromQuery] string? startDate, [FromQuery] string? endDate, [FromQuery] string? groupBy) =>
{
    var start = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-30) : DateTime.Parse(startDate);
    var end = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow : DateTime.Parse(endDate);
    
    // 生成工单统计报表
    var report = new
    {
        period = new { start = start.ToString("yyyy-MM-dd"), end = end.ToString("yyyy-MM-dd") },
        totalTickets = await db.TicketReports.CountAsync(r => r.CreatedAt >= start && r.CreatedAt <= end),
        newTickets = await db.TicketReports.CountAsync(r => r.Status == "New" && r.CreatedAt >= start && r.CreatedAt <= end),
        inProgressTickets = await db.TicketReports.CountAsync(r => r.Status == "InProgress" && r.CreatedAt >= start && r.CreatedAt <= end),
        resolvedTickets = await db.TicketReports.CountAsync(r => r.Status == "Resolved" && r.CreatedAt >= start && r.CreatedAt <= end),
        closedTickets = await db.TicketReports.CountAsync(r => r.Status == "Closed" && r.CreatedAt >= start && r.CreatedAt <= end),
        avgResolutionHours = 0,
        generationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    };
    
    return Results.Ok(new { report });
});

app.MapGet("/api/reports/devices", async (NotificationDbContext db, [FromQuery] string? startDate, [FromQuery] string? endDate) =>
{
    var start = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-30) : DateTime.Parse(startDate);
    var end = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow : DateTime.Parse(endDate);
    
    // 生成设备统计报表
    var report = new
    {
        period = new { start = start.ToString("yyyy-MM-dd"), end = end.ToString("yyyy-MM-dd") },
        totalDevices = await db.DeviceReports.CountAsync(),
        activeDevices = await db.DeviceReports.CountAsync(d => d.Status == "Active"),
        maintenanceDevices = await db.DeviceReports.CountAsync(d => d.Status == "Maintenance"),
        totalMaintenanceRecords = await db.DeviceReports.CountAsync(d => d.CreatedAt >= start && d.CreatedAt <= end),
        totalMaintenanceCost = 0,
        generationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    };
    
    return Results.Ok(new { report });
});

app.MapGet("/api/reports/materials", async (NotificationDbContext db, [FromQuery] string? startDate, [FromQuery] string? endDate) =>
{
    var start = string.IsNullOrEmpty(startDate) ? DateTime.UtcNow.AddDays(-30) : DateTime.Parse(startDate);
    var end = string.IsNullOrEmpty(endDate) ? DateTime.UtcNow : DateTime.Parse(endDate);
    
    // 生成物料统计报表
    var report = new
    {
        period = new { start = start.ToString("yyyy-MM-dd"), end = end.ToString("yyyy-MM-dd") },
        totalMaterials = await db.MaterialReports.CountAsync(),
        lowStockMaterials = await db.MaterialReports.CountAsync(m => m.CurrentStock <= m.SafetyStock),
        outOfStockMaterials = await db.MaterialReports.CountAsync(m => m.CurrentStock == 0),
        totalStockValue = 0,
        generationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    };
    
    return Results.Ok(new { report });
});

// 统计API
app.MapGet("/api/statistics/summary", async (NotificationDbContext db) =>
{
    var totalNotifications = await db.Notifications.CountAsync();
    var unreadNotifications = await db.Notifications.CountAsync(n => !n.IsRead);
    var activeAnnouncements = await db.Announcements.CountAsync(a => a.IsActive);
    
    return Results.Ok(new
    {
        statistics = new
        {
            totalNotifications,
            unreadNotifications,
            activeAnnouncements,
            messageTemplatesCount = await db.MessageTemplates.CountAsync()
        }
    });
});

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    
    // 使用原始SQL创建表（确保多服务共享数据库时表都能创建）
    await dbContext.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS `Notifications` (
            `Id` SERIAL PRIMARY KEY,
            `UserId` INT NOT NULL DEFAULT 0,
            `Title` VARCHAR(500) NOT NULL,
            `Content` TEXT NOT NULL,
            `Type` VARCHAR(50) NOT NULL,
            `Priority` VARCHAR(20) NOT NULL DEFAULT 'Normal',
            `IsRead` BOOLEAN NOT NULL DEFAULT FALSE,
            `ReadAt` TIMESTAMP,
            `RelatedEntityType` VARCHAR(100),
            `RelatedEntityId` INT,
            `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS `MessageTemplates` (
            `Id` SERIAL PRIMARY KEY,
            `Name` VARCHAR(200) NOT NULL,
            `Type` VARCHAR(50) NOT NULL,
            `Subject` VARCHAR(500) NOT NULL,
            `Content` TEXT NOT NULL,
            `Variables` TEXT,
            `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            `UpdatedAt` TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS `Announcements` (
            `Id` SERIAL PRIMARY KEY,
            `Title` VARCHAR(500) NOT NULL,
            `Content` TEXT NOT NULL,
            `Type` VARCHAR(50) NOT NULL,
            `IsTop` BOOLEAN NOT NULL DEFAULT FALSE,
            `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,
            `Priority` VARCHAR(20) NOT NULL DEFAULT 'Normal',
            `StartDate` TIMESTAMP NOT NULL,
            `EndDate` TIMESTAMP,
            `CreatedBy` INT NOT NULL,
            `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS `TicketReports` (
            `Id` SERIAL PRIMARY KEY,
            `TicketNumber` VARCHAR(50) NOT NULL,
            `Title` VARCHAR(200) NOT NULL,
            `Status` VARCHAR(50) NOT NULL,
            `Priority` VARCHAR(20) NOT NULL,
            `AssignedTo` INT,
            `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            `ResolvedAt` TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS `DeviceReports` (
            `Id` SERIAL PRIMARY KEY,
            `Code` VARCHAR(50) NOT NULL,
            `Name` VARCHAR(200) NOT NULL,
            `Status` VARCHAR(50) NOT NULL,
            `MaintenanceType` VARCHAR(100),
            `MaintenanceCost` DECIMAL(18,2),
            `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS `MaterialReports` (
            `Id` SERIAL PRIMARY KEY,
            `Code` VARCHAR(50) NOT NULL,
            `Name` VARCHAR(200) NOT NULL,
            `CurrentStock` INT NOT NULL DEFAULT 0,
            `SafetyStock` INT NOT NULL DEFAULT 0,
            `UnitPrice` DECIMAL(18,2) NOT NULL DEFAULT 0,
            `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
    ");
    
    // 添加初始数据
    if (!dbContext.MessageTemplates.Any())
    {
        dbContext.MessageTemplates.AddRange(
            new MessageTemplate
            {
                Name = "工单创建通知",
                Type = "Ticket",
                Subject = "【工单通知】新的工单 #{ticketId} 已创建",
                Content = "您有一条新的工单需要处理。\n工单编号：#{ticketId}\n标题：#{title}\n优先级：#{priority}\n创建时间：#{createdAt}",
                Variables = "ticketId,title,priority,createdAt",
                CreatedAt = DateTime.UtcNow
            },
            new MessageTemplate
            {
                Name = "工单处理通知",
                Type = "Ticket",
                Subject = "【工单更新】工单 #{ticketId} 状态已更新",
                Content = "工单状态已更新。\n工单编号：#{ticketId}\n新状态：#{status}\n处理人：#{assignedTo}\n更新时间：#{updatedAt}",
                Variables = "ticketId,status,assignedTo,updatedAt",
                CreatedAt = DateTime.UtcNow
            },
            new MessageTemplate
            {
                Name = "设备维护提醒",
                Type = "Device",
                Subject = "【设备维护】设备 #{deviceId} 维护提醒",
                Content = "设备维护提醒。\n设备编号：#{deviceId}\n设备名称：#{deviceName}\n维护类型：#{maintenanceType}\n计划日期：#{plannedDate}",
                Variables = "deviceId,deviceName,maintenanceType,plannedDate",
                CreatedAt = DateTime.UtcNow
            },
            new MessageTemplate
            {
                Name = "库存预警通知",
                Type = "Material",
                Subject = "【库存预警】物料 #{materialId} 库存不足",
                Content = "物料库存低于安全库存。\n物料编码：#{materialCode}\n物料名称：#{materialName}\n当前库存：#{currentStock}\n安全库存：#{safetyStock}",
                Variables = "materialCode,materialName,currentStock,safetyStock",
                CreatedAt = DateTime.UtcNow
            },
            new MessageTemplate
            {
                Name = "系统公告模板",
                Type = "Announcement",
                Subject = "【系统公告】#{title}",
                Content = "#{content}\n\n发布时间：#{createdAt}",
                Variables = "title,content,createdAt",
                CreatedAt = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }
    
    if (!dbContext.Announcements.Any())
    {
        dbContext.Announcements.AddRange(
            new Announcement
            {
                Title = "系统升级通知",
                Content = "WO物业管理系统将于本周六进行系统升级，届时系统将暂停服务约2小时。请各位用户提前做好相关工作安排。",
                Type = "System",
                IsTop = true,
                IsActive = true,
                Priority = "High",
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddDays(7),
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Announcement
            {
                Title = "新功能上线公告",
                Content = "物料管理模块已正式上线！欢迎各位同事使用。使用过程中如有问题，请联系技术支持部门。",
                Type = "Feature",
                IsTop = false,
                IsActive = true,
                Priority = "Normal",
                StartDate = DateTime.UtcNow.AddDays(-3),
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Announcement
            {
                Title = "端午节放假通知",
                Content = "根据国家节假日安排，端午节假期为6月10日至6月12日，共3天。放假期间如有紧急情况，请联系值班人员。",
                Type = "Holiday",
                IsTop = false,
                IsActive = true,
                Priority = "Normal",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(33),
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }
    
    if (!dbContext.Notifications.Any())
    {
        dbContext.Notifications.AddRange(
            new Notification
            {
                UserId = 1,
                Title = "欢迎使用WO物业管理系统",
                Content = "欢迎使用WO物业管理系统！系统提供工单管理、设备管理、物料管理等功能，祝您使用愉快！",
                Type = "Welcome",
                Priority = "Normal",
                IsRead = true,
                RelatedEntityType = "System",
                RelatedEntityId = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Notification
            {
                UserId = 1,
                Title = "工单处理提醒",
                Content = "您有待处理的工单，请尽快处理。",
                Type = "Ticket",
                Priority = "High",
                IsRead = false,
                RelatedEntityType = "Ticket",
                RelatedEntityId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Notification
            {
                UserId = 0,
                Title = "系统公告：新功能上线",
                Content = "物料管理模块已正式上线，欢迎使用！",
                Type = "Announcement",
                Priority = "Normal",
                IsRead = false,
                RelatedEntityType = "Announcement",
                RelatedEntityId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        );
        await dbContext.SaveChangesAsync();
    }
}

app.Run();

// 数据库上下文
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }
    
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<MessageTemplate> MessageTemplates { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<TicketReport> TicketReports { get; set; }
    public DbSet<DeviceReport> DeviceReports { get; set; }
    public DbSet<MaterialReport> MaterialReports { get; set; }
}

// 实体类
public class Notification
{
    public int Id { get; set; }
    
    [MaxLength(20)]
    public string? ProjectCode { get; set; }
    public int UserId { get; set; } // 0表示全体用户
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Ticket, Device, Material, Announcement, System
    public string Priority { get; set; } = string.Empty; // Low, Normal, High, Urgent
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MessageTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Variables { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // System, Feature, Holiday, Maintenance
    public bool IsTop { get; set; }
    public bool IsActive { get; set; }
    public string Priority { get; set; } = string.Empty; // Low, Normal, High
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 报表实体（用于存储统计报表数据）
public class TicketReport
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public class DeviceReport
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MaintenanceType { get; set; }
    public decimal? MaintenanceCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MaterialReport
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int SafetyStock { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 请求模型
public class CreateNotificationRequest
{
    [Required]
    public int UserId { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    [Required]
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

public class CreateMessageTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Type { get; set; } = string.Empty;
    [Required]
    public string Subject { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    public string? Variables { get; set; }
}

public class CreateAnnouncementRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    [Required]
    public string Type { get; set; } = string.Empty;
    public bool IsTop { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Required]
    public int CreatedBy { get; set; }
}

public class MarkAllReadRequest
{
    [Required]
    public int UserId { get; set; }
}
