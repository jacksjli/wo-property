using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.ContractService.Models;

public enum ContractType
{
    Property,
    Lease,
    Maintenance,
    Service
}

public enum ContractStatus
{
    Draft,
    Active,
    ExpiringSoon,
    Expired,
    Terminated
}

public class Contract : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ContractNumber { get; set; } = string.Empty;
    
    [Required]
    public ContractType Type { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PartyA { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? PartyAContact { get; set; }
    
    [MaxLength(50)]
    public string? PartyAPhone { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PartyB { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? PartyBContact { get; set; }
    
    [MaxLength(50)]
    public string? PartyBPhone { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(20)]
    public string Currency { get; set; } = "CNY";
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }
    
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    
    public string? ExtendedData { get; set; }
}