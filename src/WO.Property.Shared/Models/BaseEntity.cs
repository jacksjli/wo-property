using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WO.Property.Shared.Models;

/// <summary>
/// 所有实体的基类，提供统一的审计字段
/// </summary>
public abstract class BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>创建人</summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后修改人</summary>
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    /// <summary>最后修改时间（可空）</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>软删除标记</summary>
    public bool IsDeleted { get; set; } = false;
}