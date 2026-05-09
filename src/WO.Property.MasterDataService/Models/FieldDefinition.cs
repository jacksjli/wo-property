using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WO.Property.Shared.Models;

namespace WO.Property.MasterDataService.Models;

public class FieldDefinition : BaseEntity
{
    [Required][MaxLength(50)] public string FieldKey { get; set; } = string.Empty;
    [Required][MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
    [Required][MaxLength(20)] public string FieldType { get; set; } = "text";
    [Required][MaxLength(50)] public string Source { get; set; } = string.Empty;
    public bool IsShared { get; set; } = false;
    [MaxLength(50)] public string? Module { get; set; }
    public string? Options { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public int Width { get; set; } = 100;
    public int SortOrder { get; set; } = 0;
    [MaxLength(20)] public string Status { get; set; } = "Active";

    public virtual ICollection<ModuleField> ModuleFields { get; set; } = new List<ModuleField>();
}

public class ModuleField : BaseEntity
{
    [Required][MaxLength(50)] public string Module { get; set; } = string.Empty;
    public int FieldDefinitionId { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    [ForeignKey(nameof(FieldDefinitionId))]
    public virtual FieldDefinition? FieldDefinition { get; set; }
}