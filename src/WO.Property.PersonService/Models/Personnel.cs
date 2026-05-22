using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WO.Property.PersonService.Models;

[Table("Personnel")]
public class Personnel
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("EmployeeNo")]
    [MaxLength(30)]
    public string EmployeeNo { get; set; } = string.Empty;

    [Column("Name")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("Avatar")]
    [MaxLength(255)]
    public string? Avatar { get; set; }

    [Column("Gender")]
    [MaxLength(20)]
    public string Gender { get; set; } = "male";

    [Column("Birthday")]
    public DateTime? Birthday { get; set; }

    [Column("IdCard")]
    [MaxLength(30)]
    public string? IdCard { get; set; }

    [Column("Phone")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Column("Email")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Column("Address")]
    [MaxLength(200)]
    public string? Address { get; set; }

    [Column("Education")]
    [MaxLength(20)]
    public string? Education { get; set; }

    [Column("GraduateSchool")]
    [MaxLength(100)]
    public string? GraduateSchool { get; set; }

    [Column("Major")]
    [MaxLength(50)]
    public string? Major { get; set; }

    [Column("Role")]
    [MaxLength(30)]
    public string Role { get; set; } = "operator";

    [Column("DepartmentId")]
    public int? DepartmentId { get; set; }

    [Column("DepartmentName")]
    [MaxLength(50)]
    public string? DepartmentName { get; set; }

    [Column("Position")]
    [MaxLength(50)]
    public string? Position { get; set; }

    [Column("EmploymentType")]
    [MaxLength(20)]
    public string? EmploymentType { get; set; }

    [Column("HireDate")]
    public DateTime? HireDate { get; set; }

    [Column("ContractStart")]
    public DateTime? ContractStart { get; set; }

    [Column("ContractEnd")]
    public DateTime? ContractEnd { get; set; }

    [Column("Salary")]
    public decimal? Salary { get; set; }

    [Column("BankAccount")]
    [MaxLength(50)]
    public string? BankAccount { get; set; }

    [Column("SocialSecurityNo")]
    [MaxLength(50)]
    public string? SocialSecurityNo { get; set; }

    [Column("Status")]
    [MaxLength(20)]
    public string Status { get; set; } = "probation";

    [Column("Specialties")]
    public string? Specialties { get; set; }

    [Column("Backups")]
    public string? Backups { get; set; }

    [Column("EmergencyContactName")]
    [MaxLength(50)]
    public string? EmergencyContactName { get; set; }

    [Column("EmergencyContactRelationship")]
    [MaxLength(20)]
    public string? EmergencyContactRelationship { get; set; }

    [Column("EmergencyContactPhone")]
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [Column("AttendanceCount")]
    public int AttendanceCount { get; set; }

    [Column("OvertimeHours")]
    public decimal? OvertimeHours { get; set; }

    [Column("LeaveDays")]
    public int LeaveDays { get; set; }

    [Column("PerformanceScore")]
    public decimal? PerformanceScore { get; set; }

    [Column("TrainingCount")]
    public int TrainingCount { get; set; }

    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("ticket_type_ids")]
    public string? TicketTypeIds { get; set; }

    [Column("specialty_ids")]
    public string? SpecialtyIds { get; set; }

    [Column("area_ids")]
    public string? AreaIds { get; set; }

    [Column("is_supervisor")]
    public bool IsSupervisor { get; set; }

    [Column("max_concurrent_tickets")]
    public int MaxConcurrentTickets { get; set; } = 5;
}

// 请求模型
public class CreatePersonnelRequest
{
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Gender { get; set; } = "male";
    public DateTime? Birthday { get; set; }
    public string? IdCard { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Education { get; set; }
    public string? GraduateSchool { get; set; }
    public string? Major { get; set; }
    public string Role { get; set; } = "operator";
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? ContractStart { get; set; }
    public DateTime? ContractEnd { get; set; }
    public decimal? Salary { get; set; }
    public string? BankAccount { get; set; }
    public string? SocialSecurityNo { get; set; }
    public string Status { get; set; } = "probation";
    public string? Specialties { get; set; }
    public string? Backups { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Remark { get; set; }
    public string? TicketTypeIds { get; set; }
    public string? SpecialtyIds { get; set; }
    public string? AreaIds { get; set; }
    public bool IsSupervisor { get; set; }
    public int MaxConcurrentTickets { get; set; } = 5;
}

public class UpdatePersonnelRequest
{
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? IdCard { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Education { get; set; }
    public string? GraduateSchool { get; set; }
    public string? Major { get; set; }
    public string? Role { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? ContractStart { get; set; }
    public DateTime? ContractEnd { get; set; }
    public decimal? Salary { get; set; }
    public string? BankAccount { get; set; }
    public string? SocialSecurityNo { get; set; }
    public string? Status { get; set; }
    public string? Specialties { get; set; }
    public string? Backups { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Remark { get; set; }
    public string? TicketTypeIds { get; set; }
    public string? SpecialtyIds { get; set; }
    public string? AreaIds { get; set; }
    public bool? IsSupervisor { get; set; }
    public int? MaxConcurrentTickets { get; set; }
}