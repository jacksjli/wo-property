# WO Property 物业管理软件 - 项目架构概览

> 最后更新: 2026-05-25
> 版本: v2.0

## 项目概述

WO Property 物业管理软件是一个基于 ASP.NET Core 微服务架构的物业管理平台，支持多租户、单项目模式。

## 技术架构

### 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| 后端框架 | ASP.NET Core | 8.0 |
| 编程语言 | C# | 12 |
| 前端框架 | Vue3 + Vite | - |
| UI 组件库 | Element Plus | - |
| 数据库 | MySQL | 8.0 |
| 认证 | JWT Bearer | - |
| ORM | Entity Framework Core + Pomelo.EntityFrameworkCore.MySql | - |

### 系统架构图

```
                    ┌─────────────────┐
                    │   用户/客户端    │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │   API Gateway   │
                    │     (5000)       │
                    └────────┬────────┘
                             │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌──────▼──────┐      ┌──────▼──────┐      ┌──────▼──────┐
│   公共服务   │      │   业务服务   │      │   基础服务   │
│              │      │              │      │             │
│  Auth        │      │  Ticket      │      │  MySQL      │
│  (5106)      │      │  (5102)      │      │  wo_property│
│              │      │              │      │             │
│  Person      │      │  Dispatch    │      └─────────────┘
│  (5018)      │      │  (5241)      │
│              │      │              │
│  Center      │      │  Contract    │
│  (5016)      │      │  (5501)      │
│              │      │              │
│  MasterData  │      │  Material    │
│  (5019)      │      │  (5504)      │
└──────────────┘      └──────────────┘
        │                     │
        │              ┌──────▼──────┐
        │              │   其他服务   │
        │              │             │
        │              │  Finance   │
        │              │  (5509)     │
        │              │             │
        │              │  Inspection│
        │              │  (5510)     │
        │              │             │
        │              │  Key        │
        │              │  (5512)     │
        │              │             │
        │              │  Visitor    │
        │              │  (5513)     │
        │              │             │
        │              │  Device     │
        │              │  (5530)     │
        └──────────────┴─────────────┘
```

## 服务通信

### 内部通信

服务间通信通过 HTTP REST API，使用 JWT Bearer Token 认证。

### 跨服务调用规则

```
业务服务 → 公共服务（允许）
业务服务 → 业务服务（不鼓励，应该通过事件或消息队列）
```

### 公共服务

| 服务 | 端口 | 功能 |
|------|------|------|
| AuthService | 5106 | 用户认证、JWT Token 发放 |
| PersonService | 5018 | 人员信息管理 |
| CenterService | 5016 | 项目中心、租户映射 |
| MasterDataService | 5019 | 字段定义、模块字段配置 |

## 数据库架构

### 单库多租户

- **主数据库**: `wo_property`
- **中心数据库**: `center_db` (存储项目映射)
- **模式**: 所有项目共享 `wo_property`，通过 `project_code` 字段区分

### 关键表

| 表 | 说明 |
|-----|------|
| Tickets | 工单主表 |
| Contracts | 合同表 |
| Personnel | 人员表 |
| Buildings | 楼栋表 |
| Rooms | 房间表 |
| Residents | 住户表 |
| Visitors | 访客表 |
| Materials | 物料表 |
| Devices | 设备表 |
| FinanceRecords | 财务记录 |
| PaymentRecords | 支付记录 |
| InspectionRecords | 巡检记录 |

### Schema 参考

完整数据库 Schema: [SCHEMA_REFERENCE.md](../database/SCHEMA_REFERENCE.md)

## 多租户架构

### 项目映射

```sql
-- center_db.projects
project_code | database_name | project_name
YGHY001      | wo_property   | 远程物业
```

### TenantDbFactory

每个服务通过 TenantDbFactory 动态获取数据库连接：

```csharp
public string GetTenantConnectionString(string projectCode)
{
    // 1. 查询 center_db.projects 获取 database_name
    // 2. 替换连接字符串中的数据库名
    // 3. 返回完整连接字符串
}
```

### 请求头

- `Authorization: Bearer <token>` - JWT 认证
- `X-Project: <project_code>` - 项目标识（如 YGHY001）

## API 设计

### 统一响应格式

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... }
}
```

### 路径规范

```
/api/tenant/<module>/<resource>  # 多租户资源
/api/<module>/<resource>         # 公共服务
```

### API 文档

- [API_STANDARD.md](../API_STANDARD.md)
- [API.md](../API.md)

## 开发规范

### Schema 验证

**核心原则**: 数据库是唯一真相来源

**工作流程**:
1. 设计阶段: 先查看 `docs/database/SCHEMA_REFERENCE.md`
2. 实现阶段: 使用显式 `HasColumnName()` 映射
3. 验证阶段: 运行 `./scripts/validate-schema.sh [服务名]`
4. 文档阶段: 修改后更新 `SCHEMA_REFERENCE.md`

### 代码规范

- 使用 `string` 类型存储枚举值（不用 C# enum）
- 可空字段用 `?` 标记
- 显式映射所有列名：`HasColumnName("exact_db_name")`

## 相关文档

| 文档 | 说明 |
|------|------|
| [README.md](./README.md) | 主文档 |
| [SERVICES_STATUS.md](./SERVICES_STATUS.md) | 服务状态 |
| [SCHEMA_REFERENCE.md](../database/SCHEMA_REFERENCE.md) | 数据库 Schema |
| [API_STANDARD.md](../API_STANDARD.md) | API 规范 |
| [TICKET_SERVICE_ARCHITECTURE_v1.0.md](./design/TICKET_SERVICE_ARCHITECTURE_v1.0.md) | 工单服务架构 |