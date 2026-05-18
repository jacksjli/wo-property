# Phase 1 多租户改造架构标准

> **版本**：v1.0
> **日期**：2026-05-18
> **目标**：13个服务统一改造，架构一致

---

## 1. 改造原则

### 1.1 必须遵循的模式

每个服务改造必须包含以下 4 个组件：

```
{ServiceName}Service/
├── Tenant/                     ← 新建目录
│   ├── ITenantDbFactory.cs     ← 复用 TicketService 的实现
│   └── TenantDbFactory.cs      ← 复用 TicketService 的实现
├── Middleware/
│   └── TenantRoutingMiddleware.cs  ← 复用 TicketService 的实现
├── Data/
│   ├── TenantDbContext.cs       ← 服务-specific entity mapping
│   └── TenantDbContextFactory.cs ← 服务-specific 连接管理
└── Controllers/
    └── Tenant{Entity}Controller.cs  ← 新增多租户 API
```

### 1.2 复用 TicketService 的通用组件

不要重复造轮子，以下文件从 TicketService 复制：

- `Tenant/ITenantDbFactory.cs` — 通用接口
- `Tenant/TenantDbFactory.cs` — 通用实现
- `Middleware/TenantRoutingMiddleware.cs` — JWT 解析

每个服务只需实现：
- `Data/TenantDbContext.cs` — 自己的实体映射
- `Data/TenantDbContextFactory.cs` — 自己的数据库连接
- `Controllers/Tenant*Controller.cs` — 自己的多租户 API

### 1.3 API 命名规范

| 接口类型 | 路径 | 说明 |
|---------|------|------|
| 多租户查询 | GET /api/tenant/{entity} | 分页查询当前租户数据 |
| 多租户创建 | POST /api/tenant/{entity} | 创建数据 |
| 多租户详情 | GET /api/tenant/{entity}/{id} | 获取单条 |
| 多租户更新 | PUT /api/tenant/{entity}/{id} | 更新 |
| 多租户删除 | DELETE /api/tenant/{entity}/{id} | 删除 |

---

## 2. 数据库设计

### 2.1 租户库结构要求

每个租户库（tenant_a, tenant_b）必须包含与业务相关的表：

```sql
-- 以 AnnouncementService 为例（租户库表）
CREATE TABLE announcements (
  id INT PRIMARY KEY AUTO_INCREMENT,
  title VARCHAR(200) NOT NULL,
  content TEXT,
  category VARCHAR(50),
  priority VARCHAR(20) DEFAULT 'Normal',
  status VARCHAR(20) DEFAULT 'Published',
  project_id INT NOT NULL,
  created_by INT NOT NULL,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP
);
```

### 2.2 表设计规范

| 规范 | 说明 |
|------|------|
| id | INT AUTO_INCREMENT PRIMARY KEY |
| created_at | DATETIME DEFAULT CURRENT_TIMESTAMP |
| updated_at | DATETIME ON UPDATE CURRENT_TIMESTAMP |
| project_id | INT NOT NULL（多项目隔离） |
| created_by | INT NOT NULL（关联 center_db.users） |
| 软删除 | 有则用 is_deleted TINYINT(1)，无则物理删除 |
| 工单编号 | 有则 VARCHAR(50) UNIQUE，如 AN-YYYYMMDD-XXXX |

---

## 3. Program.cs 改造模板

```csharp
// === Phase 1 改造：添加多租户支持 ===

// 1. 端口改为临时端口（Phase 0 已占用正式端口）
builder.WebHost.UseUrls("http://0.0.0.0:{TEMP_PORT}");

// 2. 连接字符串（InMemoryConfiguration）
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4"
});

// 3. 注册租户组件
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WO.Property.{ServiceName}.Tenant.ITenantDbFactory,
                                WO.Property.{ServiceName}.Tenant.TenantDbFactory>();

// 4. 注册 DbContextFactory
builder.Services.AddScoped<IDbContextFactory<TenantDbContext>>(sp =>
    new TenantDbContextFactory(
        sp.GetRequiredService<ITenantDbFactory>(),
        sp.GetRequiredService<ILogger<TenantDbContextFactory>>(),
        sp.GetRequiredService<IConfiguration>()
    ));

// 5. 中间件（在 UseAuthorization 之前）
app.UseMiddleware<WO.Property.{ServiceName}.Middleware.TenantRoutingMiddleware>();
```

---

## 4. TenantDbContext 模板

```csharp
public class TenantDbContext : DbContext
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContext> _logger;

    public DbSet<Announcement> Announcements => Set<Announcement>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger) : base(options)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("announcements");

            // 列名映射（PascalCase → snake_case）
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnUpdate();

            // 忽略基类中存在但表中不存在的字段
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
        });
    }
}
```

