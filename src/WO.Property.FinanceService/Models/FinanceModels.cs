using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.FinanceService.Models;

public enum FeeType { PropertyFee, ParkingFee, WaterFee, ElectricityFee, GasFee, HeatingFee, OtherFee }
public enum BillStatus { Pending, Paid, Overdue, Cancelled }
public enum PaymentMethod { Cash, BankTransfer, WeChatPay, Alipay, Card }

public class FeeItem : BaseEntity
{
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    public FeeType Type { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [MaxLength(20)] public string Unit { get; set; } = "元/月";
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Bill : BaseEntity
{
    [Required][MaxLength(50)] public string BillNumber { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string RoomNumber { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string OwnerName { get; set; } = string.Empty;
    [MaxLength(50)] public string? OwnerPhone { get; set; }
    public FeeType FeeType { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")] public decimal ActualAmount { get; set; }
    [MaxLength(20)] public string Currency { get; set; } = "CNY";
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Pending;
    [MaxLength(500)] public string? Remarks { get; set; }
}

public class Payment : BaseEntity
{
    [Required][MaxLength(50)] public string PaymentNumber { get; set; } = string.Empty;
    [Required] public int BillId { get; set; }
    [ForeignKey(nameof(BillId))] public Bill? Bill { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [MaxLength(20)] public string Currency { get; set; } = "CNY";
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    [MaxLength(200)] public string? TransactionId { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
}

public class Receipt : BaseEntity
{
    [Required][MaxLength(50)] public string ReceiptNumber { get; set; } = string.Empty;
    [Required] public int PaymentId { get; set; }
    [ForeignKey(nameof(PaymentId))] public Payment? Payment { get; set; }
    [Required][MaxLength(100)] public string PayerName { get; set; } = string.Empty;
    [MaxLength(50)] public string? PayerPhone { get; set; }
    [Required][MaxLength(100)] public string RoomNumber { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime IssuedDate { get; set; }
}