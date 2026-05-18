using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.VisitorService.Data;
using WO.Property.VisitorService.Models;
using VisitStatusEnum = WO.Property.VisitorService.Models.VisitStatus;
using VisitTypeEnum = WO.Property.VisitorService.Models.VisitType;

namespace WO.Property.VisitorService.Controllers;

[ApiController]
[Route("api/tenant/visitor")]
public class TenantVisitorController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantVisitorController> _logger;

    public TenantVisitorController(IDbContextFactory<TenantDbContext> dbFactory, ILogger<TenantVisitorController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/visitor/visitors
    [HttpGet("visitors")]
    public async Task<IActionResult> GetVisitors([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Visitors.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(v => v.Status.ToString() == status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.ScheduledDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    id = v.Id,
                    visitorNumber = v.VisitorNumber,
                    visitorName = v.VisitorName,
                    visitorPhone = v.VisitorPhone ?? "",
                    type = v.Type.ToString(),
                    hostName = v.HostName,
                    hostUnit = v.HostUnit ?? "",
                    visitLocation = v.VisitLocation ?? "",
                    scheduledDate = v.ScheduledDate,
                    status = v.Status.ToString(),
                    accessGranted = v.AccessGranted
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetVisitors failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/visitor/visitors
    [HttpPost("visitors")]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var visitorNum = $"V-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var accessCode = new Random().Next(100000, 999999).ToString();
            var visitor = new Visitor
            {
                VisitorNumber = visitorNum,
                VisitorName = request.VisitorName,
                VisitorPhone = request.VisitorPhone,
                Type = VisitTypeEnum.Personal,
                HostName = request.HostName,
                HostPhone = request.HostPhone,
                HostUnit = request.HostUnit,
                VisitLocation = request.VisitLocation,
                ScheduledDate = request.ScheduledDate,
                Purpose = request.Purpose,
                Status = VisitStatusEnum.Pending,
                AccessCode = accessCode,
                CreatedAt = DateTime.UtcNow
            };
            db.Visitors.Add(visitor);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "访客登记成功", data = new { visitorNumber = visitorNum, accessCode } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateVisitor failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/visitor/visitors/{id}/status
    [HttpPut("visitors/{id}/status")]
    public async Task<IActionResult> UpdateVisitorStatus(int id, [FromBody] UpdateVisitorStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var visitor = await db.Visitors.FindAsync(id);
            if (visitor == null) return NotFound(new { success = false, message = "访客记录不存在" });
            if (!string.IsNullOrEmpty(request.Status))
                visitor.Status = Enum.Parse<VisitStatusEnum>(request.Status);
            if (request.ActualCheckInTime.HasValue) visitor.ActualCheckInTime = request.ActualCheckInTime;
            if (request.ActualCheckOutTime.HasValue) visitor.ActualCheckOutTime = request.ActualCheckOutTime;
            if (request.AccessGranted.HasValue) visitor.AccessGranted = request.AccessGranted.Value;
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateVisitorStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateVisitorRequest
{
    public string VisitorName { get; set; } = "";
    public string? VisitorPhone { get; set; }
    public string HostName { get; set; } = "";
    public string? HostPhone { get; set; }
    public string? HostUnit { get; set; }
    public string? VisitLocation { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Purpose { get; set; }
}

public class UpdateVisitorStatusRequest
{
    public string? Status { get; set; }
    public DateTime? ActualCheckInTime { get; set; }
    public DateTime? ActualCheckOutTime { get; set; }
    public bool? AccessGranted { get; set; }
}