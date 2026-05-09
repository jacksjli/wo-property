# WO 物业管理软件系统分析报告

**日期：** 2026-05-05
**分析人：** 软件架构分析团队
**版本：** v1.0

---

## 一、系统概述

### 1.1 项目基本信息

| 项目 | 内容 |
|------|------|
| 项目名称 | WO物业管理软件现代化重构 |
| 项目周期 | 4周（2026年4月20日 - 2026年5月18日）|
| 架构类型 | 微服务架构（16个后端服务 + 1个前端）|
| 技术栈后端 | ASP.NET Core 8.0 / .NET 10 |
| 技术栈前端 | Vue 3 + Vite + Element Plus |
| 数据库 | SQLite（开发）/ MySQL（生产）|
| 端口范围 | 5002-5020 |

### 1.2 系统服务清单

#### 后端服务（16个）

| # | 服务名 | 端口 | 数据库 | 状态 | 实现质量 |
|---|--------|------|--------|------|----------|
| 1 | WO.Property.AuthService | 5006 | SQLite | ✅ 运行中 | 中（仅基础注册/登录/工单CRUD） |
| 2 | WO.Property.ComplaintService | 5011 | SQLite | ✅ 运行中 | 中（有控制器，分层清晰） |
| 3 | WO.Property.ContractService | 5008 | SQLite | ✅ 运行中 | 低（仅2个Controller，无业务逻辑） |
| 4 | WO.Property.DeviceService | 5007 | SQLite | ✅ 运行中 | 中（Program.cs内嵌逻辑，无Controller） |
| 5 | WO.Property.DispatchService | 5003 | SQLite | ✅ 运行中 | 中（Program.cs内嵌逻辑，较完整） |
| 6 | WO.Property.FinanceService | 5009 | SQLite | ✅ 运行中 | 低（仅2个Controller骨架） |
| 7 | WO.Property.InspectionService | 5010 | SQLite | ✅ 运行中 | 低（Program.cs内嵌，无实体模型文件） |
| 8 | WO.Property.KeyService | 5012 | SQLite | ✅ 运行中 | 低（Program.cs内嵌，无Controller） |
| 9 | WO.Property.MasterDataService | 5019 | SQLite | ✅ 运行中 | 中（3个Controller，有字段管理） |
| 10 | WO.Property.MaterialService | 5004 | SQLite | ✅ 运行中 | 中（无Controller，Program.cs内嵌） |
| 11 | WO.Property.MobileService | 5015 | SQLite | ✅ 运行中 | 中（无Controller，Program.cs内嵌） |
| 12 | WO.Property.NotificationService | 5005 | SQLite | ✅ 运行中 | 中（无Controller，Program.cs内嵌） |
| 13 | WO.Property.PersonService | 5018 | MySQL | ⚠️ 依赖外部MySQL | 高（有完整分层，使用MySQL） |
| 14 | WO.Property.StatisticsService | 5014 | SQLite | ✅ 运行中 | 中（4个内嵌Controller） |
| 15 | WO.Property.TicketService | 5002 | SQLite | ✅ 运行中 | ⚠️ **严重问题**（见下方） |
| 16 | WO.Property.VisitorService | 5013 | SQLite | ✅ 运行中 | 低（Program.cs内嵌，无Controller） |

#### 前端服务（1个）

| 服务名 | 端口 | 说明 |
|--------|------|------|
| admin-portal | 5173 | Vue 3管理后台 |

### 1.3 文档与实现的重大不符

**架构设计文档声称的服务结构与实际代码存在严重偏差：**

#### 1.3.1 API Gateway（5000端口）缺失

- **文档描述：** "API Gateway (5000) 作为统一入口，基于YARP实现路由转发、负载均衡、限流熔断"
- **实际情况：** 代码仓库中完全不存在API Gateway服务，没有任何服务监听5000端口
- **影响：** 前端直接连接各服务端口，无法享受统一认证、限流、路由等能力

#### 1.3.2 TicketService 与 AuthService 功能重叠

- **架构设计文档：** TicketService(5002)专注工单，AuthService(5006)专注认证
- **实际情况：** 两个服务的Program.cs内容几乎完全相同——都包含"用户注册/登录/工单管理"功能
- **证据：** 对比两个Program.cs，均包含：
  - 用户注册 `/api/auth/register`
  - 用户登录 `/api/auth/login`
  - 当前用户 `/api/auth/me`
  - 工单列表 `/api/tickets`
  - 创建工单 `/api/tickets`
  - Ticket 和 User 实体定义完全相同

#### 1.3.3 文档描述了完整工单生命周期，实际TicketService几乎为空

- **功能规格说明书描述的完整工单流程：**
  - 创建 → 派单 → 接单/拒单 → 进度更新 → 完工申请 → 完工确认 → 评价
- **TicketService(5002)实际实现：**
  - 只有基本的 GET /api/tickets 和 POST /api/tickets
  - **没有派单、接单、拒单、完工、评价等任何状态流转API**
  - 派单功能实现在 DispatchService(5003) 中
  - 状态更新逻辑完全缺失

#### 1.3.4 文档描述7个核心服务，实际大部分是空壳

- **架构设计文档列出的核心服务：** TicketService、DispatchService、NotificationService、PersonService、MasterDataService、StatisticsService、API Gateway
- **实际情况：**
  - TicketService：工单管理功能严重不完整
  - NotificationService：无真正的通知推送（只有数据库记录）
  - StatisticsService：有框架但无真实数据聚合
  - API Gateway：**完全不存在**

### 1.4 数据库架构不符

| 文档设计 | 实际实现 |
|----------|----------|
| 生产环境 MySQL 8.0，按项目拆库 | 仅PersonService使用MySQL，其他全部SQLite |
| 每个服务独立数据库（tickets_db、dispatch_db等）| 各服务独立SQLite文件，无跨服务数据共享 |
| 设计了 Tickets、TicketProcessRecords、DispatchTasks 等关联表 | TicketService中仅有一个简化的Ticket表，无关联表 |
| 多租户通过 TenantId 隔离 | 仅文档描述，实际代码无TenantId字段 |

---

## 二、后端服务架构（16个服务详解）

