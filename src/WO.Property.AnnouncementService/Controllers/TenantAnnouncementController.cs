using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.AnnouncementService.Data;
using WO.Property.AnnouncementService.Models;
using WO.Property.AnnouncementService.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.AnnouncementService.Controllers;

/// <summary>
/// Phase 1 多租户公告控制器
/// 使用 TenantDbContextFactory 动态切换租户库
/// </summary>
[ApiController]
[Route("api/tenant/announcements")]
public class TenantAnnouncementController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantAnnouncementController> _logger;

    public TenantAnnouncementController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantAnnouncementController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private int? GetUserIdFromJwt()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return int.TryParse(jwtToken.Claims.FirstOrDefault(
                c => c.Type == ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
        }
        catch { return null; }
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int? projectId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Announcements.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Category == type);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAnnouncements failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnnouncement(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var announcement = await db.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound(new { success = false, message = "公告不存在" });

            return Ok(new { success = true, data = announcement });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAnnouncement failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantCreateAnnouncementRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var creatorId = GetUserIdFromJwt() ?? 0;

            var announcement = new Announcement
            {
                Title = request.Title,
                Content = request.Content ?? "",
                Category = request.Type ?? "通知",
                Level = request.Priority ?? "Normal",
                Status = request.Status ?? "published",
                IsPinned = request.IsTop ?? false,
                Publisher = request.Publisher ?? "物业中心",
                StartTime = request.StartDate ?? DateTime.UtcNow,
                EndTime = request.EndDate,
                CreatedBy = creatorId.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            db.Announcements.Add(announcement);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = announcement, message = "公告创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create announcement failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TenantUpdateAnnouncementRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var announcement = await db.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound(new { success = false, message = "公告不存在" });

            if (!string.IsNullOrEmpty(request.Title)) announcement.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Content)) announcement.Content = request.Content;
            if (!string.IsNullOrEmpty(request.Status)) announcement.Status = request.Status;
            if (!string.IsNullOrEmpty(request.Level)) announcement.Level = request.Level;
            if (request.IsPinned.HasValue) announcement.IsPinned = request.IsPinned.Value;

            await db.SaveChangesAsync();
            return Ok(new { success = true, data = announcement, message = "公告更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update announcement failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var announcement = await db.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound(new { success = false, message = "公告不存在" });

            db.Announcements.Remove(announcement);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "公告删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete announcement failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class TenantCreateAnnouncementRequest
{
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public bool? IsTop { get; set; }
    public string? Publisher { get; set; }
    public int ProjectId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class TenantUpdateAnnouncementRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Status { get; set; }
    public string? Level { get; set; }
    public bool? IsPinned { get; set; }
}