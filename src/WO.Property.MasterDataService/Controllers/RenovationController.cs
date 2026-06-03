using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 装修申请管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/renovation-requests")]
public class RenovationController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<RenovationController> _logger;

    public RenovationController(MySqlConnection db, ILogger<RenovationController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取装修申请列表（分页/过滤）
    /// </summary>
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
            conditions.Add("rr.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(rr.ApplicantName LIKE @keyword OR rr.ApplicantPhone LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM RenovationRequests rr {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT rr.*, r.RoomNumber, b.Name as BuildingName
            FROM RenovationRequests rr
            LEFT JOIN Rooms r ON rr.RoomId = r.Id
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            {whereClause}
            ORDER BY rr.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<RenovationRequestResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new RenovationListResponse
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
        var sql = @"
            SELECT rr.*, r.RoomNumber, b.Name as BuildingName
            FROM RenovationRequests rr
            LEFT JOIN Rooms r ON rr.RoomId = r.Id
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            WHERE rr.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "装修申请不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRenovationRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Rooms WHERE Id = @roomId", _db))
        {
            checkCmd.Parameters.AddWithValue("@roomId", request.RoomId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (!exists)
                return BadRequest(new { Success = false, Message = "房间不存在" });
        }

        var insertSql = @"INSERT INTO RenovationRequests
            (RoomId, ApplicantName, ApplicantPhone, Description, StartDate, EndDate, Status, Remarks, CreatedAt)
            VALUES (@RoomId, @ApplicantName, @ApplicantPhone, @Description, @StartDate, @EndDate, 'pending', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@RoomId", request.RoomId);
        cmd.Parameters.AddWithValue("@ApplicantName", request.ApplicantName);
        cmd.Parameters.AddWithValue("@ApplicantPhone", (object)request.ApplicantPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", request.Description);
        cmd.Parameters.AddWithValue("@StartDate", (object)request.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndDate", (object)request.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建装修申请: {Id} - {ApplicantName}", id, request.ApplicantName);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "装修申请创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRenovationRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM RenovationRequests WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "装修申请不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "pending", "approved", "rejected", "completed" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.ApplicantName))
        {
            updates.Add("ApplicantName = @applicantName");
            parameters.Add(new MySqlParameter("@applicantName", request.ApplicantName));
        }

        if (!string.IsNullOrEmpty(request.ApplicantPhone))
        {
            updates.Add("ApplicantPhone = @applicantPhone");
            parameters.Add(new MySqlParameter("@applicantPhone", request.ApplicantPhone));
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            updates.Add("Description = @description");
            parameters.Add(new MySqlParameter("@description", request.Description));
        }

        if (request.StartDate.HasValue)
        {
            updates.Add("StartDate = @startDate");
            parameters.Add(new MySqlParameter("@startDate", request.StartDate.Value));
        }

        if (request.EndDate.HasValue)
        {
            updates.Add("EndDate = @endDate");
            parameters.Add(new MySqlParameter("@endDate", request.EndDate.Value));
        }

        if (request.Remarks != null)
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE RenovationRequests SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新装修申请: {Id}", id);
        return Ok(new { Success = true, Message = "装修申请更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM RenovationRequests WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "装修申请不存在" });

        _logger.LogInformation("删除装修申请: {Id}", id);
        return Ok(new { Success = true, Message = "装修申请已删除" });
    }

    private static RenovationRequestResponse MapToResponse(MySqlDataReader reader)
    {
        return new RenovationRequestResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            RoomId = Convert.ToInt32(reader["RoomId"]),
            RoomNumber = reader["RoomNumber"]?.ToString(),
            BuildingName = reader["BuildingName"]?.ToString(),
            ApplicantName = reader["ApplicantName"].ToString() ?? "",
            ApplicantPhone = reader["ApplicantPhone"] as string,
            Description = reader["Description"].ToString() ?? "",
            StartDate = reader["StartDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartDate"]),
            EndDate = reader["EndDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["EndDate"]),
            Status = reader["Status"].ToString() ?? "pending",
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class RenovationRequestResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public string? BuildingName { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantPhone { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RenovationListResponse
{
    public bool Success { get; set; } = true;
    public List<RenovationRequestResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateRenovationRequest
{
    public int RoomId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantPhone { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateRenovationRequest
{
    public string? Status { get; set; }
    public string? ApplicantName { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Remarks { get; set; }
}