using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.MobileService.Data;
using WO.Property.MobileService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5015端口
builder.WebHost.UseUrls("http://0.0.0.0:5015");

// 添加数据库
builder.Services.AddDbContext<MobileDbContext>(options =>
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
    var context = scope.ServiceProvider.GetRequiredService<MobileDbContext>();
    context.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MobileService", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Mobile Service");
Console.WriteLine("  Port: 5015");
Console.WriteLine("===========================================");

app.Run();

// ============ 移动端控制器 ============

[ApiController]
[Route("api/mobile")]
public class MobileController : ControllerBase
{
    private readonly MobileDbContext _context;
    
    public MobileController(MobileDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 获取首页数据（聚合各服务数据）
    /// </summary>
    [HttpGet("home")]
    public async Task<IActionResult> GetHomeData()
    {
        var quickEntries = await _context.QuickEntries
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();
        
        var recentNotifications = await _context.Notifications
            .Where(n => n.IsSent)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();
        
        // 聚合各服务数据
        var homeData = new
        {
            quickEntries,
            recentNotifications,
            banners = new[] {
                new { id = 1, image = "/assets/banner1.png", title = "欢迎使用WO物业", link = "/" },
                new { id = 2, image = "/assets/banner2.png", title = "物业费缴纳", link = "/pages/payment/index" }
            },
            stats = new
            {
                unpaidBills = 0,
                activeTickets = 0,
                pendingComplaints = 0,
                newAnnouncements = 1
            }
        };
        
        return Ok(new { success = true, data = homeData });
    }
    
    /// <summary>
    /// 获取所有服务状态
    /// </summary>
    [HttpGet("services/status")]
    public async Task<IActionResult> GetServicesStatus()
    {
        var services = new[]
        {
            new { name = "认证服务", port = 5006, status = "running" },
            new { name = "物料服务", port = 5004, status = "running" },
            new { name = "通知服务", port = 5005, status = "running" },
            new { name = "合同服务", port = 5008, status = "running" },
            new { name = "财务服务", port = 5009, status = "running" },
            new { name = "巡检服务", port = 5010, status = "running" },
            new { name = "投诉服务", port = 5011, status = "running" },
            new { name = "钥匙服务", port = 5012, status = "running" },
            new { name = "访客服务", port = 5013, status = "running" },
            new { name = "统计服务", port = 5014, status = "running" }
        };
        
        return Ok(new { success = true, data = services });
    }
    
    /// <summary>
    /// 获取快捷入口
    /// </summary>
    [HttpGet("quick-entries")]
    public async Task<IActionResult> GetQuickEntries([FromQuery] string? category = null)
    {
        var query = _context.QuickEntries.Where(e => e.IsActive);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);
        
        var entries = await query.OrderBy(e => e.SortOrder).ToListAsync();
        return Ok(new { success = true, data = entries });
    }
}

// ============ 设备管理控制器 ============

[ApiController]
[Route("api/mobile/devices")]
public class DevicesController : ControllerBase
{
    private readonly MobileDbContext _context;
    
    public DevicesController(MobileDbContext context)
    {
        _context = context;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var existing = await _context.Devices.FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);
        
        if (existing != null)
        {
            existing.DeviceType = request.DeviceType;
            existing.DeviceName = request.DeviceName;
            existing.DeviceModel = request.DeviceModel;
            existing.OsVersion = request.OsVersion;
            existing.AppVersion = request.AppVersion;
            existing.UserId = request.UserId;
            existing.PushToken = request.PushToken;
            existing.NotificationsEnabled = request.NotificationsEnabled;
            existing.LastActiveAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var device = new DeviceRegistration
            {
                DeviceId = request.DeviceId,
                DeviceType = request.DeviceType,
                DeviceName = request.DeviceName,
                DeviceModel = request.DeviceModel,
                OsVersion = request.OsVersion,
                AppVersion = request.AppVersion,
                UserId = request.UserId,
                PushToken = request.PushToken,
                NotificationsEnabled = request.NotificationsEnabled,
                LastActiveAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Devices.Add(device);
        }
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "设备注册成功" });
    }
    
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserDevices(string userId)
    {
        var devices = await _context.Devices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastActiveAt)
            .ToListAsync();
        return Ok(new { success = true, data = devices });
    }
    
    [HttpPut("{deviceId}/token")]
    public async Task<IActionResult> UpdatePushToken(string deviceId, [FromBody] UpdateTokenRequest request)
    {
        var device = await _context.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        if (device == null)
            return NotFound(new { success = false, message = "设备不存在" });
        
        device.PushToken = request.PushToken;
        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "推送令牌已更新" });
    }
}

// ============ 推送通知控制器 ============