### 2.1 WO.Property.AuthService（端口5006）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.AuthService/Program.cs`
**代码行数：** 545行
**Controller数量：** 0（无Controllers目录，所有逻辑在Program.cs中）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | /api/auth/register | 否 | 用户注册 |
| POST | /api/auth/login | 否 | 用户登录，返回JWT |
| GET | /api/auth/me | 是 | 获取当前用户信息 |
| GET | /api/tickets | 是 | 获取工单列表 |
| POST | /api/tickets | 是 | 创建工单 |
| GET | /health | 否 | 健康检查 |

**数据模型：**
```csharp
User { Id, Username, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt }
Ticket { Id, TicketCode, Title, Description, Category, Priority, Status, CreatedBy, AssignedTo, CreatedAt, UpdatedAt }
```

**问题分析：**
- **P0** 此服务功能与TicketService完全重复
- **P1** 无Controllers目录，违背ASP.NET Core标准分层
- **P1** Ticket模型与数据库设计文档严重不符（缺少TenantId、Location、Images、Rating等字段）
- **P2** Program.cs过长（545行），违反单一职责原则

---

### 2.2 WO.Property.TicketService（端口5002）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.TicketService/Program.cs`
**代码行数：** 651行
**Controller数量：** 0（无Controllers目录）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/tickets | 是 | 获取工单列表（按status/priority筛选）|
| POST | /api/tickets | 是 | 创建工单 |
| GET | /internal/tickets/{id}/creator | - | 获取工单创建人（内部API）|
| GET | /internal/tickets/{id}/assignee | - | 获取工单指派人（内部API）|
| GET | /health | 否 | 健康检查 |

**与AuthService的完全重复：**
- AuthService(5006) 和 TicketService(5002) 的 Program.cs 内容几乎完全一致
- 唯一区别：TicketService调用PersonService获取创建人/指派人信息（通过HTTP Client）
- 两服务均有 `/api/tickets` 端点，会产生冲突

**问题分析：**
- **P0** 功能与AuthService完全重复，职责不清
- **P0** TicketService按照架构设计应该是核心工单服务，但实际实现的功能远少于文档描述
- **P0** 缺少完整的工单状态流转API（派单/接单/拒单/完工/评价均缺失）
- **P1** 两服务同时存在会端口冲突或功能混乱
- **P1** 使用GUID生成工单编号而非文档规定的 `WO-YYYYMMDD-XXXX` 格式

---

### 2.3 WO.Property.DispatchService（端口5003）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.DispatchService/Program.cs`
**代码行数：** 605行
**Controller数量：** 0（所有逻辑在Program.cs中）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/dispatch-rules | 是 | 获取派单规则列表 |
| GET | /api/dispatch-rules/{id} | 是 | 获取规则详情 |
| POST | /api/dispatch-rules | 是 | 创建规则 |
| PUT | /api/dispatch-rules/{id} | 是 | 更新规则 |
| DELETE | /api/dispatch-rules/{id} | 是 | 删除规则 |
| POST | /api/dispatch-rules/{id}/toggle-status | 是 | 切换状态 |
| POST | /api/dispatch-rules/match | 是 | 匹配规则 |
| GET | /api/dispatch-tasks | 是 | 获取派单任务列表 |
| POST | /api/dispatch-tasks | 是 | 创建派单任务 |
| POST | /api/dispatch-tasks/{id}/accept | 是 | 接单 |
| POST | /api/dispatch-tasks/{id}/reject | 是 | 拒单 |
| POST | /api/dispatch-tasks/{id}/complete | 是 | 完成任务 |
| POST | /api/dispatch-tasks/{id}/cancel | 是 | 取消任务 |
| GET | /api/timeout-rules | 是 | 获取超时规则 |
| PUT | /api/timeout-rules | 是 | 更新超时规则 |
| GET | /api/dispatch/stats | 是 | 统计 |
| GET | /api/dispatch/analytics | 是 | 分析数据 |
| GET | /health | 否 | 健康检查 |

**数据模型：**
```csharp
DispatchRule { Id, RuleNo, Name, Type, TicketColor, Location, Priority, AutoAssign, OperatorIds, SupervisorId, ManagerId, Status, MatchCount, SuccessCount }
DispatchTask { Id, TaskNo, TicketId, RuleId, AssignedTo, Status, AcceptedAt, CompletedAt, ResponseTime, TimeoutAt }
TimeoutRule { Id, Color, Role, Hours, Enabled }
```

**问题分析：**
- **P1** 无Controllers目录，不符合标准分层
- **P2** Program.cs过长（605行）
- **P2** DispatchRule.RuleType使用"round-robin/nearest/load-balance/skill-match"但代码中未实现具体算法

---

### 2.4 WO.Property.NotificationService（端口5005）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.NotificationService/Program.cs`
**代码行数：** 609行
**Controller数量：** 0（所有逻辑在Program.cs中）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/notifications | - | 获取通知列表（支持userId/isRead筛选）|
| GET | /api/notifications/{id} | - | 获取通知详情 |
| POST | /api/notifications | - | 创建通知 |
| PUT | /api/notifications/{id}/read | - | 标记已读 |
| PUT | /api/notifications/read-all | - | 全部标记已读 |
| GET | /api/message-templates | - | 获取消息模板 |
| POST | /api/message-templates | - | 创建模板 |
| GET | /api/announcements | - | 获取公告 |
| POST | /api/announcements | - | 创建公告 |
| GET | /api/reports/tickets | - | 工单报表 |
| GET | /api/reports/devices | - | 设备报表 |
| GET | /api/reports/materials | - | 物料报表 |
| GET | /api/statistics/summary | - | 统计摘要 |
| GET | /health | 否 | 健康检查 |

**数据模型：**
```csharp
Notification { Id, UserId, Title, Content, Type, Priority, IsRead, ReadAt, RelatedEntityType, RelatedEntityId, CreatedAt }
MessageTemplate { Id, Name, Type, Subject, Content, Variables }
Announcement { Id, Title, Content, Type, IsTop, IsActive, Priority, StartDate, EndDate, CreatedBy }
TicketReport { Id, TicketNumber, Title, Status, Priority, AssignedTo, CreatedAt, ResolvedAt }
DeviceReport { Id, Code, Name, Status, MaintenanceType, MaintenanceCost, CreatedAt }
MaterialReport { Id, Code, Name, CurrentStock, SafetyStock, UnitPrice, CreatedAt }
```

