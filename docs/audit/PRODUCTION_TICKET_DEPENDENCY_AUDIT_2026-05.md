# 工单模块 v3.0 依赖模块审查报告

**日期：** 2026-05-19
**版本：** v1.0
**状态：** 已确认

---

## 一、审查结论总览

| 依赖模块 | 支撑度 | 关键缺口 | 优先级 |
|---------|--------|---------|--------|
| DispatchService (5003) | **0%** | **空壳，无任何 API** | **P0** |
| TicketService (5102) | 40% | 缺8个表字段 + 派单触发逻辑 | P0 |
| PersonService (5018) | 70% | 缺 area_ids / is_supervisor / max_concurrent | P1 |
| NotificationService (5005) | 60% | 缺满意度调查通知 | P1 |
| MasterDataService (5019) | 100% | — | — |

---

## 二、PersonService (5018) 详细审查

### 2.1 当前 Personnel 表字段

| 字段 | 状态 | 说明 |
|------|------|------|
| `specialty_ids` (JSON) | ✅ 存在 | 工种ID列表，已支持 |
| `ticket_type_ids` (JSON) | ✅ 存在 | 工单类型关联，已支持 |
| `DepartmentId` | ✅ 存在 | 部门ID，已有 |
| `Role` (varchar) | ✅ 存在 | operator/supervisor/manager...，已有 |
| `Status` | ✅ 存在 | active/inactive，已有 |
| `area_ids` (JSON) | ❌ **缺失** | 负责区域列表，v3.0 需要 |
| `is_supervisor` | ❌ **缺失** | 显式主管标识，v3.0 需要 |
| `max_concurrent_tickets` | ❌ **缺失** | 最大同时处理工单数，v3.0 需要 |

### 2.2 修复 SQL

```sql
ALTER TABLE Personnel ADD COLUMN area_ids JSON COMMENT '负责区域ID列表 [1,2,3]';
ALTER TABLE Personnel ADD COLUMN is_supervisor TINYINT(1) DEFAULT 0 COMMENT '是否为主管';
ALTER TABLE Personnel ADD COLUMN max_concurrent_tickets INT DEFAULT 5 COMMENT '最大同时处理工单数';
```

### 2.3 修复后端模型

需同步修改：
- `Person.cs` 模型新增 3 个属性
- `PersonDtos.cs` 新增字段 DTO
- `PersonService.cs` 更新 GetPerson 接口返回新增字段

---

## 三、TicketService (5102) 详细审查

### 3.1 当前 Tickets 表字段 vs 设计要求

| 字段 | 设计要求 | 当前状态 | 缺口 |
|------|---------|---------|------|
| `tenant_id` | 显式 BIGINT | ❌ 缺失 | P0 |
| `category` | property/hr/finance | ❌ 缺失，默认 property | P0 |
| `color` | green/blue/orange/red | ❌ 缺失 | P1 |
| `current_role` | operator/supervisor/... | ❌ 缺失 | P1 |
| `escalation_level` | 已升级次数 | ❌ 缺失 | P1 |
| `dispatch_status` | pending/assigned/manual | ❌ 缺失 | P0 |
| `reject_count` | 拒单次数 | ❌ 缺失 | P1 |
| `survey_status` | pending/sent/surveyed | ❌ 缺失 | P0 |
| `custom_fields` (JSON) | 人事/财务扩展 | ❌ 缺失 | P2 |
| `location_detail` | 详细地址 | ❌ 缺失 | P0 |
| `area_id/building_id/room_id` | 位置三级 | ❌ 前端存但未写表 | P0 |
| `ticket_type_id` | 工单类型外键 | ✅ 存在（传但未写表） | 已修复 |

### 3.2 修复 SQL

