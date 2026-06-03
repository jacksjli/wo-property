using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 快递管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/express-records")]
public class ExpressController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<ExpressController> _logger;

    public ExpressController(MySqlConnection db, ILogger<ExpressController> logger)
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
            conditions.Add("e.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(e.RecipientName LIKE @keyword OR e.RecipientPhone LIKE @keyword OR e.TrackingNumber LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM ExpressRecords e {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT e.*, r.RoomNumber, b.Name as BuildingName
            FROM ExpressRecords e
            LEFT JOIN Rooms r ON e.RoomId = r.Id
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            {whereClause}
            ORDER BY e.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<ExpressRecordResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapToResponse(reader));

        return Ok(new ExpressListResponse
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
            SELECT e.*, r.RoomNumber, b.Name as BuildingName
            FROM ExpressRecords e
            LEFT JOIN Rooms r ON e.RoomId = r.Id
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            WHERE e.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        return NotFound(new { Success = false, Message = "快递记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpressRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Rooms WHERE Id = @roomId", _db))
        {
            checkCmd.Parameters.AddWithValue("@roomId", request.RoomId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (!exists)
                return BadRequest(new { Success = false, Message = "房间不存在" });
        }

        var insertSql = @"INSERT INTO ExpressRecords
            (RoomId, RecipientName, RecipientPhone, CourierCompany, TrackingNumber, PickupCode, Status, Remarks, CreatedAt)
            VALUES (@RoomId, @RecipientName, @RecipientPhone, @CourierCompany, @TrackingNumber, @PickupCode, 'pending', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@RoomId", request.RoomId);
        cmd.Parameters.AddWithValue("@RecipientName", request.RecipientName);
        cmd.Parameters.AddWithValue("@RecipientPhone", (object)request.RecipientPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CourierCompany", (object)request.CourierCompany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TrackingNumber", (object)request.TrackingNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PickupCode", (object)request.PickupCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建快递记录: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "快递登记成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpressRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM ExpressRecords WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "快递记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "pending", "informed", "picked", "returned" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (request.PickupTime.HasValue)
        {
            updates.Add("PickupTime = @pickupTime");
            parameters.Add(new MySqlParameter("@pickupTime", request.PickupTime.Value));
        }

        if (request.RecipientName != null)
        {
            updates.Add("RecipientName = @recipientName");
            parameters.Add(new MySqlParameter("@recipientName", request.RecipientName));
        }

        if (request.RecipientPhone != null)
        {
            updates.Add("RecipientPhone = @recipientPhone");
            parameters.Add(new MySqlParameter("@recipientPhone", request.RecipientPhone));
        }

        if (request.Remarks != null)
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE ExpressRecords SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新快递记录: {Id}", id);
        return Ok(new { Success = true, Message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM ExpressRecords WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { Success = false, Message = "快递记录不存在" });
        _logger.LogInformation("删除快递记录: {Id}", id);
        return Ok(new { Success = true, Message = "已删除" });
    }

    private static ExpressRecordResponse MapToResponse(MySqlDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        RoomId = Convert.ToInt32(reader["RoomId"]),
        RoomNumber = reader["RoomNumber"]?.ToString(),
        BuildingName = reader["BuildingName"]?.ToString(),
        RecipientName = reader["RecipientName"].ToString() ?? "",
        RecipientPhone = reader["RecipientPhone"] as string,
        CourierCompany = reader["CourierCompany"] as string,
        TrackingNumber = reader["TrackingNumber"] as string,
        PickupCode = reader["PickupCode"] as string,
        Status = reader["Status"].ToString() ?? "pending",
        PickupTime = reader["PickupTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["PickupTime"]),
        Remarks = reader["Remarks"] as string,
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
        UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
    };
}

public class ExpressRecordResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public string? BuildingName { get; set; }
    public string RecipientName { get; set; } = "";
    public string? RecipientPhone { get; set; }
    public string? CourierCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public string? PickupCode { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? PickupTime { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ExpressListResponse
{
    public bool Success { get; set; } = true;
    public List<ExpressRecordResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateExpressRequest
{
    public int RoomId { get; set; }
    public string RecipientName { get; set; } = "";
    public string? RecipientPhone { get; set; }
    public string? CourierCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public string? PickupCode { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateExpressRequest
{
    public string? Status { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public DateTime? PickupTime { get; set; }
    public string? Remarks { get; set; }
}
