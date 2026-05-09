using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 工单管理API
/// </summary>
[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(MySqlConnection db, ILogger<TicketsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取工单列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("t.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(priority) && priority != "all")
        {
            conditions.Add("t.Priority = @priority");
            parameters.Add(new MySqlParameter("@priority", priority));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(t.Title LIKE @keyword OR t.TicketNumber LIKE @keyword OR t.ReporterName LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Tickets t {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT t.*, b.Name as BuildingName, r.RoomNumber
            FROM Tickets t
            LEFT JOIN Buildings b ON t.BuildingId = b.Id
            LEFT JOIN Rooms r ON t.RoomId = r.Id
            {whereClause}
            ORDER BY t.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<TicketResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new TicketListResponse
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
            SELECT t.*, b.Name as BuildingName, r.RoomNumber
            FROM Tickets t
            LEFT JOIN Buildings b ON t.BuildingId = b.Id
            LEFT JOIN Rooms r ON t.RoomId = r.Id
            WHERE t.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "工单不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request)
    {
        if (string.IsNullOrEmpty(request.Title))
            return BadRequest(new { Success = false, Message = "工单标题不能为空" });

        var ticketNumber = "TK" + DateTime.UtcNow.ToString("yyyyMMdd") + new Random().Next(1000, 9999);

        var insertSql = @"INSERT INTO Tickets
            (TicketNumber, Title, Description, TicketType, Priority, Status, ReporterName, ReporterPhone, HandlerId, AssignedTo, BuildingId, RoomId, Source, Pictures, Remarks, CreatedAt)
            VALUES (@TicketNumber, @Title, @Description, @TicketType, @Priority, 'pending', @ReporterName, @ReporterPhone, @HandlerId, @AssignedTo, @BuildingId, @RoomId, @Source, @Pictures, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@TicketNumber", ticketNumber);
        cmd.Parameters.AddWithValue("@Title", request.Title);
        cmd.Parameters.AddWithValue("@Description", (object)request.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TicketType", (object)request.TicketType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Priority", request.Priority ?? "normal");
        cmd.Parameters.AddWithValue("@ReporterName", (object)request.ReporterName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ReporterPhone", (object)request.ReporterPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HandlerId", (object)request.HandlerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AssignedTo", (object)request.AssignedTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BuildingId", (object)request.BuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RoomId", (object)request.RoomId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Source", request.Source ?? "user_report");
        cmd.Parameters.AddWithValue("@Pictures", (object)request.Pictures ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建工单: {Id} - {TicketNumber}", id, ticketNumber);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "工单创建成功",
            Data = new { Id = id, TicketNumber = ticketNumber }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM Tickets WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "工单不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "pending", "assigned", "in_progress", "completed", "cancelled", "closed" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.Priority))
        {
            updates.Add("Priority = @priority");
            parameters.Add(new MySqlParameter("@priority", request.Priority));
        }

        if (!string.IsNullOrEmpty(request.AssignedTo))
        {
            updates.Add("AssignedTo = @assignedTo");
            parameters.Add(new MySqlParameter("@assignedTo", request.AssignedTo));
        }

        if (request.HandlerId.HasValue)
        {
            updates.Add("HandlerId = @handlerId");
            parameters.Add(new MySqlParameter("@handlerId", request.HandlerId.Value));
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            updates.Add("Description = @description");
            parameters.Add(new MySqlParameter("@description", request.Description));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE Tickets SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新工单: {Id}", id);
        return Ok(new { Success = true, Message = "工单更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Tickets WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "工单不存在" });

        _logger.LogInformation("删除工单: {Id}", id);
        return Ok(new { Success = true, Message = "工单已删除" });
    }

    private static TicketResponse MapToResponse(MySqlDataReader reader)
    {
        return new TicketResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            TicketNumber = reader["TicketNumber"].ToString() ?? "",
            Title = reader["Title"].ToString() ?? "",
            Description = reader["Description"] as string,
            TicketType = reader["TicketType"] as string,
            Priority = reader["Priority"].ToString() ?? "normal",
            Status = reader["Status"].ToString() ?? "pending",
            ReporterName = reader["ReporterName"] as string,
            ReporterPhone = reader["ReporterPhone"] as string,
            HandlerId = reader["HandlerId"] == DBNull.Value ? null : Convert.ToInt32(reader["HandlerId"]),
            AssignedTo = reader["AssignedTo"] as string,
            BuildingId = reader["BuildingId"] == DBNull.Value ? null : Convert.ToInt32(reader["BuildingId"]),
            RoomId = reader["RoomId"] == DBNull.Value ? null : Convert.ToInt32(reader["RoomId"]),
            BuildingName = reader["BuildingName"] as string,
            RoomNumber = reader["RoomNumber"] as string,
            Source = reader["Source"].ToString() ?? "user_report",
            Pictures = reader["Pictures"] as string,
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class TicketResponse
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TicketType { get; set; }
    public string Priority { get; set; } = "normal";
    public string Status { get; set; } = "pending";
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public int? HandlerId { get; set; }
    public string? AssignedTo { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public string? Source { get; set; }
    public string? Pictures { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TicketListResponse
{
    public bool Success { get; set; } = true;
    public List<TicketResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TicketType { get; set; }
    public string? Priority { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public int? HandlerId { get; set; }
    public string? AssignedTo { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? Source { get; set; }
    public string? Pictures { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateTicketRequest
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public int? HandlerId { get; set; }
    public string? Description { get; set; }
    public string? Remarks { get; set; }
}
