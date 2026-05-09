using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.MobileService.Models;

public enum DeviceType { Android, iOS, WeChatMiniProgram, Web }
public enum NotificationType { System, Ticket, Complaint, Payment, Visitor, Announcement }

public class DeviceRegistration : BaseEntity
{
    [Required][MaxLength(100)] public string DeviceId { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    [MaxLength(100)] public string? DeviceName { get; set; }
    [MaxLength(100)] public string? DeviceModel { get; set; }
    [MaxLength(50)] public string? OsVersion { get; set; }
    [MaxLength(100)] public string? AppVersion { get; set; }
    [MaxLength(100)] public string? UserId { get; set; }
    [MaxLength(500)] public string? PushToken { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public DateTime? LastActiveAt { get; set; }
}

public class PushNotification : BaseEntity
{
    [Required][MaxLength(50)] public string NotificationId { get; set; } = string.Empty;
    [MaxLength(100)] public string? UserId { get; set; }
    [MaxLength(100)] public string? TargetDeviceId { get; set; }
    public NotificationType Type { get; set; }
    [Required][MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required][MaxLength(500)] public string Content { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Data { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class UserSession : BaseEntity
{
    [Required][MaxLength(100)] public string SessionId { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string UserId { get; set; } = string.Empty;
    [MaxLength(100)] public string? DeviceId { get; set; }
    [MaxLength(50)] public string SessionType { get; set; } = "QRLogin";
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WeChatUser : BaseEntity
{
    [Required][MaxLength(100)] public string OpenId { get; set; } = string.Empty;
    [MaxLength(100)] public string? UnionId { get; set; }
    [MaxLength(100)] public string? UserId { get; set; }
    [MaxLength(100)] public string? NickName { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    [MaxLength(200)] public string? AvatarUrl { get; set; }
    [MaxLength(100)] public string? PhoneNumber { get; set; }
    [MaxLength(200)] public string? City { get; set; }
}

public class QuickEntry : BaseEntity
{
    [Required][MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(200)] public string? Description { get; set; }
    [MaxLength(100)] public string? Icon { get; set; }
    [MaxLength(100)] public string? TargetUrl { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    [MaxLength(50)] public string? Category { get; set; }
}