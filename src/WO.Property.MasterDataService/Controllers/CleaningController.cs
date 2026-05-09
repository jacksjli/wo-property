using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 清洁管理API
/// </summary>
[ApiController]
[Route("api/cleaning-records")]
public class CleaningController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<CleaningController> _logger;

    public CleaningController(MySqlConnection db, ILogger<CleaningController> logger)
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
            conditions.Add("(c.CleanerName LIKE @keyword OR c.CleaningArea LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM CleaningRecords c {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT c.*, b.Name as BuildingName
            FROM CleaningRecords c
            LEFT JOIN Buildings b ON c.BuildingId = b.Id
            {whereClause}
            ORDER BY c.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<CleaningRecordResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapToResponse(reader));

        return Ok(new CleaningListResponse
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
            SELECT c.*, b.Name as BuildingName
            FROM CleaningRecords c
            LEFT JOIN Buildings b ON c.BuildingId = b.Id
            WHERE c.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        return NotFound(new { Success = false, Message = "清洁记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCleaningRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Buildings WHERE Id = @buildingId", _db))
        {
            checkCmd.Parameters.AddWithValue("@buildingId", request.BuildingId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (!exists)
                return BadRequest(new { Success = false, Message = "楼栋不存在" });
        }

        var insertSql = @"INSERT INTO CleaningRecords
            (BuildingId, CleaningArea, CleanerName, CleaningType, PlanDate, Status, Remarks, CreatedAt)
            VALUES (@BuildingId, @CleaningArea, @CleanerName, @CleaningType, @PlanDate, 'pending', @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@BuildingId", request.BuildingId);
        cmd.Parameters.AddWithValue("@CleaningArea", (object)request.CleaningArea ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CleanerName", (object)request.CleanerName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CleaningType", (object)request.CleaningType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PlanDate", (object)request.PlanDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建清洁记录: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "清洁记录创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCleaningRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM CleaningRecords WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "清洁记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "pending", "in_progress", "completed", "quality_issue" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (request.CleaningArea != null)
        {
            updates.Add("CleaningArea = @cleaningArea");
            parameters.Add(new MySqlParameter("@cleaningArea", request.CleaningArea));
        }

        if (request.CleanerName != null)
        {
            updates.Add("CleanerName = @cleanerName");
            parameters.Add(new MySqlParameter("@cleanerName", request.CleanerName));
        }

        if (request.CleaningType != null)
        {
            updates.Add("CleaningType = @cleaningType");
            parameters.Add(new MySqlParameter("@cleaningType", request.CleaningType));
        }

        if (request.PlanDate.HasValue)
        {
            updates.Add("PlanDate = @planDate");
            parameters.Add(new MySqlParameter("@planDate", request.PlanDate.Value));
        }

        if (request.ActualDate.HasValue)
        {
            updates.Add("ActualDate = @actualDate");
            parameters.Add(new MySqlParameter("@actualDate", request.ActualDate.Value));
        }

        if (request.QualityLevel != null)
        {
            updates.Add("QualityLevel = @qualityLevel");
            parameters.Add(new MySqlParameter("@qualityLevel", request.QualityLevel));
        }

        if (request.Remarks != null)
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE CleaningRecords SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新清洁记录: {Id}", id);
        return Ok(new { Success = true, Message = "更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM CleaningRecords WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return NotFound(new { Success = false, Message = "清洁记录不存在" });
        _logger.LogInformation("删除清洁记录: {Id}", id);
        return Ok(new { Success = true, Message = "已删除" });
    }

    private static CleaningRecordResponse MapToResponse(MySqlDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        BuildingId = Convert.ToInt32(reader["BuildingId"]),
        BuildingName = reader["BuildingName"]?.ToString(),
        CleaningArea = reader["CleaningArea"] as string,
        CleanerName = reader["CleanerName"] as string,
        CleaningType = reader["CleaningType"] as string,
        PlanDate = reader["PlanDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["PlanDate"]),
        ActualDate = reader["ActualDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ActualDate"]),
        Status = reader["Status"].ToString() ?? "pending",
        QualityLevel = reader["QualityLevel"] as string,
        Remarks = reader["Remarks"] as string,
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
        UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
    };
}

public class CleaningRecordResponse
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? CleaningArea { get; set; }
    public string? CleanerName { get; set; }
    public string? CleaningType { get; set; }
    public DateTime? PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? QualityLevel { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CleaningListResponse
{
    public bool Success { get; set; } = true;
    public List<CleaningRecordResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateCleaningRequest
{
    public int BuildingId { get; set; }
    public string? CleaningArea { get; set; }
    public string? CleanerName { get; set; }
    public string? CleaningType { get; set; }
    public DateTime? PlanDate { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateCleaningRequest
{
    public string? Status { get; set; }
    public string? CleaningArea { get; set; }
    public string? CleanerName { get; set; }
    public string? CleaningType { get; set; }
    public DateTime? PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? QualityLevel { get; set; }
    public string? Remarks { get; set; }
}
