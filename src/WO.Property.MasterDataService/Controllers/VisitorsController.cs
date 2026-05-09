using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 访客管理API
/// </summary>
[ApiController]
[Route("api/visitors")]
public class VisitorsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<VisitorsController> _logger;

    public VisitorsController(MySqlConnection db, ILogger<VisitorsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取访客列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? visitPurpose,
        [FromQuery] DateTime? visitDate,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("v.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(visitPurpose) && visitPurpose != "all")
        {
            conditions.Add("v.VisitPurpose = @visitPurpose");
            parameters.Add(new MySqlParameter("@visitPurpose", visitPurpose));
        }

        if (visitDate.HasValue)
        {
            conditions.Add("v.VisitDate = @visitDate");
            parameters.Add(new MySqlParameter("@visitDate", visitDate.Value.Date));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(v.VisitorName LIKE @keyword OR v.VisitorPhone LIKE @keyword OR v.HostName LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Visitors v {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT v.*, b.Name as BuildingName, r.RoomNumber
            FROM Visitors v
            LEFT JOIN Buildings b ON v.BuildingId = b.Id
            LEFT JOIN Rooms r ON v.RoomId = r.Id
            {whereClause}
            ORDER BY v.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<VisitorResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new VisitorListResponse
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
            SELECT v.*, b.Name as BuildingName, r.RoomNumber
            FROM Visitors v
            LEFT JOIN Buildings b ON v.BuildingId = b.Id
            LEFT JOIN Rooms r ON v.RoomId = r.Id
            WHERE v.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "访客记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVisitorRequest request)
    {
        if (string.IsNullOrEmpty(request.VisitorName))
            return BadRequest(new { Success = false, Message = "访客姓名不能为空" });

        var insertSql = @"INSERT INTO Visitors
            (VisitorName, VisitorPhone, IdCardNumber, VisitPurpose, VisitDate, VisitTime, LeaveTime, BuildingId, RoomId, HostName, HostPhone, Status, Remarks, CreatedAt)
            VALUES (@VisitorName, @VisitorPhone, @IdCardNumber, @VisitPurpose, @VisitDate, @VisitTime, @LeaveTime, @BuildingId, @RoomId, @HostName, @HostPhone, 'checked_in', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@VisitorName", request.VisitorName);
        cmd.Parameters.AddWithValue("@VisitorPhone", (object)request.VisitorPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IdCardNumber", (object)request.IdCardNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@VisitPurpose", (object)request.VisitPurpose ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@VisitDate", (object)request.VisitDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@VisitTime", (object)request.VisitTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LeaveTime", (object)request.LeaveTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BuildingId", (object)request.BuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RoomId", (object)request.RoomId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HostName", (object)request.HostName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HostPhone", (object)request.HostPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建访客记录: {Id} - {VisitorName}", id, request.VisitorName);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "访客记录创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVisitorRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM Visitors WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "访客记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "checked_in", "checked_out", "cancelled" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (request.LeaveTime.HasValue)
        {
            updates.Add("LeaveTime = @leaveTime");
            parameters.Add(new MySqlParameter("@leaveTime", request.LeaveTime.Value));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE Visitors SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新访客记录: {Id}", id);
        return Ok(new { Success = true, Message = "访客记录更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Visitors WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "访客记录不存在" });

        _logger.LogInformation("删除访客记录: {Id}", id);
        return Ok(new { Success = true, Message = "访客记录已删除" });
    }

    private static VisitorResponse MapToResponse(MySqlDataReader reader)
    {
        return new VisitorResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            VisitorName = reader["VisitorName"].ToString() ?? "",
            VisitorPhone = reader["VisitorPhone"] as string,
            IdCardNumber = reader["IdCardNumber"] as string,
            VisitPurpose = reader["VisitPurpose"] as string,
            VisitDate = reader["VisitDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["VisitDate"]),
            VisitTime = reader["VisitTime"] as TimeSpan?,
            LeaveTime = reader["LeaveTime"] as TimeSpan?,
            BuildingId = reader["BuildingId"] == DBNull.Value ? null : Convert.ToInt32(reader["BuildingId"]),
            RoomId = reader["RoomId"] == DBNull.Value ? null : Convert.ToInt32(reader["RoomId"]),
            BuildingName = reader["BuildingName"] as string,
            RoomNumber = reader["RoomNumber"] as string,
            HostName = reader["HostName"] as string,
            HostPhone = reader["HostPhone"] as string,
            Status = reader["Status"].ToString() ?? "checked_in",
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class VisitorResponse
{
    public int Id { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? IdCardNumber { get; set; }
    public string? VisitPurpose { get; set; }
    public DateTime? VisitDate { get; set; }
    public TimeSpan? VisitTime { get; set; }
    public TimeSpan? LeaveTime { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public string? HostName { get; set; }
    public string? HostPhone { get; set; }
    public string Status { get; set; } = "checked_in";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class VisitorListResponse
{
    public bool Success { get; set; } = true;
    public List<VisitorResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateVisitorRequest
{
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public string? IdCardNumber { get; set; }
    public string? VisitPurpose { get; set; }
    public DateTime? VisitDate { get; set; }
    public TimeSpan? VisitTime { get; set; }
    public TimeSpan? LeaveTime { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? HostName { get; set; }
    public string? HostPhone { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateVisitorRequest
{
    public string? Status { get; set; }
    public TimeSpan? LeaveTime { get; set; }
    public string? Remarks { get; set; }
}
