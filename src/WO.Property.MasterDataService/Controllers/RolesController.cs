using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 角色和权限管理API
/// </summary>
[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<RolesController> _logger;

    public RolesController(MySqlConnection db, ILogger<RolesController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sql = "SELECT Id, Name, Code, Level, Description FROM Roles ORDER BY Level, Id";
        using var cmd = new MySqlCommand(sql, _db);
        using var reader = await cmd.ExecuteReaderAsync();
        var roles = new List<object>();
        while (await reader.ReadAsync())
        {
            roles.Add(new
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString(),
                Code = reader["Code"].ToString(),
                Level = Convert.ToInt32(reader["Level"]),
                Description = reader["Description"].ToString()
            });
        }
        return Ok(new { success = true, data = roles });
    }

    /// <summary>
    /// 获取单个角色
    /// </summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var sql = "SELECT Id, Name, Code, Level, Description FROM Roles WHERE Code = @code";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@code", code);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new
            {
                success = true,
                data = new
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Code = reader["Code"].ToString(),
                    Level = Convert.ToInt32(reader["Level"]),
                    Description = reader["Description"].ToString()
                }
            });
        }
        return NotFound(new { success = false, message = "角色不存在" });
    }

    /// <summary>
    /// 获取角色的菜单权限
    /// </summary>
    [HttpGet("{code}/permissions")]
    public async Task<IActionResult> GetPermissions(string code)
    {
        var sql = @"SELECT ModuleKey, CanView, CanCreate, CanEdit, CanDelete 
                    FROM RolePermissions WHERE RoleCode = @code";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@code", code);
        using var reader = await cmd.ExecuteReaderAsync();
        var permissions = new List<object>();
        while (await reader.ReadAsync())
        {
            permissions.Add(new
            {
                ModuleKey = reader["ModuleKey"].ToString(),
                CanView = Convert.ToBoolean(reader["CanView"]),
                CanCreate = Convert.ToBoolean(reader["CanCreate"]),
                CanEdit = Convert.ToBoolean(reader["CanEdit"]),
                CanDelete = Convert.ToBoolean(reader["CanDelete"])
            });
        }
        return Ok(new { success = true, data = permissions });
    }

    /// <summary>
    /// 更新角色的菜单权限
    /// </summary>
    [HttpPut("{code}/permissions")]
    public async Task<IActionResult> UpdatePermissions(string code, [FromBody] UpdatePermissionsRequest request)
    {
        // Start transaction
        await using var transaction = await _db.BeginTransactionAsync();
        try
        {
            // Delete existing permissions for this role
            using (var deleteCmd = new MySqlCommand("DELETE FROM RolePermissions WHERE RoleCode = @code", _db, transaction))
            {
                deleteCmd.Parameters.AddWithValue("@code", code);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // Insert new permissions
            if (request.Permissions != null && request.Permissions.Any())
            {
                foreach (var perm in request.Permissions)
                {
                    var insertSql = @"INSERT INTO RolePermissions 
                        (RoleCode, ModuleKey, CanView, CanCreate, CanEdit, CanDelete, CreatedAt) 
                        VALUES (@RoleCode, @ModuleKey, @CanView, @CanCreate, @CanEdit, @CanDelete, @CreatedAt)";
                    using var cmd = new MySqlCommand(insertSql, _db, transaction);
                    cmd.Parameters.AddWithValue("@RoleCode", code);
                    cmd.Parameters.AddWithValue("@ModuleKey", perm.ModuleKey);
                    cmd.Parameters.AddWithValue("@CanView", perm.CanView ? 1 : 0);
                    cmd.Parameters.AddWithValue("@CanCreate", perm.CanCreate ? 1 : 0);
                    cmd.Parameters.AddWithValue("@CanEdit", perm.CanEdit ? 1 : 0);
                    cmd.Parameters.AddWithValue("@CanDelete", perm.CanDelete ? 1 : 0);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("更新角色 {Code} 的权限，共 {Count} 条", code, request.Permissions?.Count ?? 0);

            return Ok(new { success = true, message = "权限更新成功" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "更新角色权限失败: {Code}", code);
            return BadRequest(new { success = false, message = "权限更新失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 获取所有菜单模块列表（用于前端配置权限时选择）
    /// </summary>
    [HttpGet("modules")]
    public async Task<IActionResult> GetAllModules()
    {
        // Return the 35 module keys defined in the project store
        var modules = new[]
        {
            "ticket", "device", "material", "contract", "finance", "inspection",
            "key", "visitor", "notification", "statistics", "resident", "parking",
            "payment", "personnel", "dispatch", "timeout", "ticketType", "projectTracking",
            "accessControl", "announcement", "cleaning", "community", "delivery",
            "express", "renovation", "projectConfig", "fieldDefinition", "department",
            "region", "area", "building", "room", "jobType", "supplier", "deviceType"
        };
        return Ok(new { success = true, data = modules });
    }
}

public class UpdatePermissionsRequest
{
    public List<PermissionItem> Permissions { get; set; } = new();
}

public class PermissionItem
{
    public string ModuleKey { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
