using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 住户管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/residents")]
public class ResidentsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<ResidentsController> _logger;

    public ResidentsController(MySqlConnection db, ILogger<ResidentsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取住户列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? residentType,
        [FromQuery] int? buildingId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("r.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(residentType) && residentType != "all")
        {
            conditions.Add("r.ResidentType = @residentType");
            parameters.Add(new MySqlParameter("@residentType", residentType));
        }

        if (buildingId.HasValue)
        {
            conditions.Add("r.BuildingId = @buildingId");
            parameters.Add(new MySqlParameter("@buildingId", buildingId.Value));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(r.Name LIKE @keyword OR r.Phone LIKE @keyword OR r.IdCardNumber LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM Residents r {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT r.*, b.Name as BuildingName, rm.RoomNumber
            FROM Residents r
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            LEFT JOIN Rooms rm ON r.RoomId = rm.Id
            {whereClause}
            ORDER BY r.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<ResidentResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new ResidentListResponse
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
            SELECT r.*, b.Name as BuildingName, rm.RoomNumber
            FROM Residents r
            LEFT JOIN Buildings b ON r.BuildingId = b.Id
            LEFT JOIN Rooms rm ON r.RoomId = rm.Id
            WHERE r.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "住户不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResidentRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { Success = false, Message = "住户姓名不能为空" });

        var insertSql = @"INSERT INTO Residents
            (Name, Phone, IdCardNumber, BuildingId, RoomId, ResidentType, CheckInDate, Status, Remarks, CreatedAt)
            VALUES (@Name, @Phone, @IdCardNumber, @BuildingId, @RoomId, @ResidentType, @CheckInDate, 'active', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@Name", request.Name);
        cmd.Parameters.AddWithValue("@Phone", (object)request.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IdCardNumber", (object)request.IdCardNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BuildingId", (object)request.BuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RoomId", (object)request.RoomId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ResidentType", request.ResidentType ?? "owner");
        cmd.Parameters.AddWithValue("@CheckInDate", (object)request.CheckInDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建住户: {Id} - {Name}", id, request.Name);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "住户创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateResidentRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM Residents WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "住户不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "active", "inactive" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.ResidentType))
        {
            var validTypes = new[] { "owner", "tenant", "family" };
            if (!validTypes.Contains(request.ResidentType))
                return BadRequest(new { Success = false, Message = $"无效的住户类型: {request.ResidentType}" });
            updates.Add("ResidentType = @residentType");
            parameters.Add(new MySqlParameter("@residentType", request.ResidentType));
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            updates.Add("Name = @name");
            parameters.Add(new MySqlParameter("@name", request.Name));
        }

        if (!string.IsNullOrEmpty(request.Phone))
        {
            updates.Add("Phone = @phone");
            parameters.Add(new MySqlParameter("@phone", request.Phone));
        }

        if (request.BuildingId.HasValue)
        {
            updates.Add("BuildingId = @buildingId");
            parameters.Add(new MySqlParameter("@buildingId", request.BuildingId.Value));
        }

        if (request.RoomId.HasValue)
        {
            updates.Add("RoomId = @roomId");
            parameters.Add(new MySqlParameter("@roomId", request.RoomId.Value));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE Residents SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新住户: {Id}", id);
        return Ok(new { Success = true, Message = "住户更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM Residents WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "住户不存在" });

        _logger.LogInformation("删除住户: {Id}", id);
        return Ok(new { Success = true, Message = "住户已删除" });
    }

    private static ResidentResponse MapToResponse(MySqlDataReader reader)
    {
        return new ResidentResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            Name = reader["Name"].ToString() ?? "",
            Phone = reader["Phone"] as string,
            IdCardNumber = reader["IdCardNumber"] as string,
            BuildingId = reader["BuildingId"] == DBNull.Value ? null : Convert.ToInt32(reader["BuildingId"]),
            RoomId = reader["RoomId"] == DBNull.Value ? null : Convert.ToInt32(reader["RoomId"]),
            BuildingName = reader["BuildingName"] as string,
            RoomNumber = reader["RoomNumber"] as string,
            ResidentType = reader["ResidentType"].ToString() ?? "owner",
            CheckInDate = reader["CheckInDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["CheckInDate"]),
            Status = reader["Status"].ToString() ?? "active",
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class ResidentResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdCardNumber { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public string ResidentType { get; set; } = "owner";
    public DateTime? CheckInDate { get; set; }
    public string Status { get; set; } = "active";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ResidentListResponse
{
    public bool Success { get; set; } = true;
    public List<ResidentResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateResidentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdCardNumber { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? ResidentType { get; set; }
    public DateTime? CheckInDate { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateResidentRequest
{
    public string? Status { get; set; }
    public string? ResidentType { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? Remarks { get; set; }
}
