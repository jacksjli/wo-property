using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.PersonService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnumsController : ControllerBase
{
    private readonly string _connectionString;

    public EnumsController(IConfiguration config)
    {
        _connectionString = "Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Timeout=10;";
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("SELECT * FROM Departments ORDER BY SortOrder, Id", conn);
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

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand("SELECT * FROM Roles ORDER BY Level, Id", conn);
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

    [HttpGet("person-types")]
    public IActionResult GetPersonTypes()
    {
        var types = new[] { "员工", "外包", "实习", "临时" };
        return Ok(new { success = true, data = types });
    }

    [HttpGet("statuses")]
    public IActionResult GetStatuses()
    {
        var statuses = new[] { "在职", "离职", "休假", "已删除" };
        return Ok(new { success = true, data = statuses });
    }
}