using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.Shared.Configuration;
using WO.Property.StatisticsService.Models;
using WO.Property.StatisticsService.Tenant;

var builder = WebApplication.CreateBuilder(args);

// 配置端口
builder.WebHost.UseUrls("http://0.0.0.0:5241");

// Tenant 支持
builder.Services.AddSingleton<TenantConfigLoader>();
builder.Services.AddSingleton<ITenantDbFactory, TenantDbFactory>();

// 获取数据库连接字符串（支持 X-Project 动态切换）
// 注意：StatisticsService 当前使用固定数据库连接，X-Project 路由仅用于中间件验证
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;AllowUserVariables=true";

// 注册 MySQL 连接工厂（每次请求创建新连接）
builder.Services.AddScoped<MySqlConnection>(sp => {
    var factory = sp.GetService<ITenantDbFactory>();
    if (factory != null)
    {
        try {
            var tenantCode = factory.GetCurrentTenantCode() ?? "wo_property";
            return new MySqlConnection(factory.GetTenantConnectionString(tenantCode));
        } catch { }
    }
    return new MySqlConnection(dbConnectionString);
});

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

// 初始化数据库表（MySQL 语法）
using (var connection = new MySqlConnection(dbConnectionString))
{
    await connection.OpenAsync();

    var initSql = @"
CREATE TABLE IF NOT EXISTS dashboard_configs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    layout TEXT,
    metrics TEXT,
    created_by VARCHAR(50) DEFAULT '系统',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS metric_snapshots (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    snapshot_date DATE NOT NULL UNIQUE,
    total_properties INT DEFAULT 0,
    total_units INT DEFAULT 0,
    occupied_units INT DEFAULT 0,
    occupancy_rate DECIMAL(5,2) DEFAULT 0,
    total_revenue DECIMAL(15,2) DEFAULT 0,
    property_fee_revenue DECIMAL(15,2) DEFAULT 0,
    other_revenue DECIMAL(15,2) DEFAULT 0,
    total_expense DECIMAL(15,2) DEFAULT 0,
    collection_rate DECIMAL(5,2) DEFAULT 0,
    total_complaints INT DEFAULT 0,
    resolved_complaints INT DEFAULT 0,
    complaint_resolve_rate DECIMAL(5,2) DEFAULT 0,
    total_suggestions INT DEFAULT 0,
    total_tickets INT DEFAULT 0,
    resolved_tickets INT DEFAULT 0,
    total_inspections INT DEFAULT 0,
    passed_inspections INT DEFAULT 0,
    issues_found INT DEFAULT 0,
    issues_resolved INT DEFAULT 0,
    total_visitors INT DEFAULT 0,
    active_visitors INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS reports (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    report_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200),
    type VARCHAR(20),
    category VARCHAR(20),
    start_date DATE,
    end_date DATE,
    data TEXT,
    summary TEXT,
    generated_by VARCHAR(100),
    generated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS trend_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    metric_name VARCHAR(100) NOT NULL,
    record_date DATE NOT NULL,
    value DECIMAL(15,4) DEFAULT 0,
    category VARCHAR(50),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
";

    using var cmd = new MySqlCommand(initSql, connection);
    await cmd.ExecuteNonQueryAsync();

    // 安全创建索引（如果不存在则创建）
    var createIndexSql = @"CALL AddIndexSafe('trend_records', 'idx_tr_metric_date', 'metric_name, record_date');";
    try
    {
        await using var createIdxCmd = new MySqlCommand(createIndexSql, connection);
        await createIdxCmd.ExecuteNonQueryAsync();
    }
    catch
    {
        // 忽略索引已存在的错误
    }
}

app.UseCors("AllowAdminPortal");

// X-Project 路由中间件
app.UseMiddleware<WO.Property.StatisticsService.Middleware.TenantRoutingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "StatisticsService", timestamp = DateTime.UtcNow }));

