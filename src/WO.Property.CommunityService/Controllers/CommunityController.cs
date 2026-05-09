using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.CommunityService.Data;
using WO.Property.CommunityService.Models;

namespace WO.Property.CommunityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly CommunityDbContext _context;
    public CommunityController(CommunityDbContext context) => _context = context;

    // ===== Activities =====
    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] int projectId = 1, [FromQuery] string? status = null)
    {
        var query = _context.Activities.Where(a => a.ProjectId == projectId).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
        var list = await query.OrderByDescending(a => a.StartTime).ToListAsync();
        return Ok(new { success = true, data = list });
    }

    [HttpGet("activities/{id}")]
    public async Task<IActionResult> GetActivity(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity == null) return Ok(new { success = false, message = "未找到活动" });
        return Ok(new { success = true, data = activity });
    }

    [HttpPost("activities")]
    public async Task<IActionResult> CreateActivity([FromBody] Activity activity)
    {
        activity.ProjectId = activity.ProjectId > 0 ? activity.ProjectId : 1;
        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = activity });
    }

    [HttpPut("activities/{id}")]
    public async Task<IActionResult> UpdateActivity(int id, [FromBody] Activity activity)
    {
        var existing = await _context.Activities.FindAsync(id);
        if (existing == null) return Ok(new { success = false, message = "未找到" });
        existing.Name = activity.Name;
        existing.Description = activity.Description;
        existing.Location = activity.Location;
        existing.StartTime = activity.StartTime;
        existing.EndTime = activity.EndTime;
        existing.MaxParticipants = activity.MaxParticipants;
        existing.Status = activity.Status;
        existing.CoverImage = activity.CoverImage;
        existing.Remark = activity.Remark;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = existing });
    }

    [HttpDelete("activities/{id}")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity == null) return Ok(new { success = false, message = "未找到" });
        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ===== Enrollments =====
    [HttpGet("activities/{id}/enrollments")]
    public async Task<IActionResult> GetEnrollments(int id)
    {
        var list = await _context.Enrollments.Where(e => e.ActivityId == id).OrderByDescending(e => e.EnrolledAt).ToListAsync();
        return Ok(new { success = true, data = list });
    }

    [HttpPost("enrollments")]
    public async Task<IActionResult> Enroll([FromBody] ActivityEnrollment enrollment)
    {
        var activity = await _context.Activities.FindAsync(enrollment.ActivityId);
        if (activity == null) return Ok(new { success = false, message = "活动不存在" });
        if (activity.CurrentParticipants >= activity.MaxParticipants)
            return Ok(new { success = false, message = "报名已满" });

        _context.Enrollments.Add(enrollment);
        activity.CurrentParticipants++;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = enrollment });
    }

    [HttpPut("enrollments/{id}")]
    public async Task<IActionResult> UpdateEnrollment(int id, [FromBody] ActivityEnrollment enrollment)
    {
        var existing = await _context.Enrollments.FindAsync(id);
        if (existing == null) return Ok(new { success = false, message = "未找到" });
        existing.Status = enrollment.Status;
        existing.Note = enrollment.Note;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = existing });
    }

    // ===== Stats =====
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int projectId = 1)
    {
        var total = await _context.Activities.CountAsync(a => a.ProjectId == projectId);
        var openCount = await _context.Activities.CountAsync(a => a.ProjectId == projectId && a.Status == "open");
        var totalParticipants = await _context.Activities.Where(a => a.ProjectId == projectId).SumAsync(a => a.CurrentParticipants);
        return Ok(new { success = true, data = new { totalActivities = total, openActivities = openCount, totalParticipants } });
    }
}
