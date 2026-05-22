using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.Shared.Configuration;
using WO.Property.TicketTypeService.Models;

var builder = WebApplication.CreateBuilder(args);

// === Serilog 日志配置 ===
var serviceName = "TicketTypeService";
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

// 配置端口
var port = Environment.GetEnvironmentVariable("PORT") ?? "5029";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 获取数据库连接字符串
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!";

builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(dbConnectionString));

// JWT 配置
var jwtIssuer = JwtHelper.GetIssuer();
var jwtAudience = JwtHelper.GetAudience();
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

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminPortal", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// 初始化表
using (var connection = new NpgsqlConnection(dbConnectionString))
{
    await connection.OpenAsync();
    var initSql = @"
CREATE TABLE IF NOT EXISTS ticket_types (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(20) DEFAULT 'Active',
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);";
    using var cmd = new NpgsqlCommand(initSql, connection);
    await cmd.ExecuteNonQueryAsync();
}

app.UseCors("AllowAdminPortal");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================
// GET /api/ticket-types - 获取所有类型
// ============================================
app.MapGet("/api/ticket-types", async (NpgsqlConnection db) =>
{
    await db.OpenAsync();
    var list = new List<TicketTypeDto>();
    using var cmd = new NpgsqlCommand(
        "SELECT id, name, code, status, sort_order, created_at, updated_at FROM ticket_types ORDER BY sort_order ASC, id ASC",
        db);
    using var reader = await cmd.ExecuteReaderAsync();
    int idIdx = reader.GetOrdinal("id");
    int nameIdx = reader.GetOrdinal("name");
    int codeIdx = reader.GetOrdinal("code");
    int statusIdx = reader.GetOrdinal("status");
    int sortOrderIdx = reader.GetOrdinal("sort_order");
    int createdAtIdx = reader.GetOrdinal("created_at");
    int updatedAtIdx = reader.GetOrdinal("updated_at");
    while (await reader.ReadAsync())
    {
        list.Add(new TicketTypeDto
        {
            Id = reader.GetInt64(idIdx),
            Name = reader.GetString(nameIdx),
            Code = reader.GetString(codeIdx),
            Status = reader.IsDBNull(statusIdx) ? "Active" : reader.GetString(statusIdx),
            SortOrder = reader.IsDBNull(sortOrderIdx) ? 0 : reader.GetInt32(sortOrderIdx),
            CreatedAt = reader.IsDBNull(createdAtIdx) ? DateTime.UtcNow : reader.GetDateTime(createdAtIdx),
            UpdatedAt = reader.IsDBNull(updatedAtIdx) ? null : reader.GetDateTime(updatedAtIdx)
        });
    }
    return Results.Ok(list);
}).RequireAuthorization();

// ============================================
// GET /api/ticket-types/{id} - 获取单个类型
// ============================================
app.MapGet("/api/ticket-types/{id}", async (long id, NpgsqlConnection db) =>
{
    await db.OpenAsync();
    using var cmd = new NpgsqlCommand(
        "SELECT id, name, code, status, sort_order, created_at, updated_at FROM ticket_types WHERE id = @id",
        db);
    cmd.Parameters.AddWithValue("@id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    int idIdx = reader.GetOrdinal("id");
    int nameIdx = reader.GetOrdinal("name");
    int codeIdx = reader.GetOrdinal("code");
    int statusIdx = reader.GetOrdinal("status");
    int sortOrderIdx = reader.GetOrdinal("sort_order");
    int createdAtIdx = reader.GetOrdinal("created_at");
    int updatedAtIdx = reader.GetOrdinal("updated_at");
    if (await reader.ReadAsync())
    {
        return Results.Ok(new TicketTypeDto
        {
            Id = reader.GetInt64(idIdx),
            Name = reader.GetString(nameIdx),
            Code = reader.GetString(codeIdx),
            Status = reader.IsDBNull(statusIdx) ? "Active" : reader.GetString(statusIdx),
            SortOrder = reader.IsDBNull(sortOrderIdx) ? 0 : reader.GetInt32(sortOrderIdx),
            CreatedAt = reader.IsDBNull(createdAtIdx) ? DateTime.UtcNow : reader.GetDateTime(createdAtIdx),
            UpdatedAt = reader.IsDBNull(updatedAtIdx) ? null : reader.GetDateTime(updatedAtIdx)
        });
    }
    return Results.NotFound(new { message = "工单类型不存在" });
}).RequireAuthorization();

// ============================================
// POST /api/ticket-types - 创建类型
// ============================================
app.MapPost("/api/ticket-types", async (CreateTicketTypeDto dto, NpgsqlConnection db) =>
{
    await db.OpenAsync();

    // 检查编码是否已存在
    using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM ticket_types WHERE code = @code", db))
    {
        checkCmd.Parameters.AddWithValue("@code", dto.Code);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count > 0)
        {
            return Results.BadRequest(new { message = "类型编码已存在" });
        }
    }

    var now = DateTime.UtcNow;
    using var cmd = new NpgsqlCommand(
        @"INSERT INTO ticket_types (name, code, status, sort_order, created_at, updated_at)
          VALUES (@name, @code, @status, @sortOrder, @createdAt, @updatedAt)
          RETURNING id",
        db);
    cmd.Parameters.AddWithValue("@name", dto.Name);
    cmd.Parameters.AddWithValue("@code", dto.Code);
    cmd.Parameters.AddWithValue("@status", dto.Status);
    cmd.Parameters.AddWithValue("@sortOrder", dto.SortOrder);
    cmd.Parameters.AddWithValue("@createdAt", now);
    cmd.Parameters.AddWithValue("@updatedAt", now);

    var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());

    var result = new TicketTypeDto
    {
        Id = id,
        Name = dto.Name,
        Code = dto.Code,
        Status = dto.Status,
        SortOrder = dto.SortOrder,
        CreatedAt = now,
        UpdatedAt = now
    };

    return Results.Created($"/api/ticket-types/{id}", result);
}).RequireAuthorization();

