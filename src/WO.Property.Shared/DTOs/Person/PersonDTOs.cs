namespace WO.Property.Shared.DTOs.Person;

/// <summary>
/// 人员查询参数
/// </summary>
public class PersonQueryDto
{
    public string? PersonType { get; set; }
    public string? Status { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public int? DepartmentId { get; set; }
    public int? JobTypeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 人员列表项
/// </summary>
public class PersonListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? JobTypeName { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 人员详情
/// </summary>
public class PersonDetailDto : PersonListItemDto
{
    public string? Remark { get; set; }
    public int? MaxLoad { get; set; }
    public int? OpenTicketCount { get; set; }
    public string? Role { get; set; }
}

/// <summary>
/// 可用工人员（派单场景）
/// </summary>
public class AvailablePersonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public int? JobTypeId { get; set; }
    public string? JobTypeName { get; set; }
    public int MaxLoad { get; set; } = 5;
    public int OpenTicketCount { get; set; }
    public int? DepartmentId { get; set; }
    public string Status { get; set; } = "Active";
}
