using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 房间管理API
/// </summary>
[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(MySqlConnection db, ILogger<RoomsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>获取房间列表（分页）</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? buildingId = null, [FromQuery] string? status = null)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();
        if (buildingId.HasValue) { conditions.Add("BuildingId = @buildingId"); parameters.Add(new MySqlParameter("@buildingId", buildingId.Value)); }
        if (!string.IsNullOrEmpty(status)) { conditions.Add("Status = @status"); parameters.Add(new MySqlParameter("@status", status)); }
        var where = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Rooms {where}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $"SELECT * FROM Rooms {where} ORDER BY Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<RoomItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRoom(reader));

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>获取单个房间</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM Rooms WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapRoom(reader) });
        return NotFound(new { success = false, message = "房间不存在" });
    }

    /// <summary>新增房间</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoomItem req)
    {
        var sql = @"INSERT INTO Rooms (BuildingId, Floor, Unit, RoomNumber, RoomType, Area, Status)
                    VALUES (@BuildingId, @Floor, @Unit, @RoomNumber, @RoomType, @Area, @Status);
                    SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@BuildingId", req.BuildingId);
        cmd.Parameters.AddWithValue("@Floor", (object)req.Floor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Unit", (object)req.Unit ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RoomNumber", req.RoomNumber);
        cmd.Parameters.AddWithValue("@RoomType", (object)req.RoomType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Area", req.Area.HasValue ? (object)req.Area.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "Active");
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建房间: {RoomNumber}", req.RoomNumber);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "房间创建成功", data = new { id } });
    }

    /// <summary>更新房间</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoomItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Rooms WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "房间不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "BuildingId = @buildingId", "Floor = @floor", "Unit = @unit", "RoomNumber = @roomNumber", "RoomType = @roomType", "Area = @area", "Status = @status", "UpdatedAt = @updatedAt" };
        using var cmd = new MySqlCommand($"UPDATE Rooms SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@buildingId", req.BuildingId);
        cmd.Parameters.AddWithValue("@floor", (object)req.Floor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@unit", (object)req.Unit ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@roomNumber", req.RoomNumber);
        cmd.Parameters.AddWithValue("@roomType", (object)req.RoomType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@area", req.Area.HasValue ? (object)req.Area.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新房间: {Id}", id);
        return Ok(new { success = true, message = "房间更新成功" });
    }

    /// <summary>删除房间</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Rooms WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "房间不存在" });
        _logger.LogInformation("删除房间: {Id}", id);
        return Ok(new { success = true, message = "房间已删除" });
    }

    private static RoomItem MapRoom(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        BuildingId = Convert.ToInt32(r["BuildingId"]),
        Floor = r["Floor"] as string,
        Unit = r["Unit"] as string,
        RoomNumber = r["RoomNumber"].ToString() ?? "",
        RoomType = r["RoomType"] as string,
        Area = r["Area"] == DBNull.Value ? null : Convert.ToDecimal(r["Area"]),
        Status = r["Status"].ToString() ?? "Active",
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
    };
}

public class RoomItem
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public string? Floor { get; set; }
    public string? Unit { get; set; }
    public string RoomNumber { get; set; } = "";
    public string? RoomType { get; set; }
    public decimal? Area { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}