namespace WO.Property.Shared.DTOs.Ticket;

/// <summary>
/// 工单查询参数
/// </summary>
public class TicketQueryDto
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Type { get; set; }
    public int? ProjectId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 工单列表项
/// </summary>
public class TicketListItemDto
{
    public int Id { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public string? Location { get; set; }
    public string? PhotoUrls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 创建工单请求
/// </summary>
public class CreateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public string? Location { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? PhotoUrls { get; set; }
}

/// <summary>
/// 更新工单请求
/// </summary>
public class UpdateTicketDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Type { get; set; }
    public int? AssignedTo { get; set; }
    public string? Location { get; set; }
    public string? PhotoUrls { get; set; }
    public string? Resolution { get; set; }
}
