using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.KeyService.Data;
using WO.Property.KeyService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5012端口
builder.WebHost.UseUrls("http://0.0.0.0:5012");

// 添加数据库
builder.Services.AddDbContext<KeyDbContext>(options =>
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
    var context = scope.ServiceProvider.GetRequiredService<KeyDbContext>();
    context.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "KeyService", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Key Service");
Console.WriteLine("  Port: 5012");
Console.WriteLine("===========================================");

app.Run();

// 钥匙控制器
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KeysController : ControllerBase
{
    private readonly KeyDbContext _context;
    
    public KeysController(KeyDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetKeys(
        [FromQuery] KeyStatus? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Keys.AsQueryable();
        
        if (status.HasValue)
            query = query.Where(k => k.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(k => k.KeyNumber.Contains(keyword) || k.Name.Contains(keyword) || (k.Location != null && k.Location.Contains(keyword)));
        
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(k => k.KeyNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = items });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetKey(int id)
    {
        var key = await _context.Keys.FindAsync(id);
        if (key == null)
            return NotFound(new { success = false, message = "钥匙不存在" });
        return Ok(new { success = true, data = key });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateKey([FromBody] Key key)
    {
        key.CreatedAt = DateTime.UtcNow;
        key.UpdatedAt = DateTime.UtcNow;
        _context.Keys.Add(key);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "钥匙创建成功", data = key });
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKey(int id, [FromBody] Key key)
    {
        var existing = await _context.Keys.FindAsync(id);
        if (existing == null)
            return NotFound(new { success = false, message = "钥匙不存在" });
        
        existing.Name = key.Name;
        existing.Description = key.Description;
        existing.Location = key.Location;
        existing.RoomNumber = key.RoomNumber;
        existing.Status = key.Status;
        existing.TotalCopies = key.TotalCopies;
        existing.AvailableCopies = key.AvailableCopies;
        existing.StorageLocation = key.StorageLocation;
        existing.Remarks = key.Remarks;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "钥匙更新成功", data = existing });
    }
    
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateKeyStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var key = await _context.Keys.FindAsync(id);
        if (key == null)
            return NotFound(new { success = false, message = "钥匙不存在" });
        
        key.Status = request.Status;
        key.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // 记录使用日志
        var log = new KeyUsageLog
        {
            KeyId = id,
            Action = request.Status.ToString(),
            Operator = request.Operator ?? "系统",
            Remarks = request.Remarks,
            ActionTime = DateTime.UtcNow
        };
        _context.UsageLogs.Add(log);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "状态已更新", data = key });
    }
    
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetKeyHistory(int id)
    {
        var history = await _context.UsageLogs
            .Where(l => l.KeyId == id)
            .OrderByDescending(l => l.ActionTime)
            .ToListAsync();
        
        return Ok(new { success = true, data = history });
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            totalKeys = await _context.Keys.CountAsync(),
            availableKeys = await _context.Keys.CountAsync(k => k.Status == KeyStatus.Available && k.AvailableCopies > 0),
            borrowedKeys = await _context.Keys.CountAsync(k => k.Status == KeyStatus.Borrowed),
            lostKeys = await _context.Keys.CountAsync(k => k.Status == KeyStatus.Lost),
            damagedKeys = await _context.Keys.CountAsync(k => k.Status == KeyStatus.Damaged),
            activeBorrows = await _context.Borrows.CountAsync(b => b.Status == BorrowStatus.Borrowed),
            pendingBorrows = await _context.Borrows.CountAsync(b => b.Status == BorrowStatus.Pending || b.Status == BorrowStatus.Approved)
        };
        
        return Ok(new { success = true, data = stats });
    }
}

public class UpdateStatusRequest
{
    public KeyStatus Status { get; set; }
    public string? Operator { get; set; }
    public string? Remarks { get; set; }
}

