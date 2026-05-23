using System.ComponentModel.DataAnnotations;

namespace WO.Property.MasterDataService.Models;

/// <summary>
/// 区域批量导入请求
/// </summary>
public class ImportAreasRequest
{
    public List<ImportAreaRow> Rows { get; set; } = new();
}

public class ImportAreaRow
{
    public string Name { get; set; } = "";
    public int? ParentId { get; set; }
    public string Description { get; set; } = "";
    public int Sort { get; set; }
}

/// <summary>
/// 区域批量导入结果
/// </summary>
public class ImportAreasResult
{
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}