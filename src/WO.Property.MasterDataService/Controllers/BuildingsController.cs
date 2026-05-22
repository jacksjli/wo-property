using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 楼栋管理API
/// </summary>
[ApiController]
[Route("api/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<BuildingsController> _logger;

    public BuildingsController(MySqlConnection db, ILogger<BuildingsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>获取楼栋列表（分页）</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var where = string.IsNullOrEmpty(status) ? "" : "WHERE Status = @status";
        var countSql = $"SELECT COUNT(*) FROM Buildings {where}";
        using var countCmd = new MySqlCommand(countSql, _db);
        if (!string.IsNullOrEmpty(status)) countCmd.Parameters.AddWithValue("@status", status);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $"SELECT * FROM Buildings {where} ORDER BY Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        if (!string.IsNullOrEmpty(status)) dataCmd.Parameters.AddWithValue("@status", status);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<BuildingItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapBuilding(reader));

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>获取单个楼栋</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM Buildings WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapBuilding(reader) });
        return NotFound(new { success = false, message = "楼栋不存在" });
    }

    /// <summary>新增楼栋</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BuildingItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Buildings WHERE Code = @code", _db);
        checkCmd.Parameters.AddWithValue("@code", req.Code);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = $"楼栋编号 '{req.Code}' 已存在" });

        var sql = @"INSERT INTO Buildings (Name, Code, Description, Address, TotalFloors, TotalUnits, Status)
                    VALUES (@Name, @Code, @Description, @Address, @TotalFloors, @TotalUnits, @Status);
                    SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Code", req.Code);
        cmd.Parameters.AddWithValue("@Description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalFloors", req.TotalFloors ?? 1);
        cmd.Parameters.AddWithValue("@TotalUnits", req.TotalUnits ?? 1);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "Active");
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建楼栋: {Code} ({Name})", req.Code, req.Name);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "楼栋创建成功", data = new { id } });
    }

    /// <summary>更新楼栋</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BuildingItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Buildings WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "楼栋不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "Area = @area", "Name = @name", "Code = @code", "Description = @description", "Address = @address", "TotalFloors = @totalFloors", "TotalUnits = @totalUnits", "Status = @status", "UpdatedAt = @updatedAt" };
        using var cmd = new MySqlCommand($"UPDATE Buildings SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@area", (object)req.Area ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@name", req.Name);
        cmd.Parameters.AddWithValue("@code", req.Code);
        cmd.Parameters.AddWithValue("@description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@totalFloors", req.TotalFloors ?? 1);
        cmd.Parameters.AddWithValue("@totalUnits", req.TotalUnits ?? 1);
        cmd.Parameters.AddWithValue("@status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新楼栋: {Id}", id);
        return Ok(new { success = true, message = "楼栋更新成功" });
    }

    /// <summary>删除楼栋（自动清除关联）</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // 先删除 Rooms（BuildingId 为 NOT NULL，需先删除关联记录）
        var delRoomsCmd = new MySqlCommand("DELETE FROM Rooms WHERE BuildingId = @id", _db);
        delRoomsCmd.Parameters.AddWithValue("@id", id);
        await delRoomsCmd.ExecuteNonQueryAsync();

        // 删除 CleaningRecords（BuildingId 为 NOT NULL）
        var delCleaningCmd = new MySqlCommand("DELETE FROM CleaningRecords WHERE BuildingId = @id", _db);
        delCleaningCmd.Parameters.AddWithValue("@id", id);
        await delCleaningCmd.ExecuteNonQueryAsync();

        // 清除其他关联表的 BuildingId 引用（这些列允许 NULL）
        var nullableTables = new[] { "Devices", "InspectionRecords", 
            "ParkingRecords", "Residents", "Tickets", "Visitors" };
        
        foreach (var table in nullableTables)
        {
            var clearCmd = new MySqlCommand($"UPDATE {table} SET BuildingId = NULL WHERE BuildingId = @id", _db);
            clearCmd.Parameters.AddWithValue("@id", id);
            await clearCmd.ExecuteNonQueryAsync();
        }

        // 删除楼栋
        using var cmd = new MySqlCommand("DELETE FROM Buildings WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "楼栋不存在" });
        _logger.LogInformation("删除楼栋: {Id}", id);
        return Ok(new { success = true, message = "楼栋已删除" });
    }

    private static BuildingItem MapBuilding(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Name = r["Name"].ToString() ?? "",
        Code = r["Code"].ToString() ?? "",
        Area = r["Area"] as string ?? "",
        Description = r["Description"] as string,
        Address = r["Address"] as string,
        TotalFloors = r["TotalFloors"] == DBNull.Value ? null : Convert.ToInt32(r["TotalFloors"]),
        TotalUnits = r["TotalUnits"] == DBNull.Value ? null : Convert.ToInt32(r["TotalUnits"]),
        Status = r["Status"].ToString() ?? "Active",
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
    };
}

public class BuildingItem
{
    public int Id { get; set; }
    public string? Area { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string? Address { get; set; }
    public int? TotalFloors { get; set; }
    public int? TotalUnits { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}