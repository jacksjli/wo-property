using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Serilog;
using Serilog.Events;
using WO.Property.Shared.Models;
using WO.Property.Shared.Configuration;
using WO.Property.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// === Serilog 日志配置 ===
var serviceName = "MaterialService";
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

// 添加服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

// 添加数据库上下文
builder.Services.AddDbContext<MaterialDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "Host=postgres;Database=wo_property;Username=woproperty;Password=WOProperty2026!"));

// 使用统一JWT配置（与统一认证服务一致）
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? JwtHelper.GetSecretKey();
var issuer = jwtSettings["Issuer"] ?? "wo-property-unified-auth";
var audience = jwtSettings["Audience"] ?? "wo-property-services";

// 配置JWT认证（使用统一配置）
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// 添加请求日志中间件
app.UseRequestLogging();
app.UseGlobalExceptionHandler();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// 健康检查端点
app.MapGet("/health", () =>
{
    return Results.Json(new
    {
        status = "healthy",
        service = "WO物料管理服务（统一认证版）",
        version = "2.0.1",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        features = new[] { "物料分类管理", "库存管理", "采购管理", "统一JWT认证" },
        authConfig = new
        {
            type = "统一认证",
            issuer = issuer,
            audience = audience
        }
    });
});

// 物料分类API（公开）- 放前面避免路由冲突
app.MapGet("/api/material-categories", async (MaterialDbContext db) =>
{
    var categories = await db.MaterialCategories.ToListAsync();
    return Results.Ok(new { categories });
});

// 库存统计API（需要认证）- 放前面避免路由冲突
app.MapGet("/api/materials/statistics", [Authorize] async (MaterialDbContext db) =>
{
    var totalMaterials = await db.Materials.CountAsync();
    var lowStockMaterials = await db.Materials.CountAsync(m => m.CurrentStock <= m.SafetyStock);
    var totalValue = await db.Materials.SumAsync(m => m.CurrentStock * m.UnitPrice);
    
    var categories = await db.MaterialCategories.ToListAsync();
    
    return Results.Ok(new
    {
        statistics = new
        {
            totalMaterials,
            lowStockMaterials,
            totalValue,
            categoryCount = categories.Count
        },
        categories
    });
});

// 物料列表API（需要认证）
app.MapGet("/api/materials", [Authorize] async (MaterialDbContext db) =>
{
    var materials = await db.Materials.ToListAsync();
    return Results.Ok(new { materials });
});

// 获取单个物料API（需要认证）- 放在统计API后面，避免被错误匹配
app.MapGet("/api/materials/{id}", [Authorize] async (MaterialDbContext db, int id) =>
{
    var material = await db.Materials.FindAsync(id);
    if (material == null)
        return Results.NotFound(new { message = "物料不存在" });
    return Results.Ok(new { material });
});

// 入库操作API（需要认证）
app.MapPost("/api/materials/{id}/stock-in", [Authorize(Roles = "Administrator,Technician")] async (MaterialDbContext db, int id, StockOperationRequest request) =>
{
    var material = await db.Materials.FindAsync(id);
    if (material == null)
        return Results.NotFound(new { message = "物料不存在" });
    
    material.CurrentStock += request.Quantity;
    
    var transaction = new StockTransaction
    {
        MaterialId = id,
        TransactionType = "In",
        Quantity = request.Quantity,
        UnitPrice = material.UnitPrice,
        TotalAmount = request.Quantity * material.UnitPrice,
        Operator = request.Operator,
        Notes = request.Notes,
        CreatedAt = DateTime.UtcNow
    };
    
    db.StockTransactions.Add(transaction);
    await db.SaveChangesAsync();
    
    return Results.Ok(new
    {
        success = true,
        message = "入库成功",
        newStock = material.CurrentStock
    });
});

