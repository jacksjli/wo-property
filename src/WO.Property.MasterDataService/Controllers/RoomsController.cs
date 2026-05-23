using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WO.Property.MasterDataService.Models;

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

    /// <summary>批量导入房号</summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportRooms([FromBody] ImportRoomsRequest req)
    {
        var result = new ImportResult();
        foreach (var row in req.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.RoomNumber))
            {
                result.Skipped++;
                result.Errors.Add($"跳过的行：房号名称为空");
                continue;
            }

            try
            {
                // 检查是否已存在（按 BuildingId + RoomNumber 唯一判断）
                int? existingId = null;
                if (row.BuildingId.HasValue)
                {
                    using var checkCmd = new MySqlCommand("SELECT Id FROM Rooms WHERE BuildingId = @buildingId AND RoomNumber = @roomNumber LIMIT 1", _db);
                    checkCmd.Parameters.AddWithValue("@buildingId", row.BuildingId.Value);
                    checkCmd.Parameters.AddWithValue("@roomNumber", row.RoomNumber);
                    using var reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                        existingId = Convert.ToInt32(reader["Id"]);
                    reader.Close();
                }
                else
                {
                    using var checkCmd = new MySqlCommand("SELECT Id FROM Rooms WHERE RoomNumber = @roomNumber LIMIT 1", _db);
                    checkCmd.Parameters.AddWithValue("@roomNumber", row.RoomNumber);
                    using var reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                        existingId = Convert.ToInt32(reader["Id"]);
                    reader.Close();
                }

                if (existingId.HasValue)
                {
                    // 覆盖更新
                    var updates = new List<string> { "UpdatedAt = @updatedAt" };
                    if (row.BuildingId.HasValue) updates.Add("BuildingId = @buildingId");
                    if (row.Floor != null) updates.Add("Floor = @floor");
                    if (row.Unit != null) updates.Add("Unit = @unit");
                    if (!string.IsNullOrEmpty(row.RoomType)) updates.Add("RoomType = @roomType");
                    if (row.Area.HasValue) updates.Add("Area = @area");

                    using var updateCmd = new MySqlCommand($"UPDATE Rooms SET {string.Join(", ", updates)} WHERE Id = @id", _db);
                    updateCmd.Parameters.AddWithValue("@id", existingId.Value);
                    updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                    if (row.BuildingId.HasValue) updateCmd.Parameters.AddWithValue("@buildingId", row.BuildingId.Value);
                    if (row.Floor != null) updateCmd.Parameters.AddWithValue("@floor", row.Floor);
                    if (row.Unit != null) updateCmd.Parameters.AddWithValue("@unit", row.Unit);
                    if (!string.IsNullOrEmpty(row.RoomType)) updateCmd.Parameters.AddWithValue("@roomType", row.RoomType);
                    if (row.Area.HasValue) updateCmd.Parameters.AddWithValue("@area", row.Area.Value);
                    await updateCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("覆盖房号: {RoomNumber} (Id={Id})", row.RoomNumber, existingId.Value);
                }
                else
                {
                    // 新增
                    using var insertCmd = new MySqlCommand(@"
                        INSERT INTO Rooms (BuildingId, Floor, Unit, RoomNumber, RoomType, Area, Status)
                        VALUES (@BuildingId, @Floor, @Unit, @RoomNumber, @RoomType, @Area, @Status)", _db);
                    insertCmd.Parameters.AddWithValue("@BuildingId", row.BuildingId.HasValue ? (object)row.BuildingId.Value : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Floor", (object)row.Floor ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Unit", (object)row.Unit ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@RoomNumber", row.RoomNumber);
                    insertCmd.Parameters.AddWithValue("@RoomType", (object)row.RoomType ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Area", row.Area.HasValue ? (object)row.Area.Value : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Status", "Active");
                    await insertCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("导入房号: {RoomNumber}", row.RoomNumber);
                }

                result.Success++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"房号「{row.RoomNumber}」失败：{ex.Message}");
            }
        }

        return Ok(new { success = true, data = result });
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