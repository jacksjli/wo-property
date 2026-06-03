using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 巡检管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/inspection-records")]
public class InspectionsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<InspectionsController> _logger;

    public InspectionsController(MySqlConnection db, ILogger<InspectionsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取巡检记录列表（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? result,
        [FromQuery] int? buildingId,
        [FromQuery] DateTime? inspectionDate,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("i.Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(result) && result != "all")
        {
            conditions.Add("i.Result = @result");
            parameters.Add(new MySqlParameter("@result", result));
        }

        if (buildingId.HasValue)
        {
            conditions.Add("i.BuildingId = @buildingId");
            parameters.Add(new MySqlParameter("@buildingId", buildingId.Value));
        }

        if (inspectionDate.HasValue)
        {
            conditions.Add("i.InspectionDate = @inspectionDate");
            parameters.Add(new MySqlParameter("@inspectionDate", inspectionDate.Value.Date));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(i.InspectionTitle LIKE @keyword OR i.InspectionArea LIKE @keyword OR i.InspectorName LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM InspectionRecords i {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var dataSql = $@"
            SELECT i.*, b.Name as BuildingName
            FROM InspectionRecords i
            LEFT JOIN Buildings b ON i.BuildingId = b.Id
            {whereClause}
            ORDER BY i.CreatedAt DESC
            LIMIT @offset, @pageSize";

        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<InspectionResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new InspectionListResponse
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
            SELECT i.*, b.Name as BuildingName
            FROM InspectionRecords i
            LEFT JOIN Buildings b ON i.BuildingId = b.Id
            WHERE i.Id = @id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "巡检记录不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInspectionRequest request)
    {
        if (string.IsNullOrEmpty(request.InspectionTitle))
            return BadRequest(new { Success = false, Message = "巡检标题不能为空" });

        var insertSql = @"INSERT INTO InspectionRecords
            (InspectionTitle, BuildingId, InspectionArea, InspectorName, InspectionDate, InspectionTime, Status, Result, Findings, NextInspectionDate, Remarks, CreatedAt)
            VALUES (@InspectionTitle, @BuildingId, @InspectionArea, @InspectorName, @InspectionDate, @InspectionTime, 'pending', NULL, NULL, @NextInspectionDate, @Remarks, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@InspectionTitle", request.InspectionTitle);
        cmd.Parameters.AddWithValue("@BuildingId", (object)request.BuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InspectionArea", (object)request.InspectionArea ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InspectorName", (object)request.InspectorName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InspectionDate", (object)request.InspectionDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InspectionTime", (object)request.InspectionTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NextInspectionDate", (object)request.NextInspectionDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Remarks", (object)request.Remarks ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建巡检记录: {Id} - {InspectionTitle}", id, request.InspectionTitle);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "巡检记录创建成功",
            Data = new { Id = id }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInspectionRequest request)
    {
        using (var checkCmd = new MySqlCommand("SELECT * FROM InspectionRecords WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "巡检记录不存在" });
        }

        var updates = new List<string> { "UpdatedAt = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (!string.IsNullOrEmpty(request.Status))
        {
            var validStatuses = new[] { "pending", "in_progress", "completed", "issue_found" };
            if (!validStatuses.Contains(request.Status))
                return BadRequest(new { Success = false, Message = $"无效的状态: {request.Status}" });
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        if (!string.IsNullOrEmpty(request.Result))
        {
            var validResults = new[] { "pass", "fail" };
            if (!validResults.Contains(request.Result))
                return BadRequest(new { Success = false, Message = $"无效的巡检结果: {request.Result}" });
            updates.Add("Result = @result");
            parameters.Add(new MySqlParameter("@result", request.Result));
        }

        if (!string.IsNullOrEmpty(request.Findings))
        {
            updates.Add("Findings = @findings");
            parameters.Add(new MySqlParameter("@findings", request.Findings));
        }

        if (!string.IsNullOrEmpty(request.InspectorName))
        {
            updates.Add("InspectorName = @inspectorName");
            parameters.Add(new MySqlParameter("@inspectorName", request.InspectorName));
        }

        if (request.NextInspectionDate.HasValue)
        {
            updates.Add("NextInspectionDate = @nextInspectionDate");
            parameters.Add(new MySqlParameter("@nextInspectionDate", request.NextInspectionDate.Value));
        }

        if (!string.IsNullOrEmpty(request.Remarks))
        {
            updates.Add("Remarks = @remarks");
            parameters.Add(new MySqlParameter("@remarks", request.Remarks));
        }

        var sql = $"UPDATE InspectionRecords SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新巡检记录: {Id}", id);
        return Ok(new { Success = true, Message = "巡检记录更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM InspectionRecords WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "巡检记录不存在" });

        _logger.LogInformation("删除巡检记录: {Id}", id);
        return Ok(new { Success = true, Message = "巡检记录已删除" });
    }

    private static InspectionResponse MapToResponse(MySqlDataReader reader)
    {
        return new InspectionResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            InspectionTitle = reader["InspectionTitle"].ToString() ?? "",
            BuildingId = reader["BuildingId"] == DBNull.Value ? null : Convert.ToInt32(reader["BuildingId"]),
            BuildingName = reader["BuildingName"] as string,
            InspectionArea = reader["InspectionArea"] as string,
            InspectorName = reader["InspectorName"] as string,
            InspectionDate = reader["InspectionDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["InspectionDate"]),
            InspectionTime = reader["InspectionTime"] as TimeSpan?,
            Status = reader["Status"].ToString() ?? "pending",
            Result = reader["Result"] as string,
            Findings = reader["Findings"] as string,
            NextInspectionDate = reader["NextInspectionDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["NextInspectionDate"]),
            Remarks = reader["Remarks"] as string,
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
}

public class InspectionResponse
{
    public int Id { get; set; }
    public string InspectionTitle { get; set; } = string.Empty;
    public int? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? InspectionArea { get; set; }
    public string? InspectorName { get; set; }
    public DateTime? InspectionDate { get; set; }
    public TimeSpan? InspectionTime { get; set; }
    public string Status { get; set; } = "pending";
    public string? Result { get; set; }
    public string? Findings { get; set; }
    public DateTime? NextInspectionDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InspectionListResponse
{
    public bool Success { get; set; } = true;
    public List<InspectionResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
}

public class CreateInspectionRequest
{
    public string InspectionTitle { get; set; } = string.Empty;
    public int? BuildingId { get; set; }
    public string? InspectionArea { get; set; }
    public string? InspectorName { get; set; }
    public DateTime? InspectionDate { get; set; }
    public TimeSpan? InspectionTime { get; set; }
    public DateTime? NextInspectionDate { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateInspectionRequest
{
    public string? Status { get; set; }
    public string? Result { get; set; }
    public string? Findings { get; set; }
    public string? InspectorName { get; set; }
    public DateTime? NextInspectionDate { get; set; }
    public string? Remarks { get; set; }
}
