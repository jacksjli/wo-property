# WO 物业管理软件 - 架构设计标准 (v1.1)

**版本：** v1.1
**日期：** 2026-05-19
**作者：** 软件项目负责人
**状态：** ✅ 已确认（指导性文件）

---

## 一、数据库选型标准

### 1.1 统一使用 MySQL

**所有新模块必须使用 MySQL 作为数据库，不得使用其他数据库。**

| 数据库 | 用途 | 状态 |
|--------|------|------|
| **MySQL 8.0** | 所有业务服务 | ✅ 标准 |
| PostgreSQL | ~~StatisticsService~~ | ❌ 已废弃，改用 MySQL |
| MongoDB | 无 | ❌ 暂不使用 |
| SQLServer | 无 | ❌ 暂不使用 |

### 1.2 连接字符串格式

```csharp
// MySQL 连接字符串标准格式
var connectionString = "Server=127.0.0.1;Port=3306;Database=wo_property;User=root;Password=;CharSet=utf8mb4;AllowUserVariables=true;UseAffectedRows=false";
```

### 1.3 数据库驱动

```xml
<!-- 必须使用 MySqlConnector -->
<PackageReference Include="MySqlConnector" Version="2.3.5" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.2" />
```

### 1.4 SQL 语法规范

```sql
-- 使用 MySQL 语法
CREATE TABLE IF NOT EXISTS table_name (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 注意：MySQL 不支持 CREATE INDEX IF NOT EXISTS
-- 使用单独的条件判断或忽略错误
```

---

## 二、服务端口标准

### 2.1 端口分配

| 分类 | 端口范围 | 说明 |
|------|----------|------|
| Phase 0 核心 | 5000-5200 | 核心公共服务 |
| Phase 1 业务 | 5500-5600 | 业务服务 |
| Phase 2 扩展 | 5600+ | 扩展服务 |

### 2.2 当前端口分配

```
Phase 0:
  5000  GatewayService
  5002  (旧 TicketService)
  5003  DispatchService
  5005  (旧 NotificationService)
  5018  PersonService
  5019  MasterDataService
  5026  StatisticsService
  5102  TicketService
  5106  AuthService
  5129  NotificationService

Phase 1:
  5501  ContractService
  5504  MaterialService
  5507  PaymentService
  5509  FinanceService
  5510  InspectionService
  5511  AnnouncementService
  5512  KeyService
  5513  VisitorService
  5516  CleaningService
  5517  ExpressService
  5521  RenovationService
  5522  CommunityService
  5525  ParkingService
  5530  DeviceService

前端:
  5173  admin-portal
```

---

## 三、API 路径标准

### 3.1 多租户端点

所有业务服务使用多租户端点，前缀 `/api/tenant/`：

```
GET    /api/tenant/{module}           # 列表
GET    /api/tenant/{module}/{id}       # 详情
POST   /api/tenant/{module}            # 创建
PUT    /api/tenant/{module}/{id}       # 更新
DELETE /api/tenant/{module}/{id}       # 删除
```

### 3.2 非多租户端点

公共服务不使用 `/api/tenant/` 前缀：

```
/api/persons
/api/field-definitions
/api/hierarchy/areas
/api/ticket-types
```

---

## 四、前端 API 客户端标准

### 4.1 文件命名

```
src/admin-portal/src/api/{module}.ts
```

例如：`ticket.ts`, `notification.ts`, `material.ts`

### 4.2 标准结构

```typescript
import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:{port}'
const api = createHttpClient(BASE_URL)

export interface IEntity {
  id: number
  // ...
}

export const moduleApi = {
  getAll: (params?: any) => api.get('/api/tenant/{module}', { params }),
  getById: (id: number) => api.get(`/api/tenant/{module}/${id}`),
  create: (data: Partial<IEntity>) => api.post('/api/tenant/{module}', data),
  update: (id: number, data: Partial<IEntity>) => api.put(`/api/tenant/{module}/${id}`, data),
  delete: (id: number) => api.delete(`/api/tenant/{module}/${id}`),
}

export default moduleApi
```

### 4.3 baseURL 配置

在 `config.ts` 中配置服务端口：

```typescript
export const SERVICES = {
  moduleName: {port},  // 例如: ticket: 5102
}

export const getServiceUrl = (service: keyof typeof SERVICES): string => {
  return `${API_BASE_URL}:${SERVICES[service]}`
}
```

---

## 五、X-Tenant 租户隔离标准

### 5.1 前端请求头

前端 HTTP 拦截器自动添加 `X-Tenant` header：

```typescript
// http.ts
axios.interceptors.request.use(config => {
  const tenantCode = localStorage.getItem('tenantCode')
  if (tenantCode) {
    config.headers['X-Tenant'] = tenantCode
  }
  return config
})
```

### 5.2 后端租户解析

TenantRoutingMiddleware 从 header 提取 tenantCode，默认为 `wo_property`。

---

## 六、方案C租户配置

### 6.1 配置文件

每个项目有独立的 `config/tenant-mapping.json`：

```json
{
  "project": "wo-property",
  "databaseMode": "multi",
  "defaultTenant": "wo_property",
  "tenants": {
    "wo_property": { "displayName": "WO物业", "status": "Active" },
    "tenant_a": { "displayName": "阳光物业", "status": "Active" },
    "tenant_b": { "displayName": "绿城物业", "status": "Active" }
  }
}
```

### 6.2 TenantDbFactory

使用 TenantConfigLoader 构建连接字符串：

```csharp
var config = TenantConfigLoader.LoadConfig();
var connectionString = config.BuildConnectionString(tenantCode);
```

---

## 七、决策记录

| 日期 | 决策 | 理由 |
|------|------|------|
| 2026-05-19 | 统一使用 MySQL | PostgreSQL 增加运维复杂度 |
| 2026-05-19 | StatisticsService 改用 MySQL | 统一技术栈，减少数据库依赖 |
| 2026-05-19 | 所有新模块必须使用 MySQL | 固化设计标准 |

---

**文档版本**：v1.1
**最后更新**：2026-05-19 22:18
