using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WO.Property.NotificationService.Data;
using WO.Property.NotificationService.Models;
using System.Text;

namespace WO.Property.NotificationService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/notification")]
public class TenantNotificationController : ControllerBase
{
    // 获取当前项目代码（从 X-Project header）
    private string? GetProjectCode()
    {
        if (Request.Headers.TryGetValue("X-Project", out var projectValues))
        {
            var projectCode = projectValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(projectCode))
                return projectCode;
        }
        return null;
    }


    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantNotificationController> _logger;

    public TenantNotificationController(
        IDbContextFactory<TenantDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantNotificationController> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private async Task PublishNotificationEventAsync(string eventType, object data)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var payload = new
            {
                module = "notification",
                eventType = eventType,
                data = data
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );
            await client.PostAsync("/internal/events/publish", content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish notification event: {EventType}", eventType);
        }
    }

    // GET /api/tenant/notification/notifications
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? userId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Notifications.AsQueryable();
            
            var projectCode = GetProjectCode();
            if (!string.IsNullOrEmpty(projectCode))
            {
                query = query.Where(x => x.ProjectCode == projectCode);
            }

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

            // 发布通知创建事件
            await PublishNotificationEventAsync("created", new
            {
                id = notification.Id,
                userId = notification.UserId,
                title = notification.Title,
                content = notification.Content,
                type = notification.Type,
                isRead = notification.IsRead
            });

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

            // 发布通知已读事件
            await PublishNotificationEventAsync("read", new
            {
                id = notification.Id,
                userId = notification.UserId,
                title = notification.Title
            });

            return Ok(new { success = true, message = "已标记为已读" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkAsRead failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/notification/notifications/{id}
    [HttpPut("notifications/{id}")]
    public async Task<IActionResult> UpdateNotification(int id, [FromBody] UpdateNotificationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var notification = await db.Notifications.FindAsync(id);
            if (notification == null) return NotFound(new { success = false, message = "通知不存在" });

            
            if (!string.IsNullOrEmpty(request.Title))
                notification.Title = request.Title;
            if (request.Content != null)
                notification.Content = request.Content;
            if (!string.IsNullOrEmpty(request.Type))
                notification.Type = request.Type;
            if (!string.IsNullOrEmpty(request.Priority))
                notification.Priority = request.Priority;
            
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "通知已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateNotification failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/notification/notifications/{id}
    [HttpDelete("notifications/{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var notification = await db.Notifications.FindAsync(id);
            if (notification == null) return NotFound(new { success = false, message = "通知不存在" });
            
            db.Notifications.Remove(notification);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "通知已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteNotification failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ============ 满意度调查通知 API ============
    
    // POST /api/tenant/notification/survey-invite
    // 发送满意度调查邀请（工单确认完工后触发）
    [HttpPost("survey-invite")]
    public async Task<IActionResult> SendSurveyInvite([FromBody] SurveyInviteRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            
            // 查找工单关联的住户（通过 reporter 或 contact）
            // 构建满意度调查链接
            var surveyUrl = $"http://localhost:5173/#/survey?ticketId={request.TicketId}";
            
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = "请对本次服务评分",
                Content = $"工单【{request.TicketNo}】已完成，请您对本次服务进行评价。点击查看：{surveyUrl}",
                Type = "SurveyInvite",
                Priority = "Normal",
                IsRead = false,
                RelatedEntityType = "Ticket",
                RelatedEntityId = request.TicketId,
                CreatedAt = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            
            // 同时记录满意度调查状态（如果表存在）
            // 这里只是发送通知，survey 表由 TicketService 维护
            
            await db.SaveChangesAsync();
            
            _logger.LogInformation("Survey invite sent for TicketId={TicketId}, UserId={UserId}", 
                request.TicketId, request.UserId);
            
            return Ok(new { 
                success = true, 
                message = "满意度调查通知已发送", 
                data = new { 
                    notificationId = notification.Id,
                    ticketId = request.TicketId,
                    surveyStatus = "sent"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendSurveyInvite failed for TicketId={TicketId}", request.TicketId);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/notification/survey/{ticketId}
    // 获取工单的满意度调查状态
    [HttpGet("survey/{ticketId}")]
    public async Task<IActionResult> GetSurveyStatus(int ticketId)
    {
        try
        {
            using var db = CreateDbContext();
            
            // 查找相关通知
            var notification = await db.Notifications
                .Where(n => n.RelatedEntityType == "Ticket" && n.RelatedEntityId == ticketId && n.Type == "SurveyInvite")
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (notification == null)
            {
                return Ok(new { 
                    success = true, 
                    data = new { 
                        ticketId = ticketId,
                        status = "pending",  // 还未发送调查
                        ratingOptions = new[] { 1, 2, 3, 4, 5 },
                        message = "请对本次服务评分"
                    }
                });
            }
            
            return Ok(new { 
                success = true, 
                data = new { 
                    ticketId = ticketId,
                    notificationId = notification.Id,
                    status = notification.IsRead ? "surveyed" : "sent",
                    ratingOptions = new[] { 1, 2, 3, 4, 5 },
                    message = "请对本次服务评分",
                    sentAt = notification.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSurveyStatus failed for TicketId={TicketId}", ticketId);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ==================== 消息模板管理 ====================
    [HttpGet("message-templates")]
    public async Task<IActionResult> GetMessageTemplates()
    {
        try
        {
            using var db = CreateDbContext();
            var templates = await db.MessageTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMessageTemplates failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("message-templates/{id}")]
    public async Task<IActionResult> GetMessageTemplate(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var template = await db.MessageTemplates.FindAsync(id);
            if (template == null)
                return NotFound(new { success = false, message = "模板不存在" });
            return Ok(new { success = true, data = template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMessageTemplate failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("message-templates")]
    public async Task<IActionResult> CreateMessageTemplate([FromBody] CreateMessageTemplateRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var template = new MessageTemplate
            {
                Name = request.Name,
                Type = request.Type,
                Subject = request.Subject,
                Content = request.Content,
                Variables = request.Variables ?? "",
                CreatedAt = DateTime.UtcNow
            };
            db.MessageTemplates.Add(template);
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateMessageTemplate failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("message-templates/{id}")]
    public async Task<IActionResult> UpdateMessageTemplate(int id, [FromBody] UpdateMessageTemplateRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var template = await db.MessageTemplates.FindAsync(id);
            if (template == null)
                return NotFound(new { success = false, message = "模板不存在" });
            
            if (!string.IsNullOrEmpty(request.Name)) template.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Type)) template.Type = request.Type;
            if (!string.IsNullOrEmpty(request.Subject)) template.Subject = request.Subject;
            if (!string.IsNullOrEmpty(request.Content)) template.Content = request.Content;
            if (request.Variables != null) template.Variables = request.Variables;
            template.UpdatedAt = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateMessageTemplate failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("message-templates/{id}")]
    public async Task<IActionResult> DeleteMessageTemplate(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var template = await db.MessageTemplates.FindAsync(id);
            if (template == null)
                return NotFound(new { success = false, message = "模板不存在" });
            
            db.MessageTemplates.Remove(template);
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteMessageTemplate failed");
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

public class SurveyInviteRequest
{
    public int TicketId { get; set; }
    public string? TicketNo { get; set; }
    public int UserId { get; set; }
}

public class CreateMessageTemplateRequest
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Variables { get; set; }
}

public class UpdateMessageTemplateRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string? Variables { get; set; }
}

public class UpdateNotificationRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
}
