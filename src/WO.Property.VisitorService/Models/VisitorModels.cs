using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WO.Property.VisitorService.Models;

[Table("Visitors")]
public class Visitor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("visitor_name")]
    [MaxLength(50)]
    public string VisitorName { get; set; } = string.Empty;

    [Column("visitor_phone")]
    [MaxLength(20)]
    public string? VisitorPhone { get; set; }

    [Column("id_card_number")]
    [MaxLength(30)]
    public string? IdCardNumber { get; set; }

    [Column("visit_purpose")]
    [MaxLength(50)]
    public string? VisitPurpose { get; set; }

    [Column("visit_date")]
    public DateTime? VisitDate { get; set; }

    [Column("visit_time")]
    public TimeSpan? VisitTime { get; set; }

    [Column("leave_time")]
    public TimeSpan? LeaveTime { get; set; }

    [Column("building_id")]
    public int? BuildingId { get; set; }

    [Column("room_id")]
    public int? RoomId { get; set; }

    [Column("host_name")]
    [MaxLength(50)]
    public string? HostName { get; set; }

    [Column("host_phone")]
    [MaxLength(20)]
    public string? HostPhone { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "registered";

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

// 请求模型
public class CreateVisitorRequest
{
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? IdCardNumber { get; set; }
    public string? VisitPurpose { get; set; }
    public DateTime? VisitDate { get; set; }
    public TimeSpan? VisitTime { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? HostName { get; set; }
    public string? HostPhone { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateVisitorRequest
{
    public string? Status { get; set; }
    public DateTime? LeaveTime { get; set; }
    public string? Remarks { get; set; }
}

public class ExternalPerson
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("phone")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
