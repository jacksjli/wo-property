using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.RenovationService.Data;
using WO.Property.RenovationService.Models;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.RenovationService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/renovations")]
public class TenantRenovationController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantRenovationController> _logger;

    public TenantRenovationController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantRenovationController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private string? GetProjectCode() =>
        Request.Headers.TryGetValue("X-Project", out var v) ? v.FirstOrDefault() : null;

    private string? GetUserId() =>
        User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    // GET /api/tenant/renovations
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? building = null,
        [FromQuery] string? keyword = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.RenovationApplications.AsQueryable();
            var projectCode = GetProjectCode();
            if (!string.IsNullOrEmpty(projectCode))
                query = query.Where(r => r.ProjectCode == projectCode);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(building))
                query = query.Where(r => r.Building == building);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(r => (r.Content != null && r.Content.Contains(keyword))
                    || (r.ApplicantName != null && r.ApplicantName.Contains(keyword)));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = items.Select(MapToDto),
                total,
                page,
                pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetList failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/renovations/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            return Ok(new { success = true, data = MapToDto(app) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/renovations
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRenovationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var app = new RenovationApplication
            {
                ApplicantName = request.ApplicantName,
                ApplicantPhone = request.ApplicantPhone,
                Building = request.Building,
                Unit = request.Unit,
                RoomNo = request.RoomNo,
                Content = request.Content,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = "pending",
                DepositAmount = request.DepositAmount > 0 ? request.DepositAmount : 5000m,
                ProjectId = request.ProjectId,
                ProjectCode = GetProjectCode(),
                AttachmentUrls = request.AttachmentUrls,
                Remark = request.Remark,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetUserId() ?? "system"
            };
            db.RenovationApplications.Add(app);
            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication created: {Id}", app.Id);
            return Ok(new { success = true, message = "装修申请提交成功", data = MapToDto(app) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/renovations/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRenovationRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            // pending 状态下才能修改
            if (app.Status != "pending")
                return Ok(new { success = false, message = "当前状态不允许修改，仅待审核状态可修改" });

            if (request.ApplicantName != null) app.ApplicantName = request.ApplicantName;
            if (request.ApplicantPhone != null) app.ApplicantPhone = request.ApplicantPhone;
            if (request.Building != null) app.Building = request.Building;
            if (request.Unit != null) app.Unit = request.Unit;
            if (request.RoomNo != null) app.RoomNo = request.RoomNo;
            if (request.Content != null) app.Content = request.Content;
            if (request.StartDate.HasValue) app.StartDate = request.StartDate;
            if (request.EndDate.HasValue) app.EndDate = request.EndDate;
            if (request.DepositAmount > 0) app.DepositAmount = request.DepositAmount;
            if (request.AttachmentUrls != null) app.AttachmentUrls = request.AttachmentUrls;
            if (request.Remark != null) app.Remark = request.Remark;

            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication updated: {Id}", id);
            return Ok(new { success = true, message = "装修申请更新成功", data = MapToDto(app) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/renovations/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            if (app.Status != "pending")
                return Ok(new { success = false, message = "当前状态不允许删除，仅待审核状态可删除" });

            db.RenovationApplications.Remove(app);
            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication deleted: {Id}", id);
            return Ok(new { success = true, message = "装修申请已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/renovations/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveRejectRequest? request)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            if (app.Status != "pending")
                return Ok(new { success = false, message = "当前状态不是待审核，无法审核" });

            app.Status = "approved";
            app.Remark = request?.Remark ?? app.Remark;
            app.UpdatedAt = DateTime.UtcNow;
            app.UpdatedBy = GetUserId() ?? "system";

            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication approved: {Id}", id);
            return Ok(new { success = true, message = "装修申请审核通过", data = MapToDto(app) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Approve failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/renovations/{id}/reject
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ApproveRejectRequest? request)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            if (app.Status != "pending")
                return Ok(new { success = false, message = "当前状态不是待审核，无法审核" });

            app.Status = "rejected";
            app.Remark = string.IsNullOrEmpty(request?.Remark) ? "审核拒绝" : request.Remark;
            app.UpdatedAt = DateTime.UtcNow;
            app.UpdatedBy = GetUserId() ?? "system";

            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication rejected: {Id}", id);
            return Ok(new { success = true, message = "装修申请已拒绝", data = MapToDto(app) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reject failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/renovations/{id}/inspect
    [HttpPost("{id}/inspect")]
    public async Task<IActionResult> Inspect(int id, [FromBody] InspectRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            if (app.Status != "approved" && app.Status != "in_progress")
                return Ok(new { success = false, message = "当前状态不是进行中或已审核状态，无法巡查" });

            app.Status = "in_progress";
            app.Remark = string.IsNullOrEmpty(request.Findings)
                ? (app.Remark ?? "")
                : (app.Remark + " | 巡查记录:" + request.Findings);
            app.UpdatedAt = DateTime.UtcNow;
            app.UpdatedBy = GetUserId() ?? "system";

            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication inspected: {Id}, Findings: {Findings}", id, request.Findings);
            return Ok(new { success = true, message = "巡查记录已保存", data = MapToDto(app) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inspect failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/renovations/{id}/complete
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteRequest? request)
    {
        try
        {
            using var db = CreateDbContext();
            var projectCode = GetProjectCode();
            var app = await db.RenovationApplications
                .Where(r => r.ProjectCode == projectCode || projectCode == null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (app == null)
                return Ok(new { success = false, message = "装修申请不存在" });

            if (app.Status != "approved" && app.Status != "in_progress")
                return Ok(new { success = false, message = "当前状态无法完成验收" });

            app.Status = "completed";
            if (request?.EndDate.HasValue == true)
                app.EndDate = request.EndDate;
            else
                app.EndDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(request?.CompletionRemark))
                app.Remark = (app.Remark ?? "") + " | 验收说明:" + request.CompletionRemark;
            app.UpdatedAt = DateTime.UtcNow;
            app.UpdatedBy = GetUserId() ?? "system";

            await db.SaveChangesAsync();

            _logger.LogInformation("RenovationApplication completed: {Id}", id);
            return Ok(new
            {
                success = true,
                message = "装修验收完成，押金将按流程退还",
                data = MapToDto(app)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    private static object MapToDto(RenovationApplication r) => new
    {
        id = r.Id,
        applicantName = r.ApplicantName ?? "",
        applicantPhone = r.ApplicantPhone ?? "",
        building = r.Building ?? "",
        unit = r.Unit ?? "",
        roomNo = r.RoomNo ?? "",
        content = r.Content,
        attachmentUrls = r.AttachmentUrls ?? "",
        startDate = r.StartDate,
        endDate = r.EndDate,
        status = r.Status,
        depositAmount = r.DepositAmount,
        remark = r.Remark ?? "",
        projectId = r.ProjectId,
        projectCode = r.ProjectCode ?? "",
        createdAt = r.CreatedAt,
        createdBy = r.CreatedBy ?? "",
        updatedAt = r.UpdatedAt,
        updatedBy = r.UpdatedBy ?? ""
    };
}

// ─── Request DTOs ────────────────────────────────────────────

public class CreateRenovationRequest
{
    public string? ApplicantName { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? Building { get; set; }
    public string? Unit { get; set; }
    public string? RoomNo { get; set; }
    public string? Content { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal DepositAmount { get; set; } = 5000m;
    public int ProjectId { get; set; } = 1;
    public string? AttachmentUrls { get; set; }
    public string? Remark { get; set; }
}

public class UpdateRenovationRequest
{
    public string? ApplicantName { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? Building { get; set; }
    public string? Unit { get; set; }
    public string? RoomNo { get; set; }
    public string? Content { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal DepositAmount { get; set; }
    public string? AttachmentUrls { get; set; }
    public string? Remark { get; set; }
}

public class ApproveRejectRequest
{
    public string? Remark { get; set; }
}

public class InspectRequest
{
    public string Findings { get; set; } = "";
}

public class CompleteRequest
{
    public DateTime? EndDate { get; set; }
    public string? CompletionRemark { get; set; }
}