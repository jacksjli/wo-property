# 工单服务架构设计文档

**版本：** v1.0  
**日期：** 2026-05-06  
**作者：** 软件项目负责人  
**状态：** 已确认

---

## 一、架构原则

### 1.1 服务职责划分

```
┌─────────────────────────────────────────────────────────────┐
│                        AuthService (5006)                    │
│  职责: 统一认证服务                                           │
│  API:                                                         │
│    - POST /api/auth/register  (用户注册)                     │
│    - POST /api/auth/login     (用户登录，返回JWT)            │
│    - GET  /api/auth/me        (获取当前用户信息)              │
│  限制: 不处理任何工单相关业务                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     TicketService (5002)                     │
│  职责: 工单全生命周期管理                                      │
│  依赖: PersonService (5018) 获取人员信息                      │
│  数据库: MySQL (wo_property.tickets)                         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    DispatchService (5003)                    │
│  职责: 智能派单规则 + 任务执行                                 │
│  API:                                                         │
│    - POST /api/dispatch-rules/match  (匹配派单规则)          │
│    - POST /api/dispatch-tasks        (创建派单任务)            │
│    - POST /api/dispatch-tasks/{id}/accept  (接单)             │
│    - POST /api/dispatch-tasks/{id}/reject  (拒单)            │
│    - POST /api/dispatch-tasks/{id}/complete (完成任务)       │
│  依赖: TicketService (回调通知派单结果)                        │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 服务间通信

```
TicketService  ──HTTP Client──>  DispatchService
     │                               │
     │<──────回调通知───────────────  │
     │                               │
     └──HTTP Client──> PersonService (获取人员信息)
```

---

## 二、工单编号规范

### 2.1 格式定义

```
格式: WO-YYYYMMDD-XXXX
示例: WO-20260506-0001

说明:
  - WO: 前缀，固定
  - YYYYYMMDD: 8位日期
  - XXXX: 4位流水号，每日从0001开始，不足4位补0
```

### 2.2 生成规则

```
1. 同一日期内的工单编号连续递增
2. 每日凌晨 00:00 重置流水号
3. 编号在创建时生成，不允许修改
```

---

## 三、工单状态流转

### 3.1 状态定义

| 状态值 | 名称 | 说明 |
|--------|------|------|
| Created | 已创建 | 工单创建，待派单 |
| Dispatched | 已派单 | 已分配给处理人，待接单 |
| Accepted | 已接单 | 处理人已接单，处理中 |
| Rejected | 已拒单 | 处理人拒绝，需重新派单 |
| InProgress | 处理中 | 处理人正在处理 |
| Finished | 已完工 | 处理人申请完工，待确认 |
| Confirmed | 已确认 | 业主/管理员确认完工 |
| Closed | 已关闭 | 工单结束 |

### 3.2 状态流转图

```
                         ┌──────────────┐
                         │   Created    │
                         └──────┬───────┘
                                │
                         dispatch (派单请求)
                                │
                    ┌───────────▼───────────┐
                    │    Dispatched         │
                    └───────────┬───────────┘
                                │
                    ┌───────────┼───────────┐
                    │           │           │
               accept      reject      timeout
                    │           │           │
                    ▼           ▼           │
             ┌──────────┐   ┌──────────┐   │
             │ Accepted │   │ Rejected │◀──┘
             └────┬─────┘   └──────────┘   (返回Created)
                  │
            update progress
                  │
                  ▼
             ┌──────────┐
             │InProgress │
             └────┬─────┘
                  │
             finish (完工申请)
                  │
                  ▼
             ┌──────────┐
             │ Finished │
             └────┬─────┘
                  │
            confirm (确认)
                  │
                  ▼
             ┌──────────┐
             │Confirmed │
             └────┬─────┘
                  │
               rate (评价)
                  │
                  ▼
             ┌──────────┐
             │  Closed  │
             └──────────┘
