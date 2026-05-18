# Phase 0 多物业分库架构文档

> **版本**：v1.0
> **日期**：2026-05-18
> **状态**：已完成
> **分支**：`feature/multi-tenant`

---

## 1. 架构概览

### 1.1 目标
将 WO 物业管理软件从单租户架构改造为多租户架构，支持多个物业公司（租户）共用同一套系统，数据完全隔离。

### 1.2 核心设计
```
                    ┌─────────────────┐
                    │   AuthService   │
                    │   (port 5106)   │
                    │  JWT + tenant   │
                    └────────┬────────┘
                             │ 登录获取 token（带 tenant_code）
                             ▼
                    ┌─────────────────┐
                    │ TenantRouting   │
                    │   Middleware    │
                    └────────┬────────┘
                             │ 解析 JWT，注入 TenantDbFactory
                             ▼
┌──────────────────────────────────────────────────────┐
│                 TenantDbContextFactory                │
│  AsyncLocal<tenantCode> → 动态切换 MySQL 连接        │
└──────────────────────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              ▼                              ▼
     ┌──────────────┐              ┌──────────────┐
     │  center_db   │              │  tenant_a    │
     │  (租户注册)   │              │  (租户数据)   │
     ├──────────────┤              ├──────────────┤
     │  tenants     │              │  tickets     │
     │  projects    │              │  buildings   │
     │  users       │              │  ...         │
     └──────────────┘              └──────────────┘
                                           ▲
                                           │ Token 中 tenant_code
                                           ▼
                                  ┌──────────────────────┐
                                  │  TenantTicketService  │
                                  │   (port 5102)        │
                                  │  CRUD + 租户隔离      │
                                  └──────────────────────┘
```

---

## 2. 数据库设计

### 2.1 center_db（租户中心库）
所有租户的公共数据，不含业务数据。

```sql
-- tenants 表
CREATE TABLE tenants (
  id INT PRIMARY KEY AUTO_INCREMENT,
  code VARCHAR(50) UNIQUE NOT NULL,     -- tenant_a, tenant_b
  name VARCHAR(200) NOT NULL,            -- 阳光物业，绿城物业
  status VARCHAR(20) DEFAULT 'Active',
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- projects 表
CREATE TABLE projects (
  id INT PRIMARY KEY AUTO_INCREMENT,
  tenant_id INT NOT NULL,
  code VARCHAR(50) UNIQUE NOT NULL,     -- YGHY001, LCGY001
  name VARCHAR(200) NOT NULL,            -- 阳光花园小区，绿城公寓
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);

-- users 表
CREATE TABLE users (
  id INT PRIMARY KEY AUTO_INCREMENT,
  tenant_id INT NOT NULL,
  username VARCHAR(100) NOT NULL,
  password_hash VARCHAR(200) NOT NULL,  -- BCrypt
  full_name VARCHAR(200),
  email VARCHAR(100),
  phone VARCHAR(20),
  role VARCHAR(50) DEFAULT 'User',
  status VARCHAR(20) DEFAULT 'Active',
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);

-- user_projects 表
CREATE TABLE user_projects (
  user_id INT NOT NULL,
  project_id INT NOT NULL,
  PRIMARY KEY (user_id, project_id)
);
```

### 2.2 tenant_a / tenant_b（租户业务库）
每个租户独立的业务数据库，结构相同，数据隔离。

```sql
-- tickets 表
CREATE TABLE tickets (
  id INT PRIMARY KEY AUTO_INCREMENT,
  ticket_code VARCHAR(50) UNIQUE NOT NULL,  -- WO-20260518-1001
  title VARCHAR(200) NOT NULL,
  description VARCHAR(1000),
  category VARCHAR(50),
  priority VARCHAR(20) DEFAULT 'Medium',
  status VARCHAR(20) DEFAULT 'New',
  created_by INT NOT NULL,                 -- users.id（中心库）
  assigned_to INT,
  project_id INT NOT NULL,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP
);

-- buildings 表
CREATE TABLE buildings (
  id INT PRIMARY KEY AUTO_INCREMENT,
  project_id INT NOT NULL,
  name VARCHAR(200),
  address VARCHAR(500),
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

---

## 3. 核心组件

### 3.1 TenantDbFactory
**文件**：`TicketService/Tenant/TenantDbFactory.cs`

租户上下文管理，使用 `AsyncLocal` 实现请求级别的租户隔离。

```csharp
public class TenantDbFactory : ITenantDbFactory
{
    private static readonly AsyncLocal<string?> _currentTenantCode = new();

    public void SetCurrentTenantCode(string tenantCode)
        => _currentTenantCode.Value = tenantCode;

    public string? GetCurrentTenantCode()
        => _currentTenantCode.Value;