**问题分析：**
- **P0** "通知服务"名不副实：没有真正的短信/微信/推送能力，只有数据库记录
- **P1** 报表API依赖空表（TicketReports/DeviceReports/MaterialReports），实际无数据聚合逻辑
- **P1** 所有端点均无认证（[Authorize]注解缺失），安全隐患
- **P2** Program.cs过长（609行）

---

### 2.5 WO.Property.PersonService（端口5018）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.PersonService/Program.cs`
**代码行数：** 129行
**Controller数量：** 3（EnumsController, HealthController, PersonsController）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/persons | 是 | 获取人员列表 |
| GET | /api/persons/{id} | 是 | 获取人员详情 |
| POST | /api/persons | 是 | 创建人员 |
| PUT | /api/persons/{id} | 是 | 更新人员 |
| DELETE | /api/persons/{id} | 是 | 删除人员 |
| GET | /api/enums | - | 获取枚举值 |
| GET | /health | 否 | 健康检查 |

**数据存储：** MySQL（其他服务均使用SQLite）

**问题分析：**
- **P1** MySQL连接字符串硬编码：`Server=localhost;Port=3306;Database=wo_property;User=root;Password=WO_Property_2026;`
- **P1** 使用的是 MySQL 但其他服务均用 SQLite，无法实现跨服务数据共享
- **P1** PersonDbContext依赖外部MySQL服务，如果MySQL未启动，PersonService将无法运行
- **P2** Program.cs仅129行，但数据库初始化代码在单独的DbInitializer类中

---

### 2.6 WO.Property.MasterDataService（端口5019）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.MasterDataService/Program.cs`
**代码行数：** 101行
**Controller数量：** 3（EnumsController, FieldDefinitionsController, ModuleFieldsController）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/field-definitions | - | 获取字段定义列表 |
| POST | /api/field-definitions | - | 创建字段定义 |
| GET | /api/field-definitions/{id} | - | 获取详情 |
| PUT | /api/field-definitions/{id} | - | 更新 |
| DELETE | /api/field-definitions/{id} | - | 删除 |
| GET | /api/module-fields | - | 获取模块字段 |
| POST | /api/module-fields | - | 创建模块字段关联 |
| GET | /api/enums | - | 获取枚举值 |
| GET | /health | 否 | 健康检查 |

**问题分析：**
- **P2** 无认证注解，API完全公开
- **P2** 功能相对独立，但与前端"字段管理"功能对应

---

### 2.7 WO.Property.StatisticsService（端口5014）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.StatisticsService/Program.cs`
**代码行数：** 522行
**Controller数量：** 4个内嵌Controller（DashboardController, ReportsController, MetricsController, TrendsController）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/dashboard | 是 | 仪表盘数据 |
| GET | /api/dashboard/summary | 是 | 摘要 |
| GET | /api/dashboard/configs | 是 | 仪表盘配置 |
| GET | /api/reports | 是 | 报表列表 |
| GET | /api/reports/{id} | 是 | 报表详情 |
| POST | /api/reports | 是 | 生成报表 |
| GET | /api/reports/generate/{type} | 是 | 按类型生成报表 |
| GET | /api/metrics/snapshot | 是 | 指标快照 |
| GET | /api/metrics/snapshots | 是 | 历史快照 |
| GET | /api/metrics/trends | 是 | 趋势数据 |
| GET | /api/metrics/indicators | 是 | 指标概览 |
| POST | /api/metrics/snapshot | 是 | 创建快照 |
| GET | /api/trends | 是 | 趋势记录 |
| GET | /api/trends/summary | 是 | 趋势摘要 |
| POST | /api/trends | 是 | 创建趋势记录 |
| GET | /health | 否 | 健康检查 |

**问题分析：**
- **P1** 数据聚合依赖MetricSnapshots、TrendRecords等表，但这些表需要外部数据填充
- **P1** 报表生成是空壳：生成的是静态假数据或空值
- **P2** Controller内嵌在Program.cs中，不便于维护

---

### 2.8 WO.Property.ComplaintService（端口5011）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.ComplaintService/Program.cs`
**代码行数：** 367行
**Controller数量：** 3（ComplaintsController, RepliesController, CategoriesController）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/complaints | 是 | 投诉列表 |
| GET | /api/complaints/{id} | 是 | 投诉详情 |
| POST | /api/complaints | 是 | 创建投诉/建议 |
| PUT | /api/complaints/{id}/accept | 是 | 受理 |
| PUT | /api/complaints/{id}/process | 是 | 处理中 |
| PUT | /api/complaints/{id}/resolve | 是 | 已解决 |
| PUT | /api/complaints/{id}/close | 是 | 关闭 |
| GET | /api/complaints/stats | 是 | 统计 |
| GET | /api/replies/by-complaint/{complaintId} | 是 | 回复列表 |
| POST | /api/replies | 是 | 添加回复 |
| GET | /api/categories | - | 分类列表 |
| GET | /health | 否 | 健康检查 |

**问题分析：**
- **P2** ComplaintService有完整的投诉/建议管理，但功能规格说明书中无对应描述（投诉管理是前端额外功能）
- **P2** 回复功能完整但无权限控制

---

### 2.9 WO.Property.ContractService（端口5008）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.ContractService/`
**Controller数量：** 2（ContractsController, PaymentsController）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/contracts | - | 合同列表 |
| GET | /api/contracts/{id} | - | 合同详情 |
| POST | /api/contracts | - | 创建合同 |
| PUT | /api/contracts/{id} | - | 更新合同 |
| GET | /api/payments | - | 付款记录列表 |
| GET | /api/payments/{id} | - | 付款详情 |
| POST | /api/payments | - | 创建付款记录 |

**问题分析：**
- **P1** 完全无认证注解，所有API公开
- **P1** 合同管理功能完整但无任何业务逻辑验证
- **P2** 仅骨架代码，无实际业务处理

---

### 2.10 WO.Property.FinanceService（端口5009）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.FinanceService/`
**Controller数量：** 2（BillsController, PaymentsController）

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/bills | - | 账单列表 |
| GET | /api/bills/{id} | - | 账单详情 |
| GET | /api/bills/stats | - | 账单统计 |
| GET | /api/payments | - | 付款记录 |
| POST | /api/payments | - | 创建付款记录 |

