using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.CommunityService.Data;
using WO.Property.CommunityService.Models;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.CommunityService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/community")]
public class TenantCommunityController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantCommunityController> _logger;

    public TenantCommunityController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantCommunityController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // ===== Activities =====
    // GET /api/tenant/community/activities
    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Activities.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    description = a.Description ?? "",
                    location = a.Location,
                    startTime = a.StartTime,
                    endTime = a.EndTime,
                    maxParticipants = a.MaxParticipants,
                    currentParticipants = a.CurrentParticipants,
                    status = a.Status,
                    coverImage = a.CoverImage ?? "",
                    projectId = a.ProjectId
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActivities failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/community/activities
    [HttpPost("activities")]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var activity = new Activity
            {
                Name = request.Name,
                Description = request.Description,
                Location = request.Location,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                MaxParticipants = request.MaxParticipants,
                Status = "draft",
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "活动创建成功", data = new { id = activity.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateActivity failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/community/activities/{id}
    [HttpGet("activities/{id}")]
    public async Task<IActionResult> GetActivity(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var activity = await db.Activities.FindAsync(id);
            if (activity == null)
                return Ok(new { success = false, message = "未找到活动" });
            return Ok(new { success = true, data = activity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActivity failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/community/activities/{id}
    [HttpPut("activities/{id}")]
    public async Task<IActionResult> UpdateActivity(int id, [FromBody] UpdateActivityRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var activity = await db.Activities.FindAsync(id);
            if (activity == null)
                return Ok(new { success = false, message = "未找到活动" });
            activity.Name = request.Name ?? activity.Name;
            activity.Description = request.Description ?? activity.Description;
            activity.Location = request.Location ?? activity.Location;
            if (request.StartTime.HasValue) activity.StartTime = request.StartTime.Value;
            if (request.EndTime.HasValue) activity.EndTime = request.EndTime.Value;
            if (request.MaxParticipants.HasValue) activity.MaxParticipants = request.MaxParticipants.Value;
            if (!string.IsNullOrEmpty(request.Status)) activity.Status = request.Status;
            activity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "活动更新成功", data = activity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateActivity failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/community/activities/{id}
    [HttpDelete("activities/{id}")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var activity = await db.Activities.FindAsync(id);
            if (activity == null)
                return Ok(new { success = false, message = "未找到活动" });
            db.Activities.Remove(activity);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "活动删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteActivity failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/community/activities/{id}/join
    [HttpPost("activities/{id}/join")]
    public async Task<IActionResult> JoinActivity(int id, [FromBody] JoinActivityRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var activity = await db.Activities.FindAsync(id);
            if (activity == null)
                return Ok(new { success = false, message = "未找到活动" });
            if (activity.Status != "open" && activity.Status != "draft")
                return Ok(new { success = false, message = "活动不在报名状态" });
            if (activity.CurrentParticipants >= activity.MaxParticipants)
                return Ok(new { success = false, message = "报名已满" });

            var enrollment = new ActivityEnrollment
            {
                ActivityId = id,
                Name = request.Name,
                Phone = request.Phone,
                Note = request.Note,
                Status = "enrolled",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.ActivityEnrollments.Add(enrollment);
            activity.CurrentParticipants++;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "报名成功", data = new { enrollmentId = enrollment.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JoinActivity failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ===== Notices =====
    // GET /api/tenant/community/notices
    [HttpGet("notices")]
    public async Task<IActionResult> GetNotices([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? type = null, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Notices.AsQueryable();
            if (!string.IsNullOrEmpty(type))
                query = query.Where(n => n.Type == type);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(n => n.Status == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.Top != null && n.Top > 0)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    content = n.Content ?? "",
                    type = n.Type,
                    status = n.Status,
                    top = n.Top,
                    createdAt = n.CreatedAt
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNotices failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/community/notices
    [HttpPost("notices")]
    public async Task<IActionResult> CreateNotice([FromBody] CreateNoticeRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var notice = new Notice
            {
                Title = request.Title,
                Content = request.Content,
                Type = request.Type ?? "general",
                Status = request.Status ?? "published",
                ProjectId = request.ProjectId,
                Top = request.Top,
                CreatedAt = DateTime.UtcNow
            };
            db.Notices.Add(notice);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "公告发布成功", data = new { id = notice.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateNotice failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ===== Suggestions =====
    // GET /api/tenant/community/suggestions
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Suggestions.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    id = s.Id,
                    projectId = s.ProjectId,
                    title = s.Title,
                    content = s.Content ?? "",
                    contactName = s.ContactName ?? "",
                    contactPhone = s.ContactPhone ?? "",
                    status = s.Status,
                    reply = s.Reply ?? "",
                    repliedAt = s.RepliedAt,
                    createdAt = s.CreatedAt
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSuggestions failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/community/suggestions
    [HttpPost("suggestions")]
    public async Task<IActionResult> CreateSuggestion([FromBody] CreateSuggestionRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var suggestion = new Suggestion
            {
                ProjectId = request.ProjectId,
                Title = request.Title,
                Content = request.Content,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };
            db.Suggestions.Add(suggestion);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "建议提交成功", data = new { id = suggestion.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSuggestion failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

// ===== Request DTOs =====
public class CreateActivityRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Location { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxParticipants { get; set; } = 50;
    public int ProjectId { get; set; } = 1;
}

public class UpdateActivityRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxParticipants { get; set; }
    public string? Status { get; set; }
}

public class JoinActivityRequest
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Note { get; set; }
}

public class CreateNoticeRequest
{
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public int ProjectId { get; set; } = 1;
    public int? Top { get; set; }
}

public class CreateSuggestionRequest
{
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public int ProjectId { get; set; } = 1;
}