using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WO.Property.VisitorService.Data;
using WO.Property.VisitorService.Models;

namespace WO.Property.VisitorService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/visitor/visitors")]
public class TenantVisitorController : ControllerBase
{
    // 获取当前项目代码（从 X-Project header）
    private string? GetProjectCode()
    {
        if (Request.Headers.TryGetValue("X-Project", out var projectValues))
        {
            var projectCode = projectValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(projectCode))
                return projectCode;
        }
        return null;
    }


    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantVisitorController> _logger;

    public TenantVisitorController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ILogger<TenantVisitorController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/visitor/visitors
    [HttpGet]
    public async Task<IActionResult> GetVisitors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Visitors.AsQueryable();

            var projectCode = GetProjectCode();
            if (!string.IsNullOrEmpty(projectCode))
            {
                query = query.Where(x => x.ProjectCode == projectCode);
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(v => v.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    id = v.Id,
                    visitorName = v.VisitorName,
                    visitorPhone = v.VisitorPhone ?? "",
                    idCardNumber = v.IdCardNumber ?? "",
                    visitPurpose = v.VisitPurpose ?? "",
                    visitDate = v.VisitDate,
                    visitTime = v.VisitTime,
                    leaveTime = v.LeaveTime,
                    buildingId = v.BuildingId,
                    roomId = v.RoomId,
                    hostName = v.HostName ?? "",
                    hostPhone = v.HostPhone ?? "",
                    status = v.Status,
                    remarks = v.Remarks ?? "",
                    createdAt = v.CreatedAt
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

    // GET /api/tenant/visitor/visitors/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVisitor(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var v = await db.Visitors.FindAsync(id);
            if (v == null)
                return NotFound(new { success = false, message = "访客记录不存在" });

            return Ok(new { success = true, data = v });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetVisitor {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/visitor/visitors
    [HttpPost]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            // 生成访客编号
            var count = await db.Visitors.CountAsync() + 1;
            var visitorNumber = $"V-{DateTime.Now:yyyyMMdd}-{(1000 + count):D4}";

            var visitor = new Visitor
            {
                VisitorName = request.VisitorName,
                VisitorPhone = request.VisitorPhone,
                IdCardNumber = request.IdCardNumber,
                VisitPurpose = request.VisitPurpose,
                VisitDate = request.VisitDate ?? DateTime.Today,
                VisitTime = request.VisitTime ?? DateTime.Now.TimeOfDay,
                BuildingId = request.BuildingId,
                RoomId = request.RoomId,
                HostName = request.HostName,
                HostPhone = request.HostPhone,
                Status = "registered",
                Remarks = request.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            db.Visitors.Add(visitor);
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = visitor, message = "访客登记成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateVisitor failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // PUT /api/tenant/visitor/visitors/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVisitor(int id, [FromBody] UpdateVisitorRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var visitor = await db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { success = false, message = "访客记录不存在" });

            if (!string.IsNullOrEmpty(request.Status))
                visitor.Status = request.Status;
            if (request.LeaveTime.HasValue)
                visitor.LeaveTime = request.LeaveTime.Value.TimeOfDay;
            if (request.Remarks != null)
                visitor.Remarks = request.Remarks;

            visitor.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = visitor, message = "更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateVisitor {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/visitor/visitors/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVisitor(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var visitor = await db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { success = false, message = "访客记录不存在" });

            db.Visitors.Remove(visitor);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteVisitor {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/visitor/visitors/{id}/check-in
    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckIn(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var visitor = await db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { success = false, message = "访客记录不存在" });

            visitor.Status = "checked_in";
            visitor.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "登记入住成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckIn {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/visitor/visitors/{id}/check-out
    [HttpPost("{id}/check-out")]
    public async Task<IActionResult> CheckOut(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var visitor = await db.Visitors.FindAsync(id);
            if (visitor == null)
                return NotFound(new { success = false, message = "访客记录不存在" });

            visitor.Status = "checked_out";
            visitor.LeaveTime = DateTime.Now.TimeOfDay;
            visitor.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "退房成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckOut {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ==================== 外部人员管理 ====================
    [HttpGet("external-persons")]
    public async Task<IActionResult> GetExternalPersons([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? type = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ExternalPersons.AsQueryable();
            if (!string.IsNullOrEmpty(type))
                query = query.Where(e => e.Type == type);
            
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return Ok(new { success = true, total, page, pageSize, data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetExternalPersons failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("external-persons/{id}")]
    public async Task<IActionResult> GetExternalPerson(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var person = await db.ExternalPersons.FindAsync(id);
            if (person == null)
                return NotFound(new { success = false, message = "外部人员不存在" });
            return Ok(new { success = true, data = person });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetExternalPerson failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("external-persons")]
    public async Task<IActionResult> CreateExternalPerson([FromBody] CreateExternalPersonRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var person = new ExternalPerson
            {
                Name = request.Name,
                Phone = request.Phone,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            };
            db.ExternalPersons.Add(person);
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = person });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateExternalPerson failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("external-persons/{id}")]
    public async Task<IActionResult> UpdateExternalPerson(int id, [FromBody] UpdateExternalPersonRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var person = await db.ExternalPersons.FindAsync(id);
            if (person == null)
                return NotFound(new { success = false, message = "外部人员不存在" });
            
            if (!string.IsNullOrEmpty(request.Name)) person.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Phone)) person.Phone = request.Phone;
            if (!string.IsNullOrEmpty(request.Type)) person.Type = request.Type;
            
            await db.SaveChangesAsync();
            return Ok(new { success = true, data = person });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateExternalPerson failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("external-persons/{id}")]
    public async Task<IActionResult> DeleteExternalPerson(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var person = await db.ExternalPersons.FindAsync(id);
            if (person == null)
                return NotFound(new { success = false, message = "外部人员不存在" });
            
            db.ExternalPersons.Remove(person);
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteExternalPerson failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateExternalPersonRequest
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Type { get; set; } = "";
}

public class UpdateExternalPersonRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Type { get; set; }
}
