using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WO.Property.MasterDataService.Models;
using Microsoft.AspNetCore.Authorization;

namespace WO.Property.MasterDataService.Controllers;

/// <summary>
/// 工种类型管理API
/// </summary>
[ApiController]
[Authorize]
[Route("api/job-types")]
public class JobTypesController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<JobTypesController> _logger;

    public JobTypesController(MySqlConnection db, ILogger<JobTypesController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    /// <summary>
    /// 获取所有工种（或按ticket_type_id/departmentId筛选）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? ticketTypeId = null, [FromQuery] int? departmentId = null, [FromQuery] string? status = null)
    {
        var conditions = new List<string>();
        if (ticketTypeId.HasValue) conditions.Add("ticket_type_id = @ticketTypeId");
        if (departmentId.HasValue) conditions.Add("department_id = @departmentId");
        if (!string.IsNullOrEmpty(status)) conditions.Add("Status = @status");
        var where = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $"SELECT * FROM job_types {where} ORDER BY SortOrder, Id";
        var cmd = new MySqlCommand(sql, _db);
        if (ticketTypeId.HasValue) cmd.Parameters.AddWithValue("@ticketTypeId", ticketTypeId.Value);
        if (departmentId.HasValue) cmd.Parameters.AddWithValue("@departmentId", departmentId.Value);
        if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);

        var items = new List<JobTypeItem>();
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(MapJobType(reader));
        await reader.CloseAsync();

        return Ok(new { success = true, data = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cmd = new MySqlCommand("SELECT * FROM job_types WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var item = MapJobType(reader);
            await reader.CloseAsync();
            return Ok(new { success = true, data = item });
        }
        await reader.CloseAsync();
        return NotFound(new { success = false, message = "工种类型不存在" });
    }

    /// <summary>
    /// 根据 ID 列表获取工种（用于反向查找工单类型）
    /// </summary>
    [HttpGet("by-ids")]
    public async Task<IActionResult> GetByIds([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return Ok(new { success = true, data = new { jobTypes = new List<JobTypeItem>(), ticketTypeIds = new List<int>() } });

        var idList = ids.Split(',').Select(s => int.TryParse(s.Trim(), out var n) ? n : 0).Where(n => n > 0).ToList();
        if (!idList.Any())
            return Ok(new { success = true, data = new { jobTypes = new List<JobTypeItem>(), ticketTypeIds = new List<int>() } });

        var placeholders = string.Join(",", idList.Select((_, i) => $"@id{i}"));
        var sql = $"SELECT * FROM job_types WHERE Id IN ({placeholders}) ORDER BY Id";
        var cmd = new MySqlCommand(sql, _db);
        for (int i = 0; i < idList.Count; i++) cmd.Parameters.AddWithValue($"@id{i}", idList[i]);

        var jobTypes = new List<JobTypeItem>();
        var ticketTypeIds = new HashSet<int>();
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = MapJobType(reader);
            jobTypes.Add(item);
            if (item.ticket_type_id > 0) ticketTypeIds.Add(item.ticket_type_id);
        }
        await reader.CloseAsync();

        return Ok(new { success = true, data = new { jobTypes, ticketTypeIds = ticketTypeIds.ToList() } });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobTypeItem req)
    {
        var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM job_types WHERE Code = @code", _db);
        checkCmd.Parameters.AddWithValue("@code", req.Code);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
            return Ok(new { success = false, message = $"工种编号 '{req.Code}' 已存在" });

        var sql = @"INSERT INTO job_types (Name, Code, Description, Category, ticket_type_id, department_id, Status, SortOrder)
                    VALUES (@Name, @Code, @Description, @Category, @ticketTypeId, @departmentId, @Status, @SortOrder);
                    SELECT LAST_INSERT_ID();";
        var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@Name", req.Name);
        cmd.Parameters.AddWithValue("@Code", req.Code);
        cmd.Parameters.AddWithValue("@Description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", (object)req.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ticketTypeId", req.ticket_type_id > 0 ? req.ticket_type_id : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@departmentId", req.department_id > 0 ? req.department_id : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@SortOrder", req.SortOrder);
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        _logger.LogInformation("创建工种类型: {Code} ({Name})", req.Code, req.Name);
        return Ok(new { success = true, message = "工种类型创建成功", data = new { id } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] JobTypeItem req)
    {
        var checkCmd = new MySqlCommand("SELECT * FROM job_types WHERE Id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        var reader = await checkCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) { await reader.CloseAsync(); return Ok(new { success = false, message = "工种类型不存在" }); }
        await reader.CloseAsync();

        var updates = new List<string> { "Name = @name", "Code = @code", "Description = @description", "Category = @category", "ticket_type_id = @ticketTypeId", "department_id = @departmentId", "Status = @status", "SortOrder = @sortOrder", "UpdatedAt = @updatedAt" };
        var cmd = new MySqlCommand($"UPDATE job_types SET {string.Join(", ", updates)} WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", req.Name);
        cmd.Parameters.AddWithValue("@code", req.Code);
        cmd.Parameters.AddWithValue("@description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@category", (object)req.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ticketTypeId", req.ticket_type_id > 0 ? req.ticket_type_id : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@departmentId", req.department_id > 0 ? req.department_id : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@status", req.Status ?? "Active");
        cmd.Parameters.AddWithValue("@sortOrder", req.SortOrder);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新工种类型: {Id}", id);
        return Ok(new { success = true, message = "工种类型更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cmd = new MySqlCommand("DELETE FROM job_types WHERE Id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            return Ok(new { success = false, message = "工种类型不存在" });
        _logger.LogInformation("删除工种类型: {Id}", id);
        return Ok(new { success = true, message = "工种类型已删除" });
    }

    /// <summary>
    /// 批量导入工种
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportJobTypes([FromBody] ImportJobTypesRequest req)
    {
        var result = new ImportJobTypesResult();
        foreach (var row in req.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                result.Skipped++;
                continue;
            }
            try
            {
                var existCmd = new MySqlCommand("SELECT Id FROM job_types WHERE Name = @name", _db);
                existCmd.Parameters.AddWithValue("@name", row.Name);
                var existsId = await existCmd.ExecuteScalarAsync();
                if (existsId != null && existsId != DBNull.Value)
                {
                    var updateSql = @"UPDATE job_types SET Code = @code, Category = @category,
                        Description = @description, ticket_type_id = @ticketTypeId,
                        Status = @status, SortOrder = @sortOrder, UpdatedAt = @updatedAt
                        WHERE Name = @name";
                    var updateCmd = new MySqlCommand(updateSql, _db);
                    updateCmd.Parameters.AddWithValue("@code", ToCode(row.Name));
                    updateCmd.Parameters.AddWithValue("@name", row.Name);
                    updateCmd.Parameters.AddWithValue("@category", (object)row.Category ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@description", (object)row.Description ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@ticketTypeId", row.TicketTypeId > 0 ? row.TicketTypeId : (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@status", "Active");
                    updateCmd.Parameters.AddWithValue("@sortOrder", row.SortOrder);
                    updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                    await updateCmd.ExecuteNonQueryAsync();
                    result.Success++;
                }
                else
                {
                    var insertSql = @"INSERT INTO job_types (Name, Code, Category, Description, ticket_type_id, Status, SortOrder)
                        VALUES (@name, @code, @category, @description, @ticketTypeId, @status, @sortOrder)";
                    var insertCmd = new MySqlCommand(insertSql, _db);
                    insertCmd.Parameters.AddWithValue("@name", row.Name);
                    insertCmd.Parameters.AddWithValue("@code", ToCode(row.Name));
                    insertCmd.Parameters.AddWithValue("@category", (object)row.Category ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@description", (object)row.Description ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@ticketTypeId", row.TicketTypeId > 0 ? row.TicketTypeId : (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@status", "Active");
                    insertCmd.Parameters.AddWithValue("@sortOrder", row.SortOrder);
                    await insertCmd.ExecuteNonQueryAsync();
                    result.Success++;
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"「{row.Name}」: {ex.Message}");
            }
        }
        return Ok(new { success = true, data = result });
    }

    private static string ToCode(string name) =>
        string.Join("", name.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static JobTypeItem MapJobType(MySqlDataReader r)
    {
        int? tid = r["ticket_type_id"] == DBNull.Value ? null : Convert.ToInt32(r["ticket_type_id"]);
        int? did = r["department_id"] == DBNull.Value ? null : Convert.ToInt32(r["department_id"]);
        return new JobTypeItem
        {
            Id = Convert.ToInt32(r["Id"]),
            Name = r["Name"].ToString() ?? "",
            Code = r["Code"].ToString() ?? "",
            Description = r["Description"] as string,
            Category = r["Category"] as string,
            ticket_type_id = tid ?? 0,
            department_id = did ?? 0,
            Status = r["Status"].ToString() ?? "Active",
            SortOrder = r["SortOrder"] == DBNull.Value ? 0 : Convert.ToInt32(r["SortOrder"]),
            CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
            UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["UpdatedAt"])
        };
    }
}

public class JobTypeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int ticket_type_id { get; set; }
    public int department_id { get; set; }
    public string Status { get; set; } = "Active";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}