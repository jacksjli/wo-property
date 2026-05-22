# 工单模块架构设计文档（v3.0）

**版本：** v3.0
**日期：** 2026-05-19
**作者：** 软件项目负责人
**状态：** ✅ 已确认（指导性文件）

---

## 一、架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                    TicketService (5102)                     │
│              工单全生命周期管理 + 状态流转驱动                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  │
│  │   Property    │  │      HR       │  │   Finance     │  │
│  │  Dispatcher   │  │  Dispatcher   │  │  Dispatcher   │  │
│  │   (物业工单)   │  │  (人事工单)   │  │  (财务工单)   │  │
│  └───────┬───────┘  └───────┬───────┘  └───────┬───────┘  │
│          │                  │                  │            │
│          └──────────────────┼──────────────────┘            │
│                             ↓                                │
│                   ┌─────────────────┐                       │
│                   │  Dispatcher     │                       │
│                   │  Registry       │                       │
│                   │  (规则注册中心)  │                       │
│                   └────────┬────────┘                       │
└────────────────────────────┼────────────────────────────────┘
                             ↓
┌────────────────────────────┼────────────────────────────────┐
│              DispatchService (5003)                          │
│                    派单规则引擎                              │
└────────────────────────────┼────────────────────────────────┘
                             ↓
┌────────────────────────────┼────────────────────────────────┐
│         NotificationService (5129)                          │
│              状态变更通知 + 满意度调查                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 二、工单类别（可扩展）

| category | 名称 | 派单规则 | 说明 |
|----------|------|---------|------|
| `property` | 物业工单 | 工种+区域+主管优先 | ✅ 当前实现 |
| `hr` | 人事工单 | 部门+职位+HR链 | 🔜 预留扩展 |
| `finance` | 财务工单 | 金额阈值+审批链 | 🔜 预留扩展 |

---

## 三、派单规则（核心算法）

### 3.1 匹配优先级

```
第一步：job_type 匹配（必需）
        └─ 人员 specialty_ids 必须包含工单的 job_type_id

第二步：area 匹配（放宽）
        └─ 优先选择 area 匹配的人员
        └─ 如果没有 area 完全匹配 → job_type 匹配的人员也可以派（不严格卡 area）

第三步：按优先级排序
        └─ 紧急 > 高 > 普通 > 低
        └─ 同优先级：派单最少的人优先
```

### 3.2 派单目标

```
同工单类型的多个工种 → 派给该类型的主管（1人）
不同工单类型 → 分别派给对应类型的主管（多人）
```

### 3.3 拒单处理

```
拒单次数 ≤ 3 → 自动重新派单
拒单次数 > 3 → 转人工派单（dispatch_status = 'manual'）
```

---

## 四、超时升级机制

### 4.1 复用现有 TimeoutSettings

| 颜色 | 说明 | 基础超时 |
|------|------|---------|
| green | 一般 | 24小时 |
| blue | 普通 | 12小时 |
| orange | 较急 | 6小时 |
| red | 紧急 | 2小时 |

管理员通过 `TimeoutSettings.vue` 随时调整每个颜色×角色的超时配置。

### 4.2 升级路径

```
operator（处理人）
    ↓ 超时
supervisor（主管）
    ↓ 超时
manager（经理）
    ↓ 超时
department_head（部门负责人）
    ↓ 超时
company_head（公司负责人）
```

---

## 五、满意度调查

### 5.1 设计原则

- **前置条件**：必须完成满意度调查才能关闭工单
- **简单化**：只需 5 星评分，无需多维度问卷
- **可选性**：住户可以跳过评价直接关闭

### 5.2 触发流程

```
Finished（完工申请）
    │
    ▼
Confirmed（确认完工）
    │
    ├─→ 发送满意度调查（通知住户：请您评分）
    │
    ▼
Survey_Pending（待评价）
    │
    ├── 住户提交 5 星评分 → rating 写入 satisfaction_surveys 表 → Closed
    │
    └── 7 天未评价 → 提醒 → 7 天后自动关闭（rating = null）
```