// 借用控制器
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BorrowsController : ControllerBase
{
    private readonly KeyDbContext _context;
    
    public BorrowsController(KeyDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetBorrows(
        [FromQuery] BorrowStatus? status = null,
        [FromQuery] int? keyId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Borrows.Include(b => b.Key).AsQueryable();
        
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);
        if (keyId.HasValue)
            query = query.Where(b => b.KeyId == keyId.Value);
        
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.BorrowDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = items });
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBorrow(int id)
    {
        var borrow = await _context.Borrows.Include(b => b.Key).FirstOrDefaultAsync(b => b.Id == id);
        if (borrow == null)
            return NotFound(new { success = false, message = "借用记录不存在" });
        return Ok(new { success = true, data = borrow });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateBorrow([FromBody] CreateBorrowRequest request)
    {
        var year = DateTime.Now.Year;
        var count = await _context.Borrows.CountAsync() + 1;
        var borrowNumber = $"BRW-{year}-{count:D4}";
        
        var borrow = new KeyBorrow
        {
            BorrowNumber = borrowNumber,
            KeyId = request.KeyId,
            BorrowerName = request.BorrowerName,
            BorrowerPhone = request.BorrowerPhone,
            BorrowerUnit = request.BorrowerUnit,
            BorrowDate = request.BorrowDate,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Status = BorrowStatus.Pending,
            Purpose = request.Purpose,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Borrows.Add(borrow);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "借用申请已提交", data = borrow });
    }
    
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveBorrow(int id, [FromBody] ApproveRequest request)
    {
        var borrow = await _context.Borrows.Include(b => b.Key).FirstOrDefaultAsync(b => b.Id == id);
        if (borrow == null)
            return NotFound(new { success = false, message = "借用记录不存在" });
        
        borrow.Status = BorrowStatus.Approved;
        borrow.Approver = request.Approver;
        borrow.ApprovedDate = DateTime.UtcNow;
        borrow.ApprovedRemarks = request.Remarks;
        borrow.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "已批准", data = borrow });
    }
    
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectBorrow(int id, [FromBody] RejectRequest request)
    {
        var borrow = await _context.Borrows.FindAsync(id);
        if (borrow == null)
            return NotFound(new { success = false, message = "借用记录不存在" });
        
        borrow.Status = BorrowStatus.Rejected;
        borrow.ApprovedRemarks = request.Reason;
        borrow.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "已拒绝", data = borrow });
    }
    
    [HttpPut("{id}/pickup")]
    public async Task<IActionResult> PickupKey(int id)
    {
        var borrow = await _context.Borrows.Include(b => b.Key).FirstOrDefaultAsync(b => b.Id == id);
        if (borrow == null)
            return NotFound(new { success = false, message = "借用记录不存在" });
        
        if (borrow.Status != BorrowStatus.Approved)
            return BadRequest(new { success = false, message = "只有已批准的申请才能领取钥匙" });
        
        borrow.Status = BorrowStatus.Borrowed;
        borrow.UpdatedAt = DateTime.UtcNow;
        
        // 更新钥匙状态
        if (borrow.Key != null)
        {
            borrow.Key.AvailableCopies--;
            if (borrow.Key.AvailableCopies <= 0)
                borrow.Key.Status = KeyStatus.Borrowed;
            borrow.Key.UpdatedAt = DateTime.UtcNow;
            
            // 记录使用日志
            var log = new KeyUsageLog
            {
                KeyId = borrow.KeyId,
                BorrowId = borrow.Id,
                Action = "Borrow",
                Operator = borrow.BorrowerName,
                Remarks = $"借出给{borrow.BorrowerName}",
                ActionTime = DateTime.UtcNow
            };
            _context.UsageLogs.Add(log);
        }
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "钥匙已领取", data = borrow });
    }
    
    [HttpPut("{id}/return")]
    public async Task<IActionResult> ReturnKey(int id, [FromBody] ReturnRequest request)
    {
        var borrow = await _context.Borrows.Include(b => b.Key).FirstOrDefaultAsync(b => b.Id == id);
        if (borrow == null)
            return NotFound(new { success = false, message = "借用记录不存在" });
        
        if (borrow.Status != BorrowStatus.Borrowed)
            return BadRequest(new { success = false, message = "钥匙未借出" });
        
        borrow.Status = BorrowStatus.Returned;
        borrow.ActualReturnDate = DateTime.UtcNow;
        borrow.ReturnReceiver = request.Receiver;
        borrow.ReturnRemarks = request.Remarks;
        borrow.KeyReturned = true;
        borrow.KeyConditionOk = request.ConditionOk;
        borrow.KeyConditionRemarks = request.ConditionRemarks;
        borrow.UpdatedAt = DateTime.UtcNow;
        
        // 更新钥匙状态
        if (borrow.Key != null)
        {
            borrow.Key.AvailableCopies++;
            if (borrow.Key.AvailableCopies > 0 && borrow.Key.Status == KeyStatus.Borrowed)
                borrow.Key.Status = KeyStatus.Available;
            borrow.Key.UpdatedAt = DateTime.UtcNow;
            
            // 记录使用日志
            var log = new KeyUsageLog
            {
                KeyId = borrow.KeyId,
                BorrowId = borrow.Id,
                Action = "Return",
                Operator = request.Receiver ?? "系统",
                Remarks = $"钥匙已归还，状况{(request.ConditionOk ? "良好" : "异常")}",
                ActionTime = DateTime.UtcNow
            };
            _context.UsageLogs.Add(log);
        }
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "钥匙已归还", data = borrow });
    }
    
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBorrows()
    {
        var items = await _context.Borrows
            .Include(b => b.Key)
            .Where(b => b.Status == BorrowStatus.Borrowed || b.Status == BorrowStatus.Approved)
            .OrderByDescending(b => b.BorrowDate)
            .ToListAsync();
        
        return Ok(new { success = true, data = items });
    }
}

public class CreateBorrowRequest
{
    public int KeyId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string? BorrowerPhone { get; set; }
    public string? BorrowerUnit { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? Purpose { get; set; }
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

public class ReturnRequest
{
    public string? Receiver { get; set; }
    public string? Remarks { get; set; }
    public bool ConditionOk { get; set; } = true;
    public string? ConditionRemarks { get; set; }
}
