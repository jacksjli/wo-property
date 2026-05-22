using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 区域配置管理API
/// </summary>
[ApiController]
[Route("api/regions")]
public class RegionsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<RegionsController> _logger;

    public RegionsController(MySqlConnection db, ILogger<RegionsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? status = null)
    {
        var where = string.IsNullOrEmpty(status) ? "" : "WHERE Status = @status";
        var countSql = $"SELECT COUNT(*) FROM Regions {where}";
        using var countCmd = new MySqlCommand(countSql, _db);
        if (!string.IsNullOrEmpty(status)) countCmd.Parameters.AddWithValue("@status", status);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $"SELECT * FROM Regions {where} ORDER BY SortOrder, Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        if (!string.IsNullOrEmpty(status)) dataCmd.Parameters.AddWithValue("@status", status);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<RegionItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapRegion(reader));

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM Regions WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapRegion(reader) });
        return NotFound(new { success = false, message = "区域配置不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegionItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Regions WHERE Code = @code", _db);
        checkCmd.Parameters.AddWithValue("@code", req.Code);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = $"区域配置编号 '{req.Code}' 已存在" });

        var sql = @"INSERT INTO Regions (Name, Code, Description, Config, Status)
                    VALUES (@Name, @Code, @Description, @Config, @Status);
                    SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Code", req.Code);
        cmd.Parameters.AddWithValue("@Description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Config", (object)req.Config ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "Active");
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建区域配置: {Code} ({Name})", req.Code, req.Name);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "区域配置创建成功", data = new { id } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RegionItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Regions WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "区域配置不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "Name = @name", "Code = @code", "Description = @description", "Config = @config", "Status = @status", "UpdatedAt = @updatedAt" };
        using var cmd = new MySqlCommand($"UPDATE Regions SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", req.Name);
        cmd.Parameters.AddWithValue("@code", req.Code);
        cmd.Parameters.AddWithValue("@description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@config", (object)req.Config ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新区域配置: {Id}", id);
        return Ok(new { success = true, message = "区域配置更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Regions WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "区域配置不存在" });
        _logger.LogInformation("删除区域配置: {Id}", id);
        return Ok(new { success = true, message = "区域配置已删除" });
    }

    private static RegionItem MapRegion(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Name = r["Name"].ToString() ?? "",
        Code = r["Code"].ToString() ?? "",
        Description = r["Description"] as string,
        Config = r["Config"] as string,
        Status = r["Status"].ToString() ?? "Active",
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
    };
}

public class RegionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string? Config { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}