# 工单超时自动升级系统设计文档 v1.0

> 创建时间：2026-05-28
> 版本：v1.0
> 状态：已实施

---

## 1. 背景与目标

### 1.1 问题描述

原系统工单处理存在以下问题：
1. **超时处理不灵活**：所有角色使用固定超时时间（30分钟/24小时/72小时）
2. **升级流程缺失**：超时后只知道通知当前处理人，无法升级给更高级别
3. **工作负载不均衡**：派单时不考虑人员当前工单负载

### 1.2 设计目标

1. **超时配置灵活化**：根据角色从 `timeout_rules` 表动态读取超时时间
2. **升级链路自动化**：超时后自动按路径升级（operator → supervisor → manager → department_head → company_head）
3. **派单均衡化**：优先派给工作负载最低的人员
4. **前端展示同步**：管理端和小程序同时显示升级状态

---

## 2. 系统架构

### 2.1 角色定义

| 角色 | 说明 | 升级到 |
|------|------|--------|
| operator | 操作人员/工程师 | supervisor |
| supervisor | 主管 | manager |
| manager | 经理 | department_head |
| department_head | 部门负责人 | company_head |
| company_head | 公司负责人 | 终止 |

### 2.2 数据库表结构

#### timeout_rules（超时规则表）

```sql
CREATE TABLE timeout_rules (
  id INT PRIMARY KEY AUTO_INCREMENT,
  color VARCHAR(20) NOT NULL,        -- 规则颜色：green/blue/orange/red
  role VARCHAR(50) NOT NULL,          -- 角色：operator/supervisor/manager/department_head/company_head
  hours INT DEFAULT 24,              -- 超时小时数
  enabled TINYINT(1) DEFAULT 1,       -- 是否启用
  created_at DATETIME,
  updated_at DATETIME
);
```

**默认配置**：

| color | role | hours | 说明 |
|-------|------|-------|------|
| green | operator | 24 | 绿色工单，操作人员24小时 |
| green | supervisor | 24 | 绿色工单，主管24小时 |
| orange | operator | 6 | 橙色工单，操作人员6小时 |
| orange | supervisor | 6 | 橙色工单，主管6小时 |
| red | operator | 2 | 红色工单，操作人员2小时 |
| red | supervisor | 2 | 红色工单，主管2小时 |

#### dispatch_records（派单记录表）新增字段

```sql
ALTER TABLE dispatch_records ADD COLUMN parent_dispatch_id BIGINT NULL;
ALTER TABLE dispatch_records ADD COLUMN escalation_level VARCHAR(20) DEFAULT '';
```

| 字段 | 说明 |
|------|------|
| parent_dispatch_id | 母派单ID（升级派单时关联原派单） |
| escalation_level | 升级级别（L1/L2/L3/L4） |

#### timeout_escalations（升级记录表）

```sql
CREATE TABLE timeout_escalations (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  ticket_id BIGINT NOT NULL,
  ticket_code VARCHAR(50) NOT NULL,
  dispatch_record_id BIGINT NOT NULL,
  from_person_id INT NOT NULL,
  from_person_name VARCHAR(50),
  to_person_id INT NOT NULL,
  to_person_name VARCHAR(50),
  to_role VARCHAR(50) NOT NULL,
  level INT NOT NULL DEFAULT 1,
  escalated_at DATETIME NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'Pending',
  tenant_code VARCHAR(50) NOT NULL,
  project_id INT NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_ticket_id (ticket_id),
  INDEX idx_status (status)
);
```

---

## 3. 升级流程

### 3.1 超时检测流程

```
DispatchTimeoutMonitor（每分钟执行）
    ↓
查询所有 Pending 状态的派单记录
    ↓
获取派单人角色 + 工单颜色
    ↓
从 timeout_rules 表读取对应超时时间
    ↓
判断是否超时（DispatchTime + hours < Now）
    ↓（超时）
检查是否已存在该派单的超时告警（避免重复）
    ↓（不存在）
触发升级流程
```

### 3.2 升级执行流程

```
升级触发
    ↓
1. 更新原派单记录：status = 'Escalated'
    ↓
2. 创建新派单记录：
   - status = 'Pending'
   - source = 'Escalation'
   - parent_dispatch_id = 原派单ID
   - escalation_level = 'L{n}'
    ↓
3. 记录升级信息到 timeout_escalations 表
    ↓
4. 记录超时告警到 timeout_alerts 表
    ↓
5. 调用 NotificationService 发送升级通知
    ↓
6. 发送微信消息/短信给新派单人
```

### 3.3 升级路径

