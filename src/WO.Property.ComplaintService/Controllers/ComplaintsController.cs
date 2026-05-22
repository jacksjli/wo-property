using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.ComplaintService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComplaintsController : ControllerBase
{
    private readonly MySqlConnection _db;

    public ComplaintsController(MySqlConnection db)
    {
        _db = db;
    }

    // ============================================
    // GET /api/complaints - 获取投诉列表
    // ============================================
    [HttpGet]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] string? keyword = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        await _db.OpenAsync();
        
        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(keyword))
        {
            whereClauses.Add("(Title LIKE @keyword OR ComplainantName LIKE @keyword OR ComplainantPhone LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }
        if (!string.IsNullOrEmpty(type))
        {
            whereClauses.Add("Type = @type");
            parameters.Add(new MySqlParameter("@type", type));
        }
        if (!string.IsNullOrEmpty(status))
        {
            whereClauses.Add("HandleStatus = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }
        if (!string.IsNullOrEmpty(priority))
        {
            whereClauses.Add("Priority = @priority");
            parameters.Add(new MySqlParameter("@priority", priority));
        }

        var whereSql = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

        // Count total
        using var countCmd = new MySqlCommand($"SELECT COUNT(*) FROM complaints {whereSql}", _db);
        foreach (var p in parameters) countCmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        // Get paginated data
        var offset = (page - 1) * pageSize;
        var sql = $@"
            SELECT Id, ComplaintNo, Title, Type, Source, Priority, Description,
                   ComplainantName, ComplainantPhone, ComplainantRoom, Location,
                   HandlerName, Deadline, Remark, HandleStatus, HandleProgress,
                   Feedback, Rating, CreatedAt, UpdatedAt
            FROM complaints {whereSql}
            ORDER BY CreatedAt DESC
            LIMIT @limit OFFSET @offset";
        
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", offset);

        var items = new List<ComplaintDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadComplaint(reader));
        }

        return Ok(new { success = true, total, page, pageSize, data = items });
    }

    // ============================================
    // GET /api/complaints/{id} - 获取单个投诉
    // ============================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetComplaint(long id)
    {
        await _db.OpenAsync();
        using var cmd = new MySqlCommand(
            @"SELECT Id, ComplaintNo, Title, Type, Source, Priority, Description,
                     ComplainantName, ComplainantPhone, ComplainantRoom, Location,
                     HandlerName, Deadline, Remark, HandleStatus, HandleProgress,
                     Feedback, Rating, CreatedAt, UpdatedAt
              FROM complaints WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { success = true, data = ReadComplaint(reader) });
        }
        return NotFound(new { success = false, message = "投诉不存在" });
    }

    // ============================================
    // POST /api/complaints - 创建投诉
    // ============================================
    [HttpPost]
    public async Task<IActionResult> CreateComplaint([FromBody] CreateComplaintDto dto)
    {
        await _db.OpenAsync();

        // 生成投诉编号
        var complaintNo = $"CMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var now = DateTime.UtcNow;
        using var cmd = new MySqlCommand(@"
            INSERT INTO complaints (ComplaintNo, Title, Type, Source, Priority, Description,
                                   ComplainantName, ComplainantPhone, ComplainantRoom, Location,
                                   HandleStatus, CreatedAt, UpdatedAt)
            VALUES (@complaintNo, @title, @type, @source, @priority, @description,
                    @complainantName, @complainantPhone, @complainantRoom, @location,
                    'pending', @now, @now);
            SELECT LAST_INSERT_ID();", _db);

        cmd.Parameters.AddWithValue("@complaintNo", complaintNo);
        cmd.Parameters.AddWithValue("@title", dto.Title);
        cmd.Parameters.AddWithValue("@type", dto.Type ?? "service");
        cmd.Parameters.AddWithValue("@source", dto.Source ?? "phone");
        cmd.Parameters.AddWithValue("@priority", dto.Priority ?? "normal");
        cmd.Parameters.AddWithValue("@description", dto.Description ?? "");
        cmd.Parameters.AddWithValue("@complainantName", dto.ComplainantName ?? "");
        cmd.Parameters.AddWithValue("@complainantPhone", dto.ComplainantPhone ?? "");
        cmd.Parameters.AddWithValue("@complainantRoom", dto.ComplainantRoom ?? "");
        cmd.Parameters.AddWithValue("@location", dto.Location ?? "");
        cmd.Parameters.AddWithValue("@now", now);

        var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        var result = new ComplaintDto
        {
            Id = id,
            ComplaintNo = complaintNo,
            Title = dto.Title,
            Type = dto.Type ?? "service",
            Source = dto.Source ?? "phone",
            Priority = dto.Priority ?? "normal",
            Description = dto.Description,
            ComplainantName = dto.ComplainantName,
            ComplainantPhone = dto.ComplainantPhone,
            ComplainantRoom = dto.ComplainantRoom,
            Location = dto.Location,
            HandleStatus = "pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        return Ok(new { success = true, data = result, message = "投诉创建成功" });
    }

    // ============================================
    // PUT /api/complaints/{id} - 更新投诉
    // ============================================
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComplaint(long id, [FromBody] UpdateComplaintDto dto)
    {
        await _db.OpenAsync();

        // 检查是否存在
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM complaints WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count == 0) return NotFound(new { success = false, message = "投诉不存在" });

        var setClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (dto.Title != null) { setClauses.Add("Title = @title"); parameters.Add(new MySqlParameter("@title", dto.Title)); }
        if (dto.Type != null) { setClauses.Add("Type = @type"); parameters.Add(new MySqlParameter("@type", dto.Type)); }
        if (dto.Source != null) { setClauses.Add("Source = @source"); parameters.Add(new MySqlParameter("@source", dto.Source)); }
        if (dto.Priority != null) { setClauses.Add("Priority = @priority"); parameters.Add(new MySqlParameter("@priority", dto.Priority)); }
        if (dto.Description != null) { setClauses.Add("Description = @description"); parameters.Add(new MySqlParameter("@description", dto.Description)); }
        if (dto.HandleStatus != null) { setClauses.Add("HandleStatus = @handleStatus"); parameters.Add(new MySqlParameter("@handleStatus", dto.HandleStatus)); }
        if (dto.HandlerName != null) { setClauses.Add("HandlerName = @handlerName"); parameters.Add(new MySqlParameter("@handlerName", dto.HandlerName)); }
        if (dto.Feedback != null) { setClauses.Add("Feedback = @feedback"); parameters.Add(new MySqlParameter("@feedback", dto.Feedback)); }
        if (dto.HandleProgress != null) { setClauses.Add("HandleProgress = @handleProgress"); parameters.Add(new MySqlParameter("@handleProgress", dto.HandleProgress)); }
        if (dto.Rating.HasValue) { setClauses.Add("Rating = @rating"); parameters.Add(new MySqlParameter("@rating", dto.Rating.Value)); }

        if (setClauses.Count == 0) return BadRequest(new { success = false, message = "没有需要更新的字段" });

        setClauses.Add("UpdatedAt = @updatedAt");
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));
        parameters.Add(new MySqlParameter("@id", id));

        var sql = $"UPDATE complaints SET {string.Join(", ", setClauses)} WHERE Id = @id";
        using var updateCmd = new MySqlCommand(sql, _db);
        updateCmd.Parameters.AddRange(parameters.ToArray());
        await updateCmd.ExecuteNonQueryAsync();

        // 获取更新后的数据
        using var selectCmd = new MySqlCommand(
            @"SELECT Id, ComplaintNo, Title, Type, Source, Priority, Description,
                     ComplainantName, ComplainantPhone, ComplainantRoom, Location,
                     HandlerName, Deadline, Remark, HandleStatus, HandleProgress,
                     Feedback, Rating, CreatedAt, UpdatedAt
              FROM complaints WHERE Id = @id", _db);
        selectCmd.Parameters.AddWithValue("@id", id);
        
        using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { success = true, data = ReadComplaint(reader), message = "更新成功" });
        }
        return NotFound(new { success = false, message = "更新后查询失败" });
    }

    // ============================================
    // DELETE /api/complaints/{id} - 删除投诉
    // ============================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComplaint(long id)
    {
        await _db.OpenAsync();
        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM complaints WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count == 0) return NotFound(new { success = false, message = "投诉不存在" });

        using var cmd = new MySqlCommand("DELETE FROM complaints WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "删除成功" });
    }

    // ============================================
    // GET /api/complaints/stats - 获取统计数据
    // ============================================
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        await _db.OpenAsync();
        
        using var cmd = new MySqlCommand(@"
            SELECT 
                COUNT(*) as total,
                SUM(CASE WHEN HandleStatus = 'pending' THEN 1 ELSE 0 END) as pending,
                SUM(CASE WHEN HandleStatus = 'processing' THEN 1 ELSE 0 END) as processing,
                SUM(CASE WHEN HandleStatus = 'resolved' THEN 1 ELSE 0 END) as resolved,
                SUM(CASE WHEN HandleStatus = 'closed' THEN 1 ELSE 0 END) as closed,
                SUM(CASE WHEN DATE(CreatedAt) = CURDATE() THEN 1 ELSE 0 END) as todayNew,
                SUM(CASE WHEN HandleStatus = 'resolved' AND DATE(UpdatedAt) = CURDATE() THEN 1 ELSE 0 END) as todayResolved
            FROM complaints", _db);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { 
                success = true, 
                data = new {
                    total = reader.GetInt64(0),
                    pending = reader.GetInt64(1),
                    processing = reader.GetInt64(2),
                    resolved = reader.GetInt64(3),
                    closed = reader.GetInt64(4),
                    todayNew = reader.GetInt64(5),
                    todayResolved = reader.GetInt64(6)
                }
            });
        }
        return Ok(new { success = true, data = new { total = 0, pending = 0, processing = 0, resolved = 0, closed = 0, todayNew = 0, todayResolved = 0 } });
    }

    private static ComplaintDto ReadComplaint(MySqlDataReader reader)
    {
        var dto = new ComplaintDto
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            ComplaintNo = reader.GetString(reader.GetOrdinal("ComplaintNo")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? "service" : reader.GetString(reader.GetOrdinal("Type")),
            Source = reader.IsDBNull(reader.GetOrdinal("Source")) ? "phone" : reader.GetString(reader.GetOrdinal("Source")),
            Priority = reader.IsDBNull(reader.GetOrdinal("Priority")) ? "normal" : reader.GetString(reader.GetOrdinal("Priority")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
            ComplainantName = reader.IsDBNull(reader.GetOrdinal("ComplainantName")) ? "" : reader.GetString(reader.GetOrdinal("ComplainantName")),
            ComplainantPhone = reader.IsDBNull(reader.GetOrdinal("ComplainantPhone")) ? "" : reader.GetString(reader.GetOrdinal("ComplainantPhone")),
            ComplainantRoom = reader.IsDBNull(reader.GetOrdinal("ComplainantRoom")) ? "" : reader.GetString(reader.GetOrdinal("ComplainantRoom")),
            Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? "" : reader.GetString(reader.GetOrdinal("Location")),
            HandlerName = reader.IsDBNull(reader.GetOrdinal("HandlerName")) ? null : reader.GetString(reader.GetOrdinal("HandlerName")),
            HandleStatus = reader.IsDBNull(reader.GetOrdinal("HandleStatus")) ? "pending" : reader.GetString(reader.GetOrdinal("HandleStatus")),
            HandleProgress = reader.IsDBNull(reader.GetOrdinal("HandleProgress")) ? null : reader.GetString(reader.GetOrdinal("HandleProgress")),
            Feedback = reader.IsDBNull(reader.GetOrdinal("Feedback")) ? null : reader.GetString(reader.GetOrdinal("Feedback")),
            Rating = reader.IsDBNull(reader.GetOrdinal("Rating")) ? null : reader.GetInt32(reader.GetOrdinal("Rating")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
        return dto;
    }
}

// DTOs - 适配 wo_property.complaints 表结构
public class ComplaintDto
{
    public long Id { get; set; }
    public string ComplaintNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "service";
    public string? Source { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string ComplainantName { get; set; } = "";
    public string? ComplainantPhone { get; set; }
    public string? ComplainantRoom { get; set; }
    public string? Location { get; set; }
    public string? HandlerName { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Remark { get; set; }
    public string HandleStatus { get; set; } = "pending";
    public string? HandleProgress { get; set; }
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateComplaintDto
{
    public string Title { get; set; } = "";
    public string? Type { get; set; }
    public string? Source { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? ComplainantName { get; set; }
    public string? ComplainantPhone { get; set; }
    public string? ComplainantRoom { get; set; }
    public string? Location { get; set; }
}

public class UpdateComplaintDto
{
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? HandleStatus { get; set; }
    public string? HandlerName { get; set; }
    public string? Feedback { get; set; }
    public string? HandleProgress { get; set; }
    public int? Rating { get; set; }
}