using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 社区管理API
/// </summary>
[ApiController]
[Route("api/community-activities")]
public class CommunityController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(MySqlConnection db, ILogger<CommunityController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("c.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(c.ActivityTitle LIKE @keyword OR c.Organizer LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM CommunityActivities c {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT c.* FROM CommunityActivities c
            {whereClause}
            ORDER BY c.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<CommunityActivityResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapToResponse(reader));

        return Ok(new CommunityListResponse
        {
            Success = true,
            Data = items,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sql = "SELECT * FROM CommunityActivities WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        return NotFound(new { Success = false, Message = "社区活动不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommunityRequest request)
    {
        if (string.IsNullOrEmpty(request.ActivityTitle))
            return BadRequest(new { Success = false, Message = "活动标题不能为空" });

        var insertSql = @"INSERT INTO CommunityActivities
            (ActivityTitle, ActivityType, Description, Organizer, Location, StartTime, EndTime, MaxParticipants, Status, Remarks, CreatedAt)
            VALUES (@ActivityTitle, @ActivityType, @Description, @Organizer, @Location, @StartTime, @EndTime, @MaxParticipants, 'planned', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@ActivityTitle", request.ActivityTitle);
        cmd.Parameters.AddWithValue("@ActivityType", (object)request.ActivityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object)request.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Organizer", (object)request.Organizer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Location", (object)request.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartTime", (object)request.StartTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndTime", (object)request.EndTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxParticipants", (object)request.MaxParticipants ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建社区活动: {Id} - {Title}", id, request.ActivityTitle);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "社区活动创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCommunityRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM CommunityActivities WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "社区活动不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "planned", "ongoing", "completed", "cancelled" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (request.ActivityTitle != null)
        {
            updates.Add("ActivityTitle = @activityTitle");
            parameters.Add(new MySqlParameter("@activityTitle", request.ActivityTitle));
        }

        if (request.ActivityType != null)
        {
            updates.Add("ActivityType = @activityType");
            parameters.Add(new MySqlParameter("@activityType", request.ActivityType));
        }

        if (request.Description != null)
        {
            updates.Add("Description = @description");
            parameters.Add(new MySqlParameter("@description", request.Description));
        }

        if (request.Organizer != null)
        {
            updates.Add("Organizer = @organizer");
            parameters.Add(new MySqlParameter("@organizer", request.Organizer));
        }

        if (request.Location != null)
        {
            updates.Add("Location = @location");
            parameters.Add(new MySqlParameter("@location", request.Location));
        }

        if (request.StartTime.HasValue)
        {
            updates.Add("StartTime = @startTime");
            parameters.Add(new MySqlParameter("@startTime", request.StartTime.Value));
        }

        if (request.EndTime.HasValue)
        {
            updates.Add("EndTime = @endTime");
            parameters.Add(new MySqlParameter("@endTime", request.EndTime.Value));
        }

        if (request.ParticipantCount.HasValue)
        {
            updates.Add("ParticipantCount = @participantCount");
            parameters.Add(new MySqlParameter("@participantCount", request.ParticipantCount.Value));
        }

        if (request.MaxParticipants.HasValue)
        {
            updates.Add("MaxParticipants = @maxParticipants");
            parameters.Add(new MySqlParameter("@maxParticipants", request.MaxParticipants.Value));
        }

        if (request.Remarks != null)
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE CommunityActivities SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新社区活动: {Id}", id);
        return Ok(new { Success = true, Message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM CommunityActivities WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { Success = false, Message = "社区活动不存在" });
        _logger.LogInformation("删除社区活动: {Id}", id);
        return Ok(new { Success = true, Message = "已删除" });
    }

    private static CommunityActivityResponse MapToResponse(MySqlDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        ActivityTitle = reader["ActivityTitle"].ToString() ?? "",
        ActivityType = reader["ActivityType"] as string,
        Description = reader["Description"] as string,
        Organizer = reader["Organizer"] as string,
        Location = reader["Location"] as string,
        StartTime = reader["StartTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartTime"]),
        EndTime = reader["EndTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["EndTime"]),
        Status = reader["Status"].ToString() ?? "planned",
        ParticipantCount = Convert.ToInt32(reader["ParticipantCount"]),
        MaxParticipants = reader["MaxParticipants"] == DBNull.Value ? null : Convert.ToInt32(reader["MaxParticipants"]),
        Remarks = reader["Remarks"] as string,
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
        UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
    };
}

public class CommunityActivityResponse
{
    public int Id { get; set; }
    public string ActivityTitle { get; set; } = "";
    public string? ActivityType { get; set; }
    public string? Description { get; set; }
    public string? Organizer { get; set; }
    public string? Location { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "planned";
    public int ParticipantCount { get; set; }
    public int? MaxParticipants { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CommunityListResponse
{
    public bool Success { get; set; } = true;
    public List<CommunityActivityResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateCommunityRequest
{
    public string ActivityTitle { get; set; } = "";
    public string? ActivityType { get; set; }
    public string? Description { get; set; }
    public string? Organizer { get; set; }
    public string? Location { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxParticipants { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateCommunityRequest
{
    public string? Status { get; set; }
    public string? ActivityTitle { get; set; }
    public string? ActivityType { get; set; }
    public string? Description { get; set; }
    public string? Organizer { get; set; }
    public string? Location { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? ParticipantCount { get; set; }
    public int? MaxParticipants { get; set; }
    public string? Remarks { get; set; }
}