**问题分析：**
- **P1** 完全无认证注解
- **P1** 仅骨架代码

---

### 2.11 WO.Property.MaterialService（端口5004）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.MaterialService/Program.cs`
**代码行数：** 347行
**Controller数量：** 0

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/material-categories | 否 | 物料分类（公开）|
| GET | /api/materials/statistics | 是 | 库存统计 |
| GET | /api/materials | 是 | 物料列表 |
| GET | /api/materials/{id} | 是 | 物料详情 |
| POST | /api/materials/{id}/stock-in | 是 | 入库 |
| POST | /api/materials/{id}/stock-out | 是 | 出库 |
| GET | /health | 否 | 健康检查 |

**数据模型：**
```csharp
Material { Id, Code, Name, Description, CategoryId, Unit, UnitPrice, SafetyStock, MaxStock, CurrentStock }
MaterialCategory { Id, Name, Code, Description }
StockTransaction { Id, MaterialId, TransactionType, Quantity, UnitPrice, TotalAmount, Operator, Notes }
PurchaseOrder { Id, OrderNumber, OrderDate, Supplier, TotalAmount, Status }
```

**问题分析：**
- **P2** 入库/出库API无事务保证，可能导致库存数据不一致
- **P2** 无物料新增/编辑/删除API（仅支持查询和库存操作）

---

### 2.12 WO.Property.DeviceService（端口5007）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.DeviceService/Program.cs`
**代码行数：** 465行
**Controller数量：** 0

**API端点：**
| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/devices | 是 | 设备列表 |
| GET | /api/devices/{id} | 是 | 设备详情 |
| POST | /api/devices | 是 | 创建设备 |
| PUT | /api/devices/{id} | 是 | 更新设备 |
| GET | /api/device-categories | 否 | 设备分类（公开）|
| GET | /api/locations | 否 | 位置信息（公开）|
| POST | /api/devices/{id}/maintenance | 是 | 添加维护记录 |
| GET | /api/devices/{id}/maintenance-history | 是 | 维护历史 |
| GET | /api/devices/statistics | 是 | 设备统计 |
| GET | /health | 否 | 健康检查 |

**问题分析：**
- **P2** 无Controller，所有逻辑在Program.cs
- **P2** 设备位置信息无层级校验

---

### 2.13 WO.Property.InspectionService（端口5010）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.InspectionService/`
**代码行数：** 429行
**Controller数量：** 0

**API端点：** 包含Plans/Tasks/Records管理的API，但无完整实现

**问题分析：**
- **P1** 只有Models/InspectionModels.cs和DbContext，无实际API实现
- **P1** Program.cs中可能有部分实现但未确认

---

### 2.14 WO.Property.KeyService（端口5012）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.KeyService/Program.cs`
**代码行数：** 440行
**Controller数量：** 0

**API端点：** 包含钥匙/借用管理的API

**问题分析：**
- **P1** 仅有Models/KeyModels.cs，无Controller
- **P1** Program.cs中有完整实现（钥匙管理、借用记录、审批等）

---

### 2.15 WO.Property.VisitorService（端口5013）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.VisitorService/`
**代码行数：** 450行
**Controller数量：** 0

**API端点：** 访客登记、签到、签离等

**问题分析：**
- **P2** 仅有Models/VisitorModels.cs
- **P2** Program.cs中有实现

---

### 2.16 WO.Property.MobileService（端口5015）

**文件路径：** `/Users/mac/Projects/WO-Property-Management/src/WO.Property.MobileService/Program.cs`
**代码行数：** 535行
**Controller数量：** 0

**API端点：** 移动端专用API（首页数据、快捷入口、设备注册、推送通知、二维码登录）

**问题分析：**
- **P1** 二维码登录功能存在，但token生成逻辑需要检查
- **P2** 无Controller，全部在Program.cs

---

## 三、前端模块结构

### 3.1 前端服务概览

**admin-portal（端口5173）**
- 技术栈：Vue 3 + Vite + Element Plus + Pinia
- 页面数量：约38个Vue文件
- Store数量：约24个状态管理模块
- API客户端：约7个服务客户端

### 3.2 前端Views（页面）统计

| 目录/模块 | 页面文件 | 数量 |
|-----------|----------|------|
| 根目录 | AboutView, DashboardView, FieldManagementView, HomeView, LoginView, ModuleFieldsView, NotFoundView, TestLogin, TestPage | 9 |
| complaint/ | ComplaintList.vue | 1 |
| contract/ | ContractList.vue | 1 |
| device/ | DeviceList.vue | 1 |
| dispatch/ | DispatchList, DispatchRules, TimeoutSettings | 3 |
| finance/ | FinanceList.vue | 1 |
| inspection/ | InspectionList.vue | 1 |
| key/ | KeyList.vue | 1 |
| material/ | MaterialList.vue | 1 |
| notification/ | NotificationList.vue | 1 |
| parking/ | ParkingList.vue | 1 |
| payment/ | PaymentList.vue | 1 |
| personnel/ | PersonnelList.vue | 1 |
| project/ | ProjectList, ProjectTrackingList | 2 |
| resident/ | ResidentList.vue | 1 |
| settings/ | SystemSettingsView.vue | 1 |
| statistics/ | StatisticsView.vue | 1 |
| takeout/ | TakeoutList.vue | 1 |
| ticket/ | TicketDetailView, TicketList, TicketListView | 3 |
| ticketType/ | TicketTypeList.vue | 1 |
| user/ | UserList, UserManagementView, UserProfileView | 3 |
| visitor/ | VisitorList.vue | 1 |
| **总计** | | **38** |

### 3.3 前端Stores（状态管理）统计

| Store文件 | 管理的模块 |
|-----------|-----------|
| auth.ts | 用户认证 |
| complaint.ts | 投诉管理 |
| contract.ts | 合同管理 |
| counter.ts | 计数器（示例）|
| device.ts | 设备管理 |
| dispatch.ts | 派单任务 |
| express.ts | 快递/外卖 |
| fieldConfig.ts | 字段配置 |
| finance.ts | 财务管理 |
| inspection.ts | 巡检管理 |
| key.ts | 钥匙管理 |
| material.ts | 物料管理 |
| notification.ts | 通知管理 |
| personnel.ts | 人员管理 |
| project.ts | 项目管理 |
| projectTracking.ts | 项目跟踪 |
| staff.ts | 员工管理 |
| takeout.ts | 外卖管理 |
| ticket.ts | 工单管理 |
| ticketType.ts | 工单类型 |
| timeout.ts | 超时配置 |
| visitor.ts | 访客管理 |
| **总计** | **23个** |