---

## 5. TenantDbContextFactory 模板

```csharp
public class TenantDbContextFactory : IDbContextFactory<TenantDbContext>
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantDbContextFactory> _logger;
    private readonly DbContextOptions<TenantDbContext> _centerOptions;

    public TenantDbContextFactory(
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContextFactory> logger,
        IConfiguration configuration)
    {
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;

        var centerConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Default connection string not configured");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        _centerOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(centerConnectionString, serverVersion)
            .Options;
    }

    public TenantDbContext CreateDbContext()
    {
        var tenantCode = _tenantDbFactory.GetCurrentTenantCode();

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogDebug("No tenant context - using default database");
            var opts = new DbContextOptionsBuilder<TenantDbContext>(_centerOptions).Options;
            return new TenantDbContext(opts, _tenantDbFactory, null!);
        }

        var tenantConnectionString = _tenantDbFactory.GetTenantConnectionString(tenantCode);
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
        var dbOptions = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(tenantConnectionString, serverVersion)
            .Options;

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantCode}", tenantCode);
        return new TenantDbContext(dbOptions, _tenantDbFactory, null!);
    }
}
```

---

## 6. TenantController 模板

```csharp
[ApiController]
[Route("api/tenant/announcements")]
public class TenantAnnouncementController : ControllerBase
{
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ILogger<TenantAnnouncementController> _logger;

    public TenantAnnouncementController(
        IDbContextFactory<TenantDbContext> dbFactory,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantAnnouncementController> logger)
    {
        _dbFactory = dbFactory;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    private TenantDbContext CreateDbContext() => _dbFactory.CreateDbContext();

    private int? GetUserIdFromJwt()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return int.TryParse(jwtToken.Claims.FirstOrDefault(
                c => c.Type == ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
        }
        catch { return null; }
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] int? projectId = null)
    {
        using var db = CreateDbContext();
        var query = db.Announcements.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);
        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantCreateAnnouncementRequest request)
    {
        using var db = CreateDbContext();
        var creatorId = GetUserIdFromJwt() ?? 0;

        var announcement = new Announcement
        {
            Title = request.Title,
            Content = request.Content,
            Category = request.Category ?? "通知",
            Priority = request.Priority ?? "Normal",
            Status = "Published",
            ProjectId = request.ProjectId,
            CreatedBy = creatorId,
            CreatedAt = DateTime.UtcNow
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        return Ok(new { success = true, data = announcement, message = "创建成功" });
    }
}
```

---

## 7. 改造顺序（按复杂度）

| 顺序 | 服务 | 端口 | 复杂度 | 实体 |
|------|------|------|--------|------|
| 1 | AnnouncementService | 5011 | ★☆☆ 低 | announcements |
| 2 | CleaningService | 5016 | ★☆☆ 低 | cleaning_tasks |
| 3 | ExpressService | 5017 | ★☆☆ 低 | express_records |
| 4 | KeyService | 5012 | ★☆☆ 低 | key_records |
| 5 | ParkingService | 5025 | ★☆☆ 低 | parking_records |
| 6 | CommunityService | 5022 | ★★☆ 中 | community_events |
| 7 | RenovationService | 5021 | ★★☆ 中 | renovation_applications |
| 8 | InspectionService | 5010 | ★★☆ 中 | inspection_records |
| 9 | VisitorService | 5013 | ★★☆ 中 | visitor_records |
| 10 | ContractService | 5001 | ★★☆ 中 | contracts |
| 11 | NotificationService | 5005 | ★★☆ 中 | notifications |
| 12 | MaterialService | 5004 | ★★★ 高 | material_records, material_stock |
| 13 | FinanceService | 5009 | ★★★ 高 | 收费/缴费/账单 |

---

## 8. 验收标准

| 标准 | 说明 |
|------|------|
| API 可调用 | curl 验证 GET/POST/DELETE 正常 |
| 租户隔离 | tenant_a 看不到 tenant_b 的数据 |
| 编译通过 | `dotnet build` 0 error |
| 单元测试 | 每个服务至少 5 个测试用例 |
| 文档更新 | 每个服务更新 `docs/MODULES/` 下的 API 文档 |

---

## 9. 禁止事项

- ❌ 禁止在 TenantController 中直接使用 `AppDbContext`
- ❌ 禁止在租户 API 中硬编码 `project_id`
- ❌ 禁止在 Program.cs 中使用 `app.Urls.Clear()` 再 Add（使用 UseUrls）
- ❌ 禁止在租户服务中使用 PostgreSQL/Npgsql

---

_最后更新：2026-05-18_
_维护者：🪽的芦苇_