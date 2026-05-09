using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.PersonService.Models;

[Table("Persons")]
public class Person : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Gender { get; set; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(50)]
    public string Department { get; set; } = "工程部";

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "operator";

    public DateOnly? JoinDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "在职";

    [MaxLength(18)]
    public string? IdCard { get; set; }

    [MaxLength(100)]
    public string? EmergencyContact { get; set; }

    [MaxLength(20)]
    public string? EmergencyPhone { get; set; }

    [MaxLength(200)]
    public string? PasswordHash { get; set; }

    [Required]
    [MaxLength(20)]
    public string PersonType { get; set; } = "员工";
}