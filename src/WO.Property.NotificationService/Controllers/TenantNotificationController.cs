using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.NotificationService.Data;
using WO.Property.NotificationService.Models;

namespace WO.Property.NotificationService.Controllers;

[ApiController]
[Route("api/tenant/notification")]
public class TenantNotificationController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantNotificationController> _logger;

    public TenantNotificationController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantNotificationController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/notification/notifications
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Notifications.AsQueryable();
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value || n.UserId == 0);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    id = n.Id,
                    userId = n.UserId,
                    title = n.Title,
                    content = n.Content,
                    type = n.Type,
                    priority = n.Priority,
                    isRead = n.IsRead,
                    relatedEntityType = n.RelatedEntityType ?? "",
                    relatedEntityId = n.RelatedEntityId,
                    createdAt = n.CreatedAt
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNotifications failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/notification/notifications
    [HttpPost("notifications")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Content = request.Content,
                Type = request.Type ?? "System",
                Priority = request.Priority ?? "Normal",
                IsRead = false,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                CreatedAt = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "通知创建成功", data = new { id = notification.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateNotification failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/notification/notifications/{id}/read
    [HttpPut("notifications/{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var notification = await db.Notifications.FindAsync(id);
            if (notification == null) return NotFound(new { success = false, message = "通知不存在" });
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "已标记为已读" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkAsRead failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}