// 出库操作API（需要认证）
app.MapPost("/api/materials/{id}/stock-out", [Authorize(Roles = "Administrator,Technician")] async (MaterialDbContext db, int id, StockOperationRequest request) =>
{
    var material = await db.Materials.FindAsync(id);
    if (material == null)
        return Results.NotFound(new { message = "物料不存在" });
    
    if (material.CurrentStock < request.Quantity)
    {
        return Results.BadRequest(new { success = false, message = "库存不足" });
    }
    
    material.CurrentStock -= request.Quantity;
    
    var transaction = new StockTransaction
    {
        MaterialId = id,
        TransactionType = "Out",
        Quantity = request.Quantity,
        UnitPrice = material.UnitPrice,
        TotalAmount = request.Quantity * material.UnitPrice,
        Operator = request.Operator,
        Notes = request.Notes,
        CreatedAt = DateTime.UtcNow
    };
    
    db.StockTransactions.Add(transaction);
    await db.SaveChangesAsync();
    
    return Results.Ok(new
    {
        success = true,
        message = "出库成功",
        newStock = material.CurrentStock
    });
});

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MaterialDbContext>();
    
    // 使用原始SQL创建表（确保多服务共享数据库时表都能创建）
    await dbContext.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS ""MaterialCategories"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Name"" VARCHAR(200) NOT NULL,
            ""Code"" VARCHAR(50) NOT NULL,
            ""Description"" TEXT,
            ""CreatedBy"" VARCHAR(100),
            ""CreatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedBy"" VARCHAR(100),
            ""UpdatedAt"" TIMESTAMP,
            ""IsDeleted"" BOOLEAN DEFAULT FALSE
        );
        CREATE TABLE IF NOT EXISTS ""Materials"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Code"" VARCHAR(50) NOT NULL,
            ""Name"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""CategoryId"" INT NOT NULL,
            ""Unit"" VARCHAR(20) NOT NULL,
            ""UnitPrice"" DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""SafetyStock"" INT NOT NULL DEFAULT 0,
            ""MaxStock"" INT NOT NULL DEFAULT 0,
            ""CurrentStock"" INT NOT NULL DEFAULT 0,
            ""CreatedBy"" VARCHAR(100),
            ""CreatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedBy"" VARCHAR(100),
            ""UpdatedAt"" TIMESTAMP,
            ""IsDeleted"" BOOLEAN DEFAULT FALSE
        );
        CREATE TABLE IF NOT EXISTS ""StockTransactions"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""MaterialId"" INT NOT NULL,
            ""TransactionType"" VARCHAR(20) NOT NULL,
            ""Quantity"" INT NOT NULL,
            ""UnitPrice"" DECIMAL(18,2) NOT NULL,
            ""TotalAmount"" DECIMAL(18,2) NOT NULL,
            ""Operator"" VARCHAR(100),
            ""Notes"" TEXT,
            ""CreatedBy"" VARCHAR(100),
            ""CreatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedBy"" VARCHAR(100),
            ""UpdatedAt"" TIMESTAMP,
            ""IsDeleted"" BOOLEAN DEFAULT FALSE
        );
        CREATE TABLE IF NOT EXISTS ""PurchaseOrders"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""OrderNumber"" VARCHAR(50) NOT NULL,
            ""OrderDate"" TIMESTAMP NOT NULL,
            ""Supplier"" VARCHAR(200) NOT NULL,
            ""TotalAmount"" DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""Status"" VARCHAR(20) NOT NULL DEFAULT 'Pending',
            ""Notes"" TEXT,
            ""CreatedBy"" VARCHAR(100),
            ""CreatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedBy"" VARCHAR(100),
            ""UpdatedAt"" TIMESTAMP,
            ""IsDeleted"" BOOLEAN DEFAULT FALSE
        );
    ");
    
    // 添加初始数据（如果不存在）
    if (!dbContext.MaterialCategories.Any())
    {
        dbContext.MaterialCategories.AddRange(
            new MaterialCategory { Name = "电气材料", Code = "ELEC", Description = "电线、电缆、开关等" },
            new MaterialCategory { Name = "管道材料", Code = "PLUMB", Description = "水管、阀门、接头等" },
            new MaterialCategory { Name = "建筑材料", Code = "BUILD", Description = "水泥、砂石、砖块等" },
            new MaterialCategory { Name = "清洁用品", Code = "CLEAN", Description = "清洁剂、工具等" },
            new MaterialCategory { Name = "工具设备", Code = "TOOL", Description = "扳手、螺丝刀等" }
        );
        await dbContext.SaveChangesAsync();
    }
    
    if (!dbContext.Materials.Any())
    {
        dbContext.Materials.AddRange(
            new Material
            {
                Code = "ELEC-001",
                Name = "电线（2.5平方）",
                Description = "铜芯电线，2.5平方毫米",
                CategoryId = 1,
                Unit = "卷",
                UnitPrice = 150.0m,
                SafetyStock = 10,
                MaxStock = 100,
                CurrentStock = 25
            },
            new Material
            {
                Code = "PLUMB-001",
                Name = "PVC水管（50mm）",
                Description = "直径50mm的PVC排水管",
                CategoryId = 2,
                Unit = "根",
                UnitPrice = 35.0m,
                SafetyStock = 20,
                MaxStock = 200,
                CurrentStock = 45
            },
            new Material
            {
                Code = "TOOL-001",
                Name = "工具箱",
                Description = "包含常用维修工具",
                CategoryId = 5,
                Unit = "个",
                UnitPrice = 280.0m,
                SafetyStock = 5,
                MaxStock = 30,
                CurrentStock = 8
            }
        );
        await dbContext.SaveChangesAsync();
    }
}

Log.Information("=== Material Service (Unified Auth) Started ===");
Log.Information("Service URL: http://localhost:5004");
Log.Information("JWT: Issuer={Issuer}, Audience={Audience}", issuer, audience);

app.Run("http://0.0.0.0:5004");

// 数据库上下文
public class MaterialDbContext : DbContext
{
    public MaterialDbContext(DbContextOptions<MaterialDbContext> options) : base(options) { }
    
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialCategory> MaterialCategories { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
}

// 实体类
public class Material : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int SafetyStock { get; set; }
    public int MaxStock { get; set; }
    public int CurrentStock { get; set; }
}

public class MaterialCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class StockTransaction : BaseEntity
{
    public int MaterialId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // In, Out
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Operator { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Completed
    public string? Notes { get; set; }
}

// 请求模型
public class StockOperationRequest
{
    [Required]
    public int Quantity { get; set; }
    
    public string? Operator { get; set; }
    
    public string? Notes { get; set; }
}
