using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 供应商管理API
/// </summary>
[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(MySqlConnection db, ILogger<SuppliersController> logger)
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
        var countSql = $"SELECT COUNT(*) FROM Suppliers {where}";
        using var countCmd = new MySqlCommand(countSql, _db);
        if (!string.IsNullOrEmpty(status)) countCmd.Parameters.AddWithValue("@status", status);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $"SELECT * FROM Suppliers {where} ORDER BY Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        if (!string.IsNullOrEmpty(status)) dataCmd.Parameters.AddWithValue("@status", status);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<SupplierItem>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapSupplier(reader));

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
        using var cmd = new MySqlCommand("SELECT * FROM Suppliers WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { success = true, data = MapSupplier(reader) });
        return NotFound(new { success = false, message = "供应商不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Suppliers WHERE Code = @code", _db);
        checkCmd.Parameters.AddWithValue("@code", req.Code);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = $"供应商编号 '{req.Code}' 已存在" });

        var sql = @"INSERT INTO Suppliers (Name, Code, ContactPerson, ContactPhone, Address, Type, Status)
                    VALUES (@Name, @Code, @ContactPerson, @ContactPhone, @Address, @Type, @Status);
                    SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Code", req.Code);
        cmd.Parameters.AddWithValue("@ContactPerson", (object)req.ContactPerson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContactPhone", (object)req.ContactPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Type", (object)req.Type ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "Active");
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建供应商: {Code} ({Name})", req.Code, req.Name);
        return CreatedAtAction(nameof(GetById), new { id }, new { success = true, message = "供应商创建成功", data = new { id } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierItem req)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM Suppliers WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "供应商不存在" });
        reader.Close(); // 关闭 reader 才能执行下一个命令

        var updates = new List<string> { "Name = @name", "Code = @code", "ContactPerson = @contactPerson", "ContactPhone = @contactPhone", "Address = @address", "Type = @type", "Status = @status", "UpdatedAt = @updatedAt" };
        using var cmd = new MySqlCommand($"UPDATE Suppliers SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", req.Name);
        cmd.Parameters.AddWithValue("@code", req.Code);
        cmd.Parameters.AddWithValue("@contactPerson", (object)req.ContactPerson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@contactPhone", (object)req.ContactPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@address", (object)req.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@type", (object)req.Type ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新供应商: {Id}", id);
        return Ok(new { success = true, message = "供应商更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Suppliers WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "供应商不存在" });
        _logger.LogInformation("删除供应商: {Id}", id);
        return Ok(new { success = true, message = "供应商已删除" });
    }

    private static SupplierItem MapSupplier(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt32(r["Id"]),
        Name = r["Name"].ToString() ?? "",
        Code = r["Code"].ToString() ?? "",
        ContactPerson = r["ContactPerson"] as string,
        ContactPhone = r["ContactPhone"] as string,
        Address = r["Address"] as string,
        Type = r["Type"] as string,
        Status = r["Status"].ToString() ?? "Active",
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
    };
}

public class SupplierItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Type { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}