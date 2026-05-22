using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WO.Property.ProjectTrackingService.Data;
using WO.Property.ProjectTrackingService.Models;
using WO.Property.ProjectTrackingService.Tenant;

namespace WO.Property.ProjectTrackingService.Controllers;

[ApiController]
[Route("api/tenant/project-tracking")]
public class TenantProjectTrackingController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantProjectTrackingController> _logger;

    public TenantProjectTrackingController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantProjectTrackingController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private int? GetUserIdFromJwt()
    {
        var userIdStr = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                     ?? User?.Claims?.FirstOrDefault(c => c.Type == "user_id")?.Value;
        return int.TryParse(userIdStr, out var id) ? id : null;
    }

    // GET /api/tenant/project-tracking
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Projects.Where(p => !p.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Name.Contains(keyword) || p.ProjectNo.Contains(keyword));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProjects failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/project-tracking/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var project = await db.Projects
                .Include(p => p.Records.OrderByDescending(r => r.Timestamp))
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (project == null)
                return NotFound(new { success = false, message = "项目不存在" });

            return Ok(new { success = true, data = project });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProject {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/project-tracking
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateTrackingProjectRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            var year = DateTime.Now.Year;
            var count = await db.Projects.CountAsync() + 1;
            var projectNo = $"PT-{year}{DateTime.Now:MMdd}-{(1000 + count):D4}";

            var project = new TrackingProject
            {
                ProjectNo = projectNo,
                Name = request.Name,
                Type = request.Type ?? "投标",
                Client = request.Client,
                Budget = request.Budget,
                BidAmount = request.BidAmount,
                RegisterDeadline = request.RegisterDeadline,
                BidDeadline = request.BidDeadline,
                BidOpenDate = request.BidOpenDate,
                Location = request.Location,
                Description = request.Description,
                FileStatus = request.FileStatus ?? "未获取",
                Status = request.Status ?? "意向",
                SuccessRate = request.SuccessRate ?? 50,
                Remark = request.Remark,
                CreatedAt = DateTime.UtcNow,
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = project, message = "项目创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateProject failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // PUT /api/tenant/project-tracking/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateTrackingProjectRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var project = await db.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { success = false, message = "项目不存在" });

            if (!string.IsNullOrEmpty(request.Name)) project.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Type)) project.Type = request.Type;
            if (request.Client != null) project.Client = request.Client;
            if (request.Budget.HasValue) project.Budget = request.Budget;
            if (request.BidAmount.HasValue) project.BidAmount = request.BidAmount;
            if (request.RegisterDeadline.HasValue) project.RegisterDeadline = request.RegisterDeadline;
            if (request.BidDeadline.HasValue) project.BidDeadline = request.BidDeadline;
            if (request.BidOpenDate.HasValue) project.BidOpenDate = request.BidOpenDate;
            if (request.Location != null) project.Location = request.Location;
            if (request.Description != null) project.Description = request.Description;
            if (!string.IsNullOrEmpty(request.FileStatus)) project.FileStatus = request.FileStatus;
            if (!string.IsNullOrEmpty(request.Status)) project.Status = request.Status;
            if (request.SuccessRate.HasValue) project.SuccessRate = request.SuccessRate.Value;
            if (request.Remark != null) project.Remark = request.Remark;
            project.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Ok(new { success = true, data = project, message = "项目更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateProject {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/project-tracking/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var project = await db.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { success = false, message = "项目不存在" });

            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "项目删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteProject {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/project-tracking/{id}/records
    [HttpPost("{id}/records")]
    public async Task<IActionResult> AddRecord(int id, [FromBody] AddTrackingRecordRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var project = await db.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { success = false, message = "项目不存在" });

            var operatorId = GetUserIdFromJwt();
            var record = new TrackingRecord
            {
                ProjectId = id,
                Type = request.Type ?? "备注",
                Content = request.Content,
                Operator = operatorId?.ToString(),
                Timestamp = DateTime.UtcNow
            };

            db.Records.Add(record);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = record, message = "跟踪记录添加成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddRecord failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/project-tracking/{id}/records/{recordId}
    [HttpDelete("{id}/records/{recordId}")]
    public async Task<IActionResult> DeleteRecord(int id, int recordId)
    {
        try
        {
            using var db = CreateDbContext();
            var record = await db.Records.FindAsync(recordId);
            if (record == null || record.ProjectId != id)
                return NotFound(new { success = false, message = "记录不存在" });

            db.Records.Remove(record);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "跟踪记录删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteRecord {RecordId} failed", recordId);
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateTrackingProjectRequest
{
    public string Name { get; set; } = "";
    public string? Type { get; set; }
    public string? Client { get; set; }
    public decimal? Budget { get; set; }
    public decimal? BidAmount { get; set; }
    public DateTime? RegisterDeadline { get; set; }
    public DateTime? BidDeadline { get; set; }
    public DateTime? BidOpenDate { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? FileStatus { get; set; }
    public string? Status { get; set; }
    public int? SuccessRate { get; set; }
    public string? Remark { get; set; }
}

public class UpdateTrackingProjectRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Client { get; set; }
    public decimal? Budget { get; set; }
    public decimal? BidAmount { get; set; }
    public DateTime? RegisterDeadline { get; set; }
    public DateTime? BidDeadline { get; set; }
    public DateTime? BidOpenDate { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? FileStatus { get; set; }
    public string? Status { get; set; }
    public int? SuccessRate { get; set; }
    public string? Remark { get; set; }
}

public class AddTrackingRecordRequest
{
    public string? Type { get; set; }
    public string Content { get; set; } = "";
}
