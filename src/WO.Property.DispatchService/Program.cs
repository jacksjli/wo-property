using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text;
using Npgsql;
using Serilog;
using Serilog.Events;
using WO.Property.Shared.Models;
using WO.Property.Shared.Configuration;
using WO.Property.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// === Serilog 日志配置 ===
var serviceName = "DispatchService";
var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsPath);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", serviceName)
    .Enrich.WithProperty("Application", "WO-Property")
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine))
    .WriteTo.File(
        new Serilog.Formatting.Json.JsonFormatter(renderMessage: true, closingDelimiter: Environment.NewLine),
        Path.Combine(logsPath, $"{serviceName.ToLower()}-.log"),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 100 * 1024 * 1024,
        retainedFileCountLimit: 30,
        rollOnFileSizeLimit: true,
        shared: false,
        flushToDiskInterval: TimeSpan.FromSeconds(2))
    .CreateLogger();

builder.Host.UseSerilog();

// 配置端口 - 使用5003端口
builder.WebHost.UseUrls("http://0.0.0.0:5003");

// 添加HttpClient
builder.Services.AddHttpClient();

// JWT 配置
var jwtIssuer = "wo-property-unified-auth";
var jwtAudience = "wo-property-services";
var jwtSecretKey = JwtHelper.GetSecretKey();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加数据库初始化服务
builder.Services.AddScoped<DbInitializer>();

var app = builder.Build();

// 数据库初始化
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

app.UseRequestLogging();
app.UseGlobalExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

Log.Information("===========================================");
Log.Information("  WO Property Dispatch Service Started");
Log.Information("  Port: 5003");
Log.Information("  Database: PostgreSQL");
Log.Information("===========================================");

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "DispatchService", timestamp = DateTime.UtcNow }));

// ============ 派单规则API ============
app.MapGet("/api/dispatch-rules", [Authorize] async (DbInitializer db, [FromQuery] string? status, [FromQuery] string? type) =>
{
    var rules = await db.GetDispatchRulesAsync(status, type);
    return Results.Ok(new { success = true, data = rules });
});

app.MapGet("/api/dispatch-rules/{id}", [Authorize] async (DbInitializer db, int id) =>
{
    var rule = await db.GetDispatchRuleByIdAsync(id);
    if (rule == null)
        return Results.NotFound(new { success = false, message = "规则不存在" });
    return Results.Ok(new { success = true, data = rule });
});

app.MapPost("/api/dispatch-rules", [Authorize] async (DbInitializer db, [FromBody] DispatchRule rule) =>
{
    var year = DateTime.Now.Year;
    var count = await db.GetDispatchRuleCountAsync() + 1;
    rule.RuleNo = $"DR-{year}-{count:D4}";
    rule.CreatedAt = DateTime.UtcNow;
    rule.UpdatedAt = DateTime.UtcNow;
    
    await db.CreateDispatchRuleAsync(rule);
    
    return Results.Ok(new { success = true, message = "规则创建成功", data = rule });
});

app.MapPut("/api/dispatch-rules/{id}", [Authorize] async (DbInitializer db, int id, [FromBody] DispatchRule updatedRule) =>
{
    var existing = await db.GetDispatchRuleByIdAsync(id);
    if (existing == null)
        return Results.NotFound(new { success = false, message = "规则不存在" });
    
    updatedRule.Id = id;
    updatedRule.UpdatedAt = DateTime.UtcNow;
    await db.UpdateDispatchRuleAsync(updatedRule);
    
    return Results.Ok(new { success = true, message = "规则更新成功", data = updatedRule });
});

app.MapDelete("/api/dispatch-rules/{id}", [Authorize] async (DbInitializer db, int id) =>
{
    var existing = await db.GetDispatchRuleByIdAsync(id);
    if (existing == null)
        return Results.NotFound(new { success = false, message = "规则不存在" });
    
    await db.DeleteDispatchRuleAsync(id);
    return Results.Ok(new { success = true, message = "规则删除成功" });
});

