using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.ProjectConfigService.Models;

[Table("projects")]
public class Project : BaseEntity
{
    [Required][MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required][MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(20)] public string Status { get; set; } = "Active";
    [MaxLength(50)] public string? Tier { get; set; }
    public ICollection<ProjectConfig> Configs { get; set; } = new List<ProjectConfig>();
}

[Table("project_configs")]
public class ProjectConfig : BaseEntity
{
    [Required] public int ProjectId { get; set; }
    [Required][MaxLength(50)] public string Module { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = "{}";
    public string? FieldConfig { get; set; }
    [ForeignKey("ProjectId")] public Project? Project { get; set; }
}

[Table("global_configs")]
public class GlobalConfig : BaseEntity
{
    [Required][MaxLength(100)] public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = "{}";
}