using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.CleaningService.Data;
using WO.Property.CleaningService.Models;

namespace WO.Property.CleaningService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleaningController : ControllerBase
{
    private readonly CleaningDbContext _context;
    public CleaningController(CleaningDbContext context) => _context = context;

    // ===== Staff =====
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff([FromQuery] int projectId = 1)
        => Ok(new { success = true, data = await _context.Staff.Where(s => s.ProjectId == projectId).OrderBy(s => s.Id).ToListAsync() });

    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CleaningStaff staff)
    {
        staff.ProjectId = staff.ProjectId > 0 ? staff.ProjectId : 1;
        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = staff });
    }

    [HttpPut("staff/{id}")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] CleaningStaff staff)
    {
        var existing = await _context.Staff.FindAsync(id);
        if (existing == null) return Ok(new { success = false, message = "未找到" });
        existing.Name = staff.Name;
        existing.Phone = staff.Phone;
        existing.Area = staff.Area;
        existing.WorkShift = staff.WorkShift;
        existing.IsActive = staff.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = existing });
    }

    [HttpDelete("staff/{id}")]
    public async Task<IActionResult> DeleteStaff(int id)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null) return Ok(new { success = false, message = "未找到" });
        _context.Staff.Remove(staff);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ===== Tasks =====
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks([FromQuery] int projectId = 1, [FromQuery] string? date = null)
    {
        var query = _context.Tasks.Where(t => t.ProjectId == projectId).AsQueryable();
        if (!string.IsNullOrEmpty(date))
            query = query.Where(t => t.PlanDate.Date == DateTime.Parse(date).Date);
        var tasks = await query.OrderByDescending(t => t.PlanDate).ToListAsync();
        return Ok(new { success = true, data = tasks });
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CleaningTask task)
    {
        task.ProjectId = task.ProjectId > 0 ? task.ProjectId : 1;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = task });
    }

    [HttpPut("tasks/{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] CleaningTask task)
    {
        var existing = await _context.Tasks.FindAsync(id);
        if (existing == null) return Ok(new { success = false, message = "未找到" });
        existing.Status = task.Status;
        existing.CheckInTime = task.CheckInTime;
        existing.CheckInPhoto = task.CheckInPhoto;
        existing.Remark = task.Remark;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = existing });
    }

    [HttpDelete("tasks/{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return Ok(new { success = false, message = "未找到" });
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ===== Records =====
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] int projectId = 1, [FromQuery] int? taskId = null)
    {
        var query = _context.Records.AsQueryable();
        if (taskId.HasValue) query = query.Where(r => r.TaskId == taskId.Value);
        var records = await query.OrderByDescending(r => r.RecordTime).Take(100).ToListAsync();
        return Ok(new { success = true, data = records });
    }

    [HttpPost("records")]
    public async Task<IActionResult> CreateRecord([FromBody] CleaningRecord record)
    {
        _context.Records.Add(record);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = record });
    }

    // ===== Stats =====
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int projectId = 1)
    {
        var today = DateTime.Today;
        var staffCount = await _context.Staff.CountAsync(s => s.ProjectId == projectId && s.IsActive);
        var todayTasks = await _context.Tasks.CountAsync(t => t.ProjectId == projectId && t.PlanDate.Date == today);
        var completedTasks = await _context.Tasks.CountAsync(t => t.ProjectId == projectId && t.PlanDate.Date == today && t.Status == "completed");
        var todayRecords = await _context.Records.CountAsync(r => r.RecordTime.Date == today);
        return Ok(new { success = true, data = new { staffCount, todayTasks, completedTasks, todayRecords } });
    }
}
