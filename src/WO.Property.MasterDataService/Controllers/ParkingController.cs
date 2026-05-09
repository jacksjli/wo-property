using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 车位管理API
/// </summary>
[ApiController]
[Route("api/parking-records")]
public class ParkingController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<ParkingController> _logger;

    public ParkingController(MySqlConnection db, ILogger<ParkingController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取车位列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? spaceType,
        [FromQuery] int? buildingId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("p.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(spaceType) && spaceType != "all")
        {
            conditions.Add("p.SpaceType = @spaceType");
            parameters.Add(new MySqlParameter("@spaceType", spaceType));
        }

        if (buildingId.HasValue)
        {
            conditions.Add("p.BuildingId = @buildingId");
            parameters.Add(new MySqlParameter("@buildingId", buildingId.Value));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(p.ParkingSpaceNumber LIKE @keyword OR p.LicensePlate LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM ParkingRecords p {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT p.*, b.Name as BuildingName, r.Name as ResidentName
            FROM ParkingRecords p
            LEFT JOIN Buildings b ON p.BuildingId = b.Id
            LEFT JOIN Residents r ON p.ResidentId = r.Id
            {whereClause}
            ORDER BY p.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<ParkingResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new ParkingListResponse
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
            SELECT p.*, b.Name as BuildingName, r.Name as ResidentName
            FROM ParkingRecords p
            LEFT JOIN Buildings b ON p.BuildingId = b.Id
            LEFT JOIN Residents r ON p.ResidentId = r.Id
            WHERE p.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "车位记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParkingRequest request)
    {
        if (string.IsNullOrEmpty(request.ParkingSpaceNumber))
            return BadRequest(new { Success = false, Message = "车位编号不能为空" });

        var insertSql = @"INSERT INTO ParkingRecords
            (ParkingSpaceNumber, BuildingId, Floor, SpaceType, LicensePlate, ResidentId, StartDate, EndDate, MonthlyFee, Status, Remarks, CreatedAt)
            VALUES (@ParkingSpaceNumber, @BuildingId, @Floor, @SpaceType, @LicensePlate, @ResidentId, @StartDate, @EndDate, @MonthlyFee, @Status, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@ParkingSpaceNumber", request.ParkingSpaceNumber);
        cmd.Parameters.AddWithValue("@BuildingId", (object)request.BuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Floor", (object)request.Floor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SpaceType", request.SpaceType ?? "regular");
        cmd.Parameters.AddWithValue("@LicensePlate", (object)request.LicensePlate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ResidentId", (object)request.ResidentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartDate", (object)request.StartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EndDate", (object)request.EndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MonthlyFee", (object)request.MonthlyFee ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", request.Status ?? "available");
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建车位记录: {Id} - {ParkingSpaceNumber}", id, request.ParkingSpaceNumber);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "车位记录创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateParkingRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM ParkingRecords WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "车位记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "occupied", "available", "reserved" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.LicensePlate))
        {
            updates.Add("LicensePlate = @licensePlate");
            parameters.Add(new MySqlParameter("@licensePlate", request.LicensePlate));
        }

        if (request.ResidentId.HasValue)
        {
            updates.Add("ResidentId = @residentId");
            parameters.Add(new MySqlParameter("@residentId", request.ResidentId.Value));
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

        if (request.MonthlyFee.HasValue)
        {
            updates.Add("MonthlyFee = @monthlyFee");
            parameters.Add(new MySqlParameter("@monthlyFee", request.MonthlyFee.Value));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE ParkingRecords SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新车位记录: {Id}", id);
        return Ok(new { Success = true, Message = "车位记录更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM ParkingRecords WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "车位记录不存在" });

        _logger.LogInformation("删除车位记录: {Id}", id);
        return Ok(new { Success = true, Message = "车位记录已删除" });
    }

    private static ParkingResponse MapToResponse(MySqlDataReader reader)
    {
        return new ParkingResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            ParkingSpaceNumber = reader["ParkingSpaceNumber"].ToString() ?? "",
            BuildingId = reader["BuildingId"] == DBNull.Value ? null : Convert.ToInt32(reader["BuildingId"]),
            BuildingName = reader["BuildingName"] as string,
            Floor = reader["Floor"] == DBNull.Value ? null : Convert.ToInt32(reader["Floor"]),
            SpaceType = reader["SpaceType"].ToString() ?? "regular",
            LicensePlate = reader["LicensePlate"] as string,
            ResidentId = reader["ResidentId"] == DBNull.Value ? null : Convert.ToInt32(reader["ResidentId"]),
            ResidentName = reader["ResidentName"] as string,
            StartDate = reader["StartDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartDate"]),
            EndDate = reader["EndDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["EndDate"]),
            MonthlyFee = reader["MonthlyFee"] == DBNull.Value ? null : Convert.ToDecimal(reader["MonthlyFee"]),
            Status = reader["Status"].ToString() ?? "available",
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class ParkingResponse
{
    public int Id { get; set; }
    public string ParkingSpaceNumber { get; set; } = string.Empty;
    public int? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public int? Floor { get; set; }
    public string SpaceType { get; set; } = "regular";
    public string? LicensePlate { get; set; }
    public int? ResidentId { get; set; }
    public string? ResidentName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MonthlyFee { get; set; }
    public string Status { get; set; } = "available";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ParkingListResponse
{
    public bool Success { get; set; } = true;
    public List<ParkingResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateParkingRequest
{
    public string ParkingSpaceNumber { get; set; } = string.Empty;
    public int? BuildingId { get; set; }
    public int? Floor { get; set; }
    public string? SpaceType { get; set; }
    public string? LicensePlate { get; set; }
    public int? ResidentId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MonthlyFee { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateParkingRequest
{
    public string? Status { get; set; }
    public string? LicensePlate { get; set; }
    public int? ResidentId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MonthlyFee { get; set; }
    public string? Remarks { get; set; }
}