app.MapGet("/api/metrics/indicators", [Authorize] async (MySqlConnection db) =>
{
    await db.OpenAsync();

    var snapshot = new MetricSnapshotDto();

    using var cmd = new MySqlCommand("SELECT * FROM metric_snapshots ORDER BY snapshot_date DESC LIMIT 1", db);
    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        int totalPropertiesIdx = reader.GetOrdinal("total_properties");
        int totalUnitsIdx = reader.GetOrdinal("total_units");
        int occupancyRateIdx = reader.GetOrdinal("occupancy_rate");
        int totalRevenueIdx = reader.GetOrdinal("total_revenue");
        int propertyFeeRevenueIdx = reader.GetOrdinal("property_fee_revenue");
        int collectionRateIdx = reader.GetOrdinal("collection_rate");
        int totalExpenseIdx = reader.GetOrdinal("total_expense");
        int totalComplaintsIdx = reader.GetOrdinal("total_complaints");
        int resolvedComplaintsIdx = reader.GetOrdinal("resolved_complaints");
        int complaintResolveRateIdx = reader.GetOrdinal("complaint_resolve_rate");
        int totalTicketsIdx = reader.GetOrdinal("total_tickets");
        int resolvedTicketsIdx = reader.GetOrdinal("resolved_tickets");
        int totalInspectionsIdx = reader.GetOrdinal("total_inspections");
        int passedInspectionsIdx = reader.GetOrdinal("passed_inspections");
        int issuesFoundIdx = reader.GetOrdinal("issues_found");
        int issuesResolvedIdx = reader.GetOrdinal("issues_resolved");
        int totalVisitorsIdx = reader.GetOrdinal("total_visitors");
        int activeVisitorsIdx = reader.GetOrdinal("active_visitors");

        snapshot.TotalProperties = reader.GetInt32(totalPropertiesIdx);
        snapshot.TotalUnits = reader.GetInt32(totalUnitsIdx);
        snapshot.OccupancyRate = reader.GetDecimal(occupancyRateIdx);
        snapshot.TotalRevenue = reader.GetDecimal(totalRevenueIdx);
        snapshot.PropertyFeeRevenue = reader.GetDecimal(propertyFeeRevenueIdx);
        snapshot.CollectionRate = reader.GetDecimal(collectionRateIdx);
        snapshot.TotalExpense = reader.GetDecimal(totalExpenseIdx);
        snapshot.TotalComplaints = reader.GetInt32(totalComplaintsIdx);
        snapshot.ResolvedComplaints = reader.GetInt32(resolvedComplaintsIdx);
        snapshot.ComplaintResolveRate = reader.GetDecimal(complaintResolveRateIdx);
        snapshot.TotalTickets = reader.GetInt32(totalTicketsIdx);
        snapshot.ResolvedTickets = reader.GetInt32(resolvedTicketsIdx);
        snapshot.TotalInspections = reader.GetInt32(totalInspectionsIdx);
        snapshot.PassedInspections = reader.GetInt32(passedInspectionsIdx);
        snapshot.IssuesFound = reader.GetInt32(issuesFoundIdx);
        snapshot.IssuesResolved = reader.GetInt32(issuesResolvedIdx);
        snapshot.TotalVisitors = reader.GetInt32(totalVisitorsIdx);
        snapshot.ActiveVisitors = reader.GetInt32(activeVisitorsIdx);
    }

    var result = new
    {
        operation = new
        {
            totalProperties = snapshot.TotalProperties,
            totalUnits = snapshot.TotalUnits,
            occupancyRate = snapshot.OccupancyRate
        },
        financial = new
        {
            totalRevenue = snapshot.TotalRevenue,
            propertyFeeRevenue = snapshot.PropertyFeeRevenue,
            collectionRate = snapshot.CollectionRate,
            totalExpense = snapshot.TotalExpense
        },
        customer = new
        {
            complaints = snapshot.TotalComplaints,
            resolvedComplaints = snapshot.ResolvedComplaints,
            complaintResolveRate = snapshot.ComplaintResolveRate,
            tickets = snapshot.TotalTickets,
            completedTickets = snapshot.ResolvedTickets
        },
        inspection = new
        {
            totalInspections = snapshot.TotalInspections,
            passedInspections = snapshot.PassedInspections,
            issuesFound = snapshot.IssuesFound,
            resolvedIssues = snapshot.IssuesResolved
        },
        visitor = new
        {
            totalVisitors = snapshot.TotalVisitors,
            activeVisitors = snapshot.ActiveVisitors
        },
        timestamp = DateTime.UtcNow
    };

    return Results.Ok(result);
});

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property Statistics Service");
Console.WriteLine("  Port: 5026");
Console.WriteLine("  Database: MySQL (wo_property)");
Console.WriteLine("===========================================");

