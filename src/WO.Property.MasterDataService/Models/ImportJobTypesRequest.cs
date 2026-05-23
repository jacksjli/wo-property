namespace WO.Property.MasterDataService.Models;

/// <summary>
/// 批量导入工种请求
/// </summary>
public class ImportJobTypesRequest
{
    public List<ImportJobTypeRow> Rows { get; set; } = new();
}

public class ImportJobTypeRow
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int? TicketTypeId { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// 批量导入结果
/// </summary>
public class ImportJobTypesResult
{
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}