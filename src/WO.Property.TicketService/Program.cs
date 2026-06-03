using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.Shared.Models;
using WO.Property.Shared.Configuration;
using WO.Property.TicketService.Data;
using WO.Property.TicketService.Tenant;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});

// Phase 0: 租户服务注册
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.TicketService.Tenant.ITenantDbFactory, WO.Property.TicketService.Tenant.TenantDbFactory>();

// PersonService HttpClient
builder.Services.AddHttpClient("PersonService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5018");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Gateway WebSocket Event HttpClient
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// DispatchService HttpClient
builder.Services.AddHttpClient("DispatchService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5241");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// MasterDataService HttpClient
builder.Services.AddHttpClient("MasterDataService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5019");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Phase 0: 临时使用 5102 端口
builder.WebHost.UseUrls("http://0.0.0.0:5102");

// Phase 0: 连接字符串配置
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:CenterDb"] = "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4",
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

// Phase 0: TenantDbContextFactory 注册
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<TenantDbContextFactory>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

// 注册超时检测背景服务
builder.Services.AddHostedService<TimeoutCheckerService>();

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

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminPortal", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175",
            "http://192.168.1.3:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();


app.UseCors("AllowAdminPortal");
app.UseAuthentication();
app.UseMiddleware<WO.Property.TicketService.Middleware.TenantRoutingMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Port changed to 5102 for Phase 0

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Ticket Service");
Console.WriteLine("  Port: 5002");
Console.WriteLine("===========================================");

app.Run();

// ============ 数据模型 ============

