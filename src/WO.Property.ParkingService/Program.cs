using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.ParkingService.Data;
using WO.Property.ParkingService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);
// 端口 5501
builder.WebHost.UseUrls("http://0.0.0.0:5025");
// 数据库
builder.Services.AddDbContext<ParkingDbContext>(options =>
    options.UseNpgsql("Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!"));
// JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var jwtIssuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var jwtAudience = jwtSettings["Audience"] ?? "wo-property-services";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();
// JSON
builder.Services.ConfigureHttpJsonOptions(opts => {
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy => {
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5173" };
    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
}));
var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
    context.Database.EnsureCreated();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ParkingService", timestamp = DateTime.UtcNow }));
Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Parking Service");
Console.WriteLine("  Port: 5501");
app.Run();

// ============ 停车场控制器 ============
[ApiController]
[Route("api/parking")]
public class ParkingController : ControllerBase
{
    private readonly ParkingDbContext _context;
    public ParkingController(ParkingDbContext context) => _context = context;

    // ---------- 停车场 ----------
    [HttpGet("lots")]
    public async Task<IActionResult> GetLots([FromQuery] int? projectId = null)
    {
        var q = _context.ParkingLots.AsQueryable();
        if (projectId.HasValue) q = q.Where(l => l.ProjectId == projectId.Value);
        var lots = await q.OrderBy(l => l.Name).ToListAsync();
        return Ok(new { success = true, data = lots });
    }

