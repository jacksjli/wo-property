using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.RenovationService.Data;
using WO.Property.RenovationService.Models;

namespace WO.Property.RenovationService.Controllers;

[ApiController]
[Route("api/tenant/renovation")]
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

    // GET /api/tenant/renovation/applications
    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.RenovationApplications.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    applicantName = r.ApplicantName ?? "",
                    applicantPhone = r.ApplicantPhone ?? "",
                    building = r.Building ?? "",
                    unit = r.Unit ?? "",
                    roomNo = r.RoomNo ?? "",
                    content = r.Content,
                    startDate = r.StartDate,
                    endDate = r.EndDate,
                    status = r.Status,
                    depositAmount = r.DepositAmount,
                    remark = r.Remark ?? "",
                    projectId = r.ProjectId
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetApplications failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/renovation/applications
    [HttpPost("applications")]
    public async Task<IActionResult> CreateApplication([FromBody] CreateRenovationRequest request)
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
                DepositAmount = request.DepositAmount,
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.RenovationApplications.Add(app);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "装修申请提交成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateApplication failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/renovation/applications/{id}/status
    [HttpPut("applications/{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var app = await db.RenovationApplications.FindAsync(id);
            if (app == null) return NotFound(new { success = false, message = "申请不存在" });
            if (!string.IsNullOrEmpty(request.Status)) app.Status = request.Status;
            if (!string.IsNullOrEmpty(request.Remark)) app.Remark = request.Remark;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateRenovationRequest
{
    public string? ApplicantName { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? Building { get; set; }
    public string? Unit { get; set; }
    public string? RoomNo { get; set; }
    public string Content { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal DepositAmount { get; set; } = 5000m;
    public int ProjectId { get; set; } = 1;
}

public class UpdateStatusRequest
{
    public string? Status { get; set; }
    public string? Remark { get; set; }
}