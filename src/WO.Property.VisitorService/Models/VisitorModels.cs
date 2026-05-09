using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.VisitorService.Models;

public enum VisitType { Personal, Business, Delivery, Maintenance, Interview, Other }
public enum VisitStatus { Pending, Approved, CheckIn, CheckOut, Cancelled, Rejected, Expired }
public enum IDType { IDCard, Passport, DriverLicense, Other }

public class Visitor : BaseEntity
{
    [Required][MaxLength(50)] public string VisitorNumber { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string VisitorName { get; set; } = string.Empty;
    [MaxLength(20)] public string? VisitorPhone { get; set; }
    [MaxLength(100)] public string? VisitorEmail { get; set; }
    public IDType? IDType { get; set; }
    [MaxLength(50)] public string? IDNumber { get; set; }
    public VisitType Type { get; set; } = VisitType.Personal;
    [Required][MaxLength(100)] public string HostName { get; set; } = string.Empty;
    [MaxLength(50)] public string? HostPhone { get; set; }
    [MaxLength(100)] public string? HostUnit { get; set; }
    [MaxLength(100)] public string? VisitLocation { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    [MaxLength(500)] public string? Purpose { get; set; }
    public int? ExpectedVisitors { get; set; }
    public DateTime? ActualCheckInTime { get; set; }
    public DateTime? ActualCheckOutTime { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Pending;
    [MaxLength(20)] public string? LicensePlate { get; set; }
    [MaxLength(50)] public string? ParkingSpace { get; set; }
    [MaxLength(100)] public string? Approver { get; set; }
    public DateTime? ApprovedDate { get; set; }
    [MaxLength(200)] public string? ApprovalRemarks { get; set; }
    [MaxLength(100)] public string? AccessCode { get; set; }
    public bool AccessGranted { get; set; } = false;
    [MaxLength(500)] public string? Remarks { get; set; }
}

public class VisitRecord : BaseEntity
{
    [Required] public int VisitorId { get; set; }
    [ForeignKey(nameof(VisitorId))] public Visitor? Visitor { get; set; }
    [Required][MaxLength(50)] public string RecordNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string? GateDevice { get; set; }
    [MaxLength(50)] public string? AccessDirection { get; set; }
    public DateTime AccessTime { get; set; } = DateTime.UtcNow;
    [MaxLength(200)] public string? PhotoUrl { get; set; }
    public decimal? Temperature { get; set; }
    [MaxLength(20)] public string? HealthCodeStatus { get; set; }
    [MaxLength(100)] public string? RegisteredBy { get; set; }
    [MaxLength(200)] public string? Remarks { get; set; }
}

public class VisitStatistics : BaseEntity
{
    public DateTime StatDate { get; set; }
    public int TotalVisits { get; set; }
    public int TotalVisitors { get; set; }
    public int CheckedIn { get; set; }
    public int CheckedOut { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
    public int Expired { get; set; }
    public int PersonalVisits { get; set; }
    public int BusinessVisits { get; set; }
    public int DeliveryVisits { get; set; }
    public int MaintenanceVisits { get; set; }
}