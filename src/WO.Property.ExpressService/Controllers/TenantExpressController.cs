using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ExpressService.Data;
using WO.Property.ExpressService.Models;

namespace WO.Property.ExpressService.Controllers;

/// <summary>
/// Phase 1 多租户快递服务控制器
/// </summary>
[ApiController]
[Route("api/tenant/express")]
public class TenantExpressController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly Tenant.ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantExpressController> _logger;

    public TenantExpressController(
        IDbContextFactory<TenantDbContext> dbFactory,
        Tenant.ITenantDbFactory tenantDbFactory,
        ILogger<TenantExpressController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/express/deliveries
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? roomNumber = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ExpressDeliveries.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status.ToString() == status);
            if (!string.IsNullOrEmpty(roomNumber))
                query = query.Where(d => d.RoomNumber == roomNumber);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.ReceivedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    id = d.Id,
                    trackingNumber = d.TrackingNumber,
                    expressNumber = d.ExpressNumber ?? "",
                    companyId = d.CompanyId,
                    senderName = d.SenderName,
                    senderPhone = d.SenderPhone ?? "",
                    receiverName = d.ReceiverName,
                    receiverPhone = d.ReceiverPhone,
                    roomNumber = d.RoomNumber,
                    pickupAddress = d.PickupAddress ?? "",
                    status = d.Status.ToString(),
                    remarks = d.Remarks ?? "",
                    receivedAt = d.ReceivedAt,
                    pickedUpAt = d.PickedUpAt,
                    expiresAt = d.ExpiresAt,
                    receivedBy = d.ReceivedBy ?? ""
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDeliveries failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/express/deliveries
    [HttpPost("deliveries")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateExpressRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            var delivery = new ExpressDelivery
            {
                TrackingNumber = request.TrackingNumber,
                ExpressNumber = request.ExpressNumber,
                CompanyId = request.CompanyId,
                SenderName = request.SenderName,
                SenderPhone = request.SenderPhone,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                RoomNumber = request.RoomNumber,
                PickupAddress = request.PickupAddress,
                Remarks = request.Remarks,
                Status = ExpressStatus.Pending,
                ReceivedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.StorageDays),
                CreatedAt = DateTime.UtcNow
            };

            db.ExpressDeliveries.Add(delivery);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "快递登记成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateDelivery failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // PUT /api/tenant/express/deliveries/{id}/status
    [HttpPut("deliveries/{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateExpressStatusRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var delivery = await db.ExpressDeliveries.FindAsync(id);
            if (delivery == null)
                return NotFound(new { success = false, message = "快递记录不存在" });

            delivery.Status = request.Status;
            if (!string.IsNullOrEmpty(request.Remarks))
                delivery.Remarks = request.Remarks;
            if (request.Status == ExpressStatus.Delivered)
                delivery.PickedUpAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(request.ReceivedBy))
                delivery.ReceivedBy = request.ReceivedBy;

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateStatus failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/express/companies
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies([FromQuery] bool? isActive = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ExpressCompanies.AsQueryable();
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var items = await query
                .OrderBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name, code = c.Code ?? "", phone = c.Phone ?? "", isActive = c.IsActive })
                .ToListAsync();
            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCompanies failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/tenant/express/deliveries/{id}
    [HttpDelete("deliveries/{id}")]
    public async Task<IActionResult> DeleteDelivery(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var delivery = await db.ExpressDeliveries.FindAsync(id);
            if (delivery == null)
                return NotFound(new { success = false, message = "快递记录不存在" });

            db.ExpressDeliveries.Remove(delivery);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteDelivery failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}