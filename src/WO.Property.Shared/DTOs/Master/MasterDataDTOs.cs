namespace WO.Property.Shared.DTOs.Master;

/// <summary>
/// 基础数据查询参数
/// </summary>
public class MasterDataQueryDto
{
    public string? Category { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? ParentId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

/// <summary>
/// 基础数据项（统一格式）
/// </summary>
public class MasterDataItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public int? SortOrder { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 基础数据分类（树形结构）
/// </summary>
public class MasterDataCategoryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public int ItemCount { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 批量导入结果
/// </summary>
public class BulkImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<MasterDataItemDto> Imported { get; set; } = new();
}

/// <summary>
/// 字段配置（用于前端字段配置模块）
/// </summary>
public class FieldConfigDto
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public string? DefaultValue { get; set; }
    public bool Required { get; set; }
    public string Status { get; set; } = "Active";
    public int? Width { get; set; }
    public int? SortOrder { get; set; }
}
