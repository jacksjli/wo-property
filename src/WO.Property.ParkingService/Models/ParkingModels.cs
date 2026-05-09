using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WO.Property.Shared.Models;

namespace WO.Property.ParkingService.Models;

public class ParkingLot : BaseEntity
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(200)] public string? Location { get; set; }
    public int TotalSpaces { get; set; }
    public decimal HourlyRate { get; set; } = 5m;
    public decimal MonthlyRate { get; set; } = 300m;
    public int ProjectId { get; set; } = 1;
    public ICollection<ParkingSpace> Spaces { get; set; } = new List<ParkingSpace>();
    [JsonIgnore] public ICollection<ParkingRecord> Records { get; set; } = new List<ParkingRecord>();
}

public class ParkingSpace : BaseEntity
{
    [Required, MaxLength(20)] public string SpaceNo { get; set; } = string.Empty;
    [MaxLength(20)] public string Type { get; set; } = "temporary";
    [MaxLength(20)] public string Status { get; set; } = "available";
    public int LotId { get; set; }
    public int? VehicleId { get; set; }
    [ForeignKey(nameof(LotId))] public ParkingLot? Lot { get; set; }
    [ForeignKey(nameof(VehicleId))] public Vehicle? Vehicle { get; set; }
}

public class Vehicle : BaseEntity
{
    [Required, MaxLength(20)] public string PlateNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string? Brand { get; set; }
    [MaxLength(20)] public string? Color { get; set; }
    [MaxLength(100)] public string? OwnerName { get; set; }
    [MaxLength(30)] public string? OwnerPhone { get; set; }
    [MaxLength(20)] public string Type { get; set; } = "personal";
    [MaxLength(500)] public string? PlateImage { get; set; }
    public int ProjectId { get; set; } = 1;
}

public class ParkingRecord : BaseEntity
{
    [Required, MaxLength(20)] public string PlateNumber { get; set; } = string.Empty;
    public int LotId { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    [MaxLength(20)] public string? VehicleType { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal Fee { get; set; }
    [MaxLength(20)] public string? PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    [MaxLength(20)] public string? PaymentMethod { get; set; }
    [MaxLength(200)] public string? Remark { get; set; }
    public int ProjectId { get; set; } = 1;
    [ForeignKey(nameof(LotId))] [JsonIgnore] public ParkingLot? Lot { get; set; }
}

public class ParkingPayment : BaseEntity
{
    [Required, MaxLength(50)] public string PaymentNo { get; set; } = string.Empty;
    [MaxLength(20)] public string? PlateNumber { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(20)] public string Type { get; set; } = "parking";
    [MaxLength(20)] public string Method { get; set; } = "wechat";
    public int ProjectId { get; set; } = 1;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}