[ApiController]
[Route("api/mobile/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly MobileDbContext _context;
    
    public NotificationsController(MobileDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? userId = null,
        [FromQuery] bool? unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Notifications.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(n => n.UserId == userId || n.UserId == null);
        if (unreadOnly == true)
            query = query.Where(n => n.ReadAt == null);
        
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { success = true, total, page, pageSize, data = items });
    }
    
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] string? userId = null)
    {
        var query = _context.Notifications.Where(n => n.ReadAt == null);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(n => n.UserId == userId || n.UserId == null);
        
        var count = await query.CountAsync();
        return Ok(new { success = true, data = new { count } });
    }
    
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
            return NotFound(new { success = false, message = "通知不存在" });
        
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "已标记为已读" });
    }
    
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        var notificationId = $"NOT-{DateTime.Now.Year}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        
        var notification = new PushNotification
        {
            NotificationId = notificationId,
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Content = request.Content,
            Data = request.Data,
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
        // TODO: 调用推送服务发送通知
        
        return Ok(new { success = true, message = "通知已创建", data = notification });
    }
}

// ============ 二维码登录控制器 ============

[ApiController]
[Route("api/mobile/qr")]
public class QRCodeController : ControllerBase
{
    private readonly MobileDbContext _context;
    
    public QRCodeController(MobileDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 生成二维码会话
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQRCode([FromQuery] string userId)
    {
        var sessionId = $"QR-{Guid.NewGuid().ToString()[..16].ToUpper()}";
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        
        var session = new UserSession
        {
            SessionId = sessionId,
            UserId = userId,
            SessionType = "QRLogin",
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        
        // 生成二维码内容 (URL编码的会话ID)
        var qrContent = $"wo-property://login?session={sessionId}";
        
        return Ok(new { success = true, data = new { sessionId, qrContent, expiresAt } });
    }
    
    /// <summary>
    /// 扫码确认登录
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> ScanQRCode([FromBody] ScanQRCodeRequest request)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
        
        if (session == null)
            return BadRequest(new { success = false, message = "二维码已失效" });
        
        // 更新会话状态
        session.IsActive = false;
        await _context.SaveChangesAsync();
        
        // 返回JWT Token (模拟，实际应该调用认证服务)
        return Ok(new { success = true, message = "登录成功", data = new { userId = session.UserId } });
    }
    
    /// <summary>
    /// 查询二维码状态
    /// </summary>
    [HttpGet("status/{sessionId}")]
    public async Task<IActionResult> GetQRStatus(string sessionId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        
        if (session == null)
            return NotFound(new { success = false, message = "会话不存在" });
        
        return Ok(new { success = true, data = new
        {
            session.SessionId,
            session.UserId,
            session.IsActive,
            session.ExpiresAt,
            isExpired = session.ExpiresAt < DateTime.UtcNow
        }});
    }
}

// ============ 微信小程序控制器 ============

[ApiController]
[Route("api/mobile/wechat")]
public class WeChatController : ControllerBase
{
    private readonly MobileDbContext _context;
    
    public WeChatController(MobileDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// 微信登录 (Code换Session)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> WeChatLogin([FromBody] WeChatLoginRequest request)
    {
        // TODO: 实际应该调用微信接口验证 code
        // 这里模拟返回成功
        
        var openId = $"wx_{request.Code ?? Guid.NewGuid().ToString()[..8]}";
        
        var weChatUser = await _context.WeChatUsers.FirstOrDefaultAsync(w => w.OpenId == openId);
        
        if (weChatUser == null)
        {
            weChatUser = new WeChatUser
            {
                OpenId = openId,
                UserId = null,  // 首次登录未绑定
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.WeChatUsers.Add(weChatUser);
            await _context.SaveChangesAsync();
        }
        
        return Ok(new { success = true, data = new
        {
            openId,
            isBound = weChatUser.UserId != null,
            sessionKey = Guid.NewGuid().ToString()[..16]
        }});
    }
    
    /// <summary>
    /// 绑定用户
    /// </summary>
    [HttpPost("bind")]
    public async Task<IActionResult> BindUser([FromBody] BindUserRequest request)
    {
        var weChatUser = await _context.WeChatUsers.FirstOrDefaultAsync(w => w.OpenId == request.OpenId);
        if (weChatUser == null)
            return NotFound(new { success = false, message = "微信用户不存在" });
        
        weChatUser.UserId = request.UserId;
        weChatUser.PhoneNumber = request.PhoneNumber;
        weChatUser.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "绑定成功" });
    }
    
    /// <summary>
    /// 获取微信用户信息
    /// </summary>
    [HttpGet("user/{openId}")]
    public async Task<IActionResult> GetWeChatUser(string openId)
    {
        var user = await _context.WeChatUsers.FirstOrDefaultAsync(w => w.OpenId == openId);
        if (user == null)
            return NotFound(new { success = false, message = "用户不存在" });
        
        return Ok(new { success = true, data = user });
    }
}

// ============ 请求/响应模型 ============

public class RegisterDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? UserId { get; set; }
    public string? PushToken { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
}

public class UpdateTokenRequest
{
    public string PushToken { get; set; } = string.Empty;
}

public class SendNotificationRequest
{
    public string? UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Data { get; set; }
}

public class ScanQRCodeRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public class WeChatLoginRequest
{
    public string? Code { get; set; }
    public string? IV { get; set; }
    public string? EncryptedData { get; set; }
}

public class BindUserRequest
{
    public string OpenId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
