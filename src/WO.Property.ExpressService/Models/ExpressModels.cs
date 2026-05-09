using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.ExpressService.Models;

public enum ExpressStatus { Pending, InStorage, InDelivery, Delivered, Returned, Expired }
public enum NotificationType { PickupReminder, ExpiredWarning, ExpiredNotice, DeliveredNotice }
public enum NotificationStatus { Pending, Sent, Read, Failed }

public class ExpressCompany : BaseEntity
{
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string? Code { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExpressDelivery : BaseEntity
{
    [Required][MaxLength(50)] public string TrackingNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string? ExpressNumber { get; set; }
    public int CompanyId { get; set; }
    [MaxLength(200)] public string SenderName { get; set; } = string.Empty;
    [MaxLength(50)] public string? SenderPhone { get; set; }
    [Required][MaxLength(100)] public string ReceiverName { get; set; } = string.Empty;
    [Required][MaxLength(50)] public string ReceiverPhone { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string RoomNumber { get; set; } = string.Empty;
    [MaxLength(200)] public string? PickupAddress { get; set; }
    public ExpressStatus Status { get; set; } = ExpressStatus.Pending;
    [MaxLength(500)] public string? Remarks { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PickedUpAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    [MaxLength(100)] public string? ReceivedBy { get; set; }
    public ExpressCompany? Company { get; set; }
}

public class ExpressNotification : BaseEntity
{
    public int ExpressId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    [MaxLength(500)] public string? Content { get; set; }
    public DateTime? SentAt { get; set; }
    public ExpressDelivery? Express { get; set; }
}

public class CreateExpressRequest
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string? ExpressNumber { get; set; }
    public int CompanyId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string? PickupAddress { get; set; }
    public int StorageDays { get; set; } = 3;
    public string? Remarks { get; set; }
}

public class UpdateExpressStatusRequest
{
    public ExpressStatus Status { get; set; }
    public string? Remarks { get; set; }
    public string? ReceivedBy { get; set; }
}