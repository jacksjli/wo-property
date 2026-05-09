using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WO.Property.MasterDataService.DTOs;

// ==================== FieldDefinition DTOs ====================

/// <summary>
/// 创建字段定义请求
/// </summary>
public class CreateFieldDefinitionRequest
{
    [Required]
    [MaxLength(50)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string FieldType { get; set; } = "text";

    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;

    public bool IsShared { get; set; } = false;

    [MaxLength(50)]
    public string? Module { get; set; }

    /// <summary>
    /// select类型的选项，JSON格式数组字符串
    /// </summary>
    public string? Options { get; set; }

    public string? DefaultValue { get; set; }

    public bool IsRequired { get; set; } = false;

    public int Width { get; set; } = 100;

    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// 更新字段定义请求
/// </summary>
public class UpdateFieldDefinitionRequest
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(20)]
    public string? FieldType { get; set; }

    [MaxLength(50)]
    public string? Source { get; set; }

    public bool? IsShared { get; set; }

    [MaxLength(50)]
    public string? Module { get; set; }

    public string? Options { get; set; }

    public string? DefaultValue { get; set; }

    public bool? IsRequired { get; set; }

    public int? Width { get; set; }

    public int? SortOrder { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }
}

/// <summary>
/// 字段定义响应
/// </summary>
public class FieldDefinitionResponse
{
    public int Id { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public string? Module { get; set; }
    public List<string>? Options { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public int Width { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 字段定义列表响应（带分页）
/// </summary>
public class FieldDefinitionListResponse
{
    public bool Success { get; set; } = true;
    public List<FieldDefinitionResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// 分页信息
/// </summary>
public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

// ==================== ModuleField DTOs ====================

/// <summary>
/// 为模块添加字段请求
/// </summary>
public class AddModuleFieldRequest
{
    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty;

    [Required]
    public int FieldDefinitionId { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// 更新模块字段配置请求
/// </summary>
public class UpdateModuleFieldRequest
{
    public bool? IsVisible { get; set; }

    public bool? IsActive { get; set; }

    public int? SortOrder { get; set; }
}

/// <summary>
/// 模块字段响应（包含字段定义详情）
/// </summary>
public class ModuleFieldResponse
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public int FieldDefinitionId { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public FieldDefinitionResponse? FieldDefinition { get; set; }
}

/// <summary>
/// 模块字段列表响应
/// </summary>
public class ModuleFieldListResponse
{
    public bool Success { get; set; } = true;
    public List<ModuleFieldResponse> Data { get; set; } = new();
}