using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.DeliveryService.Data;
using WO.Property.DeliveryService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5015");

builder.Services.AddDbContext<DeliveryDbContext>(options =>
    options.UseMySql("Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4", ServerVersion.Parse("8.0.35")));

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


app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "DeliveryService",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    features = new[] { "外卖订单", "骑手管理", "取餐通知", "统计报表" }
}));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Delivery Service (外卖管理)");
Console.WriteLine("  Port: 5017");
Console.WriteLine("===========================================");

app.Run();

// ==================== 订单控制器 ====================
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryDbContext _context;
    public DeliveriesController(DeliveryDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] DeliveryStatus? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? roomNumber = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Deliveries.Include(d => d.Rider).AsQueryable();
        if (status.HasValue) query = query.Where(d => d.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(d => d.OrderNumber.Contains(keyword) || d.CustomerName.Contains(keyword) || d.CustomerPhone.Contains(keyword) || (d.MerchantName != null && d.MerchantName.Contains(keyword)));
        if (!string.IsNullOrWhiteSpace(roomNumber))
            query = query.Where(d => d.RoomNumber.Contains(roomNumber));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDelivery(int id)
    {
        var delivery = await _context.Deliveries.Include(d => d.Rider).FirstOrDefaultAsync(d => d.Id == id);
        if (delivery == null) return NotFound(new { success = false, message = "订单不存在" });
        return Ok(new { success = true, data = delivery });
    }

    [HttpGet("by-room/{roomNumber}")]
    public async Task<IActionResult> GetByRoom(string roomNumber)
    {
        var items = await _context.Deliveries.Include(d => d.Rider)
            .Where(d => d.RoomNumber == roomNumber && d.Status != DeliveryStatus.Delivered && d.Status != DeliveryStatus.Cancelled)
            .OrderByDescending(d => d.CreatedAt).ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveDeliveries()
    {
        var items = await _context.Deliveries.Include(d => d.Rider)
            .Where(d => d.Status != DeliveryStatus.Delivered && d.Status != DeliveryStatus.Cancelled)
            .OrderBy(d => d.Status == DeliveryStatus.Ready ? 0 : 1).ThenBy(d => d.CreatedAt)
            .ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryRequest request)
    {
        var exists = await _context.Deliveries.AnyAsync(d => d.OrderNumber == request.OrderNumber);
        if (exists) return BadRequest(new { success = false, message = "该订单号已存在" });
        var year = DateTime.Now.Year;
        var count = await _context.Deliveries.CountAsync() + 1;
        var delivery = new FoodDelivery
        {
            OrderNumber = request.OrderNumber,
            MerchantName = request.MerchantName,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            RoomNumber = request.RoomNumber,
            DeliveryAddress = request.DeliveryAddress,
            TotalAmount = request.TotalAmount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = request.PaymentStatus,
            Remarks = request.Remarks,
            Status = DeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "订单创建成功", data = delivery });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var delivery = await _context.Deliveries.FindAsync(id);
        if (delivery == null) return NotFound(new { success = false, message = "订单不存在" });
        delivery.Status = request.Status;
        delivery.RiderId = request.RiderId ?? delivery.RiderId;
        delivery.Remarks = request.Remarks ?? delivery.Remarks;
        delivery.UpdatedAt = DateTime.UtcNow;
        if (request.Status == DeliveryStatus.Delivered) delivery.DeliveredAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var updated = await _context.Deliveries.Include(d => d.Rider).FirstAsync(d => d.Id == id);
        return Ok(new { success = true, message = "状态更新成功", data = updated });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var stats = new
        {
            total = await _context.Deliveries.CountAsync(),
            pending = await _context.Deliveries.CountAsync(d => d.Status == DeliveryStatus.Pending),
            preparing = await _context.Deliveries.CountAsync(d => d.Status == DeliveryStatus.Preparing),
            ready = await _context.Deliveries.CountAsync(d => d.Status == DeliveryStatus.Ready),
            inDelivery = await _context.Deliveries.CountAsync(d => d.Status == DeliveryStatus.InDelivery),
            delivered = await _context.Deliveries.CountAsync(d => d.Status == DeliveryStatus.Delivered),
            cancelled = await _context.Deliveries.CountAsync(d => d.Status == DeliveryStatus.Cancelled),
            todayOrders = await _context.Deliveries.CountAsync(d => d.CreatedAt >= today),
            todayRevenue = await _context.Deliveries.Where(d => d.CreatedAt >= today && d.PaymentStatus == PaymentStatus.Paid).SumAsync(d => d.TotalAmount),
            byStatus = await _context.Deliveries.GroupBy(d => d.Status).Select(g => new { status = g.Key.ToString(), count = g.Count() }).ToListAsync()
        };
        return Ok(new { success = true, data = stats });
    }
}

// ==================== 骑手控制器 ====================
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RidersController : ControllerBase
{
    private readonly DeliveryDbContext _context;
    public RidersController(DeliveryDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetRiders([FromQuery] bool activeOnly = true)
    {
        var query = activeOnly ? _context.Riders.Where(r => r.IsActive) : _context.Riders;
        var items = await query.OrderBy(r => r.Id).ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRider(int id)
    {
        var rider = await _context.Riders.FindAsync(id);
        if (rider == null) return NotFound(new { success = false, message = "骑手不存在" });
        return Ok(new { success = true, data = rider });
    }

    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetRiderDeliveries(int id, [FromQuery] int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var items = await _context.Deliveries
            .Where(d => d.RiderId == id && d.CreatedAt >= since)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRider([FromBody] Rider rider)
    {
        if (string.IsNullOrWhiteSpace(rider.Name)) return BadRequest(new { success = false, message = "姓名不能为空" });
        _context.Riders.Add(rider);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "骑手创建成功", data = rider });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRider(int id, [FromBody] Rider updated)
    {
        var rider = await _context.Riders.FindAsync(id);
        if (rider == null) return NotFound(new { success = false, message = "骑手不存在" });
        rider.Name = updated.Name;
        rider.Phone = updated.Phone;
        rider.Platform = updated.Platform;
        rider.PlateNumber = updated.PlateNumber;
        rider.IsActive = updated.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "更新成功", data = rider });
    }
}
