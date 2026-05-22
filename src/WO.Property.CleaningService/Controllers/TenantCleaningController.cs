using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.CleaningService.Data;
using WO.Property.CleaningService.Models;
using WO.Property.CleaningService.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WO.Property.CleaningService.Controllers {

/// <summary>
/// Phase 1 多租户清洁服务控制器
/// </summary>
[ApiController]
[Route("api/tenant/cleaning")]
public class TenantCleaningController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantCleaningController> _logger;

    public TenantCleaningController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantCleaningController> logger)
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

    // GET /api/tenant/cleaning/tasks
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? planDate = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.CleaningTasks.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);
            if (planDate.HasValue)
                query = query.Where(t => t.PlanDate == planDate.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.PlanDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    id = t.Id,
                    buildingId = t.BuildingId,
                    cleaningArea = t.CleaningArea,
                    cleanerName = t.CleanerName ?? "",
                    cleaningType = t.CleaningType ?? "",
                    planDate = t.PlanDate,
                    actualDate = t.ActualDate,
                    status = t.Status,
                    qualityLevel = t.QualityLevel ?? "",
                    remarks = t.Remarks ?? "",
                    createdAt = t.CreatedAt
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

    // POST /api/tenant/cleaning/tasks
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] TenantCreateCleaningTaskRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var creatorId = GetUserIdFromJwt() ?? 0;

            var task = new CleaningTask
            {
                BuildingId = request.BuildingId,
                CleaningArea = request.CleaningArea,
                CleanerName = request.CleanerName,
                CleaningType = request.CleaningType,
                PlanDate = request.PlanDate,
                ActualDate = request.ActualDate,
                Status = request.Status ?? "pending",
                QualityLevel = request.QualityLevel,
                Remarks = request.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            db.CleaningTasks.Add(task);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = task, message = "清洁任务创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateTask failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // PUT /api/tenant/cleaning/tasks/{id}/status
    [HttpPut("tasks/{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TenantUpdateTaskStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var task = await db.CleaningTasks.FindAsync(id);
            if (task == null)
                return NotFound(new { success = false, message = "任务不存在" });

            if (!string.IsNullOrEmpty(request.Status))
                task.Status = request.Status;
            if (request.ActualDate.HasValue)
                task.ActualDate = request.ActualDate;
            if (!string.IsNullOrEmpty(request.QualityLevel))
                task.QualityLevel = request.QualityLevel;
            if (!string.IsNullOrEmpty(request.Remarks))
                task.Remarks = request.Remarks;

            await db.SaveChangesAsync();
            return Ok(new { success = true, data = task, message = "任务状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateTaskStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/cleaning/tasks/{id}
    [HttpDelete("tasks/{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var task = await db.CleaningTasks.FindAsync(id);
            if (task == null)
                return NotFound(new { success = false, message = "任务不存在" });

            db.CleaningTasks.Remove(task);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "任务删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteTask failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/cleaning/staff
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff([FromQuery] bool? isActive = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.CleaningStaff.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(s => s.IsActive == isActive.Value);

            var items = await query.OrderBy(s => s.Name).ToListAsync();
            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStaff failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/cleaning/staff
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] TenantCreateStaffRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var creatorId = GetUserIdFromJwt() ?? 0;

            var staff = new CleaningStaff
            {
                Name = request.Name,
                Phone = request.Phone,
                Area = request.Area,
                WorkShift = request.WorkShift,
                ProjectId = request.ProjectId,
                IsActive = request.IsActive ?? true,
                CreatedBy = creatorId.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            db.CleaningStaff.Add(staff);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = staff, message = "员工创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateStaff failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }
}

public class TenantCreateCleaningTaskRequest
{
    public int BuildingId { get; set; }
    public string CleaningArea { get; set; } = "";
    public string? CleanerName { get; set; }
    public string? CleaningType { get; set; }
    public DateTime? PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? Status { get; set; }
    public string? QualityLevel { get; set; }
    public string? Remarks { get; set; }
}

public class TenantUpdateTaskStatusRequest
{
    public string? Status { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? QualityLevel { get; set; }
    public string? Remarks { get; set; }
}

public class TenantCreateStaffRequest
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Area { get; set; }
    public string? WorkShift { get; set; }
    public int ProjectId { get; set; }
    public bool? IsActive { get; set; }
}
}
