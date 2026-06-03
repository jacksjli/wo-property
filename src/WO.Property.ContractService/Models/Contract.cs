using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.ContractService.Models;

public class Contract : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ContractNumber { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Type { get; set; } = "Property";
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string? PartyA { get; set; }
    
    public string? PartyAContact { get; set; }
    
    public string? PartyAPhone { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string? PartyB { get; set; }
    
    public string? PartyBContact { get; set; }
    
    public string? PartyBPhone { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }
    
    [MaxLength(20)]
    public string Currency { get; set; } = "CNY";
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public string? AttachmentUrl { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
    
    public string? ExtendedData { get; set; }
    
    [MaxLength(20)]
    public string? ProjectCode { get; set; }
}
