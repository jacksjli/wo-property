using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.PaymentService.Models;

public class Bill : BaseEntity
{
    [Required, MaxLength(50)] public string BillNo { get; set; } = string.Empty;
    [MaxLength(20)] public string? Type { get; set; }
    [MaxLength(100)] public string? Description { get; set; }
    [MaxLength(100)] public string? Unit { get; set; }
    public decimal Amount { get; set; }
    public DateTime BillingMonth { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "unpaid";
    public DateTime? PaidAt { get; set; }
    [MaxLength(50)] public string? PaymentMethod { get; set; }
    [MaxLength(100)] public string? TransactionId { get; set; }
    [MaxLength(200)] public string? Remark { get; set; }
    public int ProjectId { get; set; } = 1;
    
    [MaxLength(20)]
    public string? ProjectCode { get; set; }
}

public class MeterReading : BaseEntity
{
    [MaxLength(100)] public string? Unit { get; set; }
    [MaxLength(20)] public string Type { get; set; } = "water";
    public int PreviousReading { get; set; }
    public int CurrentReading { get; set; }
    public decimal Usage { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public DateTime? ReadingDate { get; set; }
    [MaxLength(100)] public string? ReaderName { get; set; }
    public int ProjectId { get; set; } = 1;

    
    [MaxLength(20)]
    public string? ProjectCode { get; set; }

}