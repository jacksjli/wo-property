using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WO.Property.MasterDataService;
using WO.Property.MasterDataService.Data;
using WO.Property.MasterDataService.Models;
using WO.Property.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 使用5019端口
builder.WebHost.UseUrls("http://0.0.0.0:5019");

// 注册 MySQL 连接
builder.Services.AddScoped<MySqlConnection>(_ =>
    new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;"));

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
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(allowedOrigins)
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WO Property MasterData Service API", Version = "v1" });
});

// 健康检查端点（统一格式）
app.MapGet("/health", async () =>
{
    var mysqlHealthy = false;
    try
    {
        using var conn = new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=5;Connection Idle Timeout=60;Default Command Timeout=30;Connect Timeout=5;");
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync();
        mysqlHealthy = true;
    }
    catch { }
    
    var status = mysqlHealthy ? "healthy" : "unhealthy";
    var httpStatus = mysqlHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
    
    var response = new
    {
        status,
        service = "WO.Property.MasterDataService",
        version = "1.0.0",
        timestamp = DateTime.UtcNow,
        dependencies = new Dictionary<string, object>
        {
            ["mysql"] = new { status = mysqlHealthy ? "healthy" : "unhealthy" }
        }
    };
    
    return Results.Json(response, statusCode: httpStatus);
}).AllowAnonymous();