app.MapPost("/api/dispatch-rules/{id}/toggle-status", [Authorize] async (DbInitializer db, int id) =>
{
    var rule = await db.GetDispatchRuleByIdAsync(id);
    if (rule == null)
        return Results.NotFound(new { success = false, message = "规则不存在" });
    
    rule.Status = rule.Status == "active" ? "inactive" : "active";
    rule.UpdatedAt = DateTime.UtcNow;
    await db.UpdateDispatchRuleAsync(rule);
    
    return Results.Ok(new { success = true, message = "状态已切换", data = rule });
});

app.MapPost("/api/dispatch-rules/match", [Authorize] async (DbInitializer db, [FromBody] MatchRequest request) =>
{
    var rules = await db.GetDispatchRulesAsync("active", null);
    
    var matched = rules.FirstOrDefault(r =>
    {
        // 匹配工单类型
        if (!string.IsNullOrEmpty(request.TicketType) && r.Type != request.TicketType && r.Type != "other")
            return false;
        
        // 匹配颜色
        if (!string.IsNullOrEmpty(request.TicketColor) && r.TicketColor != request.TicketColor && r.TicketColor != "all")
            return false;
        
        // 匹配地点
        if (!string.IsNullOrEmpty(r.Location) && !string.IsNullOrEmpty(request.Location))
        {
            if (r.LocationType == "exact" && r.Location != request.Location)
                return false;
            if (r.LocationType == "contains" && !request.Location.Contains(r.Location))
                return false;
        }
        
        return true;
    });
    
    return Results.Ok(new { success = true, data = matched });
});

// ============ 派单任务API ============
app.MapPost("/api/dispatch-tasks", [Authorize] async (IHttpClientFactory httpFactory, HttpContext httpContext, [FromBody] CreateTaskRequest request) =>
{
    // 调用 TicketService 创建派单任务
    DispatchTask? createdTask = null;
    try
    {
        var client = httpFactory.CreateClient();
        var originalToken = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        var createPayload = new
        {
            TicketId = request.TicketId,
            RuleId = request.RuleId,
            AssignedToPersonId = request.AssignedToPersonId,
            AssignedTo = request.AssignedTo,
            AssignedBy = request.AssignedBy,
            TicketColor = request.TicketColor,
            Notes = request.Notes
        };

        var ticketServiceRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5002/internal/dispatch-tasks")
        {
            Content = JsonContent.Create(createPayload)
        };
        if (!string.IsNullOrEmpty(originalToken))
            ticketServiceRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(originalToken);

        var ticketResponse = await client.SendAsync(ticketServiceRequest);
        if (!ticketResponse.IsSuccessStatusCode)
        {
            var error = await ticketResponse.Content.ReadAsStringAsync();
            return Results.BadRequest(new { success = false, message = $"无法创建派单任务: {error}" });
        }

        var ticketResult = await ticketResponse.Content.ReadFromJsonAsync<ApiResponse<DispatchTask>>();
        createdTask = ticketResult?.Data;
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = $"无法连接 TicketService: {ex.Message}" });
    }

    return Results.Ok(new { success = true, message = "任务创建成功", data = createdTask });
});

app.MapGet("/api/dispatch-tasks", [Authorize] async (IHttpClientFactory httpFactory, HttpContext httpContext) =>
{
    try
    {
        var client = httpFactory.CreateClient();
        var originalToken = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5002/internal/dispatch-tasks");
        if (!string.IsNullOrEmpty(originalToken))
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(originalToken);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Results.BadRequest(new { success = false, message = $"无法获取派单任务: {error}" });
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DispatchTask>>>();
        return Results.Ok(new { success = true, data = result?.Data ?? new List<DispatchTask>() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = $"无法连接 TicketService: {ex.Message}" });
    }
});

// ============ 超时规则API ============
app.MapGet("/api/timeout-rules", [Authorize] async (DbInitializer db) =>
{
    var rules = await db.GetTimeoutRulesAsync();
    return Results.Ok(new { success = true, data = rules });
});

app.MapPut("/api/timeout-rules", [Authorize] async (DbInitializer db, [FromBody] List<TimeoutRule> rules) =>
{
    foreach (var rule in rules)
    {
        await db.UpdateTimeoutRuleAsync(rule);
    }
    return Results.Ok(new { success = true, message = "超时规则已更新" });
});

