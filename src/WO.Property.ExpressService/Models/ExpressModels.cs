using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WO.Property.ExpressService.Models;

[Table("ExpressRecords")]
public class ExpressRecord
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("RoomId")]
    public int RoomId { get; set; }

    [Column("RecipientName")]
    [MaxLength(50)]
    public string RecipientName { get; set; } = string.Empty;

    [Column("RecipientPhone")]
    [MaxLength(20)]
    public string? RecipientPhone { get; set; }

    [Column("CourierCompany")]
    [MaxLength(50)]
    public string? CourierCompany { get; set; }

    [Column("TrackingNumber")]
    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [Column("PickupCode")]
    [MaxLength(20)]
    public string? PickupCode { get; set; }

    [Column("Status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("PickupTime")]
    public DateTime? PickupTime { get; set; }

    [Column("Remarks")]
    public string? Remarks { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

// 快递公司
[Table("express_companies")]
public class ExpressCompany
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(50)]
    public string? Code { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 请求模型
public class CreateExpressRecordRequest
{
    public int RoomId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string? CourierCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public string? PickupCode { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateExpressRecordRequest
{
    public string? Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime? PickupTime { get; set; }
}
