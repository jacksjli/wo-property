using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.AnnouncementService.Models;

public class Announcement : BaseEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Category { get; set; } = "property";

    [MaxLength(20)]
    public string Level { get; set; } = "info";

    public bool IsPinned { get; set; } = false;

    [MaxLength(20)]
    public string Status { get; set; } = "draft";

    [MaxLength(500)]
    public string? AttachmentUrls { get; set; }

    [MaxLength(500)]
    public string? TargetBuildings { get; set; }

    [MaxLength(100)]
    public string Publisher { get; set; } = "物业中心";

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? PublishTime { get; set; }

    public int ViewCount { get; set; } = 0;

    public int ProjectId { get; set; } = 1;
    
    [MaxLength(20)]
    public string? ProjectCode { get; set; }
}

public class AnnouncementRead : BaseEntity
{
    public int AnnouncementId { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(100)]
    public string? UserName { get; set; }

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? Remark { get; set; }
}