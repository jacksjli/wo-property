using Microsoft.EntityFrameworkCore;
using WO.Property.ProjectTrackingService.Data;
using WO.Property.ProjectTrackingService.Middleware;
using WO.Property.ProjectTrackingService.Tenant;

var builder = WebApplication.CreateBuilder(args);

// 配置
builder.Services.AddSingleton<ITenantDbFactory>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<ILogger<TenantDbContextFactory>>(),
        sp.GetRequiredService<IConfiguration>()));

// DbContextFactory
builder.Services.AddDbContextFactory<TenantDbContext>((sp, options) =>
{
    var factory = sp.GetRequiredService<ITenantDbFactory>();
    var connStr = factory.GetTenantConnectionString();
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
});

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Health
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseTenantRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// 创建数据库表（如果不存在）
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<ITenantDbFactory>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        using var connection = new MySqlConnector.MySqlConnection(factory.GetTenantConnectionString());
        await connection.OpenAsync();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS project_tracking (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_no VARCHAR(20) UNIQUE NOT NULL,
                name VARCHAR(100) NOT NULL,
                type VARCHAR(10) DEFAULT '投标',
                client VARCHAR(100),
                budget DECIMAL(15,2),
                bid_amount DECIMAL(15,2),
                register_deadline DATE,
                bid_deadline DATE,
                bid_open_date DATE,
                location VARCHAR(200),
                description TEXT,
                file_status VARCHAR(10) DEFAULT '未获取',
                status VARCHAR(10) DEFAULT '意向',
                success_rate INT DEFAULT 50,
                remark TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP,
                is_deleted TINYINT(1) DEFAULT 0,
                INDEX idx_status (status),
                INDEX idx_created (created_at)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        var createRecordsSql = @"
            CREATE TABLE IF NOT EXISTS tracking_records (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_id INT NOT NULL,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                type VARCHAR(20) DEFAULT '备注',
                content TEXT NOT NULL,
                operator VARCHAR(50),
                FOREIGN KEY (project_id) REFERENCES project_tracking(id) ON DELETE CASCADE,
                INDEX idx_project (project_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        using var cmd1 = new MySqlConnector.MySqlCommand(createTableSql, connection);
        await cmd1.ExecuteNonQueryAsync();

        using var cmd2 = new MySqlConnector.MySqlCommand(createRecordsSql, connection);
        await cmd2.ExecuteNonQueryAsync();

        logger.LogInformation("Database tables verified/created successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not create database tables - will retry on first request");
    }
}

Console.WriteLine("========================================");
Console.WriteLine("  ProjectTrackingService (Phase 1)");
Console.WriteLine("  Port: 5520");
Console.WriteLine("  Health: http://localhost:5520/health");
Console.WriteLine("========================================");

app.Run();
