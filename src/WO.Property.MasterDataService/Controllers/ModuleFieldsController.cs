using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WO.Property.MasterDataService.DTOs;
using System.Text.Json;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 模块字段关联管理API
/// </summary>
[ApiController]
[Route("api/module-fields")]
public class ModuleFieldsController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<ModuleFieldsController> _logger;

    public ModuleFieldsController(MySqlConnection db, ILogger<ModuleFieldsController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取某模块已选的字段
    /// </summary>
    [HttpGet("{module}")]
    public async Task<IActionResult> GetByModule(string module)
    {
        var sql = @"SELECT mf.*, fd.FieldKey, fd.DisplayName, fd.FieldType, fd.Source, fd.IsShared, 
                    fd.Module AS FieldModule, fd.Options, fd.DefaultValue, fd.IsRequired, fd.Width, 
                    fd.SortOrder AS FieldSortOrder, fd.Status, fd.CreatedAt AS FieldCreatedAt, fd.UpdatedAt
                    FROM ModuleFields mf
                    INNER JOIN FieldDefinitions fd ON mf.FieldDefinitionId = fd.Id
                    WHERE mf.Module = @module
                    ORDER BY mf.SortOrder, mf.Id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@module", module);
        
        var items = new List<ModuleFieldResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(MapToResponse(reader));
        }

        return Ok(new ModuleFieldListResponse
        {
            Success = true,
            Data = items
        });
    }

    /// <summary>
    /// 获取某模块的字段配置（含 Alias 覆盖）
    /// 返回 key → label 映射，前端可直接用于列标签
    /// </summary>
    [HttpGet("{module}/field-config")]
    public async Task<IActionResult> GetFieldConfig(string module)
    {
        var sql = @"SELECT fd.FieldKey, fd.DisplayName, fd.FieldType, fd.IsShared,
                    mf.Alias, mf.OwnerModule, mf.IsEditable, mf.IsVisible
                    FROM ModuleFields mf
                    INNER JOIN FieldDefinitions fd ON mf.FieldDefinitionId = fd.Id
                    WHERE mf.Module = @module AND mf.IsVisible = 1
                    ORDER BY mf.SortOrder, mf.Id";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@module", module);

        var config = new Dictionary<string, FieldConfigItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var fieldKey = reader["FieldKey"].ToString() ?? "";
            var displayName = reader["DisplayName"].ToString() ?? "";
            var alias = reader["Alias"] as string;
            // Alias 优先于 DisplayName
            var label = !string.IsNullOrEmpty(alias) ? alias : displayName;
            
            config[fieldKey] = new FieldConfigItem
            {
                FieldKey = fieldKey,
                Label = label,
                DisplayName = displayName,
                Alias = alias,
                FieldType = reader["FieldType"].ToString() ?? "string",
                IsShared = Convert.ToBoolean(reader["IsShared"]),
                OwnerModule = reader["OwnerModule"] as string,
                IsEditable = Convert.ToBoolean(reader["IsEditable"])
            };
        }

        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// 为模块添加字段（从已有字段选择）
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddModuleFieldRequest request)
    {
        // 检查字段定义是否存在
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM FieldDefinitions WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", request.FieldDefinitionId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (!exists)
                return NotFound(new { Success = false, Message = "字段定义不存在" });
        }

        // 检查是否已存在相同的模块-字段关联
        using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM ModuleFields WHERE Module = @module AND FieldDefinitionId = @fieldDefinitionId", _db))
        {
            checkCmd.Parameters.AddWithValue("@module", request.Module);
            checkCmd.Parameters.AddWithValue("@fieldDefinitionId", request.FieldDefinitionId);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (exists)
                return BadRequest(new { Success = false, Message = "该字段已在模块中使用" });
        }

        var insertSql = @"INSERT INTO ModuleFields 
            (Module, FieldDefinitionId, IsVisible, IsActive, SortOrder, CreatedAt, OwnerModule, IsEditable) 
            VALUES (@Module, @FieldDefinitionId, @IsVisible, @IsActive, @SortOrder, @CreatedAt, @OwnerModule, @IsEditable);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(insertSql, _db);
        cmd.Parameters.AddWithValue("@Module", request.Module);
        cmd.Parameters.AddWithValue("@FieldDefinitionId", request.FieldDefinitionId);
        cmd.Parameters.AddWithValue("@IsVisible", request.IsVisible);
        cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
        cmd.Parameters.AddWithValue("@SortOrder", request.SortOrder);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@OwnerModule", (object)request.OwnerModule ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsEditable", request.IsEditable);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("为模块 '{Module}' 添加字段: {FieldDefinitionId}", request.Module, request.FieldDefinitionId);

        return Ok(new
        {
            Success = true,
            Message = "字段已添加到模块",
            Data = new ModuleFieldResponse { Id = id, Module = request.Module, FieldDefinitionId = request.FieldDefinitionId }
        });
    }

    /// <summary>
    /// 从模块移除字段
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        using var cmd = new MySqlCommand("DELETE FROM ModuleFields WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "模块字段关联不存在" });

        _logger.LogInformation("从模块移除字段: {Id}", id);

        return Ok(new { Success = true, Message = "字段已从模块移除" });
    }

    /// <summary>
    /// 更新某模块的字段配置（显示/隐藏）
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateModuleFieldRequest request)
    {
        var updates = new List<string>();
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };

        if (request.IsVisible.HasValue)
        {
            updates.Add("IsVisible = @isVisible");
            parameters.Add(new MySqlParameter("@isVisible", request.IsVisible.Value));
        }

        if (request.IsActive.HasValue)
        {
            updates.Add("IsActive = @isActive");
            parameters.Add(new MySqlParameter("@isActive", request.IsActive.Value));
        }

        if (request.SortOrder.HasValue)
        {
            updates.Add("SortOrder = @sortOrder");
            parameters.Add(new MySqlParameter("@sortOrder", request.SortOrder.Value));
        }

        if (request.Alias != null)
        {
            updates.Add("Alias = @alias");
            parameters.Add(new MySqlParameter("@alias", (object)request.Alias ?? DBNull.Value));
        }

        if (request.OwnerModule != null)
        {
            updates.Add("OwnerModule = @ownerModule");
            parameters.Add(new MySqlParameter("@ownerModule", (object)request.OwnerModule ?? DBNull.Value));
        }

        if (request.IsEditable.HasValue)
        {
            updates.Add("IsEditable = @isEditable");
            parameters.Add(new MySqlParameter("@isEditable", request.IsEditable.Value));
        }

        if (!updates.Any())
            return Ok(new { Success = true, Message = "没有需要更新的字段" });

        var sql = $"UPDATE ModuleFields SET {string.Join(", ", updates)} WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { Success = false, Message = "模块字段关联不存在" });

        _logger.LogInformation("更新模块字段配置: {Id}", id);

        return Ok(new { Success = true, Message = "模块字段配置已更新" });
    }

    /// <summary>
    /// 映射到响应DTO
    /// </summary>
    private static ModuleFieldResponse MapToResponse(MySqlDataReader reader)
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

        return new ModuleFieldResponse
        {
            Id = Convert.ToInt32(reader["Id"]),
            Module = reader["Module"].ToString() ?? "",
            FieldDefinitionId = Convert.ToInt32(reader["FieldDefinitionId"]),
            IsVisible = Convert.ToBoolean(reader["IsVisible"]),
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            SortOrder = Convert.ToInt32(reader["SortOrder"]),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            Alias = reader["Alias"] as string,
            OwnerModule = reader["OwnerModule"] as string,
            IsEditable = Convert.ToBoolean(reader["IsEditable"]),
            FieldDefinition = new FieldDefinitionInModuleResponse
            {
                Id = Convert.ToInt32(reader["FieldDefinitionId"]),
                FieldKey = reader["FieldKey"].ToString() ?? "",
                DisplayName = reader["DisplayName"].ToString() ?? "",
                FieldType = reader["FieldType"].ToString() ?? "",
                Source = reader["Source"].ToString() ?? "",
                IsShared = Convert.ToBoolean(reader["IsShared"]),
                Module = reader["FieldModule"] as string,
                Options = options,
                DefaultValue = reader["DefaultValue"] as string,
                IsRequired = Convert.ToBoolean(reader["IsRequired"]),
                Width = Convert.ToInt32(reader["Width"]),
                SortOrder = Convert.ToInt32(reader["FieldSortOrder"]),
                Status = reader["Status"].ToString() ?? "",
                CreatedAt = Convert.ToDateTime(reader["FieldCreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
            }
        };
    }
}

// 本地 DTO
public class AddModuleFieldRequest
{
    public string Module { get; set; } = string.Empty;
    public int FieldDefinitionId { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public string? OwnerModule { get; set; }
    public bool IsEditable { get; set; } = false;
}

public class UpdateModuleFieldRequest
{
    public bool? IsVisible { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
    public string? Alias { get; set; }
    public string? OwnerModule { get; set; }
    public bool? IsEditable { get; set; }
}

public class ModuleFieldResponse
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public int FieldDefinitionId { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Alias { get; set; }
    public string? OwnerModule { get; set; }
    public bool IsEditable { get; set; }
    public FieldDefinitionInModuleResponse? FieldDefinition { get; set; }
}

public class FieldDefinitionInModuleResponse
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

public class ModuleFieldListResponse
{
    public bool Success { get; set; } = true;
    public List<ModuleFieldResponse> Data { get; set; } = new();
}

public class FieldConfigItem
{
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;       // alias > displayName
    public string DisplayName { get; set; } = string.Empty; // 默认显示名
    public string? Alias { get; set; }                     // 模块级别名
    public string FieldType { get; set; } = "string";
    public bool IsShared { get; set; }
    public string? OwnerModule { get; set; }
    public bool IsEditable { get; set; }
}
