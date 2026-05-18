using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.CommunityService.Data;
using WO.Property.CommunityService.Models;

namespace WO.Property.CommunityService.Controllers;

[ApiController]
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
            return Ok(new { success = true, message = "活动创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateActivity failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/community/enrollments
    [HttpGet("enrollments")]
    public async Task<IActionResult> GetEnrollments([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? activityId = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ActivityEnrollments.AsQueryable();
            if (activityId.HasValue)
                query = query.Where(e => e.ActivityId == activityId.Value);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.EnrolledAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    id = e.Id,
                    activityId = e.ActivityId,
                    name = e.Name,
                    phone = e.Phone ?? "",
                    note = e.Note ?? "",
                    status = e.Status,
                    enrolledAt = e.EnrolledAt
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEnrollments failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/community/enrollments
    [HttpPost("enrollments")]
    public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var enrollment = new ActivityEnrollment
            {
                ActivityId = request.ActivityId,
                Name = request.Name,
                Phone = request.Phone,
                Note = request.Note,
                Status = "enrolled",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.ActivityEnrollments.Add(enrollment);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "报名成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateEnrollment failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

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

public class CreateEnrollmentRequest
{
    public int ActivityId { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Note { get; set; }
}