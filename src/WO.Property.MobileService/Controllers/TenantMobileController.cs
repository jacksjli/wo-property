using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WO.Property.MobileService.Data;
using WO.Property.MobileService.Models;
using WO.Property.MobileService.Tenant;

namespace WO.Property.MobileService.Controllers;

/// <summary>
/// 租户移动端控制器 - 使用租户隔离数据库
/// </summary>
[ApiController]
[Route("api/tenant/mobiles")]
public class TenantMobileController : ControllerBase
{
    private readonly TenantDbContextFactory _dbContextFactory;

    public TenantMobileController(TenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private TenantDbContext CreateDbContext() => _dbContextFactory.CreateDbContext();

    #region QuickEntries

    /// <summary>
    /// 获取快捷入口列表
    /// </summary>
    [HttpGet("quick-entries")]
    public async Task<IActionResult> GetQuickEntries([FromQuery] string? category = null)
    {
        await using var context = CreateDbContext();
        var query = context.QuickEntries.Where(e => e.IsActive);
        
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);
        
        var entries = await query.OrderBy(e => e.SortOrder).ToListAsync();
        return Ok(new { success = true, data = entries });
    }

    /// <summary>
    /// 获取单个快捷入口
    /// </summary>
    [HttpGet("quick-entries/{id}")]
    public async Task<IActionResult> GetQuickEntry(int id)
    {
        await using var context = CreateDbContext();
        var entry = await context.QuickEntries.FindAsync(id);
        
        if (entry == null)
            return NotFound(new { success = false, message = "快捷入口不存在" });
        
        return Ok(new { success = true, data = entry });
    }

    /// <summary>
    /// 创建快捷入口
    /// </summary>
    [HttpPost("quick-entries")]
    public async Task<IActionResult> CreateQuickEntry([FromBody] QuickEntryDto dto)
    {
        await using var context = CreateDbContext();
        
        var entry = new QuickEntry
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            TargetUrl = dto.TargetUrl,
            SortOrder = dto.SortOrder,
            Category = dto.Category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        context.QuickEntries.Add(entry);
        await context.SaveChangesAsync();
        
        return Ok(new { success = true, data = entry });
    }

    /// <summary>
    /// 更新快捷入口
    /// </summary>
    [HttpPut("quick-entries/{id}")]
    public async Task<IActionResult> UpdateQuickEntry(int id, [FromBody] QuickEntryDto dto)
    {
        await using var context = CreateDbContext();
        var entry = await context.QuickEntries.FindAsync(id);
        
        if (entry == null)
            return NotFound(new { success = false, message = "快捷入口不存在" });
        
        entry.Name = dto.Name;
        entry.Description = dto.Description;
        entry.Icon = dto.Icon;
        entry.TargetUrl = dto.TargetUrl;
        entry.SortOrder = dto.SortOrder;
        entry.Category = dto.Category;
        entry.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        return Ok(new { success = true, data = entry });
    }

    /// <summary>
    /// 删除快捷入口
    /// </summary>
    [HttpDelete("quick-entries/{id}")]
    public async Task<IActionResult> DeleteQuickEntry(int id)
    {
        await using var context = CreateDbContext();
        var entry = await context.QuickEntries.FindAsync(id);
        
        if (entry == null)
            return NotFound(new { success = false, message = "快捷入口不存在" });
        
        context.QuickEntries.Remove(entry);
        await context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "删除成功" });
    }

    #endregion

    #region Devices

    /// <summary>
    /// 获取用户设备列表
    /// </summary>
    [HttpGet("devices/user/{userId}")]
    public async Task<IActionResult> GetUserDevices(string userId)
    {
        await using var context = CreateDbContext();
        var devices = await context.Devices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastActiveAt)
            .ToListAsync();
        
        return Ok(new { success = true, data = devices });
    }

    /// <summary>
    /// 注册设备
    /// </summary>
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
    {
        await using var context = CreateDbContext();
        
        var existing = await context.Devices.FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);
        
        if (existing != null)
        {
            existing.DeviceType = dto.DeviceType;
            existing.DeviceName = dto.DeviceName;
            existing.DeviceModel = dto.DeviceModel;
            existing.OsVersion = dto.OsVersion;
            existing.AppVersion = dto.AppVersion;
            existing.UserId = dto.UserId;
            existing.PushToken = dto.PushToken;
            existing.NotificationsEnabled = dto.NotificationsEnabled;
            existing.LastActiveAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var device = new DeviceRegistration
            {
                DeviceId = dto.DeviceId,
                DeviceType = dto.DeviceType,
                DeviceName = dto.DeviceName,
                DeviceModel = dto.DeviceModel,
                OsVersion = dto.OsVersion,
                AppVersion = dto.AppVersion,
                UserId = dto.UserId,
                PushToken = dto.PushToken,
                NotificationsEnabled = dto.NotificationsEnabled,
                LastActiveAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Devices.Add(device);
        }
        
        await context.SaveChangesAsync();
        return Ok(new { success = true, message = "设备注册成功" });
    }

    #endregion

    #region Notifications

    /// <summary>
    /// 获取通知列表
    /// </summary>
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? userId = null,
        [FromQuery] bool? unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        await using var context = CreateDbContext();
        var query = context.Notifications.AsQueryable();
        
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

    /// <summary>
    /// 标记通知已读
    /// </summary>
    [HttpPut("notifications/{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await using var context = CreateDbContext();
        var notification = await context.Notifications.FindAsync(id);
        
        if (notification == null)
            return NotFound(new { success = false, message = "通知不存在" });
        
        notification.ReadAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "已标记为已读" });
    }

    #endregion

    #region Sessions

    /// <summary>
    /// 生成二维码会话
    /// </summary>
    [HttpPost("qr/generate")]
    public async Task<IActionResult> GenerateQRCode([FromQuery] string userId)
    {
        await using var context = CreateDbContext();
        
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
        
        context.Sessions.Add(session);
        await context.SaveChangesAsync();
        
        var qrContent = $"wo-property://login?session={sessionId}";
        
        return Ok(new { success = true, data = new { sessionId, qrContent, expiresAt } });
    }

    /// <summary>
    /// 查询二维码状态
    /// </summary>
    [HttpGet("qr/status/{sessionId}")]
    public async Task<IActionResult> GetQRStatus(string sessionId)
    {
        await using var context = CreateDbContext();
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        
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

    #endregion
}

// DTOs
public class QuickEntryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? TargetUrl { get; set; }
    public int SortOrder { get; set; }
    public string? Category { get; set; }
}

public class RegisterDeviceDto
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