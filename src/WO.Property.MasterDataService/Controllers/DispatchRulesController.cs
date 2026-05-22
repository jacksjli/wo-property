using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace WO.Property.MasterDataService.Controllers;

[ApiController]
[Route("api/dispatch-rules")]
public class DispatchRulesController : ControllerBase
{
    private readonly MySqlConnection _db;
    private readonly ILogger<DispatchRulesController> _logger;

    public DispatchRulesController(MySqlConnection db, ILogger<DispatchRulesController> logger)
    {
        _db = db;
        _logger = logger;
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? type)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            conditions.Add("status = @status");
            parameters.Add(new MySqlParameter("@status", status));
        }
        if (!string.IsNullOrEmpty(type) && type != "all")
        {
            conditions.Add("type = @type");
            parameters.Add(new MySqlParameter("@type", type));
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var sql = $"SELECT * FROM dispatch_rules {whereClause} ORDER BY id DESC";

        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);

        var items = new List<Dictionary<string, object>>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            items.Add(row);
        }
        return Ok(new { success = true, data = items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var cmd = new MySqlCommand("SELECT * FROM dispatch_rules WHERE id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            return Ok(new { success = true, data = row });
        }
        return NotFound(new { success = false, message = "规则不存在" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDispatchRuleRequest request)
    {
        // 获取当前最大ID，生成规则编号
        using var countCmd = new MySqlCommand("SELECT COUNT(*) FROM dispatch_rules", _db);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        var ruleNo = $"DR-{DateTime.Now.Year}-{(count + 1):D4}";

        var sql = @"INSERT INTO dispatch_rules 
            (rule_no, category, name, description, type, ticket_color, location, location_type,
             priority, auto_assign, notify_backup, allow_transfer, timeout_escalation,
             operator_ids, supervisor_id, manager_id, department_head_id, company_head_id,
             backup_ids, notify_methods, status, match_count, success_count, avg_response_time, remark,
             created_at, updated_at)
            VALUES (@ruleNo, @category, @name, @description, @type, @ticketColor, @location, @locationType,
             @priority, @autoAssign, @notifyBackup, @allowTransfer, @timeoutEscalation,
             @operatorIds, @supervisorId, @managerId, @departmentHeadId, @companyHeadId,
             @backupIds, @notifyMethods, @status, 0, 0, 0, @remark, @createdAt, @updatedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, _db);
        cmd.Parameters.AddWithValue("@ruleNo", ruleNo);
        cmd.Parameters.AddWithValue("@category", request.Category ?? "property");
        cmd.Parameters.AddWithValue("@name", request.Name ?? "");
        cmd.Parameters.AddWithValue("@description", request.Description ?? "");
        cmd.Parameters.AddWithValue("@type", request.Type ?? "ticket_type");
        cmd.Parameters.AddWithValue("@ticketColor", request.TicketColor ?? "all");
        cmd.Parameters.AddWithValue("@location", request.Location ?? "");
        cmd.Parameters.AddWithValue("@locationType", request.LocationType ?? "contains");
        cmd.Parameters.AddWithValue("@priority", request.Priority);
        cmd.Parameters.AddWithValue("@autoAssign", request.AutoAssign);
        cmd.Parameters.AddWithValue("@notifyBackup", request.NotifyBackup);
        cmd.Parameters.AddWithValue("@allowTransfer", request.AllowTransfer);
        cmd.Parameters.AddWithValue("@timeoutEscalation", request.TimeoutEscalation);
        cmd.Parameters.AddWithValue("@operatorIds", request.OperatorIds ?? "");
        cmd.Parameters.AddWithValue("@supervisorId", request.SupervisorId ?? "");
        cmd.Parameters.AddWithValue("@managerId", request.ManagerId ?? "");
        cmd.Parameters.AddWithValue("@departmentHeadId", request.DepartmentHeadId ?? "");
        cmd.Parameters.AddWithValue("@companyHeadId", request.CompanyHeadId ?? "");
        cmd.Parameters.AddWithValue("@backupIds", request.BackupIds ?? "");
        cmd.Parameters.AddWithValue("@notifyMethods", request.NotifyMethods ?? "app");
        cmd.Parameters.AddWithValue("@status", request.Status ?? "active");
        cmd.Parameters.AddWithValue("@remark", request.Remark ?? "");
        cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        _logger.LogInformation("创建派单规则: {Id}, {Name}", id, request.Name);

        return Ok(new { success = true, message = "规则创建成功", data = new { id, ruleNo, name = request.Name } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDispatchRuleRequest request)
    {
        using var checkCmd = new MySqlCommand("SELECT id FROM dispatch_rules WHERE id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var checkReader = await checkCmd.ExecuteReaderAsync();
        if (!await checkReader.ReadAsync())
        {
            checkReader.Close();
            return NotFound(new { success = false, message = "规则不存在" });
        }
        checkReader.Close();

        var updates = new List<string> { "updated_at = @updatedAt" };
        var parameters = new List<MySqlParameter> { new MySqlParameter("@id", id) };
        parameters.Add(new MySqlParameter("@updatedAt", DateTime.UtcNow));

        if (request.Name != null) { updates.Add("name = @name"); parameters.Add(new MySqlParameter("@name", request.Name)); }
        if (request.Description != null) { updates.Add("description = @description"); parameters.Add(new MySqlParameter("@description", request.Description)); }
        if (request.Type != null) { updates.Add("type = @type"); parameters.Add(new MySqlParameter("@type", request.Type)); }
        if (request.TicketColor != null) { updates.Add("ticket_color = @ticketColor"); parameters.Add(new MySqlParameter("@ticketColor", request.TicketColor)); }
        if (request.Location != null) { updates.Add("location = @location"); parameters.Add(new MySqlParameter("@location", request.Location)); }
        if (request.LocationType != null) { updates.Add("location_type = @locationType"); parameters.Add(new MySqlParameter("@locationType", request.LocationType)); }
        if (request.Priority.HasValue) { updates.Add("priority = @priority"); parameters.Add(new MySqlParameter("@priority", request.Priority.Value)); }
        if (request.AutoAssign.HasValue) { updates.Add("auto_assign = @autoAssign"); parameters.Add(new MySqlParameter("@autoAssign", request.AutoAssign.Value)); }
        if (request.NotifyBackup.HasValue) { updates.Add("notify_backup = @notifyBackup"); parameters.Add(new MySqlParameter("@notifyBackup", request.NotifyBackup.Value)); }
        if (request.AllowTransfer.HasValue) { updates.Add("allow_transfer = @allowTransfer"); parameters.Add(new MySqlParameter("@allowTransfer", request.AllowTransfer.Value)); }
        if (request.TimeoutEscalation.HasValue) { updates.Add("timeout_escalation = @timeoutEscalation"); parameters.Add(new MySqlParameter("@timeoutEscalation", request.TimeoutEscalation.Value)); }
        if (request.OperatorIds != null) { updates.Add("operator_ids = @operatorIds"); parameters.Add(new MySqlParameter("@operatorIds", request.OperatorIds)); }
        if (request.SupervisorId != null) { updates.Add("supervisor_id = @supervisorId"); parameters.Add(new MySqlParameter("@supervisorId", request.SupervisorId)); }
        if (request.ManagerId != null) { updates.Add("manager_id = @managerId"); parameters.Add(new MySqlParameter("@managerId", request.ManagerId)); }
        if (request.DepartmentHeadId != null) { updates.Add("department_head_id = @departmentHeadId"); parameters.Add(new MySqlParameter("@departmentHeadId", request.DepartmentHeadId)); }
        if (request.CompanyHeadId != null) { updates.Add("company_head_id = @companyHeadId"); parameters.Add(new MySqlParameter("@companyHeadId", request.CompanyHeadId)); }
        if (request.BackupIds != null) { updates.Add("backup_ids = @backupIds"); parameters.Add(new MySqlParameter("@backupIds", request.BackupIds)); }
        if (request.NotifyMethods != null) { updates.Add("notify_methods = @notifyMethods"); parameters.Add(new MySqlParameter("@notifyMethods", request.NotifyMethods)); }
        if (request.Status != null) { updates.Add("status = @status"); parameters.Add(new MySqlParameter("@status", request.Status)); }
        if (request.Remark != null) { updates.Add("remark = @remark"); parameters.Add(new MySqlParameter("@remark", request.Remark)); }

        var sql = $"UPDATE dispatch_rules SET {string.Join(", ", updates)} WHERE id = @id";
        using var cmd = new MySqlCommand(sql, _db);
        foreach (var p in parameters) cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("更新派单规则: {Id}", id);
        return Ok(new { success = true, message = "规则更新成功" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var checkCmd = new MySqlCommand("SELECT id FROM dispatch_rules WHERE id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var checkReader = await checkCmd.ExecuteReaderAsync();
        if (!await checkReader.ReadAsync())
        {
            checkReader.Close();
            return NotFound(new { success = false, message = "规则不存在" });
        }
        checkReader.Close();

        using var cmd = new MySqlCommand("DELETE FROM dispatch_rules WHERE id = @id", _db);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("删除派单规则: {Id}", id);
        return Ok(new { success = true, message = "规则删除成功" });
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        using var checkCmd = new MySqlCommand("SELECT status FROM dispatch_rules WHERE id = @id", _db);
        checkCmd.Parameters.AddWithValue("@id", id);
        using var checkReader = await checkCmd.ExecuteReaderAsync();
        if (!await checkReader.ReadAsync())
        {
            checkReader.Close();
            return NotFound(new { success = false, message = "规则不存在" });
        }
        var currentStatus = checkReader["status"].ToString();
        checkReader.Close();

        var newStatus = currentStatus == "active" ? "inactive" : "active";
        using var cmd = new MySqlCommand("UPDATE dispatch_rules SET status = @status, updated_at = @updatedAt WHERE id = @id", _db);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();

        return Ok(new { success = true, message = "状态已切换", data = new { status = newStatus } });
    }
}

public class CreateDispatchRuleRequest
{
    public string? Category { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? TicketColor { get; set; }
    public string? Location { get; set; }
    public string? LocationType { get; set; }
    public int Priority { get; set; } = 5;
    public bool AutoAssign { get; set; } = true;
    public bool NotifyBackup { get; set; } = false;
    public bool AllowTransfer { get; set; } = true;
    public bool TimeoutEscalation { get; set; } = true;
    public string? OperatorIds { get; set; }
    public string? SupervisorId { get; set; }
    public string? ManagerId { get; set; }
    public string? DepartmentHeadId { get; set; }
    public string? CompanyHeadId { get; set; }
    public string? BackupIds { get; set; }
    public string? NotifyMethods { get; set; }
    public string? Status { get; set; }
    public string? Remark { get; set; }
}

public class UpdateDispatchRuleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? TicketColor { get; set; }
    public string? Location { get; set; }
    public string? LocationType { get; set; }
    public int? Priority { get; set; }
    public bool? AutoAssign { get; set; }
    public bool? NotifyBackup { get; set; }
    public bool? AllowTransfer { get; set; }
    public bool? TimeoutEscalation { get; set; }
    public string? OperatorIds { get; set; }
    public string? SupervisorId { get; set; }
    public string? ManagerId { get; set; }
    public string? DepartmentHeadId { get; set; }
    public string? CompanyHeadId { get; set; }
    public string? BackupIds { get; set; }
    public string? NotifyMethods { get; set; }
    public string? Status { get; set; }
    public string? Remark { get; set; }
}