### 3.4 前端API客户端

| API文件 | 对应服务 | 端口 | 状态 |
|---------|----------|------|------|
| auth.ts | AuthService | 5006 | ✅ 存在 |
| config.ts | 服务配置 | - | ⚠️ **严重问题** |
| device.ts | DeviceService | 5007 | ✅ 存在 |
| http.ts | HTTP基础 | - | ⚠️ **严重问题** |
| masterDataService.ts | MasterDataService | 5019 | ✅ 存在 |
| material.ts | MaterialService | 5004 | ✅ 存在 |
| notification.ts | NotificationService | 5005 | ✅ 存在 |
| ticket.ts | TicketService | 5002 | ⚠️ **有缺陷** |
| (缺失) | DispatchService | 5003 | ❌ **缺失** |
| (缺失) | PersonService | 5018 | ❌ **缺失** |
| (缺失) | StatisticsService | 5014 | ❌ **缺失** |
| (缺失) | ComplaintService | 5011 | ❌ **缺失** |
| (缺失) | ContractService | 5008 | ❌ **缺失** |
| (缺失) | FinanceService | 5009 | ❌ **缺失** |
| (缺失) | InspectionService | 5010 | ❌ **缺失** |
| (缺失) | KeyService | 5012 | ❌ **缺失** |
| (缺失) | VisitorService | 5013 | ❌ **缺失** |
| (缺失) | MobileService | 5015 | ❌ **缺失** |

### 3.5 前端路由配置（router/index.ts）

**完整路由表：**

```typescript
/                    → DashboardView.vue
/login               → LoginView.vue
/tickets             → TicketList.vue
/device              → DeviceList.vue
/material            → MaterialList.vue
/notification        → NotificationList.vue
/contract            → ContractList.vue
/finance             → FinanceList.vue
/inspection          → InspectionList.vue
/complaint           → ComplaintList.vue
/key                 → KeyList.vue
/visitor             → VisitorList.vue
/parking             → ParkingList.vue
/payment             → PaymentList.vue
/statistics          → StatisticsView.vue
/user                → UserList.vue
/settings            → SystemSettingsView.vue
/personnel           → PersonnelList.vue
/takeout             → TakeoutList.vue
/resident            → ResidentList.vue
/ticket-type         → TicketTypeList.vue
/dispatch            → DispatchList.vue
/dispatch-rules      → DispatchRules.vue
/timeout-settings    → TimeoutSettings.vue
/project             → ProjectList.vue
/project-tracking    → ProjectTrackingList.vue
/field-management    → FieldManagementView.vue
/module-fields       → ModuleFieldsView.vue
/:pathMatch(.*)*     → NotFoundView.vue
```

**路由守卫：** 仅检查token存在性，无角色权限控制

### 3.6 MainLayout.vue侧边栏菜单

**可见菜单项（硬编码在MainLayout.vue）：**
```
仪表板（Dashboard）
工单管理（Ticket）
设备管理（Device）
物料管理（Material）
通知中心（Notification）
个人资料（User）
用户管理（User）—— 仅admin可见
系统设置（Settings）—— 仅admin可见
外卖管理（Takeout）—— 仅admin可见
```

**实际路由 vs 侧边栏不匹配：**
- 侧边栏硬编码"工单管理"链接到`/tickets`
- 但路由表同时存在`/ticket-type`等独立页面
- 派单管理、钥匙管理等未在侧边栏体现

---

## 四、模块依赖关系图（实际）

### 4.1 服务间实际依赖（代码层面）

```
TicketService(5002) ──HTTP调用──> PersonService(5018)
     │
     └─与AuthService(5006)功能完全重复（均处理auth+ticket）

DispatchService(5003) ──无跨服务调用──> [独立运行]

NotificationService(5005) ──无跨服务调用──> [独立运行]

StatisticsService(5014) ──无跨服务调用──> [独立运行]

PersonService(5018) ──依赖外部MySQL──> wo_property数据库

其他服务 ──均无跨服务调用──> [各自独立]
```

### 4.2 前端到后端的实际调用关系

```
admin-portal(5173)
    │
    ├── authApi (5006)     → AuthService
    ├── ticketApi (5002)  → TicketService ⚠️ broken（引用不存在的API_CONFIG和ticketHttp）
    ├── materialApi (5004) → MaterialService
    ├── notificationApi (5005) → NotificationService ⚠️ broken（引用不存在的API_CONFIG）
    ├── deviceApi (5007)   → DeviceService ⚠️ broken（引用不存在的API_CONFIG）
    └── (缺失) → DispatchService ❌
    └── (缺失) → PersonService ❌
    └── (缺失) → StatisticsService ❌
    └── (缺失) → ContractService ❌
    └── (缺失) → FinanceService ❌
    └── (缺失) → ComplaintService ❌
    └── (缺失) → InspectionService ❌
    └── (缺失) → KeyService ❌
    └── (缺失) → VisitorService ❌
    └── (缺失) → MobileService ❌
```

### 4.3 数据库实际分布

| 服务 | 数据库文件 | 实际表 |
|------|-----------|---------|
| AuthService(5006) | property_auth_service.db | Users, Tickets |
| TicketService(5002) | property_with_auth.db | Users, Tickets |
| DispatchService(5003) | property_dispatch.db | DispatchRules, DispatchTasks, TimeoutRules |
| NotificationService(5005) | property_notifications.db | Notifications, MessageTemplates, Announcements, TicketReports, DeviceReports, MaterialReports |
| MaterialService(5004) | property_materials.db | Materials, MaterialCategories, StockTransactions, PurchaseOrders |
| DeviceService(5007) | property_devices.db | Devices, DeviceCategories, Locations, MaintenanceRecords |
| ContractService(5008) | ? | Contracts, Payments |
| FinanceService(5009) | ? | Bills, Payments |
| PersonService(5018) | MySQL: wo_property | Persons, Departments, Roles, Staffs, Residents, Buildings, Rooms |
| MasterDataService(5019) | masterdata.db | FieldDefinitions, ModuleFields |
| StatisticsService(5014) | property_statistics.db | DashboardConfigs, MetricSnapshots, TrendRecords, Reports |
| ComplaintService(5011) | property_complaints.db | Complaints, Categories, Replies |
| VisitorService(5013) | ? | Visitors, VisitRecords |
| KeyService(5012) | ? | Keys, Borrows |
| InspectionService(5010) | ? | Plans, Tasks, Records |
| MobileService(5015) | ? | Devices, QRSessions |

