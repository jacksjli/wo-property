using System.ComponentModel.DataAnnotations;

namespace WO.Property.MasterDataService.Models;

/// <summary>
/// 批量导入房号请求
/// </summary>
public class ImportRoomsRequest
{
    public List<ImportRoomRow> Rows { get; set; } = new();
}

public class ImportRoomRow
{
    /// <summary>房号名称（必填）</summary>
    [Required]
    public string RoomNumber { get; set; } = "";

    /// <summary>所属楼栋ID</summary>
    public int? BuildingId { get; set; }

    /// <summary>楼层</summary>
    public string? Floor { get; set; }

    /// <summary>单元</summary>
    public string? Unit { get; set; }

    /// <summary>类型</summary>
    public string? RoomType { get; set; }

    /// <summary>建筑面积</summary>
    public decimal? Area { get; set; }

    /// <summary>使用面积</summary>
    public decimal? UseArea { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }
}

/// <summary>
/// 批量导入结果
/// </summary>
public class ImportResult
{
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}