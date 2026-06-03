using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.ParkingService.Data;
using WO.Property.ParkingService.Models;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.ParkingService.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/parking")]
public class TenantParkingController : ControllerBase
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
    private readonly ILogger<TenantParkingController> _logger;

    public TenantParkingController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ILogger<TenantParkingController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    // GET /api/tenant/parking/lots
    [HttpGet("lots")]
    public async Task<IActionResult> GetLots([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            using var db = CreateDbContext();
            var total = await db.ParkingLots.CountAsync();
            var items = await db.ParkingLots
                .OrderByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    location = l.Location ?? "",
                    totalSpaces = l.TotalSpaces,
                    hourlyRate = l.HourlyRate,
                    monthlyRate = l.MonthlyRate,
                    projectId = l.ProjectId
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLots failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/parking/lots
    [HttpPost("lots")]
    public async Task<IActionResult> CreateLot([FromBody] CreateLotRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var lot = new ParkingLot
            {
                Name = request.Name,
                Location = request.Location,
                TotalSpaces = request.TotalSpaces,
                HourlyRate = request.HourlyRate,
                MonthlyRate = request.MonthlyRate,
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.ParkingLots.Add(lot);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "停车场创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateLot failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/parking/vehicles
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? plateNumber = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.Vehicles.AsQueryable();
            if (!string.IsNullOrEmpty(plateNumber))
                query = query.Where(v => v.PlateNumber.Contains(plateNumber));
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    id = v.Id,
                    plateNumber = v.PlateNumber,
                    brand = v.Brand ?? "",
                    color = v.Color ?? "",
                    ownerName = v.OwnerName ?? "",
                    ownerPhone = v.OwnerPhone ?? "",
                    type = v.Type,
                    projectId = v.ProjectId
                })
                .ToListAsync();
            return Ok(new { success = true, data = items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetVehicles failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // POST /api/tenant/parking/vehicles
    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var vehicle = new Vehicle
            {
                PlateNumber = request.PlateNumber,
                Brand = request.Brand,
                Color = request.Color,
                OwnerName = request.OwnerName,
                OwnerPhone = request.OwnerPhone,
                Type = request.Type ?? "personal",
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "车辆登记成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateVehicle failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // GET /api/tenant/parking/records
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] string? plateNumber = null)
    {
        try
        {
            using var db = CreateDbContext();
            var query = db.ParkingRecords.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(plateNumber))
                query = query.Where(r => r.PlateNumber.Contains(plateNumber));
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.EntryTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    plateNumber = r.PlateNumber,
                    lotId = r.LotId,
                    entryTime = r.EntryTime,
                    exitTime = r.ExitTime,
                    status = r.Status,
                    vehicleType = r.VehicleType ?? "",
                    durationMinutes = r.DurationMinutes,
                    fee = r.Fee,
                    paymentStatus = r.PaymentStatus ?? "",
                    paidAt = r.PaidAt
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

    // POST /api/tenant/parking/records/entry
    [HttpPost("records/entry")]
    public async Task<IActionResult> RecordEntry([FromBody] RecordEntryRequest request)
    {
        try
        {
            using var db = CreateDbContext();
            var record = new ParkingRecord
            {
                PlateNumber = request.PlateNumber,
                LotId = request.LotId,
                EntryTime = DateTime.UtcNow,
                Status = "open",
                VehicleType = request.VehicleType ?? "temporary",
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
            db.ParkingRecords.Add(record);
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "入场记录成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecordEntry failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class CreateLotRequest
{
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public int TotalSpaces { get; set; }
    public decimal HourlyRate { get; set; } = 5m;
    public decimal MonthlyRate { get; set; } = 300m;
    public int ProjectId { get; set; } = 1;
}

public class CreateVehicleRequest
{
    public string PlateNumber { get; set; } = "";
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? Type { get; set; }
    public int ProjectId { get; set; } = 1;
}

public class RecordEntryRequest
{
    public string PlateNumber { get; set; } = "";
    public int LotId { get; set; }
    public string? VehicleType { get; set; }
    public int ProjectId { get; set; } = 1;
}