using System.ComponentModel.DataAnnotations;
using WO.Property.Shared.Models;

namespace WO.Property.CleaningService.Models;

public class CleaningStaff : BaseEntity
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Area { get; set; }

    [MaxLength(50)]
    public string? WorkShift { get; set; }

    public int ProjectId { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}

public class CleaningTask : BaseEntity
{
    public int BuildingId { get; set; }

    [MaxLength(100)]
    public string CleaningArea { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CleanerName { get; set; }

    [MaxLength(20)]
    public string? CleaningType { get; set; }

    public DateTime? PlanDate { get; set; }

    public DateTime? ActualDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [MaxLength(10)]
    public string? QualityLevel { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}

public class CleaningRecord : BaseEntity
{
    public int TaskId { get; set; }

    [MaxLength(20)]
    public string Result { get; set; } = "ok";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? Photo { get; set; }

    public DateTime RecordTime { get; set; } = DateTime.UtcNow;
}