```
operator 超时（2/6/24小时）
    ↓
升级给 supervisor
    ↓
supervisor 超时
    ↓
升级给 manager
    ↓
manager 超时
    ↓
升级给 department_head
    ↓
department_head 超时
    ↓
升级给 company_head
    ↓
company_head 超时
    ↓
终止（无法继续升级，记录告警）
```

---

## 4. API 设计

### 4.1 获取工单升级状态

**接口**：`GET /api/tenant/dispatch/escalations/{ticketId}`

**响应**：
```json
{
  "success": true,
  "data": {
    "currentLevel": 2,
    "currentStatus": "Pending",
    "nextEscalationRole": "manager",
    "escalations": [
      {
        "id": 1,
        "level": 1,
        "fromPersonId": 29,
        "fromPersonName": "刘保洁",
        "toPersonId": 26,
        "toPersonName": "张工程",
        "toRole": "supervisor",
        "escalatedAt": "2026-05-28T12:00:00",
        "status": "Pending"
      }
    ],
    "dispatchRecords": [
      {
        "id": 1,
        "toPersonId": 29,
        "toPersonName": "刘保洁",
        "status": "Escalated",
        "source": "Auto",
        "escalationLevel": "",
        "parentDispatchId": null,
        "dispatchTime": "2026-05-28T10:00:00"
      },
      {
        "id": 2,
        "toPersonId": 26,
        "toPersonName": "张工程",
        "status": "Pending",
        "source": "Escalation",
        "escalationLevel": "L1",
        "parentDispatchId": 1,
        "dispatchTime": "2026-05-28T12:00:00"
      }
    ]
  }
}
```

### 4.2 字段说明

| 字段 | 说明 |
|------|------|
| currentLevel | 当前升级级别（0=初始派单，1=L1，2=L2...） |
| currentStatus | 当前升级状态（Active/Pending/Resolved） |
| nextEscalationRole | 下一升级目标角色（null=已达最高级别） |
| escalations | 升级历史记录列表 |
| dispatchRecords | 该工单所有派单记录（含升级派单） |

---

## 5. 前端展示

### 5.1 管理端（admin-portal）

#### 派单列表（DispatchList.vue）

| 列 | 显示内容 |
|----|----------|
| 来源 | 升级派单显示橙色「升级」标签 |
| 级别 | 显示 L1/L2/L3/L4 标签 |
| 状态 | 新增「已升级」状态（红色） |

#### 工单详情（TicketDetailView.vue）

- 新增「升级状态」卡片（红色边框）
- 显示当前级别 + 状态
- 显示下一升级目标角色
- 升级历史时间线（el-timeline）

### 5.2 微信小程序

#### 工程师工单列表（ticket/list.vue）

- 来源列：升级派单显示橙色「升级」标签
- 新增级别列：L1/L2（橙色）、L3/L4（红色）

#### 工程师工单详情（ticket/detail.vue）

- 新增「升级状态」区块
- 显示当前级别标签
- 显示升级历史

---

## 6. 修改文件清单

### 后端（DispatchService）

| 文件 | 修改内容 |
|------|----------|
| `Models/DispatchModels.cs` | 新增 TimeoutRule、TimeoutEscalation、ParentDispatchId、EscalationLevel |
| `Data/TenantDbContext.cs` | 新增 DbSet 和表映射 |
| `Services/DispatchTimeoutMonitor.cs` | 重写超时检测和升级逻辑 |
| `Controllers/TenantDispatchController.cs` | 新增 GET /escalations/{ticketId} |

### 管理端前端

| 文件 | 修改内容 |
|------|----------|
| `api/dispatch.ts` | 新增 EscalationRecord 类型 + getEscalations API |
| `views/dispatch/DispatchList.vue` | 显示升级标识、级别、状态 |
| `views/ticket/TicketDetailView.vue` | 新增升级状态卡片和时间线 |

### 微信小程序

| 文件 | 修改内容 |
|------|----------|
| `api/dispatch.js` | 新增 getEscalations API |
| `pages/ticket/list.vue` | 显示升级标识和级别 |
| `pages/ticket/detail.vue` | 新增升级状态卡片和时间线 |

---

## 7. 相关文档

- [TICKET_SERVICE_ARCHITECTURE_v1.0.md](./TICKET_SERVICE_ARCHITECTURE_v1.0.md) - 工单服务架构
- [API_FIELD_NAMING_STANDARD_v1.0.md](./API_FIELD_NAMING_STANDARD_v1.0.md) - API 字段命名规范

---

## 8. 更新记录

| 日期 | 版本 | 更新内容 |
|------|------|----------|
| 2026-05-28 | v1.0 | 初始版本，实现超时自动升级功能 |