using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 配送管理API
/// </summary>
[ApiController]
[Route("api/delivery-requests")]
public class DeliveryController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<DeliveryController> _logger;

    public DeliveryController(MySqlConnection db, ILogger<DeliveryController> logger)
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
            conditions.Add("d.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(d.ResidentName LIKE @keyword OR d.ResidentPhone LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM DeliveryRequests d {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT d.*, r.RoomNumber, b.Name as BuildingName
            FROM DeliveryRequests d
            LEFT JOIN Rooms r ON d.RoomId = r.Id
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            {whereClause}
            ORDER BY d.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<DeliveryRequestResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapToResponse(reader));

        return Ok(new DeliveryListResponse
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
            SELECT d.*, r.RoomNumber, b.Name as BuildingName
            FROM DeliveryRequests d
            LEFT JOIN Rooms r ON d.RoomId = r.Id
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            WHERE d.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        return NotFound(new { Success = false, Message = "配送请求不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Rooms WHERE Id = @roomId", _db))
        {
            checkCmd.Parameters.AddWithValue("@roomId", request.RoomId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (!exists)
                return BadRequest(new { Success = false, Message = "房间不存在" });
        }

        var insertSql = @"INSERT INTO DeliveryRequests
            (RoomId, ResidentName, ResidentPhone, DeliveryCompany, DeliveryType, ItemDescription, Status, Remarks, CreatedAt)
            VALUES (@RoomId, @ResidentName, @ResidentPhone, @DeliveryCompany, @DeliveryType, @ItemDescription, 'pending', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@RoomId", request.RoomId);
        cmd.Parameters.AddWithValue("@ResidentName", request.ResidentName);
        cmd.Parameters.AddWithValue("@ResidentPhone", (object)request.ResidentPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DeliveryCompany", (object)request.DeliveryCompany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DeliveryType", (object)request.DeliveryType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ItemDescription", (object)request.ItemDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建配送请求: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "配送请求创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeliveryRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM DeliveryRequests WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "配送请求不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "pending", "in_transit", "delivered", "failed" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (request.ResidentName != null)
        {
            updates.Add("ResidentName = @residentName");
            parameters.Add(new MySqlParameter("@residentName", request.ResidentName));
        }

        if (request.ResidentPhone != null)
        {
            updates.Add("ResidentPhone = @residentPhone");
            parameters.Add(new MySqlParameter("@residentPhone", request.ResidentPhone));
        }

        if (request.DeliveryCompany != null)
        {
            updates.Add("DeliveryCompany = @deliveryCompany");
            parameters.Add(new MySqlParameter("@deliveryCompany", request.DeliveryCompany));
        }

        if (request.DeliveryType != null)
        {
            updates.Add("DeliveryType = @deliveryType");
            parameters.Add(new MySqlParameter("@deliveryType", request.DeliveryType));
        }

        if (request.ItemDescription != null)
        {
            updates.Add("ItemDescription = @itemDescription");
            parameters.Add(new MySqlParameter("@itemDescription", request.ItemDescription));
        }

        if (request.DeliveryTime.HasValue)
        {
            updates.Add("DeliveryTime = @deliveryTime");
            parameters.Add(new MySqlParameter("@deliveryTime", request.DeliveryTime.Value));
        }

        if (request.Remarks != null)
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE DeliveryRequests SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新配送请求: {Id}", id);
        return Ok(new { Success = true, Message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM DeliveryRequests WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { Success = false, Message = "配送请求不存在" });
        _logger.LogInformation("删除配送请求: {Id}", id);
        return Ok(new { Success = true, Message = "已删除" });
    }

    private static DeliveryRequestResponse MapToResponse(MySqlDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        RoomId = Convert.ToInt32(reader["RoomId"]),
        RoomNumber = reader["RoomNumber"]?.ToString(),
        BuildingName = reader["BuildingName"]?.ToString(),
        ResidentName = reader["ResidentName"].ToString() ?? "",
        ResidentPhone = reader["ResidentPhone"] as string,
        DeliveryCompany = reader["DeliveryCompany"] as string,
        DeliveryType = reader["DeliveryType"] as string,
        ItemDescription = reader["ItemDescription"] as string,
        Status = reader["Status"].ToString() ?? "pending",
        DeliveryTime = reader["DeliveryTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["DeliveryTime"]),
        Remarks = reader["Remarks"] as string,
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
        UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
    };
}

public class DeliveryRequestResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public string? BuildingName { get; set; }
    public string ResidentName { get; set; } = "";
    public string? ResidentPhone { get; set; }
    public string? DeliveryCompany { get; set; }
    public string? DeliveryType { get; set; }
    public string? ItemDescription { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? DeliveryTime { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DeliveryListResponse
{
    public bool Success { get; set; } = true;
    public List<DeliveryRequestResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateDeliveryRequest
{
    public int RoomId { get; set; }
    public string ResidentName { get; set; } = "";
    public string? ResidentPhone { get; set; }
    public string? DeliveryCompany { get; set; }
    public string? DeliveryType { get; set; }
    public string? ItemDescription { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateDeliveryRequest
{
    public string? Status { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentPhone { get; set; }
    public string? DeliveryCompany { get; set; }
    public string? DeliveryType { get; set; }
    public string? ItemDescription { get; set; }
    public DateTime? DeliveryTime { get; set; }
    public string? Remarks { get; set; }
}