    [HttpPost("lots")]
    public async Task<IActionResult> CreateLot([FromBody] ParkingLot lot)
    {
        _context.ParkingLots.Add(lot);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = lot });
    }

    [HttpPut("lots/{id}")]
    public async Task<IActionResult> UpdateLot(int id, [FromBody] ParkingLot patch)
    {
        var lot = await _context.ParkingLots.FindAsync(id);
        if (lot == null) return NotFound();
        lot.Name = patch.Name;
        lot.Location = patch.Location;
        lot.TotalSpaces = patch.TotalSpaces;
        lot.HourlyRate = patch.HourlyRate;
        lot.MonthlyRate = patch.MonthlyRate;
        lot.ProjectId = patch.ProjectId;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = lot });
    }

    [HttpDelete("lots/{id}")]
    public async Task<IActionResult> DeleteLot(int id)
    {
        var lot = await _context.ParkingLots.FindAsync(id);
        if (lot == null) return NotFound();
        _context.ParkingLots.Remove(lot);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ---------- 车位 ----------
    [HttpGet("spaces")]
    public async Task<IActionResult> GetSpaces([FromQuery] int? lotId = null, [FromQuery] string? status = null)
    {
        var q = _context.ParkingSpaces.AsQueryable();
        if (lotId.HasValue) q = q.Where(s => s.LotId == lotId.Value);
        if (!string.IsNullOrEmpty(status)) q = q.Where(s => s.Status == status);
        var spaces = await q.Include(s => s.Lot).OrderBy(s => s.SpaceNo).ToListAsync();
        return Ok(new { success = true, data = spaces });
    }

    [HttpPost("spaces")]
    public async Task<IActionResult> CreateSpace([FromBody] ParkingSpace space)
    {
        _context.ParkingSpaces.Add(space);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = space });
    }

    [HttpPut("spaces/{id}")]
    public async Task<IActionResult> UpdateSpace(int id, [FromBody] ParkingSpace patch)
    {
        var space = await _context.ParkingSpaces.FindAsync(id);
        if (space == null) return NotFound();
        space.SpaceNo = patch.SpaceNo;
        space.Type = patch.Type;
        space.Status = patch.Status;
        space.LotId = patch.LotId;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = space });
    }

    [HttpDelete("spaces/{id}")]
    public async Task<IActionResult> DeleteSpace(int id)
    {
        var space = await _context.ParkingSpaces.FindAsync(id);
        if (space == null) return NotFound();
        _context.ParkingSpaces.Remove(space);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ---------- 车辆 ----------
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles([FromQuery] string? ownerName = null)
    {
        var q = _context.Vehicles.AsQueryable();
        if (!string.IsNullOrEmpty(ownerName))
            q = q.Where(v => v.OwnerName.Contains(ownerName) || v.PlateNumber.Contains(ownerName));
        var vehicles = await q.OrderBy(v => v.OwnerName).ToListAsync();
        return Ok(new { success = true, data = vehicles });
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle([FromBody] Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = vehicle });
    }

    [HttpPut("vehicles/{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] Vehicle patch)
    {
        var v = await _context.Vehicles.FindAsync(id);
        if (v == null) return NotFound();
        v.PlateNumber = patch.PlateNumber;
        v.Brand = patch.Brand;
        v.Color = patch.Color;
        v.OwnerName = patch.OwnerName;
        v.OwnerPhone = patch.OwnerPhone;
        v.Type = patch.Type;
        v.ProjectId = patch.ProjectId;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = v });
    }

    [HttpDelete("vehicles/{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var v = await _context.Vehicles.FindAsync(id);
        if (v == null) return NotFound();
        _context.Vehicles.Remove(v);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ---------- 停车记录 ----------
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords(
        [FromQuery] string? plate = null,
        [FromQuery] int? lotId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _context.ParkingRecords.AsQueryable();
        if (!string.IsNullOrEmpty(plate)) q = q.Where(r => r.PlateNumber.Contains(plate));
        if (lotId.HasValue) q = q.Where(r => r.LotId == lotId.Value);
        if (startDate.HasValue) q = q.Where(r => r.EntryTime >= startDate.Value);
        if (endDate.HasValue) q = q.Where(r => r.EntryTime <= endDate.Value);
        var total = await q.CountAsync();
        var records = await q
            .Include(r => r.Lot)
            .OrderByDescending(r => r.EntryTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data = records });
    }

    /// <summary>车辆入场</summary>
    [HttpPost("records/entry")]
    public async Task<IActionResult> VehicleEntry([FromBody] EntryRequest req)
    {
        var openRecord = await _context.ParkingRecords
            .FirstOrDefaultAsync(r => r.PlateNumber == req.PlateNumber && r.Status == "open");
        if (openRecord != null)
            return BadRequest(new { success = false, message = "该车辆有未结算入场记录" });
        var record = new ParkingRecord {
            PlateNumber = req.PlateNumber,
            LotId = req.LotId,
            EntryTime = DateTime.UtcNow,
            Status = "open",
            ProjectId = req.ProjectId > 0 ? req.ProjectId : 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParkingRecords.Add(record);
        if (req.SpaceId.HasValue) {
            var space = await _context.ParkingSpaces.FindAsync(req.SpaceId.Value);
            if (space != null) space.Status = "occupied";
        }
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "入场成功", data = record });
    }

    /// <summary>车辆出场（结算费用）</summary>
    [HttpPost("records/exit")]
    public async Task<IActionResult> VehicleExit([FromBody] ExitRequest req)
    {
        var record = await _context.ParkingRecords
            .Where(r => r.PlateNumber == req.PlateNumber && r.Status == "open")
            .FirstOrDefaultAsync();
        if (record == null)
            return NotFound(new { success = false, message = "未找到入场记录" });
        var exitTime = DateTime.UtcNow;
        var durationHours = (exitTime - record.EntryTime).TotalHours;
        var lot = record.Lot;
        decimal fee = 0;
        if (record.VehicleType == "monthly") {
            fee = 0;
        } else {
            decimal hours = (decimal)Math.Ceiling(durationHours);
            fee = hours * (lot?.HourlyRate ?? 5m);
        }
        record.ExitTime = exitTime;
        record.DurationMinutes = (int)durationHours;
        record.Fee = fee;
        record.Status = req.Status ?? "closed";
        record.PaymentStatus = fee == 0 ? "paid" : "unpaid";
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = $"应缴停车费 {fee} 元", data = record });
    }

    /// <summary>获取当前在场车辆</summary>
    [HttpGet("records/current")]
    public async Task<IActionResult> GetCurrentRecords([FromQuery] int? lotId = null)
    {
        var q = _context.ParkingRecords.Where(r => r.Status == "open");
        if (lotId.HasValue) q = q.Where(r => r.LotId == lotId.Value);
        var records = await q.Include(r => r.Lot).OrderByDescending(r => r.EntryTime).ToListAsync();
        return Ok(new { success = true, data = records });
    }

    /// <summary>获取统计</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? projectId = null)
    {
        var q = _context.ParkingRecords.AsQueryable();
        if (projectId.HasValue) q = q.Where(r => r.ProjectId == projectId.Value);
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayCount = await q.Where(r => r.EntryTime >= today).CountAsync();
        var monthFee = await q.Where(r => r.EntryTime >= monthStart && r.PaymentStatus == "paid")
            .SumAsync(r => r.Fee);
        var totalVehicles = await _context.Vehicles.CountAsync();
        var occupiedSpaces = await _context.ParkingSpaces.CountAsync(s => s.Status == "occupied");
        var totalSpaces = await _context.ParkingSpaces.CountAsync();
        return Ok(new { success = true, data = new {
            todayCount, monthFee,
            totalVehicles, occupiedSpaces, totalSpaces,
            occupancyRate = totalSpaces > 0 ? Math.Round(100.0 * occupiedSpaces / totalSpaces, 1) : 0
        }});
    }

    /// <summary>结算停车费</summary>
    [HttpPost("records/{id}/pay")]
    public async Task<IActionResult> PayFee(int id, [FromBody] PayRequest req)
    {
        var record = await _context.ParkingRecords.FindAsync(id);
        if (record == null) return NotFound();
        record.PaymentStatus = "paid";
        record.PaidAt = DateTime.UtcNow;
        record.PaymentMethod = req.Method ?? "wechat";
        record.Remark = req.Remark;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = record });
    }
}

// ============ 请求模型 ============
public class EntryRequest
{
    public string PlateNumber { get; set; } = string.Empty;
    public int LotId { get; set; }
    public int? SpaceId { get; set; }
    public string? VehicleType { get; set; }
    public int ProjectId { get; set; } = 1;
}

public class ExitRequest
{
    public string PlateNumber { get; set; } = string.Empty;
    public string? Status { get; set; }
}

public class PayRequest
{
    public string? Method { get; set; }
    public string? Remark { get; set; }
}