public class AppDbContext : DbContext
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketProcessRecord> TicketProcessRecords => Set<TicketProcessRecord>();
    public DbSet<DispatchTask> DispatchTasks => Set<DispatchTask>();
    public DbSet<TicketTypeEntity> TicketTypes => Set<TicketTypeEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("tickets");
            entity.Property(e => e.TicketCode).IsRequired().HasMaxLength(50).HasColumnName("TicketNumber");
            entity.Property(e => e.Title).HasColumnName("Title");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("Status");
            entity.Property(e => e.Priority).HasMaxLength(20).HasColumnName("Priority");
            entity.Property(e => e.Category).HasMaxLength(50).HasColumnName("TicketType");
            entity.Property(e => e.Location).HasColumnName("Location");
            entity.Property(e => e.Images).HasColumnName("Pictures");
            entity.Property(e => e.Rating).HasColumnName("Rating");
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.EscalationLevel);
            entity.Ignore(e => e.CurrentRole);
            entity.Ignore(e => e.LastEscalatedAt);
            entity.HasIndex(e => e.TicketCode);
            // 显式映射列名（数据库使用 snake_case）
            entity.Property(e => e.CreatorPersonId).HasColumnName("creator_id");
            entity.Property(e => e.AssigneePersonId).HasColumnName("assignee_id");
            entity.Property(e => e.DispatchStatus).HasColumnName("dispatch_status");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(e => e.ContactPersonName).HasColumnName("ContactPersonName");
            entity.Property(e => e.ContactPhone).HasColumnName("ContactPhone");
            entity.Property(e => e.TicketTypeId).HasColumnName("ticket_type_id");
            entity.Property(e => e.AreaId).HasColumnName("area_id");
            entity.Property(e => e.BuildingId).HasColumnName("BuildingId");
            entity.Property(e => e.RoomId).HasColumnName("RoomId");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectCode).HasColumnName("project_code");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
        });

        modelBuilder.Entity<TicketProcessRecord>(entity =>
        {
            entity.ToTable("ticket_process_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(50).HasColumnName("action");
            entity.HasIndex(e => e.TicketId);
            // 显式映射列名（数据库使用 snake_case）
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.OperatorId).HasColumnName("operator_id");
            entity.Property(e => e.OperatorName).HasColumnName("operator_name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.FromStatus).HasColumnName("from_status");
            entity.Property(e => e.ToStatus).HasColumnName("to_status");
            entity.Property(e => e.Content).HasColumnName("content");
            // 忽略基类不需要的列
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.UpdatedAt);
        });

        modelBuilder.Entity<DispatchTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("dispatch_records");
            entity.Property(e => e.TaskNo).HasColumnName("ticket_code");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.AssignedToPersonId).HasColumnName("to_person_id");
            entity.Property(e => e.AssignedTo).HasColumnName("to_person_name");
            entity.Property(e => e.AssignedBy).HasColumnName("from_person_name");
            entity.Property(e => e.AssignedAt).HasColumnName("dispatch_time");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Ignore(e => e.RuleId);
            entity.Ignore(e => e.TimeoutAt);
            entity.Ignore(e => e.Notes);
            entity.HasIndex(e => e.TicketId);
        });

        modelBuilder.Entity<TicketTypeEntity>(entity =>
        {
            entity.ToTable("ticket_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        });
    }
}

public class Ticket : BaseEntity
{
    public string TicketCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = TicketStatusValues.Created;
    public string Priority { get; set; } = "Medium";
    public int? TicketTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public int? JobTypeId { get; set; }
    public string? Location { get; set; }
    public string? Images { get; set; }
    public int? CreatorPersonId { get; set; }
    public new string? CreatedBy { get; set; }
    public int? AssigneePersonId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectCode { get; set; }  // 单租户多项目：项目代码
    public int? Rating { get; set; }
    public string? ContactPersonName { get; set; }  // 标准化：contact_name
    public string? ContactPhone { get; set; }          // 标准化：phone_number
    public string? CurrentRole { get; set; } = "operator";
    public int? EscalationLevel { get; set; } = 0;
    public DateTime? LastEscalatedAt { get; set; }
    // 工单处理时间节点
    public DateTime? AssignedAt { get; set; }      // 派单时间
    public DateTime? StartedAt { get; set; }      // 开始处理时间
    public DateTime? FinishedAt { get; set; }     // 完成时间
    public DateTime? CompletedAt { get; set; }     // 确认完成时间
    public string? DispatchStatus { get; set; }    // 派工状态
}

public class TicketProcessRecord : BaseEntity
{
    public int TicketId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Content { get; set; }
}

public class TimeoutRule
{
    public int Id { get; set; }
    public string Color { get; set; } = "green";
    public string Role { get; set; } = "operator";
    public int Hours { get; set; } = 24;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class TimeoutAlert
{
    public long Id { get; set; }
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public long? DispatchRecordId { get; set; }
    public string AlertType { get; set; } = "timeout";
    public DateTime ExpectedTime { get; set; }
    public DateTime? ActualTime { get; set; }
    public int TimeoutMinutes { get; set; }
    public int Level { get; set; } = 1;
    public int NotifyTargetId { get; set; }
    public string? NotifyTargetName { get; set; }
    public long? NotificationId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? SentAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public int ProjectId { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DispatchTask : BaseEntity
{
    public string TaskNo { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public int? RuleId { get; set; }
    public int? AssignedToPersonId { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? TimeoutAt { get; set; }
    public string? Notes { get; set; }
}

public class TicketTypeEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Status { get; set; }
    public int SortOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class TicketStatusValues
{
    public const string Created = "Created";
    public const string Dispatched = "Dispatched";
    public const string Accepted = "Accepted";
    public const string Processing = "Processing";
    public const string Finished = "Finished";
    public const string Confirmed = "Confirmed";
    public const string Survey_Pending = "Survey_Pending";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}

// ============ 请求模型 ============

public class CreateTicketRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? Location { get; set; }
    public List<string>? Images { get; set; }
    public string? ContactPersonName { get; set; }  // 标准化：contact_name
    public string? ContactPhone { get; set; }          // 标准化：phone_number
    public int? AreaId { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
}

public class DispatchRequest
{
    public int? RuleId { get; set; }
    public int? AssigneeId { get; set; }
}

// ============ DispatchTask 请求/响应模型 ============
public class CreateDispatchTaskRequest
{
    public int TicketId { get; set; }
    public int? RuleId { get; set; }
    public int? AssignedToPersonId { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedBy { get; set; }
    public string? TicketColor { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDispatchTaskStatusRequest
{
    public string? Notes { get; set; }
    public string? Result { get; set; }
    public string? Reason { get; set; }
}

public class AcceptRequest
{
    public string? Notes { get; set; }
}

public class ProgressRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
}

public class FinishRequest
{
    public string? FinishNote { get; set; }
}

public class ConfirmRequest
{
    public string? Notes { get; set; }
}

public class RejectRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class RateRequest
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

// ============ Controllers ============

[ApiController]
[Route("api")]
public class TicketController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly IHttpClientFactory httpClientFactory;

    public TicketController(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        this.db = db;
        this.httpClientFactory = httpClientFactory;
    }

    // 健康检查（统一格式）
    [HttpGet("/health")]
    public async Task<IActionResult> Health()
    {
        var mysqlHealthy = false;
        try
        {
            mysqlHealthy = await db.Database.CanConnectAsync();
        }
        catch { }
        
        var status = mysqlHealthy ? "healthy" : "unhealthy";
        var httpStatus = mysqlHealthy ? 200 : 503;
        
        var response = new
        {
            status,
            service = "WO.Property.TicketService",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            dependencies = new Dictionary<string, object>
            {
                ["mysql"] = new { status = mysqlHealthy ? "healthy" : "unhealthy" },
                ["PersonService"] = new { status = "not-checked" },
                ["MasterDataService"] = new { status = "not-checked" }
            }
        };
        
        return StatusCode(httpStatus, response);
    }

    // 获取工单列表
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var query = db.Tickets.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 批量获取人员姓名
        var personIds = tickets
            .SelectMany(t => new[] { t.CreatorPersonId, t.AssigneePersonId })
            .Where(id => id.HasValue)
            .Distinct()
            .Select(id => id!.Value)
            .ToList();

        var personNames = await FetchPersonNames(personIds);

        // 从 MasterDataService 获取枚举值名称
        var (typeNames, priorityNames, statusNames) = await FetchEnumValues();

        var result = tickets.Select(t => new {
            t.Id,
            t.TicketCode,
            t.Title,
            t.Status,
            StatusName = statusNames.GetValueOrDefault(t.Status, t.Status),
            t.Priority,
            PriorityName = priorityNames.GetValueOrDefault(t.Priority, t.Priority),
            t.Category,
            CategoryName = typeNames.GetValueOrDefault(t.Category ?? "", t.Category ?? ""),
            t.Location,
            t.CreatedAt,
            t.AreaId,
            t.BuildingId,
            dispatchStatus = t.DispatchStatus,
            // 标准化字段（contact_name / phone_number）
            ContactPersonName = t.ContactPersonName,
            ContactPhone = t.ContactPhone,
            t.CreatorPersonId,
            CreatorName = t.CreatorPersonId.HasValue
                ? personNames.GetValueOrDefault(t.CreatorPersonId!.Value)
                : null,
            t.AssigneePersonId,
            AssigneeName = t.AssigneePersonId.HasValue
                ? personNames.GetValueOrDefault(t.AssigneePersonId!.Value)
                : null,
            // 工单处理时间
            t.AssignedAt,
            t.StartedAt,
            t.FinishedAt,
            t.CompletedAt
        }).ToList();

        return Ok(new { success = true, data = result, total });
    }

    // 获取创建工单的可选值（从 MasterDataService）
    [HttpGet("tickets/options")]
    public async Task<IActionResult> GetTicketOptions()
    {
        var (typeNames, priorityNames, statusNames) = await FetchEnumValues();

        return Ok(new {
            success = true,
            data = new {
                ticketTypes = typeNames.Select(kv => new { value = kv.Key, label = kv.Value }).ToList(),
                priorities = priorityNames.Select(kv => new { value = kv.Key, label = kv.Value }).ToList(),
                ticketStatuses = statusNames.Select(kv => new { value = kv.Key, label = kv.Value }).ToList()
            }
        });
    }

    // 创建工单
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var year = DateTime.Now.Year;
        var count = await db.Tickets.CountAsync() + 1;
        var ticketNo = $"WO-{year}-{count:D4}";

        // 从 JWT 获取当前用户 ID 作为 CreatorPersonId
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? creatorPersonId = null;
        if (int.TryParse(userIdStr, out var parsedUserId))
        {
            creatorPersonId = parsedUserId;
        }

        var ticket = new Ticket
        {
            TicketCode = ticketNo,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Location = request.Location,
            Images = request.Images != null ? JsonSerializer.Serialize(request.Images) : null,
            Status = TicketStatusValues.Created,
            CreatorPersonId = creatorPersonId,
            ContactPersonName = request.ContactPersonName,  // 标准化：contact_name
            ContactPhone = request.ContactPhone,              // 标准化：phone_number
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return Ok(new { success = true, data = new { ticket.Id, ticket.TicketCode, ticket.Status } });
    }

    // 获取单个工单
    [HttpGet("tickets/{id}")]
    [Authorize]
    public async Task<IActionResult> GetTicket(int id)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });

        // 获取人员姓名
        var personIds = new List<int>();
        if (ticket.CreatorPersonId.HasValue) personIds.Add(ticket.CreatorPersonId!.Value);
        if (ticket.AssigneePersonId.HasValue) personIds.Add(ticket.AssigneePersonId!.Value);
        var personNames = await FetchPersonNames(personIds);

        var result = new {
            ticket.Id,
            ticket.TicketCode,
            ticket.Title,
            ticket.Description,
            ticket.Category,
            ticket.Status,
            dispatchStatus = ticket.DispatchStatus,
            ticket.Priority,
            ticket.Location,
            ticket.Images,
            // 标准化字段（contact_name / phone_number）
            ContactPersonName = ticket.ContactPersonName,
            ContactPhone = ticket.ContactPhone,
            ticket.CreatorPersonId,
            CreatorName = ticket.CreatorPersonId.HasValue
                ? personNames.GetValueOrDefault(ticket.CreatorPersonId!.Value)
                : null,
            ticket.AssigneePersonId,
            AssigneeName = ticket.AssigneePersonId.HasValue
                ? personNames.GetValueOrDefault(ticket.AssigneePersonId!.Value)
                : null,
            ticket.Rating,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            // 工单处理时间
            ticket.AssignedAt,
            ticket.StartedAt,
            ticket.FinishedAt,
            ticket.CompletedAt
        };

        return Ok(new { success = true, data = result });
    }

    // 1. 派单请求 dispatch - Created -> Dispatched
    [HttpPost("tickets/{id}/dispatch")]
    [Authorize]
    public async Task<IActionResult> DispatchTicket(int id, [FromBody] DispatchRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        if (ticket.Status != TicketStatusValues.Created)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能派单（需先创建工单）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        // 调用 DispatchService 创建派单任务（转发原始 JWT token）
        // DispatchService 会在内部调用 TicketService 创建 DispatchTask
        try
        {
            var client = httpClientFactory.CreateClient("DispatchService");
            var originalToken = Request.Headers["Authorization"].FirstOrDefault();

            // 从 Priority 映射到 TicketColor：Low->green, Medium->blue, High->orange, Urgent->red
            var ticketColor = ticket.Priority.ToLower() switch
            {
                "low" => "green",
                "medium" => "blue",
                "high" => "orange",
                "urgent" => "red",
                _ => "blue"
            };

            var dispatchPayload = new
            {
                TicketId = ticket.Id,
                TicketCode = ticket.TicketCode,
                RuleId = request.RuleId,
                AssignedToPersonId = request.AssigneeId,
                AssignedTo = "",
                TicketColor = ticketColor,
                TicketTypeId = ticket.TicketTypeId ?? 0,
                AreaId = ticket.AreaId ?? 0,
                BuildingId = ticket.BuildingId ?? 0,
                ProjectId = ticket.ProjectId
            };

            var dispatchRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tenant/dispatch/auto")
            {
                Content = JsonContent.Create(dispatchPayload)
            };
            if (!string.IsNullOrEmpty(originalToken))
                dispatchRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(originalToken);

            var response = await client.SendAsync(dispatchRequest);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return BadRequest(new { success = false, message = $"派单服务调用失败: {response.StatusCode}" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"无法连接派单服务: {ex.Message}" });
        }

        // 更新工单状态
        ticket.Status = TicketStatusValues.Dispatched;
        ticket.AssigneePersonId = request.AssigneeId;
        ticket.AssignedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        // 记录处理历史
        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "dispatch",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Dispatched,
            Content = $"派单给处理人 ID={request.AssigneeId}",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "已派单", data = new { ticket.Status } });
    }

    // 2. 接单 accept - Dispatched -> Accepted
    [HttpPost("tickets/{id}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptTicket(int id, [FromBody] AcceptRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        if (ticket.Status != TicketStatusValues.Dispatched)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能接单（需先派单）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        ticket.Status = TicketStatusValues.Accepted;
        ticket.StartedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "accept",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Accepted,
            Content = request.Notes ?? "接单",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "已接单", data = new { ticket.Status } });
    }

    // 3. 处理进度 progress - Accepted -> Processing
    [HttpPost("tickets/{id}/progress")]
    [Authorize]
    public async Task<IActionResult> ProgressTicket(int id, [FromBody] ProgressRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        if (ticket.Status != TicketStatusValues.Accepted && ticket.Status != TicketStatusValues.Processing)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能更新进度" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        ticket.Status = TicketStatusValues.Processing;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "progress",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Processing,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "进度已更新", data = new { ticket.Status } });
    }

    // 4. 完成工单 finish - Processing -> Finished
    [HttpPost("tickets/{id}/finish")]
    [Authorize]
    public async Task<IActionResult> FinishTicket(int id, [FromBody] FinishRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        if (ticket.Status != TicketStatusValues.Processing)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能完成（需先处理中）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        ticket.Status = TicketStatusValues.Finished;
        ticket.FinishedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "finish",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Finished,
            Content = request.FinishNote ?? "工单完成",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "工单已完成", data = new { ticket.Status } });
    }

    // 5. 评价 rate - Finished -> Closed
    [HttpPost("tickets/{id}/rate")]
    [Authorize]
    public async Task<IActionResult> RateTicket(int id, [FromBody] RateRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        if (ticket.Status != TicketStatusValues.Finished && ticket.Status != TicketStatusValues.Survey_Pending)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能评价（需先确认完工）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        ticket.Status = TicketStatusValues.Closed;
        ticket.Rating = request.Rating;
        ticket.CompletedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "rate",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Closed,
            Content = $"评价: {request.Rating}星" + (request.Comment != null ? $" ({request.Comment})" : ""),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "感谢您的评价", data = new { ticket.Status, ticket.Rating } });
    }

    // 5.5 确认完工 confirm - Finished/Confirmed -> Confirmed
    [HttpPost("tickets/{id}/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTicket(int id, [FromBody] ConfirmRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        // 允许从 Finished 或 Confirmed 状态确认（幂等）
        if (ticket.Status != TicketStatusValues.Finished && ticket.Status != TicketStatusValues.Confirmed)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能确认完工（需先完成）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        ticket.Status = TicketStatusValues.Survey_Pending;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "confirm",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Survey_Pending,
            Content = request.Notes ?? "确认完工",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "已确认完工", data = new { ticket.Status } });
    }

    // 6. 拒单 reject - Dispatched -> Rejected -> (auto) Created
    [HttpPost("tickets/{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectTicket(int id, [FromBody] RejectRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { success = false, message = "工单不存在" });
        if (ticket.Status != TicketStatusValues.Dispatched)
            return BadRequest(new { success = false, message = $"状态 {ticket.Status} 不能拒单（需先派单）" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var operatorId = int.TryParse(userId, out var oid) ? oid : 0;
        var operatorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "系统";

        var fromStatus = ticket.Status;

        // 拒单后回退到 Created（重新派单）
        ticket.Status = TicketStatusValues.Created;
        ticket.AssigneePersonId = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketProcessRecords.Add(new TicketProcessRecord
        {
            TicketId = id,
            Action = "reject",
            OperatorId = operatorId,
            OperatorName = operatorName,
            FromStatus = fromStatus,
            ToStatus = TicketStatusValues.Created,
            Content = $"拒单原因: {request.Reason}",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "已拒单，请等待重新派单", data = new { ticket.Status } });
    }

    // 获取工单处理历史
    [HttpGet("tickets/{id}/history")]
    public async Task<IActionResult> GetTicketHistory(int id)
    {
        var records = await db.TicketProcessRecords
            .Where(r => r.TicketId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(new { success = true, data = records });
    }

    // 获取工单统计数据
    [HttpGet("tickets/stats/daily")]
    public async Task<IActionResult> GetDailyStats()
    {
        var today = DateTime.UtcNow.Date;
        var tickets = await db.Tickets
            .Where(t => t.CreatedAt >= today)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                date = today.ToString("yyyy-MM-dd"),
                total = tickets.Count,
                byStatus = tickets.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count()),
                byPriority = tickets.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count())
            }
        });
    }

    // 获取创建人信息（内部接口）
    [HttpGet("internal/tickets/{id}/creator")]
    public async Task<IActionResult> GetCreator(int id)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        // 使用 CreatorPersonId 调用 PersonService 获取姓名
        string? creatorName = null;
        if (ticket.CreatorPersonId.HasValue)
        {
            var personNames = await FetchPersonNames(new List<int> { ticket.CreatorPersonId.Value });
            personNames.TryGetValue(ticket.CreatorPersonId.Value, out creatorName);
        }

        return Ok(new { id = ticket.CreatorPersonId, name = creatorName });
    }

    // 获取处理人信息（内部接口）
    [HttpGet("internal/tickets/{id}/assignee")]
    public async Task<IActionResult> GetAssignee(int id)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        return Ok(new { id = ticket.AssigneePersonId });
    }

    // ============ DispatchTask 管理接口（派单任务统一由 TicketService 管理）============

    // 获取工单的派单任务
    [HttpGet("tickets/{id}/dispatch-task")]
    public async Task<IActionResult> GetDispatchTask(int id)
    {
        var task = await db.DispatchTasks.FirstOrDefaultAsync(t => t.TicketId == id);
        if (task == null)
            return NotFound(new { success = false, message = "该工单没有派单任务" });
        return Ok(new { success = true, data = task });
    }

    // 内部接口：创建派单任务（由 DispatchService 调用）
    [HttpPost("internal/dispatch-tasks")]
    public async Task<IActionResult> CreateDispatchTask([FromBody] CreateDispatchTaskRequest request)
    {
        var ticket = await db.Tickets.FindAsync(request.TicketId);
        if (ticket == null)
            return NotFound(new { success = false, message = "工单不存在" });

        // 检查是否已存在派单任务
        var existing = await db.DispatchTasks.FirstOrDefaultAsync(t => t.TicketId == request.TicketId);
        if (existing != null)
            return Ok(new { success = true, message = "派单任务已存在", data = existing });

        var year = DateTime.Now.Year;
        var count = await db.DispatchTasks.CountAsync() + 1;

        var task = new DispatchTask
        {
            TaskNo = $"DT-{year}-{count:D5}",
            TicketId = request.TicketId,
            RuleId = request.RuleId,
            AssignedToPersonId = request.AssignedToPersonId,
            AssignedTo = request.AssignedTo,
            AssignedBy = request.AssignedBy,
            AssignedAt = DateTime.UtcNow,
            Status = "pending",
            Notes = request.Notes
        };

        db.DispatchTasks.Add(task);
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "派单任务已创建", data = task });
    }

    // 接受派单任务
    [HttpPut("tickets/{id}/dispatch-task/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptDispatchTask(int id)
    {
        var task = await db.DispatchTasks.FirstOrDefaultAsync(t => t.TicketId == id);
        if (task == null)
            return NotFound(new { success = false, message = "派单任务不存在" });
        if (task.Status != "pending")
            return BadRequest(new { success = false, message = $"任务状态不是待接受: {task.Status}" });

        task.Status = "accepted";
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "已接受任务", data = task });
    }

    // 拒绝派单任务
    [HttpPut("tickets/{id}/dispatch-task/reject")]
    [Authorize]
    public async Task<IActionResult> RejectDispatchTask(int id, [FromBody] UpdateDispatchTaskStatusRequest request)
    {
        var task = await db.DispatchTasks.FirstOrDefaultAsync(t => t.TicketId == id);
        if (task == null)
            return NotFound(new { success = false, message = "派单任务不存在" });
        if (task.Status != "pending")
            return BadRequest(new { success = false, message = $"任务状态不是待接受: {task.Status}" });

        task.Status = "rejected";
        task.Notes = string.IsNullOrEmpty(task.Notes)
            ? $"拒绝原因: {request?.Reason}"
            : (task.Notes + $"; 拒绝原因: {request?.Reason}");
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "已拒绝任务", data = task });
    }

    // 完成派单任务
    [HttpPut("tickets/{id}/dispatch-task/complete")]
    [Authorize]
    public async Task<IActionResult> CompleteDispatchTask(int id, [FromBody] UpdateDispatchTaskStatusRequest request)
    {
        var task = await db.DispatchTasks.FirstOrDefaultAsync(t => t.TicketId == id);
        if (task == null)
            return NotFound(new { success = false, message = "派单任务不存在" });

        task.Status = "completed";
        task.Notes = string.IsNullOrEmpty(task.Notes)
            ? $"完成: {request?.Result}"
            : (task.Notes + $"; 完成: {request?.Result}");
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "任务已完成", data = task });
    }

    // 取消派单任务
    [HttpPut("tickets/{id}/dispatch-task/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelDispatchTask(int id, [FromBody] UpdateDispatchTaskStatusRequest request)
    {
        var task = await db.DispatchTasks.FirstOrDefaultAsync(t => t.TicketId == id);
        if (task == null)
            return NotFound(new { success = false, message = "派单任务不存在" });

        task.Status = "cancelled";
        task.Notes = string.IsNullOrEmpty(task.Notes)
            ? $"取消原因: {request?.Reason}"
            : (task.Notes + $"; 取消原因: {request?.Reason}");
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "任务已取消", data = task });
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 批量从 PersonService 获取人员姓名。
    /// 如果 PersonService 不可用，返回 ID 而非姓名，并记录警告日志。
    /// </summary>
    private async Task<Dictionary<int, string>> FetchPersonNames(List<int> personIds)
    {
        var result = new Dictionary<int, string>();
        if (personIds == null || personIds.Count == 0)
            return result;

        var uniqueIds = personIds.Distinct().ToList();
        var client = httpClientFactory.CreateClient("PersonService");

        foreach (var id in uniqueIds)
        {
            try
            {
                var response = await client.GetAsync($"/api/persons/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                        result[id] = nameProp.GetString() ?? id.ToString();
                    else if (doc.RootElement.TryGetProperty("data", out var dataProp) &&
                             dataProp.TryGetProperty("name", out var dataNameProp))
                        result[id] = dataNameProp.GetString() ?? id.ToString();
                    else
                        result[id] = id.ToString();
                }
                else
                {
                    Console.WriteLine($"[警告] PersonService 返回 {response.StatusCode} for person id={id}，返回 ID 而非姓名");
                    result[id] = id.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] 无法连接 PersonService 获取人员信息 id={id}: {ex.Message}，返回 ID 而非姓名");
                result[id] = id.ToString();
            }
        }

        return result;
    }

    /// <summary>
    /// 批量获取枚举值（工单类型、优先级、状态）。
    /// 如果 MasterDataService 不可用，返回 ID 而非名称，并记录警告日志。
    /// </summary>
    private async Task<(Dictionary<string, string> TypeNames, Dictionary<string, string> PriorityNames, Dictionary<string, string> StatusNames)> FetchEnumValues()
    {
        var typeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var priorityNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var statusNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var client = httpClientFactory.CreateClient("MasterDataService");

        // 获取工单类型
        try
        {
            var response = await client.GetAsync("/api/enums/ticket-types");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var val) && item.TryGetProperty("label", out var lbl))
                            typeNames[val.GetString() ?? ""] = lbl.GetString() ?? "";
                    }
                }
            }
            else
            {
                Console.WriteLine($"[警告] MasterDataService 返回 {response.StatusCode} 获取 ticket-types");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 无法连接 MasterDataService 获取 ticket-types: {ex.Message}");
        }

        // 获取优先级
        try
        {
            var response = await client.GetAsync("/api/enums/priorities");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var val) && item.TryGetProperty("label", out var lbl))
                            priorityNames[val.GetString() ?? ""] = lbl.GetString() ?? "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 无法连接 MasterDataService 获取 priorities: {ex.Message}");
        }

        // 获取工单状态
        try
        {
            var response = await client.GetAsync("/api/enums/ticket-statuses");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty("value", out var val) && item.TryGetProperty("label", out var lbl))
                            statusNames[val.GetString() ?? ""] = lbl.GetString() ?? "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 无法连接 MasterDataService 获取 ticket-statuses: {ex.Message}");
        }

        return (typeNames, priorityNames, statusNames);
    }
}

