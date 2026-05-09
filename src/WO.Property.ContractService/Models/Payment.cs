using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.ContractService.Models;

public enum PaymentStatus
{
    Pending,
    Paid,
    Overdue,
    Cancelled
}

public class Payment : BaseEntity
{
    [Required]
    public int ContractId { get; set; }
    
    [ForeignKey(nameof(ContractId))]
    public Contract? Contract { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string PaymentNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(20)]
    public string Currency { get; set; } = "CNY";
    
    public DateTime DueDate { get; set; }
    
    public DateTime? PaidDate { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    [MaxLength(500)]
    public string? Remarks { get; set; }
}