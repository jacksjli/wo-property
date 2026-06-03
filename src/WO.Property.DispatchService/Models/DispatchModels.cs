namespace WO.Property.DispatchService.Models;

/// <summary>
/// 派单记录
/// </summary>
public class DispatchRecord
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public DateTime DispatchTime { get; set; }
    public int FromPersonId { get; set; }
    public string? FromPersonName { get; set; }
    public int ToPersonId { get; set; }
    public string? ToPersonName { get; set; }
    public string Status { get; set; } = "Pending";
    public string Source { get; set; } = "Auto";
    public string? WorkflowInstanceId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int? ConfirmedBy { get; set; }
    public string? ConfirmedByName { get; set; }
    public long? RatingId { get; set; }
    public long? ParentDispatchId { get; set; }  // 母派单ID（用于升级派单）
    public string EscalationLevel { get; set; } = "";  // 升级级别（Escalation）
    public long? SourceDispatchId { get; set; }  // 转单来源派单ID
    public string? SourceType { get; set; }  // Normal / Transfer
    public DateTime? AcceptedAt { get; set; }  // 接单时间
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 转单申请
/// </summary>
public class TransferRequest
{
    public long Id { get; set; }
    public long DispatchRecordId { get; set; }
    public long TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public int FromPersonId { get; set; }
    public string? FromPersonName { get; set; }
    public int ToPersonId { get; set; }
    public string? ToPersonName { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedReason { get; set; }
    public long? TransferDispatchId { get; set; }
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 派单规则
/// </summary>
public class DispatchRule
{
    public long Id { get; set; }
    public string RuleName { get; set; } = "";
    public int Priority { get; set; }
    public int? TicketTypeId { get; set; }
    public string? TicketTypeName { get; set; }
    public int? AreaId { get; set; }
    public string? AreaName { get; set; }
    public int? PersonId { get; set; }
    public string? PersonName { get; set; }
    public string BalanceStrategy { get; set; } = "LeastWorkload";
    public bool IsActive { get; set; } = true;
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 人员负载
/// </summary>
public class PersonWorkload
{
    public int PersonId { get; set; }
    public string? PersonName { get; set; }
    public int ActiveTicketCount { get; set; }
    public int TotalDispatched { get; set; }
    public int TotalCompleted { get; set; }
    public DateTime? LastDispatchTime { get; set; }
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 满意度评价
/// </summary>
public class SatisfactionRating
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public long DispatchRecordId { get; set; }
    public int RaterId { get; set; }
    public string? RaterName { get; set; }
    public int RateeId { get; set; }
    public string? RateeName { get; set; }
    public int QualityScore { get; set; } = 5;
    public int AttitudeScore { get; set; } = 5;
    public int TimelinessScore { get; set; } = 5;
    public int OverallScore { get; set; } = 5;
    public string? Comment { get; set; }
    public string? Images { get; set; }
    public DateTime RatedAt { get; set; }
    public bool IsAutoRated { get; set; }
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 超时告警
/// </summary>
public class TimeoutAlert
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public long? DispatchRecordId { get; set; }
    public string AlertType { get; set; } = "";
    public DateTime ExpectedTime { get; set; }
    public DateTime? ActualTime { get; set; }
    public int TimeoutMinutes { get; set; }
    public int Level { get; set; } = 1;
    public int NotifyTargetId { get; set; }
    public string? NotifyTargetName { get; set; }
    public long? NotificationId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? SentAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}
/// <summary>
/// 超时规则配置（从 timeout_rules 表读取）
/// </summary>
public class TimeoutRule
{
    public int Id { get; set; }
    public string Color { get; set; } = "";
    public string Role { get; set; } = "";
    public int Hours { get; set; } = 24;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 超时升级记录
/// </summary>
public class TimeoutEscalation
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string TicketCode { get; set; } = "";
    public long DispatchRecordId { get; set; }
    public int FromPersonId { get; set; }
    public string? FromPersonName { get; set; }
    public int ToPersonId { get; set; }
    public string? ToPersonName { get; set; }
    public string ToRole { get; set; } = "";
    public int Level { get; set; } = 1;
    public DateTime EscalatedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string TenantCode { get; set; } = "";
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}