```

### 3.3 状态流转 API

| 方法 | 路径 | 触发者 | 说明 |
|------|------|--------|------|
| POST | /api/tickets | 用户 | 创建工单，状态=Created |
| POST | /api/tickets/{id}/dispatch | TicketService→DispatchService | 派单请求，状态→Dispatched |
| POST | /api/tickets/{id}/accept | 处理人 | 接单，状态→Accepted |
| POST | /api/tickets/{id}/reject | 处理人 | 拒单，状态→Rejected |
| PUT | /api/tickets/{id}/progress | 处理人 | 更新进度，状态→InProgress |
| POST | /api/tickets/{id}/finish | 处理人 | 完工申请，状态→Finished |
| POST | /api/tickets/{id}/confirm | 业主/管理员 | 确认完工，状态→Confirmed |
| POST | /api/tickets/{id}/rate | 业主 | 评价(1-5星)，状态→Closed |

---

## 四、数据模型

### 4.1 工单主表 (Tickets)

```sql
CREATE TABLE tickets (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_no VARCHAR(20) NOT NULL UNIQUE COMMENT '工单编号 WO-YYYYMMDD-XXXX',
    title VARCHAR(200) NOT NULL COMMENT '工单标题',
    description TEXT COMMENT '工单描述',
    category VARCHAR(50) COMMENT '工单分类',
    priority INT DEFAULT 2 COMMENT '优先级 1=紧急 2=高 3=中 4=低',
    status VARCHAR(20) NOT NULL DEFAULT 'Created' COMMENT '状态',
    creator_id BIGINT NOT NULL COMMENT '创建人ID',
    assignee_id BIGINT COMMENT '处理人ID',
    location VARCHAR(500) COMMENT '位置',
    images JSON COMMENT '图片附件列表',
    rating INT COMMENT '评价 1-5',
    tenant_id BIGINT COMMENT '租户ID',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_ticket_no (ticket_no),
    INDEX idx_status (status),
    INDEX idx_creator (creator_id),
    INDEX idx_assignee (assignee_id),
    INDEX idx_tenant (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 4.2 工单处理记录表 (TicketProcessRecords)

```sql
CREATE TABLE ticket_process_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ticket_id BIGINT NOT NULL,
    action VARCHAR(50) NOT NULL COMMENT '动作: dispatch/accept/reject/progress/finish/confirm/rate',
    operator_id BIGINT NOT NULL COMMENT '操作人ID',
    content TEXT COMMENT '操作说明/备注',
    from_status VARCHAR(20) COMMENT '操作前状态',
    to_status VARCHAR(20) NOT NULL COMMENT '操作后状态',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ticket_id) REFERENCES tickets(id),
    INDEX idx_ticket (ticket_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 4.3 关联表关系

```
Tickets (1) ──< TicketProcessRecords (N)
     │
     └──< TicketComments (N) [预留]
     └──< TicketImages (N) [预留]
```

---

## 五、API 详细定义

### 5.1 工单列表

```
GET /api/tickets
Authorization: Bearer {token}

Query Parameters:
  - status: string (可选)
  - priority: int (可选)
  - startDate: date (可选)
  - endDate: date (可选)
  - page: int (默认1)
  - pageSize: int (默认20)

Response:
{
  "success": true,
  "data": {
    "items": [...],
    "total": 100,
    "page": 1,
    "pageSize": 20
  }
}
```

### 5.2 创建工单

```
POST /api/tickets
Authorization: Bearer {token}

Request:
{
  "title": "string (必填)",
  "description": "string",
  "category": "string",
  "priority": 1-4,
  "location": "string",
  "images": ["url1", "url2"]
}

Response:
{
  "success": true,
  "data": {
    "id": 1,
    "ticketNo": "WO-20260506-0001",
    "status": "Created"
  }
}
```

### 5.3 派单请求

```
POST /api/tickets/{id}/dispatch
Authorization: Bearer {token}

Request:
{
  "ruleId": "long (可选，指定派单规则)",
  "assigneeId": "long (可选，指定处理人)"
}

说明:
  - 只在 TicketService 内部调用 DispatchService
  - 不对外暴露
```

### 5.4 接单

```
POST /api/tickets/{id}/accept
Authorization: Bearer {token}

Request: {}

Response:
{
  "success": true,
  "message": "已接单",
  "data": {
    "status": "Accepted",
    "acceptedAt": "2026-05-06T08:30:00Z"
  }
}
```

### 5.5 拒单

```
POST /api/tickets/{id}/reject
Authorization: Bearer {token}

Request:
{
  "reason": "string (必填)"
}

Response:
{
  "success": true,
  "message": "已拒单，请等待重新派单",
  "data": {
    "status": "Rejected"
  }
}
```

### 5.6 更新进度

```
PUT /api/tickets/{id}/progress
Authorization: Bearer {token}

Request:
{
  "content": "string (必填，说明处理进度)"
}

Response:
{
  "success": true,
  "data": {
    "status": "InProgress",
    "lastProgress": "已完成管道维修"
  }
}
```

### 5.7 完工申请

```
POST /api/tickets/{id}/finish
Authorization: Bearer {token}

Request:
{
  "finishNote": "string (可选，完成说明)"
}

Response:
{
  "success": true,
  "message": "已提交完工申请，请等待确认",
  "data": {
    "status": "Finished"
  }
}
```

### 5.8 确认完工

```
POST /api/tickets/{id}/confirm
Authorization: Bearer {token}

Request: {}

Response:
{
  "success": true,
  "message": "已确认完工",
  "data": {
    "status": "Confirmed"
  }
}
```

### 5.9 评价

```
POST /api/tickets/{id}/rate
Authorization: Bearer {token}

Request:
{
  "rating": 1-5,
  "comment": "string (可选)"
}

Response:
{
  "success": true,
  "message": "评价成功",
  "data": {
    "status": "Closed",
    "rating": 5
  }
}
```

---

## 六、时序图

### 6.1 完整工单生命周期

```
用户/业主              TicketService        DispatchService        处理人
   │                       │                      │                   │
   │──── 创建工单 ─────────>│                      │                   │
   │                       │                      │                   │
   │                       │──── 派单请求 ─────────────────────────────>
   │                       │                      │                   │
   │                       │                      │── 匹配规则 ───────>
   │                       │                      │<── 返回处理人 ────
   │                       │<── 通知: 已分配 ──────                   │
   │                       │                      │                   │
   │<──────── 通知: 已派单 ──                       │                   │
   │                       │                      │                   │
   │                       │                      │<────── 接单 ──────
   │                       │<── 通知: 已接单 ──────                   │
   │<──────── 通知: 已接单 ──                       │                   │
   │                       │                      │                   │
   │                       │                      │<──── 更新进度 ────
   │                       │                      │                   │
   │                       │                      │<──── 完工申请 ────
   │                       │<── 通知: 申请完工 ────                   │
   │<──────── 通知: 完工待确认 ──                    │                   │
   │                       │                      │                   │
   │──── 确认完工 ─────────>│                      │                   │
   │                       │                      │                   │
   │<──── 通知: 已确认 ──────                       │                   │
   │                       │                      │                   │
   │──── 评价(5星) ────────>│                      │                   │
   │                       │                      │                   │
   │<──── 工单关闭 ─────────                       │                   │
```

---

## 七、实施计划

### Phase 1: AuthService 精简
- [ ] 移除 AuthService 中的 /api/tickets 相关代码
- [ ] 验证认证功能正常

### Phase 2: TicketService 工单状态流转
- [ ] 增加 8 个状态流转 API
- [ ] 修改工单编号生成逻辑
- [ ] 实现 HTTP Client 调用 DispatchService
- [ ] 对接 PersonService 获取人员信息

### Phase 3: 数据库迁移
- [ ] 创建 tickets 表
- [ ] 创建 ticket_process_records 表
- [ ] 迁移现有数据

### Phase 4: 联调测试
- [ ] 完整流程测试
- [ ] 前后端联调验证

---

## 八、决策记录

| 日期 | 决策 | 理由 |
|------|------|------|
| 2026-05-06 | AuthService 专注认证，移除工单代码 | 职责单一，避免重复 |
| 2026-05-06 | 工单派单由 TicketService 调用 DispatchService | 解耦，DispatchService 专注派单规则 |
| 2026-05-06 | 工单编号格式: WO-YYYYMMDD-XXXX | 规范化，便于检索和统计 |
| 2026-05-06 | 使用 MySQL (wo_property) 统一存储 | 架构决策，所有服务统一数据库 |