---

## 五、功能重复分析

### 5.1 服务职责重叠

#### 5.1.1 AuthService vs TicketService（P0级重复）

- **证据：**
  - 两个Program.cs内容几乎完全相同
  - 均包含 `/api/auth/register`, `/api/auth/login`, `/api/auth/me`, `/api/tickets`, `/api/tickets` (POST)
  - 两个服务同时运行会产生API端点冲突
  - 均定义了相同的 User 和 Ticket 模型

- **重复代码量：** 约500行完全相同

- **建议：** 合并这两个服务，保留一个统一的"认证+工单"服务（或将认证抽取为独立AuthService，将工单留给TicketService）

#### 5.1.2 NotificationService vs StatisticsService（报表功能重叠）

- **问题：** NotificationService的 `/api/reports/*` 端点与StatisticsService功能重复
- NotificationService生成空壳报表（依赖不存在的表）
- StatisticsService有完整的报表框架但缺数据

---

## 六、架构问题清单

### P0级问题（必须修复）

#### 问题#P0-1：API Gateway完全缺失

- **描述：** 架构设计要求5000端口的API Gateway，实际代码中完全不存在
- **证据：** 搜索代码库，无YARP相关配置，无5000端口监听
- **影响：** 前端无法通过统一入口访问各服务；无统一认证、限流、路由能力
- **建议：** 紧急开发API Gateway服务，实现路由转发、统一认证

#### 问题#P0-2：TicketService与AuthService功能完全重复

- **描述：** 两个服务功能几乎完全相同，同时运行会产生冲突
- **证据：** 对比Program.cs，约500行代码相同
- **影响：** 系统无法正常运作（端口/路由冲突）；维护困难
- **建议：** 立即合并，保留TicketService作为工单核心服务，AuthService作为认证服务（仅保留认证相关API）

#### 问题#P0-3：前端API客户端严重损坏

- **描述：** ticket.ts引用不存在的`ticketHttp`和`API_CONFIG.TICKET_SERVICE`
- **证据：**
  - `http.ts`仅导出`createHttpClient`函数，无`ticketHttp` export
  - `config.ts`仅导出`SERVICES`对象，无`API_CONFIG`对象
  - 但`ticket.ts`第1行 `import { ticketHttp } from './http'` → 编译失败
  - `notification.ts`, `device.ts`同样引用不存在的`API_CONFIG`
- **影响：** 工单、设备、通知等功能的前端调用全部无法工作
- **建议：** 修复API客户端配置，使用实际存在的export

#### 问题#P0-4：工单核心流程完全未实现

- **描述：** 功能规格说明书描述的完整工单流程（派单→接单→拒单→进度更新→完工申请→完工确认→评价）在TicketService中完全缺失
- **证据：** TicketService(5002)仅有两个API：GET /api/tickets, POST /api/tickets
- **影响：** 核心功能不可用
- **建议：** 在TicketService中补充完整的状态流转API，或由DispatchService统一实现

#### 问题#P0-5：JWT认证配置错误导致认证失效

- **描述：** 大多数服务虽然添加了JWT认证中间件，但配置参数不完整或使用了错误的验证参数
- **证据：**
  - PersonService: `ValidateIssuer = false, ValidateAudience = false`（完全关闭验证）
  - 其他服务均设置为`true`但issuer/audience配置一致
- **影响：** 认证机制不可靠
- **建议：** 统一JWT配置，确保issuer/audience/secretkey一致

---

### P1级问题（应该修复）

#### 问题#P1-1：前端路由与侧边栏菜单不匹配

- **描述：** 侧边栏硬编码菜单项，部分路由无菜单入口（如/dispatch, /key, /visitor等）
- **影响：** 用户无法通过UI访问这些功能
- **建议：** 完善侧边栏菜单配置

#### 问题#P1-2：前端多个API客户端缺失

- **描述：** 10个后端服务无对应前端API客户端
- **影响：** 这些模块的前端无法调用后端
- **建议：** 为缺失的服务补充API客户端

#### 问题#P1-3：PersonService使用MySQL与其他服务SQLite不一致

- **描述：** PersonService连接外部MySQL，其他15个服务使用SQLite
- **影响：** 无法实现跨服务数据共享；PersonService强依赖MySQL
- **建议：** 考虑统一数据库技术栈，或在微服务架构下通过API交互

#### 问题#P1-4：大量服务无Controllers目录

- **描述：** 11/16个服务无Controllers目录，所有逻辑堆积在Program.cs中
- **影响：** 代码可维护性差；违背ASP.NET Core标准分层
- **建议：** 逐步重构，将业务逻辑移至Controller/Service层

#### 问题#P1-5：数据聚合逻辑缺失

- **描述：** NotificationService的报表API和StatisticsService依赖空表，无真实数据聚合
- **影响：** 统计报表功能不可用
- **建议：** 实现从TicketService等源服务拉取数据的机制

#### 问题#P1-6：文档与实现严重不符

- **描述：** 架构设计文档描述的很多功能在代码中未实现
- **影响：** 文档失去指导意义；甲乙双方对系统功能认知不一致
- **建议：** 更新文档以匹配实际实现，或补充缺失功能

---

### P2级问题（建议修复）

#### 问题#P2-1：Program.cs过长

- **描述：** 多个服务的Program.cs超过400行
  - DispatchService: 605行
  - NotificationService: 609行
  - TicketService: 651行
- **建议：** 重构为标准分层架构

#### 问题#P2-2：部分服务无认证保护

- **描述：** ContractService, FinanceService, MasterDataService的API完全公开
- **建议：** 添加适当的认证和权限控制

#### 问题#P2-3：部分服务无HTTPS配置

