using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.AnnouncementService.Data;
using WO.Property.AnnouncementService.Models;
using WO.Property.Shared.Configuration;
using WO.Property.AnnouncementService.Tenant;
using WO.Property.AnnouncementService.Middleware;
using WO.Property.AnnouncementService.Data;

var builder = WebApplication.CreateBuilder(args);

// 端口配置
builder.WebHost.UseUrls("http://0.0.0.0:5511");

// 添加连接字符串配置（供 TenantDbFactory 使用）
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4",
    ["ConnectionStrings:CenterDb"] = "Server=127.0.0.1;Port=3306;Database=center_db;User=root;Password=;CharSet=utf8mb4"
});

builder.Services.AddDbContext<AnnouncementDbContext>(options =>
{
    var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    options.UseMySql(new MySqlConnection(connectionString), serverVersion);
});

var jwtKey = JwtHelper.GetSecretKey();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = false, ValidateAudience = false,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(opts => {
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddControllers();

// Gateway HttpClient for event publishing
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Phase 1: 多租户连接字符串配置
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy => {
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5173" };
    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));


// Phase 1: 租户服务注册（必须在 builder.Build() 之前）
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();

// Phase 1: TenantDbContextFactory 注册
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<TenantDbContextFactory>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

var app = builder.Build();;

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantRoutingMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AnnouncementService", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Announcement Service");
Console.WriteLine("  Port: 5502");
Console.WriteLine("===========================================");

app.Run();

// ============ 公告控制器 ============

[ApiController]
[Route("api/announcements")]
public class AnnouncementController : ControllerBase
{
    private readonly AnnouncementDbContext _context;

    public AnnouncementController(AnnouncementDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int? projectId = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? pinned = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _context.Announcements.AsQueryable();
        if (projectId.HasValue) q = q.Where(a => a.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(category)) q = q.Where(a => a.Category == category);
        if (pinned == true) q = q.Where(a => a.IsPinned);
        
        var activeOnly = q.Where(a => a.Status == "published" && 
            (a.StartTime == null || a.StartTime <= DateTime.UtcNow) &&
            (a.EndTime == null || a.EndTime >= DateTime.UtcNow));

        var total = await activeOnly.CountAsync();
        var items = await activeOnly
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new { success = true, total, page, pageSize, data = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnnouncement(int id)
    {
        var a = await _context.Announcements.FindAsync(id);
        if (a == null) return NotFound();
        return Ok(new { success = true, data = a });
    }

    [HttpPost]
    public async Task<IActionResult> CreateAnnouncement([FromBody] Announcement announcement)
    {
        announcement.PublishTime = DateTime.UtcNow;
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        // 如果立即发布，推送给所有住户
        if (announcement.Status == "published") {
            // 通知写入 MobileService（假设在5015）
            try {
                using var hc = new HttpClient();
                await hc.PostAsJsonAsync("http://localhost:5015/api/mobile/notifications", new {
                    userId = "all_users",
                    type = "Announcement",
                    title = announcement.Title,
                    content = announcement.Content.Length > 50 
                        ? announcement.Content[..50] + "..." 
                        : announcement.Content,
                    data = JsonSerializer.Serialize(new { announcementId = announcement.Id })
                });
            } catch { /* notification is non-blocking */ }
        }

        return Ok(new { success = true, data = announcement });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAnnouncement(int id, [FromBody] Announcement patch)
    {
        var a = await _context.Announcements.FindAsync(id);
        if (a == null) return NotFound();
        a.Title = patch.Title;
        a.Content = patch.Content;
        a.Category = patch.Category;
        a.Level = patch.Level;
        a.IsPinned = patch.IsPinned;
        a.Status = patch.Status;
        a.StartTime = patch.StartTime;
        a.EndTime = patch.EndTime;
        a.TargetBuildings = patch.TargetBuildings;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = a });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var a = await _context.Announcements.FindAsync(id);
        if (a == null) return NotFound();
        _context.Announcements.Remove(a);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        return Ok(new { success = true, data = new[] {
            new { value = "property", label = "物业通知" },
            new { value = "security", label = "安全公告" },
            new { value = "maintenance", label = "设施维护" },
            new { value = "activity", label = "社区活动" },
            new { value = "emergency", label = "紧急通知" },
            new { value = "other", label = "其他" }
        }});
    }
}
