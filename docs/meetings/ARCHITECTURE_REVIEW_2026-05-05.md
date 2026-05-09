# 架构方案评审会 - 议题备忘
**会议主题**：PersonService + MasterDataService 实现方案 & MySQL 统一迁移
**日期**：2026-05-05
**主持人**：软件架构师
**参与**：软件负责人（列席）

---

## 一、议题背景

甲方明确三件事：
1. PersonService (5018) 和 MasterDataService (5019) 必须实现
2. 14 个微服务之间必须有真正的通信机制
3. 所有服务统一使用 MySQL，放弃 SQLite

当前问题：
- PersonService 和 MasterDataService 只有目录，无 Program.cs（服务不存在）
- 各服务完全独立，无任何服务间调用（14 个孤岛）
- 除 FinanceService 外，其他服务均使用 SQLite（不适合生产）

---

## 二、需要架构师回答的核心问题

### 问题 A：PersonService 和 MasterDataService 的职责边界

**PersonService (5018)** 需要提供什么 API？

```
方案候选：
  A1. 仅管理员工/住户基础信息（name, phone, email, department, role）
  A2. 管理员工 + 住户 + 角色权限 + 认证
  A3. 管理人员 + 各模块的业务人员关联（如工单的 Assignee）

当前 TicketService 的 Tickets 表里：
  - CreatedBy → 指向 TicketService 本地 Users 表
  - AssignedTo → 指向 TicketService 本地 Users 表
  
期望迁移后：
  - CreatedBy → PersonService 的统一人员 ID
  - 所有服务不再自己存 Users 表，统一调用 PersonService
```

### 问题 B：MasterDataService (5019) 提供什么数据？

```
需要确认：
  - 楼栋/房间数据（buildingId, roomNo, floor）
  - 枚举值（工单类型、设备类型、物料分类等）
  - 还是只有楼栋/房间，枚举值放其他地方？

当前各模块的 options 是硬编码在前端 fieldConfig.ts 的
需要确认：options 枚举值是否也从 MasterDataService 获取？
```

### 问题 C：服务间通信方式

```
方案候选：
  C1. HTTP REST（简单，现有服务改动最小）
  C2. gRPC（高性能，但需要所有服务支持 proto）
  C3. 消息队列（异步，适合状态同步类场景）

考虑到：
  - .NET 10 服务（现有）
  - 需要保持简单，不要过度设计
  - 优先推荐：C1 HTTP REST 作为第一步
```

### 问题 D：MySQL 迁移策略

```
现状：
  - 14 个服务 × 各自 SQLite 文件
  - FinanceService 已用 MySQL (5001)

迁移候选方案：
  D1. 一次性全部迁移（风险大，工作量大）
  D2. 先建 PersonService + MasterDataService 使用 MySQL，
      其他服务逐步迁移
  D3. 新增服务用 MySQL，现有服务维持 SQLite，3 个月后全部切换

推荐：D2（风险可控，先跑通核心路径）
```

### 问题 E：14 个服务是否都需要和 PersonService/MasterDataService 通信？

```
服务列表：
  TicketService, MaterialService, ContractService, FinanceService,
  VisitorService, NotificationService, InspectionService,
  ComplaintService, KeyService, DeviceService, StatisticsService,
  AuthService, DispatchService, MobileService

分析：
  - TicketService：高度依赖人员（Creator, Assignee）
  - ContractService：依赖人员（负责人）
  - 所有服务：依赖 MasterDataService（房间、位置、枚举）

结论：所有服务都需要调用这两个公共服务
```

---

## 三、架构师需要输出的文档

### 3.1 PersonService API 规范

```
必须包含：
  - GET /api/persons - 人员列表（支持分页、搜索）
  - GET /api/persons/{id} - 人员详情
  - POST /api/persons - 创建人员
  - PUT /api/persons/{id} - 更新人员
  - GET /api/persons/by-role/{role} - 按角色获取人员
  - GET /api/enums/departments - 部门枚举
  - GET /api/enums/roles - 职位枚举

数据模型：
  Person {
    id, staffId, name, gender, phone, email,
    department, role, joinDate, status,
    idCard, emergencyContact, emergencyPhone, ...
  }
```

### 3.2 MasterDataService API 规范

```
必须包含：
  - GET /api/buildings - 楼栋列表
  - GET /api/buildings/{id}/rooms - 楼栋下的房间
  - GET /api/rooms/{id} - 房间详情
  - GET /api/enums/ticket-types - 工单类型枚举
  - GET /api/enums/device-types - 设备类型枚举
  - GET /api/enums/material-categories - 物料分类枚举
  - GET /api/enums/priorities - 优先级枚举

数据模型：
  Building { id, name, code, ... }
  Room { id, buildingId, floor, unit, ... }
```

### 3.3 服务间调用协议

```
调用约定：
  - 所有服务调用 PersonService/MasterDataService 使用 HTTP
  - baseURL: http://localhost:5018 (PersonService)
  - baseURL: http://localhost:5019 (MasterDataService)
  - 认证：使用 JWT Bearer Token（各服务自己的 token）

错误处理：
  - 如果 PersonService 不可用，相关功能降级（不阻塞主流程）
  - 如果 MasterDataService 不可用，使用硬编码枚举值作为 fallback
```

### 3.4 MySQL 迁移方案

```
数据库规划：
  - MySQL Host: localhost 或 Docker container
  - MySQL Port: 3306
  - Database: wo_property (统一数据库)
    或按服务分库：
      - wo_property_tickets
      - wo_property_materials
      - wo_property_persons (PersonService)
      - wo_property_masterdata (MasterDataService)
      - wo_property_finance (FinanceService)

迁移步骤（推荐分批）：
  1. 新建 PersonService + MasterDataService（MySQL）
  2. 修改 TicketService，调用 PersonService（保持本地数据）
  3. 一次性迁移所有 SQLite → MySQL
  4. 删除各服务的本地 Users 表（迁移到 PersonService）
```

---

## 四、决策待确认项（需甲方拍板）

### 决策 D1：数据库是统一一个大库还是按服务分库？

```
选项：
  D1-A：统一一个大库 wo_property（简单，所有服务共享）
  D1-B：按服务分库（wo_property_tickets, wo_property_materials 等）
        优点：服务间隔离好；缺点：跨库 JOIN 不存在了
```

### 决策 D2：服务间调用的错误处理策略？

```
选项：
  D2-A：强依赖（PersonService 不可用则相关操作全部失败）
  D2-B：弱依赖（PersonService 不可用则降级，保留本地缓存数据）
```

### 决策 D3：迁移期间如何保证数据不丢失？

```
选项：
  D3-A：停机迁移（所有服务停止，迁移完成后重启）
  D3-B：双写策略（新数据同时写 SQLite 和 MySQL，逐步切换）
  D3-C：一次性切换（先备份 SQLite，切换到 MySQL，验证后删除旧数据）
```

---

## 五、下次会议目标

**时间**：2026-05-06 14:00
**目标**：架构师输出完整的 API 规范文档 + MySQL 迁移方案 + 时间表

**待甲方确认**：
1. 数据库统一大库 vs 分库
2. 服务间调用错误处理策略
3. 迁移期间数据保护策略

---

**软件负责人**：已记录甲方决策，准备召集架构师会议
**下次更新**：2026-05-06 14:00