// ============================================
// PUT /api/ticket-types/{id} - 更新类型
// ============================================
app.MapPut("/api/ticket-types/{id}", async (long id, UpdateTicketTypeDto dto, NpgsqlConnection db) =>
{
    await db.OpenAsync();

    // 检查是否存在
    using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM ticket_types WHERE id = @id", db))
    {
        checkCmd.Parameters.AddWithValue("@id", id);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count == 0)
        {
            return Results.NotFound(new { message = "工单类型不存在" });
        }
    }

    // 如果更新了编码，检查唯一性
    if (!string.IsNullOrEmpty(dto.Code))
    {
        using var checkCodeCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM ticket_types WHERE code = @code AND id != @id", db);
        checkCodeCmd.Parameters.AddWithValue("@code", dto.Code);
        checkCodeCmd.Parameters.AddWithValue("@id", id);
        var count = Convert.ToInt32(await checkCodeCmd.ExecuteScalarAsync());
        if (count > 0)
        {
            return Results.BadRequest(new { message = "类型编码已存在" });
        }
    }

    var setClauses = new List<string>();
    var parameters = new List<NpgsqlParameter>();

    if (dto.Name != null)
    {
        setClauses.Add("name = @name");
        parameters.Add(new NpgsqlParameter("@name", dto.Name));
    }
    if (dto.Code != null)
    {
        setClauses.Add("code = @code");
        parameters.Add(new NpgsqlParameter("@code", dto.Code));
    }
    if (dto.Status != null)
    {
        setClauses.Add("status = @status");
        parameters.Add(new NpgsqlParameter("@status", dto.Status));
    }
    if (dto.SortOrder.HasValue)
    {
        setClauses.Add("sort_order = @sortOrder");
        parameters.Add(new NpgsqlParameter("@sortOrder", dto.SortOrder.Value));
    }

    if (setClauses.Count == 0)
    {
        return Results.BadRequest(new { message = "没有需要更新的字段" });
    }

    setClauses.Add("updated_at = @updatedAt");
    parameters.Add(new NpgsqlParameter("@updatedAt", DateTime.UtcNow));
    parameters.Add(new NpgsqlParameter("@id", id));

    var sql = $"UPDATE ticket_types SET {string.Join(", ", setClauses)} WHERE id = @id";

    using var updateCmd = new NpgsqlCommand(sql, db);
    updateCmd.Parameters.AddRange(parameters.ToArray());
    await updateCmd.ExecuteNonQueryAsync();

    // 查询更新后的数据
    using var selectCmd = new NpgsqlCommand(
        "SELECT id, name, code, status, sort_order, created_at, updated_at FROM ticket_types WHERE id = @id", db);
    selectCmd.Parameters.AddWithValue("@id", id);
    using var reader = await selectCmd.ExecuteReaderAsync();
    int idIdx = reader.GetOrdinal("id");
    int nameIdx = reader.GetOrdinal("name");
    int codeIdx = reader.GetOrdinal("code");
    int statusIdx = reader.GetOrdinal("status");
    int sortOrderIdx = reader.GetOrdinal("sort_order");
    int createdAtIdx = reader.GetOrdinal("created_at");
    int updatedAtIdx = reader.GetOrdinal("updated_at");
    if (await reader.ReadAsync())
    {
        return Results.Ok(new TicketTypeDto
        {
            Id = reader.GetInt64(idIdx),
            Name = reader.GetString(nameIdx),
            Code = reader.GetString(codeIdx),
            Status = reader.IsDBNull(statusIdx) ? "Active" : reader.GetString(statusIdx),
            SortOrder = reader.IsDBNull(sortOrderIdx) ? 0 : reader.GetInt32(sortOrderIdx),
            CreatedAt = reader.IsDBNull(createdAtIdx) ? DateTime.UtcNow : reader.GetDateTime(createdAtIdx),
            UpdatedAt = reader.IsDBNull(updatedAtIdx) ? null : reader.GetDateTime(updatedAtIdx)
        });
    }
    return Results.NotFound(new { message = "更新后查询失败" });
}).RequireAuthorization();

// ============================================
// DELETE /api/ticket-types/{id} - 删除类型
// ============================================
app.MapDelete("/api/ticket-types/{id}", async (long id, NpgsqlConnection db) =>
{
    await db.OpenAsync();

    using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM ticket_types WHERE id = @id", db);
    checkCmd.Parameters.AddWithValue("@id", id);
    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
    if (count == 0)
    {
        return Results.NotFound(new { message = "工单类型不存在" });
    }

    using var cmd = new NpgsqlCommand("DELETE FROM ticket_types WHERE id = @id", db);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();

    return Results.NoContent();
}).RequireAuthorization();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TicketTypeService", port, timestamp = DateTime.UtcNow }));

Log.Information("===========================================");
Log.Information("  WO Property TicketType Service Started");
Log.Information("  Port: {Port}", port);
Log.Information("===========================================");

app.Run();