    public string GetTenantConnectionString(string tenantCode)
    {
        // 动态替换连接字符串中的数据库名
        var baseConnStr = _configuration.GetConnectionString("Default");
        return Regex.Replace(baseConnStr, @"Database\s*=\s*[^;]+",
            $"Database={tenantCode}");
    }
}
```

### 3.2 TenantRoutingMiddleware
**文件**：`TicketService/Middleware/TenantRoutingMiddleware.cs`

从 JWT 提取 `tenant_code` 并设置到 TenantDbFactory。

```csharp
public async Task InvokeAsync(HttpContext context, ITenantDbFactory tenantDbFactory)
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring("Bearer ".Length).Trim();
        var tenantCode = ExtractTenantCode(token); // 从 JWT 解析
        if (!string.IsNullOrEmpty(tenantCode))
        {
            tenantDbFactory.SetCurrentTenantCode(tenantCode);
        }
    }
    await _next(context);
}
```

### 3.3 TenantDbContext
**文件**：`TicketService/Data/TenantDbContext.cs`

EF Core DbContext，根据当前租户上下文切换连接。

```csharp
public class TenantDbContext : DbContext
{
    private readonly ITenantDbFactory _tenantDbFactory;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantDbFactory tenantDbFactory,
        ILogger<TenantDbContext> logger)
        : base(options) { ... }
}
```

### 3.4 TenantDbContextFactory
**文件**：`TicketService/Data/TenantDbContextFactory.cs`

IDbContextFactory 实现，每次 CreateDbContext() 根据当前租户返回对应库的 Context。

---

## 4. API 契约

### 4.1 AuthService (port 5106)

#### POST /api/auth/login
**请求**：
```json
{
  "username": "admin_a",
  "password": "Test@123",
  "tenantCode": "tenant_a"
}
```

**响应**：
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGci...",
    "expiresAt": "2026-05-25T...",
    "tenantId": 1,
    "tenantCode": "tenant_a",
    "projectIds": [1, 2],
    "user": { "id": 1, "username": "admin_a", "role": "Administrator" }
  }
}
```

#### JWT Claims
| Claim | 说明 |
|-------|------|
| nameidentifier | 用户 ID（center_db.users.id） |
| tenant_id | 租户 ID |
| tenant_code | 租户代码（tenant_a / tenant_b） |
| project_ids | 用户有权限的项目 ID 列表 |

---

### 4.2 TenantTicketService (port 5102)

所有 `/api/tenant/tickets/*` 接口需要携带 `Authorization: Bearer <token>`

#### GET /api/tenant/tickets
分页查询当前租户工单

#### POST /api/tenant/tickets
创建工单（自动分配工单编号 `WO-YYYYMMDD-XXXX`）

#### GET /api/tenant/tickets/{id}
获取单个工单

#### PUT /api/tenant/tickets/{id}
更新工单

#### DELETE /api/tenant/tickets/{id}
删除工单

---

## 5. 租户隔离机制

### 5.1 三层隔离

| 层级 | 机制 | 验证方式 |
|------|------|---------|
| 网络层 | 不同 MySQL 数据库实例 | 直接查询 tenant_a/tenant_b，数据不互通 |
| 连接层 | TenantDbContextFactory 动态切换连接 | 同一 Token 访问不同租户库 |
| API 层 | TenantRoutingMiddleware 解析 JWT | 伪造 tenant_code 无法突破数据库隔离 |

### 5.2 验证结果（2026-05-18）

```
tenant_a 登录 → 创建工单 → 查询 → 只看到自己 ✓
tenant_b 登录 → 创建工单 → 查询 → 只看到自己 ✓
tenant_a 看不到 tenant_b 的工单 ✓
MySQL 直接查询：tenant_a.tickets=3条, tenant_b.tickets=2条 ✓
```

---

## 6. 测试覆盖

### 6.1 单元测试（13 个通过）
- `TenantDbFactoryTests` (6): 租户上下文 Set/Get/Clear
- `TenantRoutingMiddlewareTests` (3): JWT 解析和路由
- `TenantTicketControllerTests` (4): 请求/响应模型

### 6.2 集成测试（16 个场景通过）
- `scripts/phase0-integration-test.sh`
- 覆盖：健康检查、登录、CRUD、租户隔离、数据库层面

---

## 7. 分支策略

| 分支 | 用途 | 状态 |
|------|------|------|
| `main` | 生产环境，21 个服务正常运行 | 稳定 |
| `feature/multi-tenant` | Phase 0 开发分支 | 开发中，未推 GitHub |

---

## 8. 已知限制

1. **StatisticsService 未改造**：当前 main 分支中 5014 端口运行的是 MobileService 而非 StatisticsService。驾驶舱页面暂无数据（metric_snapshots 表为空）。

2. **admin-portal 硬编码**：Phase 0 中 `auth.ts` 的 `AUTH_BASE_URL` 硬编码为 `http://localhost:5106`，后续通过环境变量或配置中心统一管理。

3. **GitHub 未推送**：`feature/multi-tenant` 分支因网络超时尚未推送到远程。

4. **旧 API 未废弃**：单租户 TicketController (5002) 和多租户 TenantTicketController (5102) 并存，Phase 1 需决定是否废弃旧接口。

---

## 9. Phase 1 规划

### 9.1 待改造服务
- MaterialService (5004)
- DeviceService (5003)
- ContractService (5001)
- FinanceService (5009)
- NotificationService (5005)
- InspectionService (5010)
- AnnouncementService (5011)
- KeyService (5012)
- VisitorService (5013)
- CleaningService (5016)
- ExpressService (5017)
- ParkingService (5025)
- RenovationService (5021)
- CommunityService (5022)

### 9.2 改造原则
1. 每个服务实现 `ITenantDbFactory` + `TenantRoutingMiddleware`
2. 共享 `center_db` 做认证和权限
3. 各服务使用各自的租户库（tenant_a / tenant_b）
4. 前端通过 Gateway 统一路由，根据 JWT 中的 tenant_code 动态代理

---

_文档版本：v1.0_
_最后更新：2026-05-18_
_维护者：🪽的芦苇_