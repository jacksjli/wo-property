using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ParkingService.Data;
using WO.Property.ParkingService.Models;

namespace WO.Property.ParkingService.Controllers;

[ApiController]
[Route("api/tenant/parkings")]
public class ParkingSpaceController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ILogger<ParkingSpaceController> _logger;

    public ParkingSpaceController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ILogger<ParkingSpaceController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    /// <summary>
    /// 车位列表查询（支持按状态/类型/楼栋过滤）
    /// GET /api/tenant/parkings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSpaces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int? lotId = null,
        [FromQuery] string? spaceNo = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ParkingSpaces.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(s => s.Type == type);
            if (lotId.HasValue)
                query = query.Where(s => s.LotId == lotId.Value);
            if (!string.IsNullOrEmpty(spaceNo))
                query = query.Where(s => s.SpaceNo.Contains(spaceNo));

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.LotId)
                .ThenBy(s => s.SpaceNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    id = s.Id,
                    spaceNo = s.SpaceNo,
                    type = s.Type,
                    status = s.Status,
                    lotId = s.LotId,
                    vehicleId = s.VehicleId,
                    lotName = db.ParkingLots.Where(l => l.Id == s.LotId).Select(l => l.Name).FirstOrDefault(),
                    projectCode = s.ProjectCode,
                    createdAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSpaces failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 车位详情
    /// GET /api/tenant/parkings/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSpace(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var space = await db.ParkingSpaces
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    id = s.Id,
                    spaceNo = s.SpaceNo,
                    type = s.Type,
                    status = s.Status,
                    lotId = s.LotId,
                    vehicleId = s.VehicleId,
                    lotName = db.ParkingLots.Where(l => l.Id == s.LotId).Select(l => l.Name).FirstOrDefault(),
                    vehiclePlate = s.VehicleId != null
                        ? db.Vehicles.Where(v => v.Id == s.VehicleId).Select(v => v.PlateNumber).FirstOrDefault()
                        : null,
                    projectCode = s.ProjectCode,
                    createdAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (space == null)
                return Ok(new { success = false, message = "车位不存在" });

            return Ok(new { success = true, data = space });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSpace {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 添加车位
    /// POST /api/tenant/parkings
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSpace([FromBody] CreateSpaceRequest request)
    {
        try
        {
            using var db = CreateDbContext();

            // 验证楼栋存在
            var lotExists = await db.ParkingLots.AnyAsync(l => l.Id == request.LotId);
            if (!lotExists)
                return Ok(new { success = false, message = "停车场不存在" });

            // 检查车位号唯一性（同一停车场内）
            var spaceExists = await db.ParkingSpaces
                .AnyAsync(s => s.LotId == request.LotId && s.SpaceNo == request.SpaceNo);
            if (spaceExists)
                return Ok(new { success = false, message = "车位号已存在" });

            var space = new ParkingSpace
            {
                SpaceNo = request.SpaceNo,
                Type = request.Type ?? "temporary",
                Status = request.Status ?? "available",
                LotId = request.LotId,
                VehicleId = request.VehicleId,
                ProjectCode = request.ProjectCode,
                CreatedAt = DateTime.UtcNow
            };

            db.ParkingSpaces.Add(space);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "车位创建成功", data = new { id = space.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSpace failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 更新车位
    /// PUT /api/tenant/parkings/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSpace(int id, [FromBody] UpdateSpaceRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var space = await db.ParkingSpaces.FindAsync(id);
            if (space == null)
                return Ok(new { success = false, message = "车位不存在" });

            if (!string.IsNullOrEmpty(request.SpaceNo) && request.SpaceNo != space.SpaceNo)
            {
                var duplicate = await db.ParkingSpaces
                    .AnyAsync(s => s.LotId == space.LotId && s.SpaceNo == request.SpaceNo && s.Id != id);
                if (duplicate)
                    return Ok(new { success = false, message = "车位号已存在" });
                space.SpaceNo = request.SpaceNo;
            }

            if (request.LotId.HasValue) space.LotId = request.LotId.Value;
            if (request.Type != null) space.Type = request.Type;
            if (request.Status != null) space.Status = request.Status;
            if (request.VehicleId.HasValue) space.VehicleId = request.VehicleId;

            space.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "车位更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSpace {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 删除车位
    /// DELETE /api/tenant/parkings/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSpace(int id)
    {
        try
        {
            using var db = CreateDbContext();
            var space = await db.ParkingSpaces.FindAsync(id);
            if (space == null)
                return Ok(new { success = false, message = "车位不存在" });

            // 只能删除 available 状态的车位
            if (space.Status != "available")
                return Ok(new { success = false, message = "只能删除空闲状态的车位" });

            db.ParkingSpaces.Remove(space);
            await db.SaveChangesAsync();

            return Ok(new { success = true, message = "车位删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteSpace {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 停车入场
    /// POST /api/tenant/parkings/{id}/check-in
    /// </summary>
    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckIn(int id, [FromBody] CheckInRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var space = await db.ParkingSpaces.FindAsync(id);
            if (space == null)
                return Ok(new { success = false, message = "车位不存在" });

            if (space.Status != "available")
                return Ok(new { success = false, message = "车位不可用或已被占用" });

            // 更新车位状态为占用
            space.Status = "occupied";
            space.VehicleId = request.VehicleId;
            space.UpdatedAt = DateTime.UtcNow;

            // 查找或创建车辆
            Vehicle? vehicle = null;
            if (!string.IsNullOrEmpty(request.PlateNumber))
            {
                vehicle = await db.Vehicles
                    .FirstOrDefaultAsync(v => v.PlateNumber == request.PlateNumber);
                if (vehicle == null)
                {
                    vehicle = new Vehicle
                    {
                        PlateNumber = request.PlateNumber,
                        Brand = request.Brand,
                        Color = request.Color,
                        OwnerName = request.OwnerName,
                        OwnerPhone = request.OwnerPhone,
                        Type = request.VehicleType ?? "temporary",
                        ProjectId = request.ProjectId,
                        ProjectCode = request.ProjectCode,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Vehicles.Add(vehicle);
                }
                space.VehicleId = vehicle.Id;
            }

            // 创建停车记录
            var record = new ParkingRecord
            {
                PlateNumber = request.PlateNumber ?? (vehicle?.PlateNumber ?? ""),
                LotId = space.LotId,
                SpaceId = space.Id,
                EntryTime = DateTime.UtcNow,
                Status = "open",
                VehicleType = request.VehicleType ?? "temporary",
                ProjectId = request.ProjectId,
                ProjectCode = request.ProjectCode,
                CreatedAt = DateTime.UtcNow
            };

            db.ParkingRecords.Add(record);
            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "入场成功",
                data = new
                {
                    recordId = record.Id,
                    spaceId = space.Id,
                    plateNumber = record.PlateNumber,
                    entryTime = record.EntryTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckIn {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 停车出场 - 计算费用并更新状态
    /// POST /api/tenant/parkings/{id}/check-out
    /// </summary>
    [HttpPost("{id}/check-out")]
    public async Task<IActionResult> CheckOut(int id, [FromBody] CheckOutRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var space = await db.ParkingSpaces.FindAsync(id);
            if (space == null)
                return Ok(new { success = false, message = "车位不存在" });

            // 查找该车位的在场记录
            var record = await db.ParkingRecords
                .Where(r => r.SpaceId == id && r.Status == "open")
                .OrderByDescending(r => r.EntryTime)
                .FirstOrDefaultAsync();

            if (record == null)
                return Ok(new { success = false, message = "无在场停车记录" });

            // 计算费用
            var exitTime = DateTime.UtcNow;
            var durationMinutes = (int)(exitTime - record.EntryTime).TotalMinutes;
            record.ExitTime = exitTime;
            record.DurationMinutes = durationMinutes;

            // 获取费率
            var lot = await db.ParkingLots.FindAsync(space.LotId);
            decimal fee = 0m;

            if (record.VehicleType == "monthly" || record.VehicleType == "reserved")
            {
                // 月租车/固定车位：按月计费（不足一天按一天算）
                var days = Math.Max(1, (int)Math.Ceiling(durationMinutes / 1440.0));
                fee = (lot?.MonthlyRate ?? 300m) * days / 30;
                record.Fee = Math.Round(fee, 2);
            }
            else
            {
                // 临停车：按小时计费
                var hours = Math.Max(1, (int)Math.Ceiling(durationMinutes / 60.0));
                fee = (lot?.HourlyRate ?? 5m) * hours;
                record.Fee = Math.Round(fee, 2);
            }

            record.Status = "closed";
            record.PaymentStatus = request.PaymentStatus ?? "pending";

            // 更新车位状态为空闲
            space.Status = "available";
            space.VehicleId = null;
            space.UpdatedAt = DateTime.UtcNow;

            // 产生支付记录
            if (fee > 0)
            {
                var payment = new ParkingPayment
                {
                    PaymentNo = $"P{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                    PlateNumber = record.PlateNumber,
                    Amount = record.Fee,
                    Type = "parking",
                    Method = request.PaymentMethod ?? "wechat",
                    ProjectId = record.ProjectId,
                    ProjectCode = record.ProjectCode,
                    PaidAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                db.ParkingPayments.Add(payment);
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "出场成功",
                data = new
                {
                    recordId = record.Id,
                    plateNumber = record.PlateNumber,
                    entryTime = record.EntryTime,
                    exitTime = record.ExitTime,
                    durationMinutes = record.DurationMinutes,
                    fee = record.Fee,
                    paymentStatus = record.PaymentStatus
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckOut {Id} failed", id);
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

// ─── Request DTOs ───

public class CreateSpaceRequest
{
    public string SpaceNo { get; set; } = "";
    public string? Type { get; set; }
    public string? Status { get; set; }
    public int LotId { get; set; }
    public int? VehicleId { get; set; }
    public int ProjectId { get; set; } = 1;
    public string? ProjectCode { get; set; }
}

public class UpdateSpaceRequest
{
    public string? SpaceNo { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public int? LotId { get; set; }
    public int? VehicleId { get; set; }
}

public class CheckInRequest
{
    public string? PlateNumber { get; set; }
    public int? VehicleId { get; set; }
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? VehicleType { get; set; }
    public int ProjectId { get; set; } = 1;
    public string? ProjectCode { get; set; }
}

public class CheckOutRequest
{
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
}
