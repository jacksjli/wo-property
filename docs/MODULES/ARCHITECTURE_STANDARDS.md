# WO 物业管理软件 — 模块架构规范

> **版本：** v1.0
> **创建：** 2026-06-03
> **状态：** 已确认
> **目的：** 所有新增模块必须遵守的架构原则，违反即审计，整改。

---

## 一、接口层（API Contract）

### 1.1 路由路径规范

```
/api/tenant/{module}/{resource}     租户化资源（标准）
/api/tenant/{module}/{resource}/{id} 单个资源
/api/{module}-types                  枚举/字典类（工种、区域等）
/api/{module}/options               可选值（新建时用到）
```

**强制规则：**
- 所有业务资源必须经过 `TenantRoutingMiddleware`
- 不允许出现 `/api/{module}/{resource}`（无 tenant 前缀）除非是纯查询工具接口
- Gateway 必须注册该路由的 path rewrite（如有 path 转换）

### 1.2 响应格式统一

**成功：**
```json
{ "success": true, "data": {...}, "message": "" }
```

**列表：**
```json
{ "success": true, "data": [...], "total": 100, "page": 1, "pageSize": 20 }
```

**错误：**
```json
{ "success": false, "message": "错误描述" }
{ "success": false, "message": "详细错误", "code": "ERROR_CODE" }
```

### 1.3 HTTP 状态码

| 场景 | 状态码 |
|------|--------|
| 成功 | 200 |
| 未登录/无权限 | 401 |
| 禁止访问 | 403 |
| 找不到资源 | 404 |
| 参数错误 | 400 |
| 服务器错误 | 500 |

---

## 二、配置层

### 2.1 端口号集中管理

**位置：** `src/admin-portal/src/api/config.ts`

```typescript
export const SERVICES = {
  moduleName: portNumber,  // 唯一来源
}
export const getServiceUrl = (service: keyof typeof SERVICES): string =>
  `${API_BASE}:${SERVICES[service]}`
```

**强制规则：**
- 新增模块：只能在 `SERVICES` 中添加
- 各 API 文件：必须调用 `getServiceUrl('moduleName')`，**禁止硬编码 IP 或端口**
- 禁止在 `views/` 目录下出现 `http://` 或端口号

### 2.2 IP 地址集中管理

**当前服务器 IP：** `192.168.1.3`（记录在 `.server-ip`）

**IP 变更流程：**
1. 修改 `.server-ip` 文件内容
2. `config.ts` 中 `getServiceUrl()` 读取新 IP
3. 全量编译 + 重启所有服务

**禁止：**
```typescript
// ❌ 禁止
const BASE_URL = 'http://192.168.1.3:5509'

// ✅ 必须
import { getServiceUrl } from '@/api/config'
const BASE_URL = getServiceUrl('finance')
```

---

## 三、数据层

### 3.1 表名单一来源

**文档位置：** `init-scripts/canonical-schema.sql`

**规则：**
- 表名用 `snake_case`（如 `job_types`）**或** `PascalCase`（如 `Departments`），但**必须与 canonical-schema.sql 一致**
- 数据库中只能有一套表（不重复建相同业务含义的表）
- 所有表必须有 `CreatedAt` / `UpdatedAt` 字段

**表名使用规范（按场景）：**

| 场景 | 表名风格 | 例 |
|------|---------|-----|
| 业务主表 | snake_case | `job_types` |
| 系统字典表 | PascalCase | `Departments` |
| 枚举定义表 | PascalCase | `EnumDefinitions` |

### 3.2 字段命名规范

```
Name          → 名称（用户可见）
Code          → 业务编码（如 REPAIR）
Status        → 状态（Active/Pending/Disabled）
SortOrder     → 排序（整数，默认 0）
CreatedAt     → 创建时间（UTC）
UpdatedAt     → 更新时间（UTC，可为 null）
DeletedAt     → 软删除时间（UTC，可为 null）
```

**禁止：**
- `CreateTime` / `UpdateTime`（不用 PascalCase）
- `create_time`（不用全小写）
- `create_at`（不用下划线分隔）

### 3.3 外键字段规范

```
{Resource}Id     → 外键 ID（如 TicketId）
{Resource}Name   → 外键关联的名称（冗余字段，可加快查询）
```

---

## 四、安全层

### 4.1 认证 Header

| Header | 用途 | 必须 |
|--------|------|------|
| `Authorization: Bearer {token}` | JWT Token | ✅ |
| `X-Project: {projectCode}` | 项目隔离 | ✅ |
| `Tenant-Code: {tenantCode}` | 租户隔离 | ✅ |

### 4.2 权限控制

- Controller 必须加 `[Authorize]` 装饰器
- 角色验证走角色服务（RolePermission），不在 Controller 内硬编码

---

## 五、代码组织

### 5.1 目录结构规范

```
src/WO.Property.{Module}Service/
  Controllers/
    Tenant{Resource}Controller.cs    ← API 入口（租户化）
    {Resource}TypesController.cs     ← 枚举/字典类
  Models/
    {Resource}.cs                  ← 数据库实体
    {Resource}Request.cs            ← 请求 DTO
    {Resource}Response.cs            ← 响应 DTO
  Data/
    TenantDbContext.cs              ← EF Core DbContext
  Services/
    {Resource}Service.cs            ← 业务逻辑
  Middleware/
    TenantRoutingMiddleware.cs       ← 租户路由中间件（如有）
  Program.cs                        ← 入口（端口 + DI 配置）
  appsettings.json                  ← 环境配置
```

