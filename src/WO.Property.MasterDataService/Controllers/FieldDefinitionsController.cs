using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WO.Property.MasterDataService.DTOs;
using System.Text.Json;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 字段定义管理API
/// </summary>
[ApiController]
[Route("api/field-definitions")]
public class FieldDefinitionsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<FieldDefinitionsController> _logger;

    public FieldDefinitionsController(MySqlConnection db, ILogger<FieldDefinitionsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取所有字段定义（分页/过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? module,
        [FromQuery] bool? isShared,
        [FromQuery] string? source,
        [FromQuery] string? status,
        [FromQuery] string? fieldType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(module))
        {
            conditions.Add("Module = @module");
            parameters.Add(new MySqlParameter("@module", module));
        }

        if (isShared.HasValue)
        {
            conditions.Add("IsShared = @isShared");
            parameters.Add(new MySqlParameter("@isShared", isShared.Value));
        }

        if (!string.IsNullOrEmpty(source))
        {
            conditions.Add("Source = @source");
            parameters.Add(new MySqlParameter("@source", source));
        }

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        if (!string.IsNullOrEmpty(fieldType))
        {
            conditions.Add("FieldType = @fieldType");
            parameters.Add(new MySqlParameter("@fieldType", fieldType));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        // Count query
        var countSql = $"SELECT COUNT(*) FROM FieldDefinitions {whereClause}";
        using var countCmd = new MySqlCommand(countSql, _db);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        // Data query
        var dataSql = $"SELECT * FROM FieldDefinitions {whereClause} ORDER BY SortOrder, Id LIMIT @offset, @pageSize";
        using var dataCmd = new MySqlCommand(dataSql, _db);
        foreach (var p in parameters) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

        var items = new List<FieldDefinitionResponse>();
        using var reader = await dataCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new FieldDefinitionListResponse
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

    /// <summary>
    /// 获取单个字段定义
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM FieldDefinitions WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new { Success = true, Data = MapToResponse(reader) });
        }
        return NotFound(new { Success = false, Message = "字段定义不存在" });
    }

    /// <summary>
    /// 新增字段定义（仅系统管理员）
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFieldDefinitionRequest request)
    {
        // 检查 FieldKey 是否已存在
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM FieldDefinitions WHERE FieldKey = @fieldKey", _db))
        {
            checkCmd.Parameters.AddWithValue("@fieldKey", request.FieldKey);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (exists)
                return BadRequest(new { Success = false, Message = $"字段标识 '{request.FieldKey}' 已存在" });
        }

        // 验证 FieldType
        var validTypes = new[] { "text", "number", "date", "select", "textarea" };
        if (!validTypes.Contains(request.FieldType))
            return BadRequest(new { Success = false, Message = $"无效的字段类型: {request.FieldType}" });

        var insertSql = @"INSERT INTO FieldDefinitions 
            (FieldKey, DisplayName, FieldType, Source, IsShared, Module, Options, DefaultValue, IsRequired, Width, SortOrder, Status, CreatedAt) 
            VALUES (@FieldKey, @DisplayName, @FieldType, @Source, @IsShared, @Module, @Options, @DefaultValue, @IsRequired, @Width, @SortOrder, 'Active', @CreatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@FieldKey", request.FieldKey);
        cmd.Parameters.AddWithValue("@DisplayName", request.DisplayName);
        cmd.Parameters.AddWithValue("@FieldType", request.FieldType);
        cmd.Parameters.AddWithValue("@Source", request.Source);
        cmd.Parameters.AddWithValue("@IsShared", request.IsShared);
        cmd.Parameters.AddWithValue("@Module", (object)request.Module ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Options", (object)request.Options ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DefaultValue", (object)request.DefaultValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsRequired", request.IsRequired);
        cmd.Parameters.AddWithValue("@Width", request.Width);
        cmd.Parameters.AddWithValue("@SortOrder", request.SortOrder);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建字段定义: {FieldKey} ({DisplayName})", request.FieldKey, request.DisplayName);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            Success = true,
            Message = "字段定义创建成功",
            Data = new FieldDefinitionResponse { Id = id, FieldKey = request.FieldKey, DisplayName = request.DisplayName }
        });
    }

    /// <summary>
    /// 更新字段定义
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFieldDefinitionRequest request)
    {
        // 检查是否存在
        using (var checkCmd = new MySqlCommand("SELECT * FROM FieldDefinitions WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { Success = false, Message = "字段定义不存在" });
        }

        var updates = new List<string>();
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };

        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            updates.Add("DisplayName = @displayName");
            parameters.Add(new MySqlParameter("@displayName", request.DisplayName));
        }

        if (!string.IsNullOrEmpty(request.FieldType))
        {
            var validTypes = new[] { "text", "number", "date", "select", "textarea" };
            if (!validTypes.Contains(request.FieldType))
                return BadRequest(new { Success = false, Message = $"无效的字段类型: {request.FieldType}" });
            updates.Add("FieldType = @fieldType");
            parameters.Add(new MySqlParameter("@fieldType", request.FieldType));
        }

        if (!string.IsNullOrEmpty(request.Source))
        {
            updates.Add("Source = @source");
            parameters.Add(new MySqlParameter("@source", request.Source));
        }

        if (request.IsShared.HasValue)
        {
            updates.Add("IsShared = @isShared");
            parameters.Add(new MySqlParameter("@isShared", request.IsShared.Value));
        }

        if (request.Module != null)
        {
            updates.Add("Module = @module");
            parameters.Add(new MySqlParameter("@module", request.Module));
        }

        if (request.Options != null)
        {
            updates.Add("Options = @options");
            parameters.Add(new MySqlParameter("@options", request.Options));
        }

        if (request.DefaultValue != null)
        {
            updates.Add("DefaultValue = @defaultValue");
            parameters.Add(new MySqlParameter("@defaultValue", request.DefaultValue));
        }

        if (request.IsRequired.HasValue)
        {
            updates.Add("IsRequired = @isRequired");
            parameters.Add(new MySqlParameter("@isRequired", request.IsRequired.Value));
        }

        if (request.Width.HasValue)
        {
            updates.Add("Width = @width");
            parameters.Add(new MySqlParameter("@width", request.Width.Value));
        }

        if (request.SortOrder.HasValue)
        {
            updates.Add("SortOrder = @sortOrder");
            parameters.Add(new MySqlParameter("@sortOrder", request.SortOrder.Value));
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            updates.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", request.Status));
        }

        updates.Add("UpdatedAt = @updatedAt");
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        var sql = $"UPDATE FieldDefinitions SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新字段定义: {Id}", id);

        return Ok(new { Success = true, Message = "字段定义更新成功" });
    }

    /// <summary>
    /// 删除字段（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var cmd = new MySqlCommand("UPDATE FieldDefinitions SET Status = 'Inactive', UpdatedAt = @updatedAt WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "字段定义不存在" });

        _logger.LogInformation("软删除字段定义: {Id}", id);

        return Ok(new { Success = true, Message = "字段定义已删除" });
    }

    /// <summary>
    /// 获取所有字段定义（不分页，用于下拉选择等场景）
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllNoPagination()
    {
        var sql = "SELECT * FROM FieldDefinitions WHERE Status = 'Active' ORDER BY Module, SortOrder, Id";
        using var cmd = new MySqlCommand(sql, _db);
        var items = new List<FieldDefinitionResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new { Success = true, Data = items, Pagination = new { TotalCount = items.Count } });
    }

    /// <summary>
    /// 获取所有共享字段
    /// </summary>
    [HttpGet("shared")]
    public async Task<IActionResult> GetShared()
    {
        var sql = "SELECT * FROM FieldDefinitions WHERE IsShared = 1 AND Status = 'Active' ORDER BY SortOrder, Id";
        using var cmd = new MySqlCommand(sql, _db);
        var items = new List<FieldDefinitionResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new { Success = true, Data = items });
    }

    /// <summary>
    /// 获取某模块可用的字段（包括私有给这个模块 + 所有共享字段）
    /// </summary>
    [HttpGet("by-module/{module}")]
    public async Task<IActionResult> GetByModule(string module)
    {
        var sql = "SELECT * FROM FieldDefinitions WHERE Status = 'Active' AND (IsShared = 1 OR Module = @module) ORDER BY SortOrder, Id";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@module", module);
        var items = new List<FieldDefinitionResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new { Success = true, Data = items });
    }

    /// <summary>
    /// 映射到响应DTO
    /// </summary>
    private static FieldDefinitionResponse MapToResponse(MySqlDataReader reader)
    {
        List<string>? options = null;
        var optionsStr = reader["Options"] as string;
        if (!string.IsNullOrEmpty(optionsStr))
        {
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(optionsStr);
            }
            catch
            {
                options = new List<string> { optionsStr };
            }
        }

        return new FieldDefinitionResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            FieldKey = reader["FieldKey"].ToString() ?? "",
            DisplayName = reader["DisplayName"].ToString() ?? "",
            FieldType = reader["FieldType"].ToString() ?? "",
            Source = reader["Source"].ToString() ?? "",
            IsShared = Convert.ToBoolean(reader["IsShared"]),
            Module = reader["Module"] as string,
            Options = options,
            DefaultValue = reader["DefaultValue"] as string,
            IsRequired = Convert.ToBoolean(reader["IsRequired"]),
            Width = Convert.ToInt32(reader["Width"]),
            SortOrder = Convert.ToInt32(reader["SortOrder"]),
            Status = reader["Status"].ToString() ?? "",
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
        };
    }
    /// <summary>
    /// 获取所有字段等价映射
    /// </summary>
    [HttpGet("~/api/field-equivalences")]
    public async Task<IActionResult> GetAllEquivalences()
    {
        var sql = @"SELECT fe.*, fd.DisplayName as CanonicalDisplayName 
                    FROM field_equivalences fe
                    LEFT JOIN FieldDefinitions fd ON fd.FieldKey = fe.canonical_field AND fd.Module = fe.module
                    ORDER BY fe.module, fe.canonical_field";
        using var cmd = new MySqlCommand(sql, _db);
        var items = new List<FieldEquivalenceResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new FieldEquivalenceResponse
            {
                Id = Convert.ToInt32(reader["id"]),
                CanonicalField = reader["canonical_field"].ToString() ?? "",
                EquivalentField = reader["equivalent_field"].ToString() ?? "",
                Module = reader["module"].ToString() ?? "",
                FieldType = reader["field_type"].ToString() ?? "string",
                DisplayName = reader["display_name"].ToString(),
                CanonicalDisplayName = reader["CanonicalDisplayName"]?.ToString(),
                Status = reader["status"].ToString() ?? "Active"
            });
        }
        return Ok(new { Success = true, Data = items });
    }

    /// <summary>
    /// 解析字段名到标准名（支持等价映射）
    /// </summary>
    [HttpPost("~/api/field-equivalences/resolve")]
    public async Task<IActionResult> ResolveFields([FromBody] ResolveFieldsRequest request)
    {
        if (request.Fields == null || request.Fields.Length == 0)
            return Ok(new { Success = true, Data = new Dictionary<string, string>() });

        var result = new Dictionary<string, string>();
        var placeholders = string.Join(",", request.Fields.Select((_, i) => $"@f{i}"));
        var sql = $@"SELECT equivalent_field, canonical_field 
                     FROM field_equivalences 
                     WHERE equivalent_field IN ({placeholders})";
        
        using var cmd = new MySqlCommand(sql, _db);
        for (int i = 0; i < request.Fields.Length; i++)
            cmd.Parameters.AddWithValue($"@f{i}", request.Fields[i]);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var equiv = reader["equivalent_field"].ToString() ?? "";
            var canon = reader["canonical_field"].ToString() ?? "";
            result[equiv] = canon;
            // 同时添加标准名（如果字段本身就是标准名）
            if (!result.ContainsKey(canon))
                result[canon] = canon;
        }

        // 对于没有等价映射的字段，直接返回自身
        foreach (var field in request.Fields)
        {
            if (!result.ContainsKey(field))
                result[field] = field;
        }

        return Ok(new { Success = true, Data = result });
    }

    /// <summary>
    /// 获取某模块的等价映射列表
    /// </summary>
    [HttpGet("~/api/field-equivalences/module/{module}")]
    public async Task<IActionResult> GetEquivalencesByModule(string module)
    {
        var sql = @"SELECT canonical_field, equivalent_field, display_name, field_type 
                     FROM field_equivalences 
                     WHERE module = @module";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@module", module);
        var items = new List<object>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new 
            {
                CanonicalField = reader["canonical_field"].ToString(),
                Equivalents = new[] { reader["equivalent_field"].ToString() },
                DisplayName = reader["display_name"].ToString(),
                FieldType = reader["field_type"].ToString()
            });
        }
        return Ok(new { Success = true, Data = items });
    }
}

// =====================================================
// 字段等价映射相关 DTO
// =====================================================

public class ResolveFieldsRequest
{
    public string[] Fields { get; set; } = Array.Empty<string>();
}

public class FieldEquivalenceResponse
{
    public int Id { get; set; }
    public string CanonicalField { get; set; } = string.Empty;
    public string EquivalentField { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string FieldType { get; set; } = "string";
    public string? DisplayName { get; set; }
    public string? CanonicalDisplayName { get; set; }
    public string Status { get; set; } = "Active";
}

// 保留原有模型
public class FieldDefinitionResponse
{
    public int Id { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public string? Module { get; set; }
    public List<string>? Options { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public int Width { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class FieldDefinitionListResponse
{
    public bool Success { get; set; } = true;
    public List<FieldDefinitionResponse> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