// 初始化数据库表
using (var connection = new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;"))
{
    connection.Open();
    
    var initSql = @"
CREATE TABLE IF NOT EXISTS FieldDefinitions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FieldKey VARCHAR(50) NOT NULL UNIQUE,
    DisplayName VARCHAR(100) NOT NULL,
    FieldType VARCHAR(20) NOT NULL DEFAULT 'text',
    Source VARCHAR(50) NOT NULL,
    IsShared TINYINT(1) DEFAULT 0,
    Module VARCHAR(50),
    Options TEXT,
    DefaultValue TEXT,
    IsRequired TINYINT(1) DEFAULT 0,
    Width INT DEFAULT 100,
    SortOrder INT DEFAULT 0,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_field_key (FieldKey),
    INDEX idx_module (Module),
    INDEX idx_is_shared (IsShared),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS ModuleFields (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Module VARCHAR(50) NOT NULL,
    FieldDefinitionId INT NOT NULL,
    IsVisible TINYINT(1) DEFAULT 1,
    IsActive TINYINT(1) DEFAULT 1,
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_module (Module),
    UNIQUE INDEX idx_module_field (Module, FieldDefinitionId),
    FOREIGN KEY (FieldDefinitionId) REFERENCES FieldDefinitions(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Suppliers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL UNIQUE,
    ContactPerson VARCHAR(50),
    ContactPhone VARCHAR(20),
    Address VARCHAR(200),
    Type VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_code (Code),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS DeviceTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Category VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Active',
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_code (Code),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS JobTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Category VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Active',
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_code (Code),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS Areas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Region VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Active',
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_code (Code),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS Regions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Config TEXT,
    Status VARCHAR(20) DEFAULT 'Active',
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_code (Code),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS FinanceRecords (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RecordNumber VARCHAR(30) UNIQUE NOT NULL,
    Type VARCHAR(10) NOT NULL,
    Category VARCHAR(30),
    Amount DECIMAL(14,2) NOT NULL,
    Balance DECIMAL(14,2) DEFAULT 0,
    PaymentMethod VARCHAR(20),
    RecordDate DATE,
    Handler VARCHAR(50),
    RelatedParty VARCHAR(100),
    ContractNo VARCHAR(30),
    BillNo VARCHAR(30),
    Description TEXT,
    ReceiptNo VARCHAR(30),
    Status VARCHAR(20) DEFAULT 'completed',
    Remarks TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
);
";
    
    using var cmd = new MySqlCommand(initSql, connection);
    cmd.ExecuteNonQuery();
    
    Console.WriteLine("数据库表初始化完成");
}

// 初始化种子数据
await InitializeSeedDataAsync();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 数据库初始化
async Task InitializeSeedDataAsync()
{
    using var connection = new MySqlConnection("Server=localhost;Port=3306;Database=wo_property;User=woproperty;Password=WOProperty2026!;CharSet=utf8mb4;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=10;Connection Timeout=10;Connection Idle Timeout=60;Default Command Timeout=30;");
    await connection.OpenAsync();
    
    // 检查是否已有数据
    using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM FieldDefinitions", connection))
    {
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count > 0)
        {
            Console.WriteLine("字段定义数据已存在，跳过初始化");
            return;
        }
    }
    
    Console.WriteLine("开始初始化字段定义数据...");
    
    // 获取种子数据
    var fields = FieldDefinitionSeedData.GetSeedData();
    
    // 添加所有字段
    foreach (var field in fields)
    {
        var insertSql = @"INSERT INTO FieldDefinitions 
            (FieldKey, DisplayName, FieldType, Source, IsShared, Module, Options, DefaultValue, IsRequired, Width, SortOrder, Status, CreatedAt) 
            VALUES (@FieldKey, @DisplayName, @FieldType, @Source, @IsShared, @Module, @Options, @DefaultValue, @IsRequired, @Width, @SortOrder, @Status, @CreatedAt);
            SELECT LAST_INSERT_ID();";
        
        using var cmd = new MySqlCommand(insertSql, connection);
        cmd.Parameters.AddWithValue("@FieldKey", field.FieldKey);
        cmd.Parameters.AddWithValue("@DisplayName", field.DisplayName);
        cmd.Parameters.AddWithValue("@FieldType", field.FieldType);
        cmd.Parameters.AddWithValue("@Source", field.Source);
        cmd.Parameters.AddWithValue("@IsShared", field.IsShared);
        cmd.Parameters.AddWithValue("@Module", (object)field.Module ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Options", (object)field.Options ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DefaultValue", (object)field.DefaultValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsRequired", field.IsRequired);
        cmd.Parameters.AddWithValue("@Width", field.Width);
        cmd.Parameters.AddWithValue("@SortOrder", field.SortOrder);
        cmd.Parameters.AddWithValue("@Status", field.Status);
        cmd.Parameters.AddWithValue("@CreatedAt", field.CreatedAt);
        
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        field.Id = id;
    }
    
    Console.WriteLine($"成功初始化 {fields.Count} 个字段定义");
    
    // 初始化财务记录种子数据
    using (var financeCheckCmd = new MySqlCommand("SELECT COUNT(*) FROM FinanceRecords", connection))
    {
        var financeCount = Convert.ToInt32(await financeCheckCmd.ExecuteScalarAsync());
        if (financeCount == 0)
        {
            Console.WriteLine("开始初始化财务记录种子数据...");
            var seedRecords = SeedDataHelper.GetFinanceSeedData();
            foreach (var record in seedRecords)
            {
                var insertSql = @"INSERT INTO FinanceRecords
                    (RecordNumber, Type, Category, Amount, Balance, PaymentMethod, RecordDate, Handler, RelatedParty, ContractNo, BillNo, Description, ReceiptNo, Status, Remarks, CreatedAt)
                    VALUES (@RecordNumber, @Type, @Category, @Amount, @Balance, @PaymentMethod, @RecordDate, @Handler, @RelatedParty, @ContractNo, @BillNo, @Description, @ReceiptNo, @Status, @Remarks, @CreatedAt)";
                using var cmd = new MySqlCommand(insertSql, connection);
                cmd.Parameters.AddWithValue("@RecordNumber", record.RecordNumber);
                cmd.Parameters.AddWithValue("@Type", record.Type);
                cmd.Parameters.AddWithValue("@Category", (object)record.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", record.Amount);
                cmd.Parameters.AddWithValue("@Balance", record.Balance);
                cmd.Parameters.AddWithValue("@PaymentMethod", (object)record.PaymentMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RecordDate", (object)record.RecordDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Handler", (object)record.Handler ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RelatedParty", (object)record.RelatedParty ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ContractNo", (object)record.ContractNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BillNo", (object)record.BillNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object)record.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReceiptNo", (object)record.ReceiptNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", record.Status);
                cmd.Parameters.AddWithValue("@Remarks", (object)record.Remarks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", record.CreatedAt);
                await cmd.ExecuteNonQueryAsync();
            }
            Console.WriteLine($"成功初始化 {seedRecords.Count} 条财务记录");
        }
        else
        {
            Console.WriteLine("财务记录数据已存在，跳过初始化");
        }
    }
    
    // 初始化工单模块的字段关联
    var ticketModuleFields = FieldDefinitionSeedData.GetTicketModuleFields(fields);
    if (ticketModuleFields.Any())
    {
        foreach (var mf in ticketModuleFields)
        {
            var insertSql = @"INSERT INTO ModuleFields 
                (Module, FieldDefinitionId, IsVisible, IsActive, SortOrder, CreatedAt) 
                VALUES (@Module, @FieldDefinitionId, @IsVisible, @IsActive, @SortOrder, @CreatedAt)";
            
            using var cmd = new MySqlCommand(insertSql, connection);
            cmd.Parameters.AddWithValue("@Module", mf.Module);
            cmd.Parameters.AddWithValue("@FieldDefinitionId", mf.FieldDefinitionId);
            cmd.Parameters.AddWithValue("@IsVisible", mf.IsVisible);
            cmd.Parameters.AddWithValue("@IsActive", mf.IsActive);
            cmd.Parameters.AddWithValue("@SortOrder", mf.SortOrder);
            cmd.Parameters.AddWithValue("@CreatedAt", mf.CreatedAt);
            
            await cmd.ExecuteNonQueryAsync();
        }
        Console.WriteLine($"成功初始化 {ticketModuleFields.Count} 个工单模块字段关联");
    }
    
    Console.WriteLine("MasterDataService 数据库初始化完成");
}

Console.WriteLine("===========================================");
Console.WriteLine("  WO Property MasterData Service");
Console.WriteLine("  Port: 5019");
Console.WriteLine("===========================================");

app.Run();
