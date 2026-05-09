using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.RenovationService.Models;

public class RenovationApplication : BaseEntity
{
    [MaxLength(100)] public string? ApplicantName { get; set; }
    [MaxLength(30)] public string? ApplicantPhone { get; set; }
    [MaxLength(100)] public string? Building { get; set; }
    [MaxLength(50)] public string? Unit { get; set; }
    [MaxLength(50)] public string? RoomNo { get; set; }
    public string Content { get; set; } = string.Empty;
    [MaxLength(500)] public string? AttachmentUrls { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "pending";
    public decimal DepositAmount { get; set; } = 5000m;
    [MaxLength(200)] public string? Remark { get; set; }
    public int ProjectId { get; set; } = 1;
}