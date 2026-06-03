using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ExpressService.Data;
using WO.Property.ExpressService.Models;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.ExpressService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/express/express-records")]
public class TenantExpressController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<TenantExpressController> _logger;

    public TenantExpressController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ILogger<TenantExpressController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/express/express-records
    [HttpGet]
    public async Task<IActionResult> GetRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ExpressRecords.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(r =>
                    r.RecipientName.Contains(keyword) ||
                    (r.TrackingNumber != null && r.TrackingNumber.Contains(keyword)) ||
                    (r.CourierCompany != null && r.CourierCompany.Contains(keyword)));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    roomId = r.RoomId,
                    recipientName = r.RecipientName,
                    recipientPhone = r.RecipientPhone ?? "",
                    courierCompany = r.CourierCompany ?? "",
                    trackingNumber = r.TrackingNumber ?? "",
                    pickupCode = r.PickupCode ?? "",
                    status = r.Status,
                    pickupTime = r.PickupTime,
                    remarks = r.Remarks ?? "",
                    createdAt = r.CreatedAt
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

    // GET /api/tenant/express/express-records/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecord(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var r = await db.ExpressRecords.FindAsync(id);
            if (r == null)
                return NotFound(new { success = false, message = "记录不存在" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = r.Id,
                    roomId = r.RoomId,
                    recipientName = r.RecipientName,
                    recipientPhone = r.RecipientPhone ?? "",
                    courierCompany = r.CourierCompany ?? "",
                    trackingNumber = r.TrackingNumber ?? "",
                    pickupCode = r.PickupCode ?? "",
                    status = r.Status,
                    pickupTime = r.PickupTime,
                    remarks = r.Remarks ?? "",
                    createdAt = r.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRecord {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/express/express-records
    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] CreateExpressRecordRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            // 生成取件码
            var pickupCode = string.IsNullOrEmpty(request.PickupCode)
                ? new Random().Next(1000, 9999).ToString()
                : request.PickupCode;

            var record = new ExpressRecord
            {
                RoomId = request.RoomId,
                RecipientName = request.RecipientName,
                RecipientPhone = request.RecipientPhone,
                CourierCompany = request.CourierCompany,
                TrackingNumber = request.TrackingNumber,
                PickupCode = pickupCode,
                Status = "pending",
                Remarks = request.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            db.ExpressRecords.Add(record);
            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = record.Id,
                    roomId = record.RoomId,
                    recipientName = record.RecipientName,
                    recipientPhone = record.RecipientPhone ?? "",
                    courierCompany = record.CourierCompany ?? "",
                    trackingNumber = record.TrackingNumber ?? "",
                    pickupCode = record.PickupCode,
                    status = record.Status,
                    remarks = record.Remarks ?? "",
                    createdAt = record.CreatedAt
                },
                message = "快递登记成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateRecord failed");
            return Ok(new { success = false, message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // PUT /api/tenant/express/express-records/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] UpdateExpressRecordRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var record = await db.ExpressRecords.FindAsync(id);
            if (record == null)
                return NotFound(new { success = false, message = "记录不存在" });

            if (!string.IsNullOrEmpty(request.Status))
                record.Status = request.Status;
            if (request.PickupTime.HasValue)
                record.PickupTime = request.PickupTime;
            if (request.Remarks != null)
                record.Remarks = request.Remarks;

            record.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, data = record, message = "更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateRecord {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/express/express-records/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecord(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var record = await db.ExpressRecords.FindAsync(id);
            if (record == null)
                return NotFound(new { success = false, message = "记录不存在" });

            db.ExpressRecords.Remove(record);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteRecord {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/express/express-records/rooms/{roomId}
    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetRecordsByRoom(int roomId)
    {
        try
        {
            using var db = CreateDbContext();
            var records = await db.ExpressRecords
                .Where(r => r.RoomId == roomId && r.Status == "pending")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    recipientName = r.RecipientName,
                    courierCompany = r.CourierCompany ?? "",
                    trackingNumber = r.TrackingNumber ?? "",
                    pickupCode = r.PickupCode,
                    status = r.Status,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = records });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRecordsByRoom {RoomId} failed", roomId);
            return Ok(new { success = false, message = ex.Message });
        }
    }
}