app.Run();

// ==================== 设备报表 API ====================
app.MapGet("/api/device-reports", async (HttpContext context) =>
{
    try
    {
        var projectCode = context.Request.Headers["X-Project"].FirstOrDefault() ?? "wo_property";
        var connStr = $"Server=127.0.0.1;Port=3306;Database={projectCode};User=root;Password=;CharSet=utf8mb4";
        
        await using var conn = new MySqlConnector.MySqlConnection(connStr);
        await conn.OpenAsync();
        
        await using var cmd = new MySqlConnector.MySqlCommand("SELECT * FROM DeviceReports ORDER BY CreatedAt DESC LIMIT 100", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var results = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
        }
        
        return Results.Ok(new { success = true, data = results });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = ex.Message });
    }
});

// ==================== 工单报表 API ====================
app.MapGet("/api/ticket-reports", async (HttpContext context) =>
{
    try
    {
        var projectCode = context.Request.Headers["X-Project"].FirstOrDefault() ?? "wo_property";
        var connStr = $"Server=127.0.0.1;Port=3306;Database={projectCode};User=root;Password=;CharSet=utf8mb4";
        
        await using var conn = new MySqlConnector.MySqlConnection(connStr);
        await conn.OpenAsync();
        
        await using var cmd = new MySqlConnector.MySqlCommand("SELECT * FROM TicketReports ORDER BY CreatedAt DESC LIMIT 100", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var results = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
        }
        
        return Results.Ok(new { success = true, data = results });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = ex.Message });
    }
});

// ==================== 物料报表 API ====================
app.MapGet("/api/material-reports", async (HttpContext context) =>
{
    try
    {
        var projectCode = context.Request.Headers["X-Project"].FirstOrDefault() ?? "wo_property";
        var connStr = $"Server=127.0.0.1;Port=3306;Database={projectCode};User=root;Password=;CharSet=utf8mb4";
        
        await using var conn = new MySqlConnector.MySqlConnection(connStr);
        await conn.OpenAsync();
        
        await using var cmd = new MySqlConnector.MySqlCommand("SELECT * FROM MaterialReports ORDER BY CreatedAt DESC LIMIT 100", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var results = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
        }
        
        return Results.Ok(new { success = true, data = results });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = ex.Message });
    }
});

// ==================== 满意度调查 API ====================
app.MapGet("/api/satisfaction-surveys", async (HttpContext context) =>
{
    try
    {
        var projectCode = context.Request.Headers["X-Project"].FirstOrDefault() ?? "wo_property";
        var connStr = $"Server=127.0.0.1;Port=3306;Database={projectCode};User=root;Password=;CharSet=utf8mb4";
        
        await using var conn = new MySqlConnector.MySqlConnection(connStr);
        await conn.OpenAsync();
        
        await using var cmd = new MySqlConnector.MySqlCommand("SELECT * FROM SatisfactionSurveys ORDER BY CreatedAt DESC LIMIT 100", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var results = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
        }
        
        return Results.Ok(new { success = true, data = results });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = ex.Message });
    }
});

app.Run();