app.MapPost("/api/timeout-rules/reset", [Authorize] async (DbInitializer db) =>
{
    await db.ResetTimeoutRulesAsync();
    return Results.Ok(new { success = true, message = "超时规则已重置为默认值" });
});

// ============ 统计API ============
app.MapGet("/api/dispatch/stats", [Authorize] async (DbInitializer db) =>
{
    var totalRules = await db.GetDispatchRuleCountAsync();
    var activeRules = (await db.GetDispatchRulesAsync("active", null)).Count;

    var stats = new
    {
        rules = new
        {
            total = totalRules,
            active = activeRules,
            inactive = totalRules - activeRules,
            avgResponseTime = 0
        },
        note = "任务统计请调用 TicketService /api/tickets/stats/daily"
    };

    return Results.Ok(new { success = true, data = stats });
});

app.MapGet("/api/dispatch/analytics", [Authorize] async (DbInitializer db, [FromQuery] int days = 7) =>
{
    var rules = await db.GetDispatchRulesAsync(null, null);
    var topRules = rules
        .Where(r => r.MatchCount > 0)
        .OrderByDescending(r => r.MatchCount)
        .Take(5)
        .Select(r => new { ruleId = r.Id, ruleName = r.Name, ruleNo = r.RuleNo, matchCount = r.MatchCount, successRate = r.SuccessCount * 100.0 / r.MatchCount })
        .ToList();

    return Results.Ok(new { success = true, data = new { topRules, note = "任务统计请调用 TicketService" } });
});

app.Run();

// ============ 数据库访问层 ============
public class DbInitializer
{
    private readonly string _connectionString;

    public DbInitializer(IConfiguration configuration)
    {
        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("DB_CONNECTION_STRING not configured");
    }

    public async Task InitializeAsync()
    {
        // 创建数据库（如果不存在）- 使用 Npgsql 连接 PostgreSQL
        var connString = new NpgsqlConnectionStringBuilder(_connectionString);
        var masterBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = connString.Host,
            Port = connString.Port,
            Username = connString.Username,
            Password = connString.Password,
            Database = "postgres"
        };
        
        await using var masterConn = new NpgsqlConnection(masterBuilder.ToString());
        await masterConn.OpenAsync();
        
        // 检查数据库是否存在，不存在则创建
        var checkDbCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", masterConn);
        checkDbCmd.Parameters.AddWithValue("@dbName", connString.Database);
        var exists = await checkDbCmd.ExecuteScalarAsync();
        
        if (exists == null)
        {
            var createDbCmd = new NpgsqlCommand($"CREATE DATABASE \"{connString.Database}\"", masterConn);
            await createDbCmd.ExecuteNonQueryAsync();
        }

        // 连接目标数据库创建表
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // dispatch_rules 表
        var createDispatchRulesTable = @"
        CREATE TABLE IF NOT EXISTS dispatch_rules (
            id SERIAL PRIMARY KEY,
            rule_no VARCHAR(50) NOT NULL,
            name VARCHAR(200) NOT NULL,
            description TEXT,
            type VARCHAR(50) DEFAULT 'ticket_type',
            ticket_color VARCHAR(20) DEFAULT 'all',
            location VARCHAR(500),
            location_type VARCHAR(20) DEFAULT 'contains',
            priority INT DEFAULT 5,
            auto_assign BOOLEAN DEFAULT true,
            notify_backup BOOLEAN DEFAULT false,
            allow_transfer BOOLEAN DEFAULT true,
            timeout_escalation BOOLEAN DEFAULT true,
            operator_ids TEXT,
            supervisor_id VARCHAR(100),
            manager_id VARCHAR(100),
            department_head_id VARCHAR(100),
            company_head_id VARCHAR(100),
            backup_ids TEXT,
            notify_methods VARCHAR(50) DEFAULT 'app',
            status VARCHAR(20) DEFAULT 'active',
            match_count INT DEFAULT 0,
            success_count INT DEFAULT 0,
            avg_response_time DECIMAL(10,2) DEFAULT 0,
            remark TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );";

        // timeout_rules 表
        var createTimeoutRulesTable = @"
        CREATE TABLE IF NOT EXISTS timeout_rules (
            id SERIAL PRIMARY KEY,
            color VARCHAR(20) NOT NULL,
            role VARCHAR(50) NOT NULL,
            hours INT DEFAULT 24,
            enabled BOOLEAN DEFAULT true,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );";