- **描述：** 大多数服务无HTTPS强制跳转配置
- **建议：** 生产环境应启用HTTPS

#### 问题#P2-4：前端无角色权限控制

- **描述：** 路由守卫仅检查token存在性，无角色权限控制
- **建议：** 实现基于角色的菜单和路由保护

#### 问题#P2-5：工单编号格式不符

- **描述：** TicketService使用GUID生成工单号（如`TICKET-20260505-a1b2c3`），而非文档规定的`WO-YYYYMMDD-XXXX`格式
- **建议：** 统一工单编号格式

---

## 七、接口规范问题

### 7.1 响应格式不统一

| 服务 | 成功响应 | 失败响应 |
|------|---------|---------|
| AuthService | `{ Success, Message, Data, Pagination }` | `{ Success: false, Message }` |
| TicketService | `{ Success, Data, Pagination }` | `{ Success: false, Message }` |
| DispatchService | `{ success, message, data, total }` | `{ success: false, message }` |
| NotificationService | `{ notifications }` 或 `{ report }` | `{ message }` |
| PersonService | 标准控制器格式 | 标准控制器格式 |

**问题：** AuthService/TicketService用PascalCase（Success），DispatchService用camelCase（success）

### 7.2 认证方式混乱

| 服务 | 端口 | JWT验证 | 问题 |
|------|------|---------|------|
| AuthService | 5006 | ✅ 完整验证 | 正常 |
| TicketService | 5002 | ✅ 完整验证 | 正常 |
| DispatchService | 5003 | ✅ 完整验证 | 正常 |
| NotificationService | 5005 | ✅ 完整验证 | 正常 |
| MaterialService | 5004 | ✅ 完整验证 | 正常 |
| PersonService | 5018 | ❌ Issuer/Audience验证关闭 | 安全隐患 |
| ContractService | 5008 | ❌ 无认证注解 | 完全公开 |
| FinanceService | 5009 | ❌ 无认证注解 | 完全公开 |
| MasterDataService | 5019 | ❌ 无认证注解 | 完全公开 |

### 7.3 路径命名不规范

| 服务 | 路径模式 | 问题 |
|------|---------|------|
| AuthService | `/api/auth/*`, `/api/tickets/*` | 嵌套资源路径 |
| DispatchService | `/api/dispatch-rules/*`, `/api/dispatch-tasks/*` | 复数名词 |
| NotificationService | `/api/notifications/*`, `/api/reports/*` | 复数名词 |
| StatisticsService | `/api/dashboard/*`, `/api/metrics/*` | 嵌套资源 |

### 7.4 端口分配混乱

| 服务 | 文档端口 | 实际端口 | 状态 |
|------|---------|---------|------|
| API Gateway | 5000 | **不存在** | ❌ |
| TicketService | 5002 | 5002 | ✅ |
| DispatchService | 5003 | 5003 | ✅ |
| NotificationService | 5005 | 5005 | ✅ |
| PersonService | 5018 | 5018 | ✅ |
| MasterDataService | 5019 | 5019 | ✅ |
| StatisticsService | 5014 | 5014 | ✅ |
| MaterialService | (文档无) | 5004 | ✅ |
| DeviceService | (文档无) | 5007 | ✅ |
| ContractService | (文档无) | 5008 | ✅ |
| FinanceService | (文档无) | 5009 | ✅ |
| InspectionService | (文档无) | 5010 | ✅ |
| ComplaintService | (文档无) | 5011 | ✅ |
| KeyService | (文档无) | 5012 | ✅ |
| VisitorService | (文档无) | 5013 | ✅ |
| MobileService | (文档无) | 5015 | ✅ |
| AuthService | (文档无) | 5006 | ✅ |

---

## 八、待确认事项（需要和甲方讨论）

### 8.1 架构层面的重大决策

1. **API Gateway是否需要实现？**
   - 文档描述了完整的YARP API Gateway，实际未开发
   - 需要确认：是补充开发还是修改文档？

2. **服务拆分粒度是否合理？**
   - 当前16个服务，部分服务功能极简（如FinanceService仅2个空Controller）
   - 是否考虑合并功能相近的服务？

3. **数据库技术选型：**
   - 文档规定MySQL+分库分表，实际仅PersonService用MySQL
   - 是保持现状（SQLite+MySQL混合）还是统一技术栈？

4. **TicketService与AuthService合并：**
   - 两个服务功能完全重复，需要立即决策
   - 建议：AuthService专注认证，TicketService专注工单

### 8.2 功能层面的重要缺失

1. **工单完整流程未实现**
   - 功能规格说明书中的派单→接单→拒单→完工→评价流程完全缺失
   - 确认：是否在本次开发周期内完成？

2. **通知推送能力缺失**
   - NotificationService无真正的短信/微信/推送能力
   - 确认：是否需要接入第三方推送服务？

3. **统计报表无数据**
   - StatisticsService框架完整但无真实数据聚合逻辑
   - 确认：报表数据来源是实时聚合还是定期同步？

### 8.3 前端实现确认

1. **派单管理、钥匙管理、访客管理等前端页面存在但无API客户端**
   - 这些页面的数据从哪里来？
   - 是否需要补充API客户端？

2. **前端角色权限控制**
   - 当前仅在侧边栏用v-if="authStore.isAdmin"控制
   - 是否需要完整的RBAC权限系统？

### 8.4 部署与运维

1. **文档描述的Docker/Kubernetes部署，实际是否已配置？**
   - 代码库中是否有Dockerfile或docker-compose.yml？

2. **监控告警配置**
   - ELK Stack、Prometheus+Grafana等是否已部署？

---

## 九、建议的优先级处理顺序

### 第一优先级（立即处理，1-2天内）

| 序号 | 问题 | 负责人 | 预计工时 |
|------|------|--------|---------|
| 1 | 修复前端API客户端配置（ticketHttp/API_CONFIG缺失）| 前端工程师 | 4小时 |
| 2 | 合并或删除重复的AuthService/TicketService | 后端架构师 | 8小时 |
| 3 | 实现API Gateway基础路由功能（或明确放弃）| 后端架构师 | 8小时 |

### 第二优先级（本周内）

