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

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

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
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
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
            row[reader.GetName(i)] = val == DBNull.Value ? null : val;
        }

        return Ok(new { success = true, data = row });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePerson([FromBody] Dictionary<string, object> dto)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // Check duplicate staffId
        await using var checkStaff = new MySqlCommand("SELECT COUNT(*) FROM Personnel WHERE StaffId = @staffId", conn);
        checkStaff.Parameters.AddWithValue("@staffId", dto.GetValueOrDefault("staffId") ?? "");
        if (Convert.ToInt32(await checkStaff.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = "Staff ID already exists" });

        // Check duplicate phone
        await using var checkPhone = new MySqlCommand("SELECT COUNT(*) FROM Personnel WHERE Phone = @phone", conn);
        checkPhone.Parameters.AddWithValue("@phone", dto.GetValueOrDefault("phone") ?? "");
        if (Convert.ToInt32(await checkPhone.ExecuteScalarAsync()) > 0)
            return BadRequest(new { success = false, message = "Phone already exists" });

        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new List<MySqlParameter>();

        foreach (var kvp in dto)
        {
            if (kvp.Value == null) continue;
            columns.Add(kvp.Key);
            values.Add($"@{kvp.Key}");
            parameters.Add(new MySqlParameter($"@{kvp.Key}", kvp.Value.ToString()));
        }

        var sql = $"INSERT INTO Persons ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)}); SELECT LAST_INSERT_ID();";
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

        // Check phone duplicate if being updated
        if (dto.ContainsKey("phone"))
        {
            await using var checkPhone = new MySqlCommand(
                "SELECT COUNT(*) FROM Personnel WHERE Phone = @phone AND Id != @id", conn);
            checkPhone.Parameters.AddWithValue("@phone", dto["phone"].ToString() ?? "");
            checkPhone.Parameters.AddWithValue("@id", id);
            if (Convert.ToInt32(await checkPhone.ExecuteScalarAsync()) > 0)
                return BadRequest(new { success = false, message = "Phone already exists" });
        }

        var sets = dto.Keys.Select(k => $"{k} = @{k}").ToList();
        var parameters = dto.Select(kvp => new MySqlParameter($"@{kvp.Key}", kvp.Value?.ToString() ?? (object)DBNull.Value)).ToList();

        var sql = $"UPDATE Persons SET {string.Join(", ", sets)} WHERE Id = @id";
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
            "UPDATE Persons SET Status = '已删除' WHERE Id = @id", conn);
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
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
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
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
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
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
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