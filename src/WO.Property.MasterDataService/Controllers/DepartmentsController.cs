using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 部门管理API
/// </summary>
[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(MySqlConnection db, ILogger<DepartmentsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var countSql = "SELECT COUNT(*) FROM Departments";
        using var countCmd = new MySqlCommand(countSql, _db);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = "SELECT * FROM Departments ORDER BY SortOrder, Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<DepartmentItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapDepartment(reader));

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
        using var cmd = new MySqlCommand("SELECT * FROM Departments WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapDepartment(reader) });
        return NotFound(new { success = false, message = "部门不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Departments WHERE Code = @code OR Name = @name", _db);
        checkCmd.Parameters.AddWithValue("@code", req.Code);
        checkCmd.Parameters.AddWithValue("@name", req.Name);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = "部门编号或名称已存在" });

        var sql = @"INSERT INTO Departments (Name, Code, Description, SortOrder)
                    VALUES (@Name, @Code, @Description, @SortOrder);
                    SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Code", req.Code);
        cmd.Parameters.AddWithValue("@Description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SortOrder", req.SortOrder);
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建部门: {Code} ({Name})", req.Code, req.Name);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "部门创建成功", data = new { id } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Departments WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "部门不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "Name = @name", "Code = @code", "Description = @description", "SortOrder = @sortOrder" };
        using var cmd = new MySqlCommand($"UPDATE Departments SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", req.Name);
        cmd.Parameters.AddWithValue("@code", req.Code);
        cmd.Parameters.AddWithValue("@description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sortOrder", req.SortOrder);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新部门: {Id}", id);
        return Ok(new { success = true, message = "部门更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Departments WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "部门不存在" });
        _logger.LogInformation("删除部门: {Id}", id);
        return Ok(new { success = true, message = "部门已删除" });
    }

    private static DepartmentItem MapDepartment(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Name = r["Name"].ToString() ?? "",
        Code = r["Code"].ToString() ?? "",
        Description = r["Description"] as string,
        SortOrder = r["SortOrder"] == DBNull.Value ? 0 : Convert.ToInt32(r["SortOrder"])
    };
}

public class DepartmentItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}