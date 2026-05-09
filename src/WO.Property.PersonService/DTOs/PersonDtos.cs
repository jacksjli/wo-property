namespace WO.Property.PersonService.DTOs;

public class PersonDto
{
    public int Id { get; set; }
    public string StaffId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateOnly? JoinDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IdCard { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePersonDto
{
    public string StaffId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Department { get; set; } = "工程部";
    public string Role { get; set; } = "operator";
    public DateOnly? JoinDate { get; set; }
    public string Status { get; set; } = "在职";
    public string? IdCard { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Password { get; set; }
    public string PersonType { get; set; } = "员工";
}

public class UpdatePersonDto
{
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public DateOnly? JoinDate { get; set; }
    public string? Status { get; set; }
    public string? IdCard { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Password { get; set; }
    public string? PersonType { get; set; }
}

public class PersonQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public string? PersonType { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class PersonStatisticsDto
{
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public Dictionary<string, int> ByDepartment { get; set; } = new();
    public Dictionary<string, int> ByRole { get; set; } = new();
    public Dictionary<string, int> ByPersonType { get; set; } = new();
}

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? Description { get; set; }
}