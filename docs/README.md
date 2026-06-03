# WO Property 物业管理软件 - 设计文档

> 最后更新: 2026-05-27
> 版本: v2.1
> 状态: 运营中

---

## 📋 文档目录

1. [系统架构](#系统架构)
2. [服务列表](#服务列表)
3. [数据库设计](#数据库设计)
4. [API 设计规范](#api-设计规范)
5. [多租户架构](#多租户架构)
6. [开发规范](#开发规范)
7. [当前状态](#当前状态)

---

## 系统架构

### 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                        客户端                                │
│  (Vue3 Admin Portal / 微信小程序 / 移动端)                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (5000)                        │
│              统一入口 - JWT认证 + 路由分发                      │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│  公共服务     │     │  业务服务     │     │  基础服务     │
│  - Auth       │     │  - Ticket    │     │  - MySQL     │
│  - Person     │     │  - Dispatch  │     │  - Redis     │
│  - Center     │     │  - Contract  │     └───────────────┘
│  - MasterData │     │  - Material  │
│               │     │  - Device    │
│               │     │  - Finance   │
│               │     │  - Inspection│
└───────────────┘     └───────────────┘
```

### 技术栈

| 组件 | 技术 |
|------|------|
| 后端 | ASP.NET Core 8.0 微服务 |
| 前端 | Vue3 + Vite + Element Plus |
| 数据库 | MySQL 8.0 (单库多租户) |
| 认证 | JWT Bearer Token |
| 缓存 | (预留) Redis |
| 部署 | Docker (预留) |

---

## 服务列表

### 服务端口对照表

| 服务名 | 端口 | 说明 | 数据库表 |
|--------|------|------|---------|
| GatewayService | 5000 | API网关，统一入口 | N/A |
| AuthService | 5106 | 用户认证服务 | Users, RefreshTokens |
| TicketService | 5102 | 工单管理 | Tickets, ticket_process_records |
| DispatchService | 5241 | 智能派单 | dispatch_records, dispatch_rules |
| PersonService | 5018 | 统一人员中心 | Personnel |
| MasterDataService | 5019 | 基础数据+字段管理 | FieldDefinitions, ModuleFields |
| MaterialService | 5504 | 物料管理 | materials |
| NotificationService | 5105 | 通知服务 | Notifications |
| DeviceService | 5530 | 设备管理 | Devices, DeviceTypes |
| ContractService | 5501 | 合同管理 | Contracts |
| FinanceService | 5509 | 财务管理 | FinanceRecords, PaymentRecords |
| InspectionService | 5510 | 巡检管理 | InspectionRecords |
| ComplaintService | 5201 | 投诉管理 | complaints |
| KeyService | 5512 | 钥匙管理 | keys |
| VisitorService | 5513 | 访客管理 | Visitors |
| StatisticsService | 5250 | 统计服务 | MetricSnapshots, trend_records |
| MobileService | 5526 | 移动端服务 | N/A |
| CenterService | 5016 | 项目中心 | center_db.projects |

### 服务状态

**最后检查: 2026-05-27 16:45*/

| 服务 | 状态 |
|------|------|
| Gateway | ✅ 正常 |
| AuthService | ✅ 正常 |
| TicketService | ✅ 正常 |
| DispatchService | ✅ 正常 |
| PersonService | ✅ 正常 |
| MasterDataService | ✅ 正常 |
| MaterialService | ✅ 正常 |
| DeviceService | ✅ 正常 |
| ContractService | ✅ 正常 |
| FinanceService | ✅ 正常 |
| InspectionService | ✅ 正常 |
| ComplaintService | ⚠️ 待检查 |
| KeyService | ✅ 正常 |
| VisitorService | ✅ 正常 |
| StatisticsService | ✅ 正常 |
| MobileService | ✅ 正常 |
| CenterService | ✅ 正常 |

---

## 数据库设计

### 核心原则

**数据库是唯一真相来源 (Database is the source of truth)**

所有代码实现必须以数据库表结构为准。

### 关键表结构

详见: [SCHEMA_REFERENCE.md](../database/SCHEMA_REFERENCE.md)

| 表名 | 说明 |
|------|------|
| Tickets | 工单主表 |
| Contracts | 合同表 |
| keys | 钥匙表 |
| Visitors | 访客表 |
| FinanceRecords | 财务记录 |
| PaymentRecords | 支付记录 |
| InspectionRecords | 巡检记录 |
| materials | 物料表 |
| Devices | 设备表 |
| Personnel | 人员表 |
| Buildings | 楼栋表 |
| Rooms | 房间表 |
| Residents | 住户表 |
| complaints | 投诉表 |
| dispatch_records | 派单记录 |

### 字段映射规范

```csharp
// ❌ 错误：假设 EF 自动推断
entity.Property(e => e.SomeField);

// ✅ 正确：显式映射
entity.Property(e => e.SomeField).HasColumnName("some_field");
```

### 类型映射

| 数据库 | C# |
|--------|-----|
| varchar | string |
| int | int |
| bigint | long |
| decimal | decimal |
| datetime | DateTime? |
| date | DateTime? |
| time | TimeSpan? |
| text | string? |

### 枚举处理

数据库存储字符串，不存储枚举名：

```csharp
// 数据库: "active", "pending", "completed"
// 代码: string，不是 enum
public string Status { get; set; }
```

---

## API 设计规范

### 统一响应格式

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... }
}
```

### 认证

- Header: `Authorization: Bearer <token>`
- 多租户: Header: `X-Project: <project_code>`

### API 路径规范

```
/api/tenant/<module>/<resource>  # 多租户资源
/api/suppliers                    # 公共服务
/api/projects                     # 项目管理
```

### 详细规范

见: [API_STANDARD.md](../API_STANDARD.md)

---

## 多租户架构

### 单库多租户

- 所有项目共用 `wo_property` 数据库
- 通过 `center_db.projects` 表动态解析项目→数据库映射
- TenantDbFactory 统一管理租户连接

### 项目映射

```sql
-- center_db.projects 表
project_code | database_name
YGHY001      | wo_property
```

### TenantDbFactory

每个服务都通过 TenantDbFactory 动态获取数据库连接：

```csharp
public string GetTenantConnectionString(string projectCode)
{
    // 1. 查询 center_db.projects 获取 database_name
    // 2. 替换连接字符串中的数据库名
    // 3. 返回完整连接字符串
}
```

---

## 开发规范

### Schema 验证流程

**每次修改代码前必须执行：**

1. **设计阶段**: 先查看 `docs/database/SCHEMA_REFERENCE.md`
2. **实现阶段**: 使用显式 `HasColumnName()` 映射
3. **验证阶段**: 运行 `./scripts/validate-schema.sh [服务名]`
4. **文档阶段**: 修改后更新 `SCHEMA_REFERENCE.md`

### 验证脚本

```bash
# 验证单个服务
./scripts/validate-schema.sh KeyService

# 验证所有表
./scripts/validate-schema.sh
```

### 相关技能

- `schema-validation`: Schema 验证工作流
- `wo-document-audit`: 文档审计工作流

---

## 当前状态

### 2026-05-25 更新

1. **修复的服务**: KeyService, InspectionService, FinanceService
2. **问题**: 代码模型与数据库表结构不匹配
3. **解决方案**: 重写模型层以匹配数据库实际结构

### 创建的文件

| 文件 | 说明 |
|------|------|
| `docs/database/SCHEMA_REFERENCE.md` | 完整数据库 Schema 对照表 |
| `scripts/validate-schema.sh` | Schema 验证脚本 |
| `skills/schema-validation/SKILL.md` | Schema 验证技能 |

### 测试账号

| 用户名 | 密码 | 角色 |
|--------|------|------|
| admin | Admin@123 | 管理员 |
| tech | Tech@123 | 技术 |
| user | User@123 | 普通用户 |

### 项目代码

- YGHY001 (远程物业)
- 默认使用 YGHY001 进行测试

---

## 相关文档

- [SCHEMA_REFERENCE.md](../database/SCHEMA_REFERENCE.md) - 数据库 Schema 完整参考
- [API_STANDARD.md](../API_STANDARD.md) - API 设计规范
- [AUDIT_LOG.md](../AUDIT_LOG.md) - 审计日志
- [TICKET_SERVICE_ARCHITECTURE_v1.0.md](./design/TICKET_SERVICE_ARCHITECTURE_v1.0.md) - 工单服务架构
- [SERVICE_REFACTOR_2026-05-25.md](./design/SERVICE_REFACTOR_2026-05-25.md) - 服务重构记录