| 序号 | 问题 | 负责人 | 预计工时 |
|------|------|--------|---------|
| 4 | 补充工单状态流转API（派单/接单/完工/评价）| 后端工程师 | 16小时 |
| 5 | 修复JWT认证配置，统一issuer/audience | 后端工程师 | 4小时 |
| 6 | 补充缺失的前端API客户端（dispatch/person/statistics等）| 前端工程师 | 8小时 |
| 7 | 完善前端侧边栏菜单（匹配实际路由）| 前端工程师 | 4小时 |

### 第三优先级（第2周）

| 序号 | 问题 | 负责人 | 预计工时 |
|------|------|--------|---------|
| 8 | 重构大Service的Program.cs为标准分层（Controller/Service）| 后端工程师 | 24小时 |
| 9 | 实现统计报表数据聚合逻辑 | 后端工程师 | 16小时 |
| 10 | 添加服务间HTTP调用实现（如Dispatch→PersonService）| 后端工程师 | 8小时 |
| 11 | 补充无认证服务的权限控制 | 后端工程师 | 8小时 |

### 第四优先级（后续迭代）

| 序号 | 问题 | 负责人 | 预计工时 |
|------|------|--------|---------|
| 12 | 完善NotificationService的真正推送能力 | 后端工程师 | 24小时 |
| 13 | 实现完整的RBAC前端权限系统 | 前端工程师 | 16小时 |
| 14 | 更新文档以匹配实际实现 | 文档负责人 | 8小时 |
| 15 | 配置Docker容器化部署 | 运维工程师 | 8小时 |
| 16 | 配置监控告警系统 | 运维工程师 | 8小时 |

---

## 附录A：服务完整性评分

| 服务 | 评分(0-10) | 说明 |
|------|------------|------|
| AuthService | 4 | 功能基础但与TicketService重复 |
| TicketService | 2 | 功能严重不完整，与AuthService重复 |
| DispatchService | 6 | 功能较完整，无标准分层 |
| NotificationService | 5 | 有框架但无真正推送能力 |
| PersonService | 7 | 结构清晰，使用MySQL |
| MasterDataService | 5 | 有Controller，字段管理功能完整 |
| StatisticsService | 5 | 有框架但无真实数据 |
| MaterialService | 6 | 功能较完整，库存管理可用 |
| DeviceService | 6 | 功能较完整，设备台账可用 |
| ContractService | 3 | 仅骨架，无业务逻辑 |
| FinanceService | 3 | 仅骨架，无业务逻辑 |
| ComplaintService | 7 | 功能完整，投诉管理可用 |
| InspectionService | 3 | 仅骨架，模型存在但无API |
| KeyService | 5 | 功能较完整，钥匙管理可用 |
| VisitorService | 5 | 功能较完整，访客管理可用 |
| MobileService | 5 | 有框架，移动端支持可用 |
| admin-portal | 4 | 页面多但API调用大量损坏 |

---

## 附录B：前端API客户端问题详细分析

### 问题B-1：ticket.ts的编译错误

```typescript
// ticket.ts 第1-2行：
import { ticketHttp } from './http';        // ❌ http.ts无此export
import { API_CONFIG } from './config';      // ❌ config.ts无此export
```

### 问题B-2：notification.ts的编译错误

```typescript
// notification.ts 第2行：
import { API_CONFIG } from './config';      // ❌ config.ts无API_CONFIG
// 使用示例：
notificationHttp.get(API_CONFIG.NOTIFICATION_SERVICE.ENDPOINTS.NOTIFICATIONS, { params })
// API_CONFIG.NOTIFICATION_SERVICE不存在
```

### 问题B-3：device.ts的编译错误

```typescript
// device.ts 第2行：
import { API_CONFIG } from './config';      // ❌ 同上
// 使用示例：
deviceHttp.get(API_CONFIG.DEVICE_SERVICE.ENDPOINTS.DEVICES, { params })
// API_CONFIG.DEVICE_SERVICE不存在
```

### 正确的config.ts实际内容

```typescript
export const SERVICES = {
  auth: 5006,
  material: 5004,
  notification: 5005,
  contract: 5008,
  finance: 5009,
  inspection: 5010,
  complaint: 5011,
  key: 5012,
  visitor: 5013,
  statistics: 5014,
  mobile: 5015,
  ticket: 5002,
  device: 5007,
}
```

前端代码应使用 `getServiceUrl('ticket')` 配合 `createHttpClient()` 创建实例。

---

## 附录C：代码库结构速查

```
/Users/mac/Projects/WO-Property-Management/
├── src/
│   ├── WO.Property.AuthService/          ← 空壳，Program.cs
│   ├── WO.Property.TicketService/        ← 空壳，Program.cs（与AuthService重复）
│   ├── WO.Property.DispatchService/      ← 空壳，Program.cs
│   ├── WO.Property.NotificationService/  ← 空壳，Program.cs
│   ├── WO.Property.PersonService/        ← 有Controller，MySQL
│   ├── WO.Property.MasterDataService/    ← 有Controller
│   ├── WO.Property.StatisticsService/    ← 有内嵌Controller
│   ├── WO.Property.MaterialService/      ← 空壳，Program.cs
│   ├── WO.Property.DeviceService/        ← 空壳，Program.cs
│   ├── WO.Property.ContractService/      ← 有Controller（骨架）
│   ├── WO.Property.FinanceService/        ← 有Controller（骨架）
│   ├── WO.Property.InspectionService/     ← 有Models（骨架）
│   ├── WO.Property.KeyService/            ← 有Models（骨架）
│   ├── WO.Property.VisitorService/        ← 有Models（骨架）
│   ├── WO.Property.MobileService/         ← 有Models（骨架）
│   └── admin-portal/
│       └── src/
│           ├── api/                       ← 7个文件，部分损坏
│           ├── stores/                    ← 23个状态管理
│           ├── views/                     ← 38个页面
│           ├── layouts/MainLayout.vue    ← 侧边栏菜单
│           └── router/index.ts           ← 27条路由
├── docs/analysis/                         ← 本报告输出目录
└── (无solution文件)
```

---

**报告结束**

*本报告由系统分析团队编写，基于2026年5月5日的代码快照。*
*报告中的问题分类基于以下标准：*
- *P0：系统无法正常运行或存在严重安全隐患*
- *P1：功能不完整或实现与文档严重不符*
- *P2：代码质量问题或次要功能缺陷*
