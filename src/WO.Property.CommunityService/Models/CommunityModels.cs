using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.CommunityService.Models;

public class Activity : BaseEntity
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int MaxParticipants { get; set; } = 50;

    public int CurrentParticipants { get; set; } = 0;

    [MaxLength(20)]
    public string Status { get; set; } = "draft";

    [MaxLength(200)]
    public string? CoverImage { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public int ProjectId { get; set; } = 1;
}

public class ActivityEnrollment : BaseEntity
{
    public int ActivityId { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Note { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "enrolled";

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}