        await using var cmd1 = new NpgsqlCommand(createDispatchRulesTable, connection);
        await cmd1.ExecuteNonQueryAsync();

        await using var cmd2 = new NpgsqlCommand(createTimeoutRulesTable, connection);
        await cmd2.ExecuteNonQueryAsync();

        // 初始化超时规则（如果为空）
        await using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM timeout_rules", connection);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        
        if (count == 0)
        {
            var colors = new[] { "green", "blue", "orange", "red" };
            var roles = new[] { "operator", "supervisor", "manager", "department_head", "company_head" };
            var hours = new[] { 72, 48, 24, 12, 4 };

            foreach (var color in colors)
            {
                for (int i = 0; i < roles.Length; i++)
                {
                    await using var insertCmd = new NpgsqlCommand(
                        "INSERT INTO timeout_rules (color, role, hours, enabled) VALUES (@color, @role, @hours, true)", connection);
                    insertCmd.Parameters.AddWithValue("@color", color);
                    insertCmd.Parameters.AddWithValue("@role", roles[i]);
                    insertCmd.Parameters.AddWithValue("@hours", hours[i]);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
        }

        Console.WriteLine("  Database initialized: dispatch_rules, timeout_rules");
    }

    // DispatchRules 操作
    public async Task<List<DispatchRule>> GetDispatchRulesAsync(string? status, string? type)
    {
        var rules = new List<DispatchRule>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM dispatch_rules WHERE 1=1";
        if (!string.IsNullOrEmpty(status)) sql += " AND status = @status";
        if (!string.IsNullOrEmpty(type)) sql += " AND type = @type";
        sql += " ORDER BY id DESC";

        await using var cmd = new NpgsqlCommand(sql, connection);
        if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@status", status);
        if (!string.IsNullOrEmpty(type)) cmd.Parameters.AddWithValue("@type", type);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rules.Add(ReadDispatchRule(reader));
        }
        return rules;
    }

