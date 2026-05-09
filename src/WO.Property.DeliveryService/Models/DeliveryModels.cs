using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.DeliveryService.Models;

public enum DeliveryStatus
{
    Pending, Preparing, Ready, InDelivery, Delivered, Cancelled, Problem
}

public enum PaymentMethod { Online, Cash, Card }
public enum PaymentStatus { Unpaid, Paid, Refunded }
public enum NotificationStatus { Pending, Sent, Read, Failed }

public class Rider : BaseEntity
{
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string? Phone { get; set; }
    [MaxLength(100)] public string? Platform { get; set; }
    [MaxLength(50)] public string? PlateNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FoodDelivery : BaseEntity
{
    [Required][MaxLength(50)] public string OrderNumber { get; set; } = string.Empty;
    [MaxLength(200)] public string? MerchantName { get; set; }
    [Required][MaxLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required][MaxLength(50)] public string CustomerPhone { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string RoomNumber { get; set; } = string.Empty;
    [MaxLength(500)] public string? DeliveryAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public int? RiderId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
    public Rider? Rider { get; set; }
}

public class DeliveryNotification : BaseEntity
{
    public int DeliveryId { get; set; }
    [MaxLength(500)] public string? Content { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public FoodDelivery? Delivery { get; set; }
}

public class CreateDeliveryRequest
{
    public string OrderNumber { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? Remarks { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public DeliveryStatus Status { get; set; }
    public int? RiderId { get; set; }
    public string? Remarks { get; set; }
}