using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.ExpressService.Data;
using WO.Property.ExpressService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5024");

builder.Services.AddDbContext<ExpressDbContext>(options =>
    options.UseNpgsql("Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!"));

var jwtIssuer = "wo-property-unified-auth";
var jwtAudience = "wo-property-services";
var jwtSecretKey = JwtHelper.GetSecretKey();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 允许前端开发服务器跨域
builder.Services.AddCors(options => {
  options.AddPolicy("AllowFrontend", policy => {
    policy.WithOrigins(
      "http://localhost:5173",
      "http://localhost:5174",
      "http://localhost:5175"
    ).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
  });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ExpressDbContext>();
    context.Database.EnsureCreated();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "ExpressService",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    features = new[] { "快递登记", "取件管理", "快递公司", "通知推送", "统计报表" }
}));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Express Service (快递管理)");
Console.WriteLine("  Port: 5016");
Console.WriteLine("===========================================");

app.Run();

// ==================== 快递控制器 ====================
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpressController : ControllerBase
{
    private readonly ExpressDbContext _context;
    public ExpressController(ExpressDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] ExpressStatus? status = null,
        [FromQuery] int? companyId = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? roomNumber = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Deliveries.Include(e => e.Company).AsQueryable();
        if (status.HasValue) query = query.Where(e => e.Status == status.Value);
        if (companyId.HasValue) query = query.Where(e => e.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(e => e.TrackingNumber.Contains(keyword) || e.ReceiverName.Contains(keyword) || e.ReceiverPhone.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(roomNumber))
            query = query.Where(e => e.RoomNumber.Contains(roomNumber));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.ReceivedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDelivery(int id)
    {
        var delivery = await _context.Deliveries.Include(e => e.Company).FirstOrDefaultAsync(e => e.Id == id);
        if (delivery == null) return NotFound(new { success = false, message = "快递记录不存在" });
        return Ok(new { success = true, data = delivery });
    }

    [HttpGet("by-tracking/{trackingNumber}")]
    public async Task<IActionResult> GetByTrackingNumber(string trackingNumber)
    {
        var delivery = await _context.Deliveries.Include(e => e.Company).FirstOrDefaultAsync(e => e.TrackingNumber == trackingNumber);
        if (delivery == null) return NotFound(new { success = false, message = "未找到该快递" });
        return Ok(new { success = true, data = delivery });
    }

    [HttpGet("by-room/{roomNumber}")]
    public async Task<IActionResult> GetByRoom(string roomNumber)
    {
        var items = await _context.Deliveries.Include(e => e.Company)
            .Where(e => e.RoomNumber == roomNumber && e.Status != ExpressStatus.Delivered && e.Status != ExpressStatus.Returned)
            .OrderByDescending(e => e.ReceivedAt).ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateExpressRequest request)
    {
        var exists = await _context.Deliveries.AnyAsync(e => e.TrackingNumber == request.TrackingNumber);
        if (exists) return BadRequest(new { success = false, message = "该快递单号已存在" });
        var company = await _context.Companies.FindAsync(request.CompanyId);
        if (company == null || !company.IsActive) return BadRequest(new { success = false, message = "快递公司不存在或已停用" });
        var year = DateTime.Now.Year;
        var count = await _context.Deliveries.CountAsync() + 1;
        var delivery = new ExpressDelivery
        {
            TrackingNumber = request.TrackingNumber,
            ExpressNumber = request.ExpressNumber ?? $"EXP-{year}-{count:D4}",
            CompanyId = request.CompanyId,
            SenderName = request.SenderName,
            SenderPhone = request.SenderPhone,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            RoomNumber = request.RoomNumber,
            PickupAddress = request.PickupAddress,
            Remarks = request.Remarks,
            Status = ExpressStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(request.StorageDays),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "快递登记成功", data = delivery });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateExpressStatusRequest request)
    {
        var delivery = await _context.Deliveries.FindAsync(id);
        if (delivery == null) return NotFound(new { success = false, message = "快递记录不存在" });
        delivery.Status = request.Status;
        delivery.Remarks = request.Remarks ?? delivery.Remarks;
        delivery.ReceivedBy = request.ReceivedBy ?? delivery.ReceivedBy;
        delivery.UpdatedAt = DateTime.UtcNow;
        if (request.Status == ExpressStatus.Delivered) delivery.PickedUpAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "状态更新成功", data = delivery });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);
        var stats = new
        {
            total = await _context.Deliveries.CountAsync(),
            pending = await _context.Deliveries.CountAsync(e => e.Status == ExpressStatus.Pending),
            inStorage = await _context.Deliveries.CountAsync(e => e.Status == ExpressStatus.InStorage),
            delivered = await _context.Deliveries.CountAsync(e => e.Status == ExpressStatus.Delivered),
            expired = await _context.Deliveries.CountAsync(e => e.Status == ExpressStatus.Expired),
            todayReceived = await _context.Deliveries.CountAsync(e => e.ReceivedAt >= today),
            weekReceived = await _context.Deliveries.CountAsync(e => e.ReceivedAt >= weekAgo),
            expiringToday = await _context.Deliveries.CountAsync(e => e.ExpiresAt.Date == today && e.Status == ExpressStatus.InStorage),
            byCompany = await _context.Deliveries.GroupBy(e => e.Company!.Name).Select(g => new { name = g.Key, count = g.Count() }).ToListAsync()
        };
        return Ok(new { success = true, data = stats });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var pendingDeliveries = await _context.Deliveries.Include(e => e.Company)
            .Where(e => e.Status == ExpressStatus.Pending || e.Status == ExpressStatus.InStorage)
            .OrderBy(e => e.ExpiresAt).Take(10).ToListAsync();
        var todayStats = await _context.Deliveries.Where(e => e.ReceivedAt.Date == today)
            .GroupBy(e => e.Status).Select(g => new { status = g.Key.ToString(), count = g.Count() }).ToListAsync();
        return Ok(new { success = true, data = new { pendingDeliveries, todayStats, totalPending = pendingDeliveries.Count } });
    }
}

// ==================== 快递公司控制器 ====================
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ExpressDbContext _context;
    public CompaniesController(ExpressDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetCompanies([FromQuery] bool activeOnly = true)
    {
        var query = activeOnly ? _context.Companies.Where(c => c.IsActive) : _context.Companies;
        var items = await query.OrderBy(c => c.Id).ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] ExpressCompany company)
    {
        if (string.IsNullOrWhiteSpace(company.Name)) return BadRequest(new { success = false, message = "公司名称不能为空" });
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "创建成功", data = company });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] ExpressCompany updated)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return NotFound(new { success = false, message = "公司不存在" });
        company.Name = updated.Name;
        company.Code = updated.Code;
        company.Phone = updated.Phone;
        company.IsActive = updated.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "更新成功", data = company });
    }
}

// ==================== 通知控制器 ====================
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ExpressDbContext _context;
    public NotificationsController(ExpressDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int? expressId = null, [FromQuery] NotificationStatus? status = null)
    {
        var query = _context.Notifications.AsQueryable();
        if (expressId.HasValue) query = query.Where(n => n.ExpressId == expressId.Value);
        if (status.HasValue) query = query.Where(n => n.Status == status.Value);
        var items = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] ExpressNotification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "通知已创建", data = notification });
    }
}