    public async Task<DispatchRule?> GetDispatchRuleByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM dispatch_rules WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadDispatchRule(reader);
        }
        return null;
    }

    public async Task<int> GetDispatchRuleCountAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT COUNT(*) FROM dispatch_rules";
        await using var cmd = new NpgsqlCommand(sql, connection);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task CreateDispatchRuleAsync(DispatchRule rule)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"INSERT INTO dispatch_rules (rule_no, name, description, type, ticket_color, location, location_type,
            priority, auto_assign, notify_backup, allow_transfer, timeout_escalation, operator_ids, supervisor_id,
            manager_id, department_head_id, company_head_id, backup_ids, notify_methods, status, match_count,
            success_count, avg_response_time, remark, created_at, updated_at)
            VALUES (@ruleNo, @name, @description, @type, @ticketColor, @location, @locationType,
            @priority, @autoAssign, @notifyBackup, @allowTransfer, @timeoutEscalation, @operatorIds, @supervisorId,
            @managerId, @departmentHeadId, @companyHeadId, @backupIds, @notifyMethods, @status, @matchCount,
            @successCount, @avgResponseTime, @remark, @createdAt, @updatedAt);
            SELECT lastval();";

        await using var cmd = new NpgsqlCommand(sql, connection);
        AddDispatchRuleParameters(cmd, rule);

        rule.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task UpdateDispatchRuleAsync(DispatchRule rule)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"UPDATE dispatch_rules SET rule_no=@ruleNo, name=@name, description=@description, type=@type,
            ticket_color=@ticketColor, location=@location, location_type=@locationType, priority=@priority,
            auto_assign=@autoAssign, notify_backup=@notifyBackup, allow_transfer=@allowTransfer, timeout_escalation=@timeoutEscalation,
            operator_ids=@operatorIds, supervisor_id=@supervisorId, manager_id=@managerId, department_head_id=@departmentHeadId,
            company_head_id=@companyHeadId, backup_ids=@backupIds, notify_methods=@notifyMethods, status=@status,
            match_count=@matchCount, success_count=@successCount, avg_response_time=@avgResponseTime, remark=@remark, updated_at=@updatedAt
            WHERE id=@id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        AddDispatchRuleParameters(cmd, rule);
        cmd.Parameters.AddWithValue("@id", rule.Id);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteDispatchRuleAsync(int id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM dispatch_rules WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    private void AddDispatchRuleParameters(NpgsqlCommand cmd, DispatchRule rule)
    {
        cmd.Parameters.AddWithValue("@ruleNo", rule.RuleNo ?? "");
        cmd.Parameters.AddWithValue("@name", rule.Name ?? "");
        cmd.Parameters.AddWithValue("@description", (object?)rule.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@type", rule.Type ?? "ticket_type");
        cmd.Parameters.AddWithValue("@ticketColor", rule.TicketColor ?? "all");
        cmd.Parameters.AddWithValue("@location", (object?)rule.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@locationType", rule.LocationType ?? "contains");
        cmd.Parameters.AddWithValue("@priority", rule.Priority);
        cmd.Parameters.AddWithValue("@autoAssign", rule.AutoAssign);
        cmd.Parameters.AddWithValue("@notifyBackup", rule.NotifyBackup);
        cmd.Parameters.AddWithValue("@allowTransfer", rule.AllowTransfer);
        cmd.Parameters.AddWithValue("@timeoutEscalation", rule.TimeoutEscalation);
        cmd.Parameters.AddWithValue("@operatorIds", (object?)rule.OperatorIds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@supervisorId", (object?)rule.SupervisorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@managerId", (object?)rule.ManagerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@departmentHeadId", (object?)rule.DepartmentHeadId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@companyHeadId", (object?)rule.CompanyHeadId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@backupIds", (object?)rule.BackupIds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@notifyMethods", rule.NotifyMethods ?? "app");
        cmd.Parameters.AddWithValue("@status", rule.Status ?? "active");
        cmd.Parameters.AddWithValue("@matchCount", rule.MatchCount);
        cmd.Parameters.AddWithValue("@successCount", rule.SuccessCount);
        cmd.Parameters.AddWithValue("@avgResponseTime", rule.AvgResponseTime);
        cmd.Parameters.AddWithValue("@remark", (object?)rule.Remark ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@createdAt", rule.CreatedAt);
    }

    private DispatchRule ReadDispatchRule(NpgsqlDataReader reader)
    {
        return new DispatchRule
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            RuleNo = reader.GetString(reader.GetOrdinal("rule_no")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            Type = reader.GetString(reader.GetOrdinal("type")),
            TicketColor = reader.GetString(reader.GetOrdinal("ticket_color")),
            Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
            LocationType = reader.GetString(reader.GetOrdinal("location_type")),
            Priority = reader.GetInt32(reader.GetOrdinal("priority")),
            AutoAssign = reader.GetBoolean(reader.GetOrdinal("auto_assign")),
            NotifyBackup = reader.GetBoolean(reader.GetOrdinal("notify_backup")),
            AllowTransfer = reader.GetBoolean(reader.GetOrdinal("allow_transfer")),
            TimeoutEscalation = reader.GetBoolean(reader.GetOrdinal("timeout_escalation")),
            OperatorIds = reader.IsDBNull(reader.GetOrdinal("operator_ids")) ? null : reader.GetString(reader.GetOrdinal("operator_ids")),
            SupervisorId = reader.IsDBNull(reader.GetOrdinal("supervisor_id")) ? null : reader.GetString(reader.GetOrdinal("supervisor_id")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("manager_id")) ? null : reader.GetString(reader.GetOrdinal("manager_id")),
            DepartmentHeadId = reader.IsDBNull(reader.GetOrdinal("department_head_id")) ? null : reader.GetString(reader.GetOrdinal("department_head_id")),
            CompanyHeadId = reader.IsDBNull(reader.GetOrdinal("company_head_id")) ? null : reader.GetString(reader.GetOrdinal("company_head_id")),
            BackupIds = reader.IsDBNull(reader.GetOrdinal("backup_ids")) ? null : reader.GetString(reader.GetOrdinal("backup_ids")),
            NotifyMethods = reader.GetString(reader.GetOrdinal("notify_methods")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            MatchCount = reader.GetInt32(reader.GetOrdinal("match_count")),
            SuccessCount = reader.GetInt32(reader.GetOrdinal("success_count")),
            AvgResponseTime = reader.GetDecimal(reader.GetOrdinal("avg_response_time")),
            Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    // TimeoutRules 操作
    public async Task<List<TimeoutRule>> GetTimeoutRulesAsync()
    {
        var rules = new List<TimeoutRule>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM timeout_rules ORDER BY CASE color WHEN 'green' THEN 1 WHEN 'blue' THEN 2 WHEN 'orange' THEN 3 WHEN 'red' THEN 4 END, CASE role WHEN 'operator' THEN 1 WHEN 'supervisor' THEN 2 WHEN 'manager' THEN 3 WHEN 'department_head' THEN 4 WHEN 'company_head' THEN 5 END";
        await using var cmd = new NpgsqlCommand(sql, connection);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rules.Add(new TimeoutRule
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Color = reader.GetString(reader.GetOrdinal("color")),
                Role = reader.GetString(reader.GetOrdinal("role")),
                Hours = reader.GetInt32(reader.GetOrdinal("hours")),
                Enabled = reader.GetBoolean(reader.GetOrdinal("enabled"))
            });
        }
        return rules;
    }

    public async Task UpdateTimeoutRuleAsync(TimeoutRule rule)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "UPDATE timeout_rules SET hours=@hours, enabled=@enabled WHERE id=@id";
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hours", rule.Hours);
        cmd.Parameters.AddWithValue("@enabled", rule.Enabled);
        cmd.Parameters.AddWithValue("@id", rule.Id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetTimeoutRulesAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // 清空并重置
        await using var deleteCmd = new NpgsqlCommand("DELETE FROM timeout_rules", connection);
        await deleteCmd.ExecuteNonQueryAsync();

        var colors = new[] { "green", "blue", "orange", "red" };
        var roles = new[] { "operator", "supervisor", "manager", "department_head", "company_head" };
        var hours = new[] { 72, 48, 24, 12, 4 };

        for (int i = 0; i < colors.Length; i++)
        {
            for (int j = 0; j < roles.Length; j++)
            {
                await using var insertCmd = new NpgsqlCommand(
                    "INSERT INTO timeout_rules (color, role, hours, enabled) VALUES (@color, @role, @hours, true)", connection);
                insertCmd.Parameters.AddWithValue("@color", colors[i]);
                insertCmd.Parameters.AddWithValue("@role", roles[j]);
                insertCmd.Parameters.AddWithValue("@hours", hours[j]);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }
}

// ============ 数据模型 ============
public class DispatchRule : BaseEntity
{
    public string RuleNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "ticket_type";
    public string TicketColor { get; set; } = "all";
    public string? Location { get; set; }
    public string LocationType { get; set; } = "contains";
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
    public string NotifyMethods { get; set; } = "app";
    public string Status { get; set; } = "active";
    public int MatchCount { get; set; } = 0;
    public int SuccessCount { get; set; } = 0;
    public decimal AvgResponseTime { get; set; } = 0;
    public string? Remark { get; set; }
}

public class TimeoutRule : BaseEntity
{
    public string Color { get; set; } = "green";
    public string Role { get; set; } = "operator";
    public int Hours { get; set; } = 24;
    public bool Enabled { get; set; } = true;
}

// 请求模型
public class MatchRequest
{
    public string? TicketType { get; set; }
    public string? TicketColor { get; set; }
    public string? Location { get; set; }
}

public class CreateTaskRequest
{
    public int? TicketId { get; set; }
    public int? RuleId { get; set; }
    public int? AssignedToPersonId { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedBy { get; set; }
    public string? TicketColor { get; set; }
    public string? Notes { get; set; }
}

public class PersonDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

// 通用 API 响应封装
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

// 用于反序列化 TicketService 返回的 DispatchTask
public class DispatchTask
{
    public int Id { get; set; }
    public string TaskNo { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public int? RuleId { get; set; }
    public int? AssignedToPersonId { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? TimeoutAt { get; set; }
    public string? Notes { get; set; }
}

// JSON Content 扩展
public static class JsonContent
{
    public static StringContent Create(object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}