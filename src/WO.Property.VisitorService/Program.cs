using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.VisitorService.Data;
using WO.Property.VisitorService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5013端口
builder.WebHost.UseUrls("http://0.0.0.0:5013");

// 添加数据库
builder.Services.AddDbContext<VisitorDbContext>(options =>
    options.UseNpgsql("Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!"));

// JWT 配置
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

var app = builder.Build();

// 数据库初始化
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VisitorDbContext>();
    context.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "VisitorService", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Visitor Service");
Console.WriteLine("  Port: 5013");
Console.WriteLine("===========================================");

app.Run();

// 访客控制器
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisitorsController : ControllerBase
{
    private readonly VisitorDbContext _context;
    
    public VisitorsController(VisitorDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetVisitors(
        [FromQuery] VisitStatus? status = null,
        [FromQuery] VisitType? type = null,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Visitors.AsQueryable();
        
        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);
        if (type.HasValue)
            query = query.Where(v => v.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(v => v.VisitorName.Contains(keyword) || v.HostName.Contains(keyword) || v.VisitorNumber.Contains(keyword));
        if (fromDate.HasValue)
            query = query.Where(v => v.ScheduledDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(v => v.ScheduledDate <= toDate.Value);
        
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.ScheduledDate)
            .ThenByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = items });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVisitor(int id)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        
        var records = await _context.VisitRecords
            .Where(r => r.VisitorId == id)
            .OrderByDescending(r => r.AccessTime)
            .ToListAsync();
        
        return Ok(new { success = true, data = new { visitor, records } });
    }
    
    [HttpGet("number/{number}")]
    public async Task<IActionResult> GetVisitorByNumber(string number)
    {
        var visitor = await _context.Visitors.FirstOrDefaultAsync(v => v.VisitorNumber == number);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        return Ok(new { success = true, data = visitor });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorRequest request)
    {
        var year = DateTime.Now.Year;
        var count = await _context.Visitors.CountAsync() + 1;
        var visitorNumber = $"VIS-{year}-{count:D4}";
        var accessCode = $"V{year}{count:D4}";
        
        var visitor = new Visitor
        {
            VisitorNumber = visitorNumber,
            VisitorName = request.VisitorName,
            VisitorPhone = request.VisitorPhone,
            VisitorEmail = request.VisitorEmail,
            IDType = request.IDType,
            IDNumber = request.IDNumber,
            Type = request.Type,
            HostName = request.HostName,
            HostPhone = request.HostPhone,
            HostUnit = request.HostUnit,
            VisitLocation = request.VisitLocation,
            ScheduledDate = request.ScheduledDate,
            ScheduledStartTime = request.ScheduledStartTime,
            ScheduledEndTime = request.ScheduledEndTime,
            Purpose = request.Purpose,
            ExpectedVisitors = request.ExpectedVisitors,
            Status = VisitStatus.Pending,
            LicensePlate = request.LicensePlate,
            Remarks = request.Remarks,
            AccessCode = accessCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "访客预约已创建", data = visitor });
    }
    
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveVisitor(int id, [FromBody] ApproveRequest request)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        
        visitor.Status = VisitStatus.Approved;
        visitor.Approver = request.Approver;
        visitor.ApprovedDate = DateTime.UtcNow;
        visitor.ApprovalRemarks = request.Remarks;
        visitor.AccessGranted = true;
        visitor.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "已批准访问", data = visitor });
    }
    
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectVisitor(int id, [FromBody] RejectRequest request)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        
        visitor.Status = VisitStatus.Rejected;
        visitor.ApprovalRemarks = request.Reason;
        visitor.AccessGranted = false;
        visitor.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "已拒绝访问", data = visitor });
    }
    
    [HttpPut("{id}/checkin")]
    public async Task<IActionResult> CheckInVisitor(int id, [FromBody] CheckInRequest? request)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        
        if (visitor.Status != VisitStatus.Approved)
            return BadRequest(new { success = false, message = "只有已批准的访问才能签到" });
        
        visitor.Status = VisitStatus.CheckIn;
        visitor.ActualCheckInTime = DateTime.UtcNow;
        visitor.UpdatedAt = DateTime.UtcNow;
        
        // 添加访问记录
        var record = new VisitRecord
        {
            VisitorId = id,
            RecordNumber = $"REC-{DateTime.Now.Year}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            GateDevice = request?.GateDevice ?? "手动登记",
            AccessDirection = "In",
            AccessTime = DateTime.UtcNow,
            Temperature = request?.Temperature,
            RegisteredBy = request?.RegisteredBy,
            Remarks = "访客签到",
            CreatedAt = DateTime.UtcNow
        };
        _context.VisitRecords.Add(record);
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "签到成功", data = visitor });
    }
    
    [HttpPut("{id}/checkout")]
    public async Task<IActionResult> CheckOutVisitor(int id, [FromBody] CheckOutRequest? request)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        
        if (visitor.Status != VisitStatus.CheckIn)
            return BadRequest(new { success = false, message = "访客未签到" });
        
        visitor.Status = VisitStatus.CheckOut;
        visitor.ActualCheckOutTime = DateTime.UtcNow;
        visitor.UpdatedAt = DateTime.UtcNow;
        
        // 添加访问记录
        var record = new VisitRecord
        {
            VisitorId = id,
            RecordNumber = $"REC-{DateTime.Now.Year}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            GateDevice = request?.GateDevice ?? "手动登记",
            AccessDirection = "Out",
            AccessTime = DateTime.UtcNow,
            RegisteredBy = request?.RegisteredBy,
            Remarks = "访客签离",
            CreatedAt = DateTime.UtcNow
        };
        _context.VisitRecords.Add(record);
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "签离成功", data = visitor });
    }
    
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelVisitor(int id, [FromBody] CancelRequest request)
    {
        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            return NotFound(new { success = false, message = "访客记录不存在" });
        
        visitor.Status = VisitStatus.Cancelled;
        visitor.Remarks = request.Reason;
        visitor.AccessGranted = false;
        visitor.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "访问已取消", data = visitor });
    }
    
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayVisitors()
    {
        var today = DateTime.Today;
        var items = await _context.Visitors
            .Where(v => v.ScheduledDate == today)
            .OrderBy(v => v.ScheduledStartTime)
            .ToListAsync();
        
        return Ok(new { success = true, data = items });
    }
    
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveVisitors()
    {
        var items = await _context.Visitors
            .Where(v => v.Status == VisitStatus.CheckIn)
            .OrderByDescending(v => v.ActualCheckInTime)
            .ToListAsync();
        
        return Ok(new { success = true, data = items });
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        
        var stats = new
        {
            todayTotal = await _context.Visitors.CountAsync(v => v.ScheduledDate == today),
            todayPending = await _context.Visitors.CountAsync(v => v.ScheduledDate == today && v.Status == VisitStatus.Pending),
            todayApproved = await _context.Visitors.CountAsync(v => v.ScheduledDate == today && v.Status == VisitStatus.Approved),
            todayCheckedIn = await _context.Visitors.CountAsync(v => v.ScheduledDate == today && v.Status == VisitStatus.CheckIn),
            todayCheckedOut = await _context.Visitors.CountAsync(v => v.ScheduledDate == today && v.Status == VisitStatus.CheckOut),
            weekTotal = await _context.Visitors.CountAsync(v => v.ScheduledDate >= weekStart && v.ScheduledDate <= today),
            activeVisitors = await _context.Visitors.CountAsync(v => v.Status == VisitStatus.CheckIn),
            byType = await _context.Visitors.Where(v => v.ScheduledDate == today).GroupBy(v => v.Type).Select(g => new { type = g.Key.ToString(), count = g.Count() }).ToListAsync()
        };
        
        return Ok(new { success = true, data = stats });
    }
}