---

## 六、数据库模型

### 6.1 工单主表（Tickets）

```sql
CREATE TABLE tickets (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    tenant_id       BIGINT NOT NULL COMMENT '租户ID（显式字段）',
    ticket_no       VARCHAR(20) NOT NULL UNIQUE COMMENT '工单编号',

    -- 分类
    category        VARCHAR(20) NOT NULL DEFAULT 'property' COMMENT 'property/hr/finance',
    ticket_type_id  BIGINT COMMENT '工单类型 → ticket_types.id',
    priority        VARCHAR(10) DEFAULT 'Normal' COMMENT 'urgent/high/normal/low',
    color           VARCHAR(10) DEFAULT 'blue' COMMENT 'green/blue/orange/red（对应超时配置）',

    -- 内容
    title           VARCHAR(200) NOT NULL,
    description     TEXT,
    location_detail VARCHAR(500) COMMENT '详细地址',

    -- 位置（物业用）
    area_id         BIGINT,
    building_id     BIGINT,
    room_id         BIGINT,

    -- 人员
    creator_id      BIGINT NOT NULL COMMENT '创建人',
    reporter_name   VARCHAR(50) COMMENT '报修人',
    reporter_phone  VARCHAR(20) COMMENT '报修电话',
    assignee_id     BIGINT COMMENT '指派人（主管）',
    handler_id      BIGINT COMMENT '实际处理人',

    -- 扩展字段（人事/财务用）
    custom_fields   JSON COMMENT '{ departmentId, amount, costType, ... }',

    -- 状态
    status          VARCHAR(20) DEFAULT 'New',
    current_role    VARCHAR(20) DEFAULT 'operator' COMMENT 'operator/supervisor/manager/...',
    escalation_level INT DEFAULT 0 COMMENT '已升级次数',
    dispatch_status VARCHAR(20) DEFAULT 'pending' COMMENT 'pending/assigned/manual/escalated',
    reject_count    INT DEFAULT 0 COMMENT '拒单次数',

    -- 满意度
    survey_status   VARCHAR(20) DEFAULT 'pending' COMMENT 'pending/sent/surveyed/closed_without_survey',

    -- 时间戳
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME ON UPDATE CURRENT_TIMESTAMP,
    last_escalated_at DATETIME,
    assigned_at     DATETIME,
    accepted_at     DATETIME,
    finished_at     DATETIME,
    closed_at       DATETIME,

    INDEX idx_tenant (tenant_id),
    INDEX idx_category (category),
    INDEX idx_status (status),
    INDEX idx_color (color)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 6.2 工单-工种-人员多派中间表

```sql
CREATE TABLE ticket_dispatch_mapping (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id       BIGINT NOT NULL,
    job_type_id     BIGINT NOT NULL,
    assignee_id     BIGINT NOT NULL,
    dispatch_type   VARCHAR(20) DEFAULT 'auto' COMMENT 'auto/manual/escalate',
    dispatch_status VARCHAR(20) DEFAULT 'waiting' COMMENT 'waiting/accepted/rejected/finished',
    reject_reason   VARCHAR(200),
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    accepted_at     DATETIME,
    rejected_at     DATETIME,
    finished_at     DATETIME,
    UNIQUE KEY uk_ticket_jobtype_assignee (ticket_id, job_type_id, assignee_id),
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 6.3 派单记录表

```sql
CREATE TABLE dispatch_records (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id       BIGINT NOT NULL,
    assignee_id     BIGINT NOT NULL,
    dispatcher_type VARCHAR(20) NOT NULL COMMENT 'auto/manual/escalate',
    rule_id         BIGINT COMMENT '命中的规则ID',
    reason          VARCHAR(200),
    status          VARCHAR(20) DEFAULT 'waiting',
    reject_reason   VARCHAR(200),
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 6.4 派单规则表

```sql
CREATE TABLE dispatch_rules (
    id                  BIGINT AUTO_INCREMENT PRIMARY KEY,
    tenant_id           BIGINT NOT NULL,
    category            VARCHAR(20) NOT NULL DEFAULT 'property',
    name                VARCHAR(100),
    enabled             TINYINT(1) DEFAULT 1,
    sort_order          INT DEFAULT 0,

    -- 匹配条件
    ticket_type_id      BIGINT COMMENT '工单类型（NULL=所有）',
    job_type_id         BIGINT COMMENT '工种（NULL=所有）',
    area_id             BIGINT COMMENT '区域（NULL=所有）',
    priority            VARCHAR(10),

    -- 派单目标
    target_person_id    BIGINT COMMENT '指定处理人',
    target_department_id BIGINT COMMENT '指定部门（部门内轮询）',
    supervisor_id       BIGINT COMMENT '主管（物业用）',
    target_role         VARCHAR(50),

    -- 配置
    dispatch_type       VARCHAR(20) DEFAULT 'auto' COMMENT 'auto/round_robin/random',
    max_reject_count    INT DEFAULT 3,

    INDEX idx_tenant_category (tenant_id, category),
    INDEX idx_sort (tenant_id, category, sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 6.5 满意度调查表

```sql
CREATE TABLE satisfaction_surveys (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id       BIGINT NOT NULL UNIQUE,
    rating          INT COMMENT '1-5星',
    comment         VARCHAR(500) COMMENT '选填备注',
    respondent_name VARCHAR(50),
    submitted_at    DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 6.6 超时配置表

```sql
CREATE TABLE timeout_settings (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    tenant_id       BIGINT NOT NULL,
    config          JSON COMMENT '复用现有 timeout.ts 格式',
    check_interval_seconds INT DEFAULT 60,
    enabled             TINYINT(1) DEFAULT 1,
    created_at          DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_tenant (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 七、工单状态流转图

```
                    ┌────────────────────┐
                    │       New          │ ← 工单创建（category 确定）
                    └─────────┬──────────┘
                              │
                    ┌─────────▼──────────┐
                    │     Pending         │ ← 等待派单
                    └─────────┬──────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
      自动派单成功        无匹配人员      拒单>3次
              │               │               │
              ▼               ▼               ▼
    ┌─────────────────┐  ┌──────────┐  ┌──────────────┐
    │   Dispatched    │  │  Pending │  │    Manual    │ ← 人工派单
    └────────┬────────┘  └──────────┘  └──────────────┘
             │
     ┌──────┴──────┐
     │             │
接受            拒单(≤3)
     │             │
     ▼             ▼
  ┌────────┐  ┌─────────────────┐
  │Accepted│  │   Pending       │ ← 重新派单
  └────┬───┘  └─────────────────┘
       │ progress
       ▼
  ┌───────────┐
  │Processing │
  └─────┬─────┘
        │ finish
        ▼
  ┌──────────┐
  │ Finished │
  └────┬─────┘
       │ confirm
       ▼
  ┌───────────┐
  │ Confirmed │
  └─────┬─────┘
        │
        ├─→ 发送满意度调查通知
        │
        ▼
  ┌─────────────────┐
  │ Survey_Pending  │
  └────┬────────────┘
       │
  ┌────┴────────────┐
  │                  │
评分提交           超时关闭
  │                  │
  ▼                  ▼
┌────────┐       ┌────────┐
│ Closed │       │ Closed │
│(surveyed)      │(no survey)│
└────────┘       └────────┘
```

---

## 八、API 总览

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/tenant/tickets` | 工单列表（支持 category 筛选） |
| POST | `/api/tenant/tickets` | 创建工单（自动派单） |
| GET | `/api/tenant/tickets/{id}` | 工单详情 |
| PUT | `/api/tenant/tickets/{id}` | 更新工单 |
| POST | `/api/tenant/tickets/{id}/status` | 更新状态 |
| GET | `/api/tenant/tickets/{id}/timeout-status` | 超时状态 |
| GET | `/api/tenant/tickets/overdue` | 超时工单列表 |
| POST | `/api/tenant/tickets/{id}/survey` | 提交满意度（关闭工单） |
| POST | `/api/tenant/tickets/{id}/close-without-survey` | 跳过评价关闭 |
| GET | `/api/tenant/tickets/{id}/dispatch-status` | 派单状态 |
| PUT | `/api/tenant/tickets/{id}/reassign` | 人工重新指派 |
| GET | `/api/tenant/tickets/{id}/survey` | 获取满意度调查 |

---

## 九、实现优先级

| 阶段 | 内容 | 状态 | 说明 |
|------|------|------|------|
| **P0.1** | Tickets 表新增字段（tenant_id/category/color/custom_fields/survey_status） | ✅ 已完成 | 2026-05-19 |
| **P0.2** | ticket_dispatch_mapping / dispatch_records / satisfaction_surveys 表 | ✅ 已完成 | 2026-05-19 |
| **P0.3** | DispatcherRegistry + PropertyDispatcher（物业工单） | ✅ 已完成 | 2026-05-19 |
| **P0.4** | 自动派单（工单创建触发 DispatchService） | ✅ 已完成 | 2026-05-19 |
| **P0.5** | 满意度调查（5星评分，前置关闭） | ✅ 已完成 | 2026-05-19 |
| **P1.1** | TicketTimeoutMonitor（L1~L4 超时升级） | ✅ 已完成 | 2026-05-19，后台每60秒检查 |
| **P1.2** | 拒单处理 + 超阈值转人工派单 | ✅ 已完成 | RejectCount>3 转 manual |
| **P1.3** | 调度员监控台（overdue 工单列表） | ✅ 已完成 | /api/tenant/tickets/overdue |
| **P2.1** | HRDispatcher + FinanceDispatcher（人事/财务扩展） | ✅ 已完成 | 框架就绪，HR/财务逻辑待实现 |
| **P2.2** | 人工重新指派 API | ✅ 已完成 | PUT /api/tenant/tickets/{id}/reassign |

---

## 十、服务依赖

```
TicketService (5102)
  ← MasterDataService (5019)：工单类型、工种、区域/楼栋/房号
  ← PersonService (5018)：人员信息（specialty_ids / area_ids / is_supervisor）
  → DispatchService (5003)：触发自动派单
  → NotificationService (5005)：状态变更通知 + 满意度调查通知
```

---

## 十一、决策记录

| 日期 | 决策 | 理由 |
|------|------|------|
| 2026-05-06 | AuthService 专注认证，移除工单代码 | 职责单一，避免重复 |
| 2026-05-06 | 工单派单由 TicketService 调用 DispatchService | 解耦，DispatchService 专注派单规则 |
| 2026-05-19 | 工单编号格式：`{类型代码}{年月}{5位序号}` | 便于按类型统计 |
| 2026-05-19 | 工单类别泛化：property/hr/finance | 支持未来扩展人事/财务工单 |
| 2026-05-19 | 派单优先级：job_type 优先 > area 其次 > 主管优先 | 符合物业实际场景 |
| 2026-05-19 | 超时升级复用现有 TimeoutSettings | 管理员可人工调整颜色×角色超时配置 |
| 2026-05-19 | 满意度调查：5星评分，工单关闭前置条件 | 简单化，可选跳过 |
| 2026-05-19 | 拒单>3次转人工派单 | 保证工单最终有人处理 |
| 2026-05-19 | 同一工单多个工种 → 分别派给对应类型主管 | 主管只处理自己擅长领域的工单 |

---

## 十二、关联文档

| 文档 | 说明 |
|------|------|
| `docs/MODULES/ticket/API.md` | 工单 API 详细定义（v1.1） |
| `docs/MODULES/ticket/FIELDS.md` | 工单字段定义（v1.1） |
| `docs/MODULES/ticket/STATUS_FLOW.md` | 状态流转图（v1.1） |
| `docs/PHASE0_MULTI_TENANT_ARCHITECTURE.md` | Phase 0 多租户架构 |

---

**文档版本**：v3.0
**作者**：软件项目负责人
**审核**：软件架构师
**状态**：✅ 已确认为工单模块开发指导性文件
**最后更新**：2026-05-19
---

## 十三、租户配置方案（方案C）

### 13.1 背景

Phase 0 原有租户体系依赖 `center_db.tenants` 表，存在以下问题：
- tenant_a → 数据库 tenant_a（不存在）
- tenant_b → 数据库 tenant_b（不存在）
- wo_property 租户不在 center_db 中

**核心问题**：租户编码和数据库名耦合，且依赖全局 center_db

### 13.2 方案C：项目级租户配置映射

每个项目有独立的 `config/tenant-mapping.json`，不再依赖 center_db。

#### 配置文件结构

```json
{
  "project": "wo-property",
  "projectName": "WO物业管理软件",
  "databaseMode": "multi",
  "defaultTenant": "wo_property",
  "database": {
    "type": "mysql",
    "server": "127.0.0.1",
    "port": 3306,
    "username": "root",
    "password": "",
    "charset": "utf8mb4"
  },
  "tenants": {
    "wo_property": { "displayName": "WO物业", "status": "Active" },
    "tenant_a": { "displayName": "阳光物业", "status": "Active" },
    "tenant_b": { "displayName": "绿城物业", "status": "Active" }
  }
}
```

#### databaseMode 两种模式

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| `single` | 所有租户共用一个数据库，用 `tenant_id` 隔离 | 数据量小的项目 |
| `multi` | 每个租户独立数据库 | 大型项目，租户间完全隔离 |

#### 文件位置

```
WO-Property-Management/
  config/
    tenant-mapping.json          ← 租户配置文件
  src/WO.Property.TicketService/
    Tenant/
      TenantConfigLoader.cs     ← 配置加载器
      TenantDbFactory.cs        ← 改造：使用配置加载器
```

### 13.3 TenantConfigLoader 核心逻辑

```csharp
public string BuildConnectionString(string tenantCode)
{
    var config = GetConfig();
    var dbName = GetDatabaseName(tenantCode);  // multi模式：dbName = tenantCode
    
    return $"Server={config.Database.Server};Port={config.Database.Port};Database={dbName};...";
}
```

### 13.4 扩展性优势

| 传统方案（依赖 center_db） | 方案C（项目级配置） |
|--------------------------|------------------|
| 所有项目共享 center_db | 每个项目独立配置 |
| 换数据库名要改 center_db | 只需改项目自己的配置文件 |
| 新项目需要注册 center_db | 新项目直接创建配置文件 |
| 项目间耦合 | 项目间完全解耦 |

### 13.5 当前状态

| 组件 | 状态 | 说明 |
|------|------|------|
| config/tenant-mapping.json | ✅ 已创建 | wo_property / tenant_a / tenant_b |
| TenantConfigLoader.cs | ✅ 已实现 | TicketService 已使用 |
| TenantDbFactory.cs | ✅ 已改造 | 使用 TenantConfigLoader |
| AuthService | ❌ 未改造 | 仍在使用 center_db |
| 前端 LoginView | ✅ 已修复 | 默认 tenantCode = 'wo_property' |

### 13.6 后续工作

- [ ] AuthService 改造支持方案C（不依赖 center_db）
- [ ] 初始化 tenant_a 数据库结构
- [ ] 初始化 tenant_b 数据库结构
- [ ] 前端动态获取租户列表（调用 /api/tenant/list）

---

**最后更新**：2026-05-19 19:13
**更新内容**：新增第十三章「租户配置方案（方案C）」