```sql
ALTER TABLE Tickets ADD COLUMN tenant_id BIGINT NOT NULL DEFAULT 1 COMMENT '租户ID';
ALTER TABLE Tickets ADD COLUMN category VARCHAR(20) NOT NULL DEFAULT 'property' COMMENT 'property/hr/finance';
ALTER TABLE Tickets ADD COLUMN color VARCHAR(10) DEFAULT 'blue' COMMENT 'green/blue/orange/red';
ALTER TABLE Tickets ADD COLUMN current_role VARCHAR(20) DEFAULT 'operator';
ALTER TABLE Tickets ADD COLUMN escalation_level INT DEFAULT 0;
ALTER TABLE Tickets ADD COLUMN dispatch_status VARCHAR(20) DEFAULT 'pending' COMMENT 'pending/assigned/manual';
ALTER TABLE Tickets ADD COLUMN reject_count INT DEFAULT 0;
ALTER TABLE Tickets ADD COLUMN survey_status VARCHAR(20) DEFAULT 'pending' COMMENT 'pending/sent/surveyed/closed_without_survey';
ALTER TABLE Tickets ADD COLUMN location_detail VARCHAR(500);
ALTER TABLE Tickets ADD COLUMN area_id BIGINT COMMENT '区域ID';
ALTER TABLE Tickets ADD COLUMN building_id BIGINT COMMENT '楼栋ID';
ALTER TABLE Tickets ADD COLUMN room_id BIGINT COMMENT '房号ID';

-- 新增关联表
CREATE TABLE ticket_dispatch_mapping (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id BIGINT NOT NULL,
    job_type_id BIGINT NOT NULL,
    assignee_id BIGINT NOT NULL,
    dispatch_type VARCHAR(20) DEFAULT 'auto',
    dispatch_status VARCHAR(20) DEFAULT 'waiting',
    reject_reason VARCHAR(200),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    accepted_at DATETIME,
    rejected_at DATETIME,
    finished_at DATETIME,
    UNIQUE KEY uk_ticket_jobtype_assignee (ticket_id, job_type_id, assignee_id),
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE dispatch_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id BIGINT NOT NULL,
    assignee_id BIGINT NOT NULL,
    dispatcher_type VARCHAR(20) NOT NULL,
    rule_id BIGINT,
    reason VARCHAR(200),
    status VARCHAR(20) DEFAULT 'waiting',
    reject_reason VARCHAR(200),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE satisfaction_surveys (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id BIGINT NOT NULL UNIQUE,
    rating INT COMMENT '1-5星',
    comment VARCHAR(500),
    respondent_name VARCHAR(50),
    submitted_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 四、DispatchService (5003) 详细审查

### 4.1 当前状态

```
DispatchService/
├── Program.cs ✅（端口5003，JWT配置）
├── property_dispatch.db ✅（SQLite）
├── dispatch.log ✅（有日志）
└── Controllers/ ❌【空壳 — 无任何 Controller】
```

### 4.2 需要开发的 API

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/dispatch/auto-assign` | 自动派单 |
| POST | `/api/dispatch/match` | 匹配派单规则 |
| PUT | `/api/dispatch/{id}/accept` | 接单 |
| PUT | `/api/dispatch/{id}/reject` | 拒单 |
| GET | `/api/dispatch/tickets/{id}/status` | 派单状态 |

### 4.3 需要实现的类

- `DispatcherRegistry` — 规则注册中心
- `PropertyDispatcher` — 物业工单派单器
- `DispatchRulesController` — API 控制器

---

## 五、NotificationService (5005) 详细审查

### 5.1 当前 API

| API | 状态 | 说明 |
|-----|------|------|
| `POST /api/tenant/notification/notifications` | ✅ 正常 | 发送通知 |
| 满意度调查触发 | ❌ 缺失 | 没有专门的 survey 发送接口 |

### 5.2 需要新增

- `POST /api/tenant/notification/survey-invite` — 发送满意度调查邀请

---

## 六、修复优先级总览

| 优先级 | 模块 | 行动 | 工作量 |
|--------|------|------|--------|
| **P0** | DispatchService | 全新开发：DispatcherRegistry + PropertyDispatcher + auto-assign API | 2-3天 |
| **P0** | TicketService | 新增8个表字段 + 派单触发调用 + 工单创建触发派单 | 0.5天 |
| **P0** | PersonService | 新增3个字段（area_ids / is_supervisor / max_concurrent） | 0.5天 |
| **P1** | NotificationService | 新增满意度调查发送接口 | 0.5天 |
| **P2** | TicketService | custom_fields JSON 扩展字段 | 待定 |
| **P2** | — | HRDispatcher / FinanceDispatcher 扩展 | 待定 |

---

**文档版本：** v1.0
**作者：** 软件项目负责人
**审核：** 软件架构师
**状态：** 已确认
**最后更新：** 2026-05-19