// 访问记录控制器
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecordsController : ControllerBase
{
    private readonly VisitorDbContext _context;
    
    public RecordsController(VisitorDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetRecords(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? direction = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.VisitRecords.Include(r => r.Visitor).AsQueryable();
        
        if (fromDate.HasValue)
            query = query.Where(r => r.AccessTime >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.AccessTime <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(direction))
            query = query.Where(r => r.AccessDirection == direction);
        
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.AccessTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = items });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecord(int id)
    {
        var record = await _context.VisitRecords.Include(r => r.Visitor).FirstOrDefaultAsync(r => r.Id == id);
        if (record == null)
            return NotFound(new { success = false, message = "记录不存在" });
        return Ok(new { success = true, data = record });
    }
    
    [HttpGet("visitor/{visitorId}")]
    public async Task<IActionResult> GetVisitorRecords(int visitorId)
    {
        var records = await _context.VisitRecords
            .Where(r => r.VisitorId == visitorId)
            .OrderByDescending(r => r.AccessTime)
            .ToListAsync();
        return Ok(new { success = true, data = records });
    }
}

public class CreateVisitorRequest
{
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? VisitorEmail { get; set; }
    public IDType? IDType { get; set; }
    public string? IDNumber { get; set; }
    public VisitType Type { get; set; } = VisitType.Personal;
    public string HostName { get; set; } = string.Empty;
    public string? HostPhone { get; set; }
    public string? HostUnit { get; set; }
    public string? VisitLocation { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public string? Purpose { get; set; }
    public int? ExpectedVisitors { get; set; }
    public string? LicensePlate { get; set; }
    public string? Remarks { get; set; }
}

public class ApproveRequest
{
    public string Approver { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class RejectRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CancelRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CheckInRequest
{
    public string? GateDevice { get; set; }
    public decimal? Temperature { get; set; }
    public string? RegisteredBy { get; set; }
}

public class CheckOutRequest
{
    public string? GateDevice { get; set; }
    public string? RegisteredBy { get; set; }
}
