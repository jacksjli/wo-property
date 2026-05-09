using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.InspectionService.Models;

public enum InspectionType { Daily, Weekly, Monthly, Quarterly, Annual, Special }
public enum TaskStatus { Pending, InProgress, Completed, Overdue, Cancelled }
public enum IssueSeverity { Low, Medium, High, Critical }
public enum IssueStatus { Open, InProgress, Resolved, Closed }

public class InspectionPlan : BaseEntity
{
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public InspectionType Type { get; set; }
    [MaxLength(500)] public string? Area { get; set; }
    [MaxLength(200)] public string? TargetItems { get; set; }
    public int IntervalDays { get; set; } = 1;
    public DateTime NextExecutionDate { get; set; }
    [MaxLength(100)] public string? AssignedTo { get; set; }
    public int EstimatedMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
}

public class InspectionTask : BaseEntity
{
    [Required][MaxLength(50)] public string TaskNumber { get; set; } = string.Empty;
    public int? PlanId { get; set; }
    [ForeignKey(nameof(PlanId))] public InspectionPlan? Plan { get; set; }
    [Required][MaxLength(100)] public string Title { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public InspectionType Type { get; set; }
    [MaxLength(500)] public string? Area { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    [MaxLength(100)] public string? AssignedTo { get; set; }
    [MaxLength(100)] public string? CompletedBy { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
}

public class InspectionRecord : BaseEntity
{
    [Required] public int TaskId { get; set; }
    [ForeignKey(nameof(TaskId))] public InspectionTask? Task { get; set; }
    [Required][MaxLength(50)] public string RecordNumber { get; set; } = string.Empty;
    public DateTime InspectionDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [Required][MaxLength(100)] public string Inspector { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int PassedItems { get; set; }
    public int FailedItems { get; set; }
    [MaxLength(1000)] public string? Findings { get; set; }
    [MaxLength(500)] public string? Suggestions { get; set; }
    public bool IsPassed { get; set; }
    [MaxLength(500)] public string? Attachments { get; set; }
}

public class InspectionIssue : BaseEntity
{
    [Required][MaxLength(50)] public string IssueNumber { get; set; } = string.Empty;
    public int? RecordId { get; set; }
    [ForeignKey(nameof(RecordId))] public InspectionRecord? Record { get; set; }
    [Required][MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;
    public IssueStatus Status { get; set; } = IssueStatus.Open;
    [MaxLength(200)] public string? Location { get; set; }
    [MaxLength(100)] public string? Category { get; set; }
    [MaxLength(1000)] public string? Solution { get; set; }
    [MaxLength(100)] public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    [MaxLength(100)] public string? ResolvedBy { get; set; }
    [MaxLength(500)] public string? Remarks { get; set; }
}