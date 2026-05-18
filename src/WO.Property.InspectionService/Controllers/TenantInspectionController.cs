using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.InspectionService.Data;
using WO.Property.InspectionService.Models;
using InspectionTaskStatus = WO.Property.InspectionService.Models.TaskStatus;

namespace WO.Property.InspectionService.Controllers;

[ApiController]
[Route("api/tenant/inspection")]
public class TenantInspectionController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantInspectionController> _logger;

    public TenantInspectionController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantInspectionController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/inspection/tasks
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.InspectionTasks.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status.ToString() == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.ScheduledDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    id = t.Id,
                    taskNumber = t.TaskNumber,
                    title = t.Title,
                    description = t.Description ?? "",
                    type = t.Type.ToString(),
                    area = t.Area ?? "",
                    scheduledDate = t.ScheduledDate,
                    status = t.Status.ToString(),
                    assignedTo = t.AssignedTo ?? "",
                    completedBy = t.CompletedBy ?? ""
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTasks failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/inspection/tasks
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CreateInspectionTaskRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var taskNum = $"IT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var task = new InspectionTask
            {
                TaskNumber = taskNum,
                Title = request.Title,
                Description = request.Description,
                Type = Enum.Parse<InspectionType>(request.Type ?? "Daily"),
                Area = request.Area,
                ScheduledDate = request.ScheduledDate,
                Status = InspectionTaskStatus.Pending,
                AssignedTo = request.AssignedTo,
                CreatedAt = DateTime.UtcNow
            };
            db.InspectionTasks.Add(task);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "巡检任务创建成功", data = new { taskNumber = taskNum } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateTask failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/inspection/tasks/{id}/status
    [HttpPut("tasks/{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var task = await db.InspectionTasks.FindAsync(id);
            if (task == null) return NotFound(new { success = false, message = "任务不存在" });
            if (!string.IsNullOrEmpty(request.Status))
                task.Status = Enum.Parse<InspectionTaskStatus>(request.Status);
            if (!string.IsNullOrEmpty(request.CompletedBy))
                task.CompletedBy = request.CompletedBy;
            if (request.EndTime.HasValue) task.EndTime = request.EndTime;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateTaskStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/inspection/records
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            using var db = CreateDbContext();
            var total = await db.InspectionRecords.CountAsync();
            var items = await db.InspectionRecords
                .OrderByDescending(r => r.InspectionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    taskId = r.TaskId,
                    recordNumber = r.RecordNumber,
                    inspectionDate = r.InspectionDate,
                    inspector = r.Inspector,
                    totalItems = r.TotalItems,
                    passedItems = r.PassedItems,
                    failedItems = r.FailedItems,
                    isPassed = r.IsPassed
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRecords failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateInspectionTaskRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Area { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? AssignedTo { get; set; }
}

public class UpdateTaskStatusRequest
{
    public string? Status { get; set; }
    public string? CompletedBy { get; set; }
    public DateTime? EndTime { get; set; }
}