// 超时检测背景服务
public class TimeoutCheckerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimeoutCheckerService> _logger;
    private readonly System.Timers.Timer _timer;
    
    private const int CHECK_INTERVAL_MS = 5 * 60 * 1000;

    public TimeoutCheckerService(IServiceProvider serviceProvider, ILogger<TimeoutCheckerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timer = new System.Timers.Timer(CHECK_INTERVAL_MS);
        _timer.Elapsed += async (s, e) => await CheckTimeoutsAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimeoutCheckerService started");
        _timer.Start();
        await CheckTimeoutsAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CHECK_INTERVAL_MS, stoppingToken);
        }
    }

    private async Task CheckTimeoutsAsync()
    {
        try
        {
            _logger.LogInformation("Checking ticket timeouts...");
            
            using var scope = _serviceProvider.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();

            var timeoutRules = await db.TimeoutRules.Where(r => r.Enabled).ToListAsync();
            if (!timeoutRules.Any())
            {
                _logger.LogWarning("No timeout rules found");
                return;
            }

            var activeStatuses = new[] { "Created", "Dispatched", "Accepted", "Processing" };
            var tickets = await db.Tickets.Where(t => activeStatuses.Contains(t.Status)).ToListAsync();

            _logger.LogInformation("Found {Count} active tickets to check", tickets.Count);

            foreach (var ticket in tickets)
            {
                await CheckTicketTimeout(db, ticket, timeoutRules);
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Timeout check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking timeouts");
        }
    }

    private async Task CheckTicketTimeout(TenantDbContext db, Ticket ticket, List<TimeoutRule> rules)
    {
        try
        {
            var color = MapPriorityToColor(ticket.Priority);
            var currentRole = ticket.CurrentRole ?? "operator";
            
            var rule = rules.FirstOrDefault(r => r.Color == color && r.Role == currentRole && r.Enabled);
            if (rule == null)
            {
                rule = rules.FirstOrDefault(r => r.Color == color && r.Enabled);
            }

            if (rule == null) return;

            var createdAt = ticket.CreatedAt;
            var expectedTime = createdAt.AddHours(rule.Hours);
            var now = DateTime.UtcNow;

            if (now > expectedTime)
            {
                var roleLevel = GetRoleLevel(currentRole);
                var existingAlert = await db.TimeoutAlerts
                    .FirstOrDefaultAsync(a => a.TicketId == ticket.Id && a.Status == "Pending" && a.Level == roleLevel);

                if (existingAlert == null)
                {
                    var alert = new TimeoutAlert
                    {
                        TicketId = ticket.Id,
                        TicketCode = ticket.TicketCode,
                        AlertType = "timeout",
                        ExpectedTime = expectedTime,
                        ActualTime = now,
                        TimeoutMinutes = (int)(now - expectedTime).TotalMinutes,
                        Level = roleLevel,
                        NotifyTargetId = 0,
                        NotifyTargetName = GetNextRoleName(currentRole),
                        Status = "Pending",
                        TenantCode = "YGHY001",
                        ProjectId = ticket.ProjectId,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.TimeoutAlerts.Add(alert);
                    
                    _logger.LogWarning("Ticket {TicketCode} timeout! Color={Color}, Role={Role}, ExpectedTime={ExpectedTime}", 
                        ticket.TicketCode, color, currentRole, expectedTime);

                    await EscalateTicket(db, ticket, currentRole);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ticket {TicketId} timeout", ticket.Id);
        }
    }

    private async Task EscalateTicket(TenantDbContext db, Ticket ticket, string currentRole)
    {
        var nextRole = GetNextRole(currentRole);
        if (nextRole != null)
        {
            ticket.CurrentRole = nextRole;
            ticket.EscalationLevel = (ticket.EscalationLevel ?? 0) + 1;
            ticket.LastEscalatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Ticket {TicketCode} escalated from {OldRole} to {NewRole}", 
                ticket.TicketCode, currentRole, nextRole);
        }
    }

    private string MapPriorityToColor(string? priority)
    {
        return priority?.ToLower() switch
        {
            "urgent" or "high" => "red",
            "medium" or "normal" => "blue",
            "low" => "green",
            _ => "green"
        };
    }

    private int GetRoleLevel(string role)
    {
        return role?.ToLower() switch
        {
            "operator" => 1,
            "supervisor" => 2,
            "manager" => 3,
            "department_head" => 4,
            "company_head" => 5,
            _ => 1
        };
    }

    private string? GetNextRole(string currentRole)
    {
        return currentRole?.ToLower() switch
        {
            "operator" => "supervisor",
            "supervisor" => "manager",
            "manager" => "department_head",
            "department_head" => "company_head",
            "company_head" => null,
            _ => null
        };
    }

    private string GetNextRoleName(string currentRole)
    {
        var nextRole = GetNextRole(currentRole);
        return nextRole?.ToUpper() ?? "UNKNOWN";
    }

    public override void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        base.Dispose();
    }
}
