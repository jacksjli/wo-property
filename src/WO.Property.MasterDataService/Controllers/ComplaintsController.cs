using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 投诉管理API
/// </summary>
[ApiController]
[Route("api/complaints")]
public class ComplaintsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<ComplaintsController> _logger;

    public ComplaintsController(MySqlConnection db, ILogger<ComplaintsController> logger)
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
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(ComplaintNo LIKE @keyword OR Title LIKE @keyword OR ComplainantName LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        if (!string.IsNullOrEmpty(type) && type != "all")
        {
            conditions.Add("Type = @type");
            parameters.Add(new MySqlParameter("@type", type));
        }

        if (!string.IsNullOrEmpty(priority) && priority != "all")
        {
            conditions.Add("Priority = @priority");
            parameters.Add(new MySqlParameter("@priority", priority));
        }

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("HandleStatus = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM complaints {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT * FROM complaints
            {whereClause}
            ORDER BY CreatedAt DESC
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
        using var cmd = new MySqlCommand("SELECT * FROM complaints WHERE Id = @id", _db);
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
        return NotFound(new { success = false, message = "投诉不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateComplaintRequest request)
    {
        var insertSql = @"INSERT INTO complaints
            (ComplaintNo, Title, Type, Source, Priority, Description, ComplainantName, ComplainantPhone, ComplainantRoom, Location, HandlerName, Deadline, Remark, HandleStatus, CreatedAt)
            VALUES (@ComplaintNo, @Title, @Type, @Source, @Priority, @Description, @ComplainantName, @ComplainantPhone, @ComplainantRoom, @Location, @HandlerName, @Deadline, @Remark, @HandleStatus, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@ComplaintNo", request.ComplaintNo ?? "");
        cmd.Parameters.AddWithValue("@Title", request.Title ?? "");
        cmd.Parameters.AddWithValue("@Type", request.Type ?? "service");
        cmd.Parameters.AddWithValue("@Source", request.Source ?? "phone");
        cmd.Parameters.AddWithValue("@Priority", request.Priority ?? "normal");
        cmd.Parameters.AddWithValue("@Description", request.Description ?? "");
        cmd.Parameters.AddWithValue("@ComplainantName", request.ComplainantName ?? "");
        cmd.Parameters.AddWithValue("@ComplainantPhone", request.ComplainantPhone ?? "");
        cmd.Parameters.AddWithValue("@ComplainantRoom", request.ComplainantRoom ?? "");
        cmd.Parameters.AddWithValue("@Location", request.Location ?? "");
        cmd.Parameters.AddWithValue("@HandlerName", request.HandlerName ?? "");
        cmd.Parameters.AddWithValue("@Deadline", request.Deadline ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Remark", request.Remark ?? "");
        cmd.Parameters.AddWithValue("@HandleStatus", request.HandleStatus ?? "pending");
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建投诉: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            success = true,
            message = "投诉创建成功",
            data = new { id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateComplaintRequest request)
    {
        using var checkCmd = new MySqlCommand("SELECT * FROM complaints WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            reader.Close();
            return NotFound(new { success = false, message = "投诉不存在" });
        }
        reader.Close();

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (request.Title != null) { updates.Add("Title = @Title"); parameters.Add(new MySqlParameter("@Title", request.Title)); }
        if (request.Type != null) { updates.Add("Type = @Type"); parameters.Add(new MySqlParameter("@Type", request.Type)); }
        if (request.Source != null) { updates.Add("Source = @Source"); parameters.Add(new MySqlParameter("@Source", request.Source)); }
        if (request.Priority != null) { updates.Add("Priority = @Priority"); parameters.Add(new MySqlParameter("@Priority", request.Priority)); }
        if (request.Description != null) { updates.Add("Description = @Description"); parameters.Add(new MySqlParameter("@Description", request.Description)); }
        if (request.ComplainantName != null) { updates.Add("ComplainantName = @ComplainantName"); parameters.Add(new MySqlParameter("@ComplainantName", request.ComplainantName)); }
        if (request.ComplainantPhone != null) { updates.Add("ComplainantPhone = @ComplainantPhone"); parameters.Add(new MySqlParameter("@ComplainantPhone", request.ComplainantPhone)); }
        if (request.ComplainantRoom != null) { updates.Add("ComplainantRoom = @ComplainantRoom"); parameters.Add(new MySqlParameter("@ComplainantRoom", request.ComplainantRoom)); }
        if (request.Location != null) { updates.Add("Location = @Location"); parameters.Add(new MySqlParameter("@Location", request.Location)); }
        if (request.HandlerName != null) { updates.Add("HandlerName = @HandlerName"); parameters.Add(new MySqlParameter("@HandlerName", request.HandlerName)); }
        if (request.Deadline.HasValue) { updates.Add("Deadline = @Deadline"); parameters.Add(new MySqlParameter("@Deadline", request.Deadline.Value)); }
        if (request.Remark != null) { updates.Add("Remark = @Remark"); parameters.Add(new MySqlParameter("@Remark", request.Remark)); }
        if (request.HandleStatus != null) { updates.Add("HandleStatus = @HandleStatus"); parameters.Add(new MySqlParameter("@HandleStatus", request.HandleStatus)); }
        if (request.HandleProgress != null) { updates.Add("HandleProgress = @HandleProgress"); parameters.Add(new MySqlParameter("@HandleProgress", request.HandleProgress)); }
        if (request.Feedback != null) { updates.Add("Feedback = @Feedback"); parameters.Add(new MySqlParameter("@Feedback", request.Feedback)); }
        if (request.Rating.HasValue) { updates.Add("Rating = @Rating"); parameters.Add(new MySqlParameter("@Rating", request.Rating.Value)); }

        var sql = $"UPDATE complaints SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新投诉: {Id}", id);
        return Ok(new { success = true, message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM complaints WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { success = false, message = "投诉不存在" });
        _logger.LogInformation("删除投诉: {Id}", id);
        return Ok(new { success = true, message = "已删除" });
    }
}

public class CreateComplaintRequest
{
    public string? ComplaintNo { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? ComplainantName { get; set; }
    public string? ComplainantPhone { get; set; }
    public string? ComplainantRoom { get; set; }
    public string? Location { get; set; }
    public string? HandlerName { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Remark { get; set; }
    public string? HandleStatus { get; set; }
}

public class UpdateComplaintRequest
{
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? ComplainantName { get; set; }
    public string? ComplainantPhone { get; set; }
    public string? ComplainantRoom { get; set; }
    public string? Location { get; set; }
    public string? HandlerName { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Remark { get; set; }
    public string? HandleStatus { get; set; }
    public string? HandleProgress { get; set; }
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
}