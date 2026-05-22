using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WO.Property.ProjectTrackingService.Models;

[Table("project_tracking")]
public class TrackingProject
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("project_no")]
    [MaxLength(20)]
    public string ProjectNo { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [MaxLength(10)]
    public string Type { get; set; } = "投标";

    [Column("client")]
    [MaxLength(100)]
    public string? Client { get; set; }

    [Column("budget")]
    public decimal? Budget { get; set; }

    [Column("bid_amount")]
    public decimal? BidAmount { get; set; }

    [Column("register_deadline")]
    public DateTime? RegisterDeadline { get; set; }

    [Column("bid_deadline")]
    public DateTime? BidDeadline { get; set; }

    [Column("bid_open_date")]
    public DateTime? BidOpenDate { get; set; }

    [Column("location")]
    [MaxLength(200)]
    public string? Location { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("file_status")]
    [MaxLength(10)]
    public string? FileStatus { get; set; }

    [Column("status")]
    [MaxLength(10)]
    public string Status { get; set; } = "意向";

    [Column("success_rate")]
    public int SuccessRate { get; set; } = 50;

    [Column("remark")]
    public string? Remark { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    public ICollection<TrackingRecord> Records { get; set; } = new List<TrackingRecord>();
}

[Table("tracking_records")]
public class TrackingRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [ForeignKey("ProjectId")]
    public TrackingProject? Project { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = "备注";

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("operator")]
    [MaxLength(50)]
    public string? Operator { get; set; }
}
