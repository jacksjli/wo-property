using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.PersonService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly string _connectionString;

    public PersonsController(IConfiguration config)
    {
        _connectionString = "Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Timeout=10;";
    }

    // 工具方法：将列名转为 camelCase（如 CreatedAt, EmployeeNo → createdAt, employeeNo）
    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        // 处理 snake_case
        if (str.Contains('_'))
        {
            var parts = str.Split('_');
            var result = parts[0].ToLower();
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    result += char.ToUpperInvariant(parts[i][0]) + parts[i][1..].ToLower();
            }
            return result;
        }
        // 处理 PascalCase
        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    // 获取当前项目代码（从 X-Project header）
    private string? GetProjectCode()
    {
        if (Request.Headers.TryGetValue("X-Project", out var projectValues))
        {
            var projectCode = projectValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(projectCode))
                return projectCode;
        }
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetPersons(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null,
        [FromQuery] string? phone = null,
        [FromQuery] string? department = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? personType = null)
    {
        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        // 按 project_code 过滤（单租户多项目）
        var projectCode = GetProjectCode();
        if (!string.IsNullOrEmpty(projectCode))
        {
            whereClauses.Add("project_code = @projectCode");
            parameters.Add(new MySqlParameter("@projectCode", projectCode));
        }

        if (!string.IsNullOrEmpty(name))
        {
            whereClauses.Add("Name LIKE @name");
            parameters.Add(new MySqlParameter("@name", $"%{name}%"));
        }
        if (!string.IsNullOrEmpty(phone))
        {
            whereClauses.Add("Phone LIKE @phone");
            parameters.Add(new MySqlParameter("@phone", $"%{phone}%"));
        }
        if (!string.IsNullOrEmpty(department))
        {
            whereClauses.Add("Department = @dept");
            parameters.Add(new MySqlParameter("@dept", department));
        }
        if (!string.IsNullOrEmpty(role))
        {
            whereClauses.Add("Role = @role");
            parameters.Add(new MySqlParameter("@role", role));
        }
        if (!string.IsNullOrEmpty(status))
        {
            whereClauses.Add("Status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }
        if (!string.IsNullOrEmpty(personType))
        {
            whereClauses.Add("PersonType = @personType");
            parameters.Add(new MySqlParameter("@personType", personType));
        }

        whereClauses.Add("Status != '已删除'");
        var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // Count
        await using var countCmd = new MySqlCommand($"SELECT COUNT(*) FROM Personnel {whereSql}", conn);
        foreach (var p in parameters) countCmd.Parameters.Add(p);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        // Data
        var offset = (page - 1) * pageSize;
        await using var cmd = new MySqlCommand(
            $"SELECT * FROM Personnel {whereSql} ORDER BY Id DESC LIMIT @limit OFFSET @offset", conn);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        cmd.Parameters.Add(new MySqlParameter("@limit", pageSize));
        cmd.Parameters.Add(new MySqlParameter("@offset", offset));

        await using var reader = await cmd.ExecuteReaderAsync();
        var items = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i);
                if (val != DBNull.Value)
                {
                    var colName = reader.GetName(i);
                    var camelName = ToCamelCase(colName);
                    row[camelName] = val;
                }
            }
            items.Add(row);
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                items,
                totalCount,
                page,
                pageSize
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPerson(int id)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("SELECT * FROM Personnel WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { success = false, message = "Person not found" });

        var row = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var val = reader.GetValue(i);
            if (val != DBNull.Value)
            {
                var colName = reader.GetName(i);
                var camelName = ToCamelCase(colName);
                row[camelName] = val;
            }
        }

        return Ok(new { success = true, data = row });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePerson([FromBody] Dictionary<string, object> dto)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // 辅助方法：从 object 中提取值（处理 JsonElement）
        static object? GetValue(object? v)
        {
            if (v == null) return null;
            if (v is System.Text.Json.JsonElement je)
            {
                return je.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => je.GetString(),
                    System.Text.Json.JsonValueKind.Number => je.TryGetInt32(out var i) ? i : je.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => je.ToString()
                };
            }
            return v;
        }

        // Check duplicate phone
        var phoneVal = GetValue(dto.TryGetValue("phone", out var pv) ? pv : null);
        var phoneValue = phoneVal?.ToString() ?? "";
        if (!dto.ContainsKey("employeeNo") || string.IsNullOrEmpty(GetValue(dto.GetValueOrDefault("employeeNo"))?.ToString()))
            dto["employeeNo"] = "EMP" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString().Substring(5);
        if (!dto.ContainsKey("status"))
            dto["status"] = "active";
        await using var checkPhone = new MySqlCommand("SELECT COUNT(*) FROM Personnel WHERE Phone = @phone", conn);
        checkPhone.Parameters.AddWithValue("@phone", phoneValue);
        if (Convert.ToInt32(await checkPhone.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = "Phone already exists" });

        // 前端字段名 -> 数据库列名 映射
        var fieldMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TicketTypeIds", "ticket_type_ids" },
            { "SpecialtyIds", "specialty_ids" },
            { "AreaIds", "area_ids" },
            { "BuildingIds", "building_ids" },
            { "IsSupervisor", "is_supervisor" },
            { "MaxConcurrentTickets", "max_concurrent_tickets" }
        };

        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new List<MySqlParameter>();

        foreach (var kvp in dto)
        {
            var rawValue = GetValue(kvp.Value);
            if (rawValue == null) continue;
            var columnName = fieldMapping.TryGetValue(kvp.Key, out var mapped) ? mapped : kvp.Key;
            columns.Add(columnName);
            values.Add($"@{columnName}");
            parameters.Add(new MySqlParameter($"@{columnName}", rawValue));
        }

        var sql = $"INSERT INTO Personnel ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)}); SELECT LAST_INSERT_ID();";
        await using var cmd = new MySqlCommand(sql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(p);

        var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return Ok(new { success = true, data = new { id = newId } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePerson(int id, [FromBody] Dictionary<string, object> dto)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // 辅助方法：从 object 中提取值（处理 JsonElement）
        static object? GetValue(object? v)
        {
            if (v == null) return null;
            if (v is System.Text.Json.JsonElement je)
            {
                return je.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => je.GetString(),
                    System.Text.Json.JsonValueKind.Number => je.TryGetInt32(out var i) ? i : je.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => je.ToString()
                };
            }
            return v;
        }

        // Check phone duplicate if being updated
        if (dto.ContainsKey("phone"))
        {
            await using var checkPhone = new MySqlCommand(
                "SELECT COUNT(*) FROM Personnel WHERE Phone = @phone AND Id != @id", conn);
            checkPhone.Parameters.AddWithValue("@phone", GetValue(dto["phone"])?.ToString() ?? "");
            checkPhone.Parameters.AddWithValue("@id", id);
            if (Convert.ToInt32(await checkPhone.ExecuteScalarAsync()) > 0)
                return BadRequest(new { success = false, message = "Phone already exists" });
        }

        // 前端字段名 -> 数据库列名 映射
        var fieldMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TicketTypeIds", "ticket_type_ids" },
            { "SpecialtyIds", "specialty_ids" },
            { "AreaIds", "area_ids" },
            { "BuildingIds", "building_ids" },
            { "IsSupervisor", "is_supervisor" },
            { "MaxConcurrentTickets", "max_concurrent_tickets" }
        };

        var sets = new List<string>();
        var parameters = new List<MySqlParameter>();
        foreach (var kvp in dto)
        {
            var rawValue = GetValue(kvp.Value);
            if (rawValue == null) continue;
            var columnName = fieldMapping.TryGetValue(kvp.Key, out var mapped) ? mapped : kvp.Key;
            sets.Add($"{columnName} = @{columnName}");
            parameters.Add(new MySqlParameter($"@{columnName}", rawValue));
        }

        var sql = $"UPDATE Personnel SET {string.Join(", ", sets)} WHERE Id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePerson(int id)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(
            "UPDATE Personnel SET Status = '已删除' WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();

        return Ok(new { success = true });
    }

    [HttpGet("by-role/{role}")]
    public async Task<IActionResult> GetByRole(string role)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("SELECT * FROM Personnel WHERE Role = @role", conn);
        cmd.Parameters.AddWithValue("@role", role);

        var items = new List<Dictionary<string, object>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i);
                if (val != DBNull.Value)
                {
                    var colName = reader.GetName(i);
                    var camelName = ToCamelCase(colName);
                    row[camelName] = val;
                }
            }
            items.Add(row);
        }
        return Ok(new { success = true, data = items });
    }

    [HttpGet("by-department/{dept}")]
    public async Task<IActionResult> GetByDepartment(string dept)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("SELECT * FROM Personnel WHERE Department = @dept", conn);
        cmd.Parameters.AddWithValue("@dept", dept);

        var items = new List<Dictionary<string, object>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i);
                if (val != DBNull.Value)
                {
                    var colName = reader.GetName(i);
                    var camelName = ToCamelCase(colName);
                    row[camelName] = val;
                }
            }
            items.Add(row);
        }
        return Ok(new { success = true, data = items });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("SELECT * FROM Personnel", conn);
        var items = new List<Dictionary<string, object>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i);
                if (val != DBNull.Value)
                {
                    var colName = reader.GetName(i);
                    var camelName = ToCamelCase(colName);
                    row[camelName] = val;
                }
            }
            items.Add(row);
        }

        var total = items.Count;
        var active = items.Count(p => p.GetValueOrDefault("Status")?.ToString() == "在职");
        var byDept = items.GroupBy(p => p.GetValueOrDefault("Department")?.ToString() ?? "").ToDictionary(g => g.Key, g => g.Count());
        var byRole = items.GroupBy(p => p.GetValueOrDefault("Role")?.ToString() ?? "").ToDictionary(g => g.Key, g => g.Count());

        return Ok(new
        {
            success = true,
            data = new { total, activeCount = active, inactiveCount = total - active, byDepartment = byDept, byRole }
        });
    }
}