### 5.2 Controller 命名

| 类型 | 命名规则 | 路由前缀 |
|------|---------|---------|
| 租户化资源 | `Tenant{Resource}Controller` | `api/tenant/{module}` |
| 枚举/字典 | `{Resource}Controller` | `api/{module}-types` |

### 5.3 文件命名

```
控制器：CamelCase + Controller.cs
  TenantTicketController.cs
  JobTypesController.cs

模型：CamelCase + .cs（PascalCase 表名对应）
  Ticket.cs
  JobType.cs

中间件：CamelCase + Middleware.cs
  TenantRoutingMiddleware.cs
```

---

## 六、可观测性

### 6.1 健康检查

每个服务必须有 `GET /health` 接口：

```json
{
  "status": "healthy",
  "service": "WO.Property.{Module}Service",
  "timestamp": "2026-06-03T00:00:00Z",
  "dependencies": {
    "mysql": { "status": "healthy" }
  }
}
```

### 6.2 日志格式

```
logger.LogInformation("{Action} {Resource} {Code}: {Detail}", ...)
例：logger.LogInformation("创建工种类型 REPAIR ({Name})", req.Name);
```

---

## 七、基础设施

### 7.1 服务注册流程

新增模块必须完成以下全部注册：

| 步骤 | 文件 | 操作 |
|------|------|------|
| 1 | `ecosystem.config.js` | 添加 PM2 进程配置 |
| 2 | `start-all.sh` | 添加启动命令 |
| 3 | `SERVICES_STATUS.md` | 登记端口 + 状态 |
| 4 | `config.ts SERVICES` | 注册端口号 |
| 5 | `docs/SERVICES_STATUS.md` | 同步更新 |

### 7.2 数据库迁移

- 新增表：写入 `canonical-schema.sql`
- 数据修复：`init-scripts/` 下建立 `fix_{module}_{issue}.sql`
- 所有 SQL 文件必须在 `canonical-schema.sql` 中可追溯

---

## 八、违规处理

| 级别 | 场景 | 处理 |
|------|------|------|
| 🔴 P0 | API 文件硬编码 IP | 立即整改，下次审计必查 |
| 🔴 P0 | MasterDataService 使用 PascalCase 表名 | 必须改为小写（module_fields/field_definitions），立即整改 |
| 🟡 P1 | 响应格式不统一 | 下次发布前修复 |
| 🟡 P1 | 表名与文档不一致 | 更新文档或修改代码 |
| 🟢 P2 | 日志格式不规范 | 下下次迭代优化 |

---

## 九、模板工具

使用 `quick-generator` 生成新模块骨架，确保结构一致：

---

## 三、数据层（补充）

### 3.3 MySQL 表名大小写规范（仅 macOS + MySqlConnector）

**问题背景：** macOS 上 MySQL `lower_case_table_names=2` 使表名大小写不敏感，但 .NET **MySqlConnector 对 SQL 中的表名大小写敏感**。这导致 `FROM ModuleFields` 查不到数据（返回 0 或少量行），而 `FROM module_fields` 正常。

**已确认敏感的表（MasterDataService 范围）：**

| PascalCase（❌ 禁用） | lowercase（✅ 必用） | 正确数据量 |
|----------------------|---------------------|-----------|
| `ModuleFields` | `module_fields` | 209 vs 13 |
| `FieldDefinitions` | `field_definitions` | 235 vs 33 |
| `EnumDefinitions` | `enum_definitions` | 0 vs 正确 |

**强制规则（MasterDataService）：**
- 所有 Controller 的 SQL 中，`FROM`/`INTO`/`UPDATE`/`DELETE FROM` 后的表名**必须使用小写**
- 不允许使用 PascalCase 表名（即使数据库实际存在该表）
- 这是 macOS 特有的兼容性问题，生产 Linux 环境无此问题

**检查命令：**
```bash
bash scripts/module-consistency-check.sh
```

**检测范围：** `src/WO.Property.MasterDataService/Controllers/*.cs`

---

## 九、自动化检查机制

### 9.1 Git Hook（新增模块自动触发）

**文件：** `.git/hooks/pre-commit`（已安装）

**触发时机：** `git commit` 时自动检测新增 Controller

**流程：**
```
git commit
  ├─ 检测新增 Controller
  │   ├─ 有 → 运行 module-consistency-check.sh
  │   │       ├─ P0 问题 → 直接拒绝提交 ❌
  │   │       ├─ P1 问题 → 警告提示，按 Enter 可强制提交 ⚠️
  │   │       └─ 无问题 → 允许提交 ✅
  │   └─ 无 → 跳过检查，直接提交
```

**绕过方式：**
```bash
git commit --no-verify -m "message"
```

### 9.2 Cron 定时检查（每周一 09:00）

自动运行 `scripts/module-consistency-check.sh`，结果写入：
```
docs/audit/MODULE_CONSISTENCY_{YYYY-MM-DD}.md
```

### 9.3 模板工具（待 CLAWHUB 安装）

```bash
openclaw generate module ModuleName
```

---

## 附：当前已验证的端口映射

```
5000  Gateway
5106  AuthService
5019  MasterDataService
5018  PersonService
5102  TicketService
5509  FinanceService
5512  KeyService
5521  RenovationService
5501  ContractService
5250  StatisticsService
```

> **IP 变更：** 当前为 `192.168.1.3`，如需变更只需修改 `config.ts` 一处。

---

**最后更新：** 2026-06-03
**下次审计：** 2026-06-10（周）
**审计执行人：** 芦苇（CI 自动触发）
