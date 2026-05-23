namespace WO.Property.MasterDataService.DTOs;

public class ImportBuildingsRequest
{
    public List<ImportBuildingRow> Rows { get; set; } = new();
}

public class ImportBuildingRow
{
    /// <summary>楼栋名称（必填）</summary>
    public string? Name { get; set; }

    /// <summary>所属区域ID</summary>
    public string? Area { get; set; }

    /// <summary>楼层数</summary>
    public int? TotalFloors { get; set; }

    /// <summary>每层户数</summary>
    public int? TotalUnits { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>排序</summary>
    public int? SortOrder { get; set; }
}

public class ImportBuildingsResponse
{
    public bool Success { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}