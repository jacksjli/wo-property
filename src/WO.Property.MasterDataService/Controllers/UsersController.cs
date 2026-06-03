using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 用户管理API（角色分配）
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(MySqlConnection db, ILogger<UsersController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 分页获取用户列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(Username LIKE @keyword OR FullName LIKE @keyword OR Email LIKE @keyword)");
            parameters.Add(new MySqlParameter("@keyword", $"%{keyword}%"));
        }

        if (!string.IsNullOrEmpty(role))
        {
            conditions.Add("Role = @role");
            parameters.Add(new MySqlParameter("@role", role));
        }

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        // Count
        var countSql = $"SELECT COUNT(*) FROM Users {whereClause}";
        using (var countCmd = new MySqlCommand(countSql, _db))
        {
            foreach (var p in parameters) countCmd.Parameters.Add(p);
            var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // Data
            var dataSql = $"SELECT Id, Username, FullName, Email, Phone, Role, Status, CreatedAt FROM Users {whereClause} ORDER BY Id LIMIT @offset, @pageSize";
            using var dataCmd = new MySqlCommand(dataSql, _db);
            foreach (var p in parameters) dataCmd.Parameters.Add(p);
            dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

            var users = new List<object>();
            using var reader = await dataCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Username = reader["Username"].ToString(),
                    FullName = reader["FullName"].ToString(),
                    Email = reader["Email"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    Role = reader["Role"].ToString(),
                    Status = reader["Status"].ToString(),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                });
            }

            return Ok(new
            {
                success = true,
                data = users,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount = total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
        }
    }

    /// <summary>
    /// 获取单个用户
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sql = "SELECT Id, Username, FullName, Email, Phone, Role, Status, CreatedAt FROM Users WHERE Id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Ok(new
            {
                success = true,
                data = new
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Username = reader["Username"].ToString(),
                    FullName = reader["FullName"].ToString(),
                    Email = reader["Email"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    Role = reader["Role"].ToString(),
                    Status = reader["Status"].ToString(),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                }
            });
        }
        return NotFound(new { success = false, message = "用户不存在" });
    }

    /// <summary>
    /// 更新用户角色
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        // Verify user exists
        using (var checkCmd = new MySqlCommand("SELECT Id FROM Users WHERE Id = @id", _db))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { success = false, message = "用户不存在" });
        }

        // Verify role exists
        using (var roleCmd = new MySqlCommand("SELECT COUNT(*) FROM Roles WHERE Code = @code", _db))
        {
            roleCmd.Parameters.AddWithValue("@code", request.Role);
            var roleExists = Convert.ToInt32(await roleCmd.ExecuteScalarAsync()) > 0;
            if (!roleExists)
                return BadRequest(new { success = false, message = $"角色 '{request.Role}' 不存在" });
        }

        // Update role
        using var updateCmd = new MySqlCommand("UPDATE Users SET Role = @role WHERE Id = @id", _db);
        updateCmd.Parameters.AddWithValue("@role", request.Role);
        updateCmd.Parameters.AddWithValue("@id", id);
        await updateCmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新用户 {Id} 的角色为 {Role}", id, request.Role);

        return Ok(new { success = true, message = "角色更新成功" });
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        using var cmd = new MySqlCommand("UPDATE Users SET Status = @status WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@status", request.Status);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return NotFound(new { success = false, message = "用户不存在" });

        _logger.LogInformation("更新用户 {Id} 的状态为 {Status}", id, request.Status);

        return Ok(new { success = true, message = "状态更新成功" });
    }
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
