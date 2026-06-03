using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 物料管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/materials")]
public class MaterialsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<MaterialsController> _logger;

    public MaterialsController(MySqlConnection db, ILogger<MaterialsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(MaterialNo LIKE @keyword OR Name LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            conditions.Add("Category = @category");
            parameters.Add(new MySqlParameter("@category", category));
        }

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM materials {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT * FROM materials
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
        using var cmd = new MySqlCommand("SELECT * FROM materials WHERE Id = @id", _db);
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
        return NotFound(new { success = false, message = "物料不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaterialRequest request)
    {
        var insertSql = @"INSERT INTO materials
            (MaterialNo, Name, Category, Spec, Unit, Quantity, MinQuantity, Price, Location, Status, Supplier, PurchaseDate, ExpirationDate, LastCheckDate, NextCheckDate, CheckCycle, Remark, CreatedAt)
            VALUES (@MaterialNo, @Name, @Category, @Spec, @Unit, @Quantity, @MinQuantity, @Price, @Location, @Status, @Supplier, @PurchaseDate, @ExpirationDate, @LastCheckDate, @NextCheckDate, @CheckCycle, @Remark, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@MaterialNo", request.MaterialNo ?? "");
        cmd.Parameters.AddWithValue("@Name", request.Name ?? "");
        cmd.Parameters.AddWithValue("@Category", request.Category ?? "repair_parts");
        cmd.Parameters.AddWithValue("@Spec", request.Spec ?? "");
        cmd.Parameters.AddWithValue("@Unit", request.Unit ?? "个");
        cmd.Parameters.AddWithValue("@Quantity", request.Quantity);
        cmd.Parameters.AddWithValue("@MinQuantity", request.MinQuantity);
        cmd.Parameters.AddWithValue("@Price", request.Price);
        cmd.Parameters.AddWithValue("@Location", request.Location ?? "");
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "normal");
        cmd.Parameters.AddWithValue("@Supplier", request.Supplier ?? "");
        cmd.Parameters.AddWithValue("@PurchaseDate", request.PurchaseDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpirationDate", request.ExpirationDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@LastCheckDate", request.LastCheckDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@NextCheckDate", request.NextCheckDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@CheckCycle", request.CheckCycle ?? "monthly");
        cmd.Parameters.AddWithValue("@Remark", request.Remark ?? "");
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建物料: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            success = true,
            message = "物料创建成功",
            data = new { id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMaterialRequest request)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM materials WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            reader.Close();
            return NotFound(new { success = false, message = "物料不存在" });
        }
        reader.Close();

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (request.MaterialNo != null) { updates.Add("MaterialNo = @MaterialNo"); parameters.Add(new MySqlParameter("@MaterialNo", request.MaterialNo)); }
        if (request.Name != null) { updates.Add("Name = @Name"); parameters.Add(new MySqlParameter("@Name", request.Name)); }
        if (request.Category != null) { updates.Add("Category = @Category"); parameters.Add(new MySqlParameter("@Category", request.Category)); }
        if (request.Spec != null) { updates.Add("Spec = @Spec"); parameters.Add(new MySqlParameter("@Spec", request.Spec)); }
        if (request.Unit != null) { updates.Add("Unit = @Unit"); parameters.Add(new MySqlParameter("@Unit", request.Unit)); }
        if (request.Quantity.HasValue) { updates.Add("Quantity = @Quantity"); parameters.Add(new MySqlParameter("@Quantity", request.Quantity.Value)); }
        if (request.MinQuantity.HasValue) { updates.Add("MinQuantity = @MinQuantity"); parameters.Add(new MySqlParameter("@MinQuantity", request.MinQuantity.Value)); }
        if (request.Price.HasValue) { updates.Add("Price = @Price"); parameters.Add(new MySqlParameter("@Price", request.Price.Value)); }
        if (request.Location != null) { updates.Add("Location = @Location"); parameters.Add(new MySqlParameter("@Location", request.Location)); }
        if (request.Status != null) { updates.Add("Status = @Status"); parameters.Add(new MySqlParameter("@Status", request.Status)); }
        if (request.Supplier != null) { updates.Add("Supplier = @Supplier"); parameters.Add(new MySqlParameter("@Supplier", request.Supplier)); }
        if (request.PurchaseDate.HasValue) { updates.Add("PurchaseDate = @PurchaseDate"); parameters.Add(new MySqlParameter("@PurchaseDate", request.PurchaseDate.Value)); }
        if (request.ExpirationDate.HasValue) { updates.Add("ExpirationDate = @ExpirationDate"); parameters.Add(new MySqlParameter("@ExpirationDate", request.ExpirationDate.Value)); }
        if (request.Remark != null) { updates.Add("Remark = @Remark"); parameters.Add(new MySqlParameter("@Remark", request.Remark)); }

        var sql = $"UPDATE materials SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新物料: {Id}", id);
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM materials WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "物料不存在" });
        _logger.LogInformation("删除物料: {Id}", id);
        return Ok(new { success = true, message = "已删除" });
    }
}

public class CreateMaterialRequest
{
    public string? MaterialNo { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public decimal Price { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public string? Supplier { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? LastCheckDate { get; set; }
    public DateTime? NextCheckDate { get; set; }
    public string? CheckCycle { get; set; }
    public string? Remark { get; set; }
}

public class UpdateMaterialRequest
{
    public string? MaterialNo { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Spec { get; set; }
    public string? Unit { get; set; }
    public int? Quantity { get; set; }
    public int? MinQuantity { get; set; }
    public decimal? Price { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public string? Supplier { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Remark { get; set; }
}