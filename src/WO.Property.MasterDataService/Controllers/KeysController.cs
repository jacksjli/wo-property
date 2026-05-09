using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 钥匙管理API
/// </summary>
[ApiController]
[Route("api/keys")]
public class KeysController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<KeysController> _logger;

    public KeysController(MySqlConnection db, ILogger<KeysController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(KeyNo LIKE @keyword OR Name LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        if (!string.IsNullOrEmpty(type) && type != "all")
        {
            conditions.Add("Type = @type");
            parameters.Add(new MySqlParameter("@type", type));
        }

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM `keys` {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT * FROM `keys`
            {whereClause}
            ORDER BY Id DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<Dictionary<string, object>>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            items.Add(row);
        }

        return Ok(new
        {
            success = true,
            data = items,
            total,
            page,
            pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM `keys` WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            reader.Close();
            return Ok(new { success = true, data = row });
        }
        reader.Close();
        return NotFound(new { success = false, message = "钥匙不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKeyRequest request)
    {
        var insertSql = @"INSERT INTO `keys`
            (KeyNo, Name, Type, Location, Building, Floor, DoorNo, Quantity, Status, Holder, HolderPhone, LastBorrowTime, BorrowCount, Photo, Remark, CreatedAt)
            VALUES (@KeyNo, @Name, @Type, @Location, @Building, @Floor, @DoorNo, @Quantity, @Status, @Holder, @HolderPhone, @LastBorrowTime, @BorrowCount, @Photo, @Remark, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@KeyNo", request.KeyNo ?? "");
        cmd.Parameters.AddWithValue("@Name", request.Name ?? "");
        cmd.Parameters.AddWithValue("@Type", request.Type ?? "door_key");
        cmd.Parameters.AddWithValue("@Location", request.Location ?? "");
        cmd.Parameters.AddWithValue("@Building", request.Building ?? "");
        cmd.Parameters.AddWithValue("@Floor", request.Floor ?? "");
        cmd.Parameters.AddWithValue("@DoorNo", request.DoorNo ?? "");
        cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "available");
        cmd.Parameters.AddWithValue("@Holder", request.Holder ?? "");
        cmd.Parameters.AddWithValue("@HolderPhone", request.HolderPhone ?? "");
        cmd.Parameters.AddWithValue("@LastBorrowTime", request.LastBorrowTime ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BorrowCount", request.BorrowCount);
        cmd.Parameters.AddWithValue("@Photo", request.Photo ?? "");
        cmd.Parameters.AddWithValue("@Remark", request.Remark ?? "");
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建钥匙: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            success = true,
            message = "钥匙创建成功",
            data = new { id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateKeyRequest request)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM `keys` WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            reader.Close();
            return NotFound(new { success = false, message = "钥匙不存在" });
        }
        reader.Close();

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (request.KeyNo != null) { updates.Add("KeyNo = @KeyNo"); parameters.Add(new MySqlParameter("@KeyNo", request.KeyNo)); }
        if (request.Name != null) { updates.Add("Name = @Name"); parameters.Add(new MySqlParameter("@Name", request.Name)); }
        if (request.Type != null) { updates.Add("Type = @Type"); parameters.Add(new MySqlParameter("@Type", request.Type)); }
        if (request.Location != null) { updates.Add("Location = @Location"); parameters.Add(new MySqlParameter("@Location", request.Location)); }
        if (request.Building != null) { updates.Add("Building = @Building"); parameters.Add(new MySqlParameter("@Building", request.Building)); }
        if (request.Floor != null) { updates.Add("Floor = @Floor"); parameters.Add(new MySqlParameter("@Floor", request.Floor)); }
        if (request.DoorNo != null) { updates.Add("DoorNo = @DoorNo"); parameters.Add(new MySqlParameter("@DoorNo", request.DoorNo)); }
        if (request.Quantity.HasValue) { updates.Add("Quantity = @Quantity"); parameters.Add(new MySqlParameter("@Quantity", request.Quantity.Value)); }
        if (request.Status != null) { updates.Add("Status = @Status"); parameters.Add(new MySqlParameter("@Status", request.Status)); }
        if (request.Holder != null) { updates.Add("Holder = @Holder"); parameters.Add(new MySqlParameter("@Holder", request.Holder)); }
        if (request.HolderPhone != null) { updates.Add("HolderPhone = @HolderPhone"); parameters.Add(new MySqlParameter("@HolderPhone", request.HolderPhone)); }
        if (request.LastBorrowTime.HasValue) { updates.Add("LastBorrowTime = @LastBorrowTime"); parameters.Add(new MySqlParameter("@LastBorrowTime", request.LastBorrowTime.Value)); }
        if (request.BorrowCount.HasValue) { updates.Add("BorrowCount = @BorrowCount"); parameters.Add(new MySqlParameter("@BorrowCount", request.BorrowCount.Value)); }
        if (request.Photo != null) { updates.Add("Photo = @Photo"); parameters.Add(new MySqlParameter("@Photo", request.Photo)); }
        if (request.Remark != null) { updates.Add("Remark = @Remark"); parameters.Add(new MySqlParameter("@Remark", request.Remark)); }

        var sql = $"UPDATE `keys` SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新钥匙: {Id}", id);
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM `keys` WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "钥匙不存在" });
        _logger.LogInformation("删除钥匙: {Id}", id);
        return Ok(new { success = true, message = "已删除" });
    }
}

public class CreateKeyRequest
{
    public string? KeyNo { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? DoorNo { get; set; }
    public int Quantity { get; set; }
    public string? Status { get; set; }
    public string? Holder { get; set; }
    public string? HolderPhone { get; set; }
    public DateTime? LastBorrowTime { get; set; }
    public int BorrowCount { get; set; }
    public string? Photo { get; set; }
    public string? Remark { get; set; }
}

public class UpdateKeyRequest
{
    public string? KeyNo { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? DoorNo { get; set; }
    public int? Quantity { get; set; }
    public string? Status { get; set; }
    public string? Holder { get; set; }
    public string? HolderPhone { get; set; }
    public DateTime? LastBorrowTime { get; set; }
    public int? BorrowCount { get; set; }
    public string? Photo { get; set; }
    public string? Remark { get; set; }
}