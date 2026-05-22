# 工单模块 API 定义

> **版本**：v1.1
> **服务**：TicketService (5102)
> **状态**：已确认（反映当前实际实现）
> **更新日期**：2026-05-19

---

## 一、API 基础信息

| 项目 | 值 |
|------|---|
| 基础路径 | `http://localhost:5102/api/tenant/tickets` |
| 租户路由 | `X-Tenant: wo_property` (开发环境) |
| 数据格式 | JSON |
| 认证方式 | JWT Bearer Token 或 X-Tenant Header（开发） |

---

## 二、接口列表

### 2.1 工单列表

```
GET /api/tenant/tickets
```

**Query Parameters**:
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| page | int | 1 | 页码 |
| pageSize | int | 20 | 每页条数 |
| status | string | - | 状态筛选（可选） |
| projectId | int | - | 项目ID筛选（可选） |

**Request Headers**:
```
Content-Type: application/json
X-Tenant: wo_property
Authorization: Bearer {token} (生产环境)
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 16,
        "ticketCode": "REPAIR20260500002",
        "title": "测试工单",
        "category": "一般",
        "status": "New",
        "priority": "Medium",
        "creatorPersonId": 0,
        "projectId": 1,
        "ticketType": "REPAIR",
        "createdAt": "2026-05-19T02:54:20.537718Z"
      }
    ],
    "total": 16,
    "page": 1,
    "pageSize": 20
  }
}
```

---

### 2.2 创建工单

```
POST /api/tenant/tickets
```

**Request Body**:
```json
{
  "title": "水龙头漏水",
  "description": "厨房水龙头滴水",
  "category": "一般",
  "ticketTypeId": 1,
  "priority": "Normal",
  "projectId": 1
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 工单标题（可自动以工单类型名称填充） |
| description | string | 否 | 工单描述 |
| category | string | 否 | 默认"一般" |
| ticketTypeId | int | 是 | 工单类型ID（对应 ticket_types 表） |
| priority | string | 否 | urgent/high/normal/low，默认 Normal |
| location | string | 否 | 位置描述 |
| projectId | int | 是 | 默认 1 |
| images | array | 否 | 图片URL列表 |

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 16,
    "ticketCode": "REPAIR20260500002",
    "title": "测试工单",
    "category": "一般",
    "status": "New",
    "priority": "Medium",
    "creatorPersonId": 0,
    "projectId": 1,
    "ticketType": "REPAIR",
    "createdAt": "2026-05-19T02:54:20.537718Z"
  },
  "message": "工单创建成功"
}
```

---

### 2.3 更新工单

```
PUT /api/tenant/tickets/{id}
```

**Request Body**:
```json
{
  "title": "更新后的标题",
  "description": "更新后的描述",
  "status": "Processing",
  "priority": "High",
  "category": "维修"
}
```

**Response**:
```json
{
  "success": true,
  "message": "工单更新成功"
}
```

---

### 2.4 更新工单状态

```
PUT /api/tenant/tickets/{id}/status
```

**Request Body**:
```json
{
  "status": "Processing"
}
```

**Response**:
```json
{
  "success": true,
  "message": "状态已更新"
}
```

---

### 2.5 工单类型列表

```
GET /api/ticket-types
```

**说明**：此接口由 TicketService (5102) 提供，供前端获取工单类型下拉列表。

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "维修",
      "code": "REPAIR",
      "description": "维修工单",
      "status": "Active",
      "sortOrder": 0,
      "jobTypes": [
        { "id": 1, "name": "强弱电", "ticketTypeId": 1 },
        { "id": 2, "name": "空调通风", "ticketTypeId": 1 },
        { "id": 3, "name": "漏水", "ticketTypeId": 1 }
      ]
    }
  ]
}
```

---

## 三、错误响应格式

```json
{
  "success": false,
  "message": "错误描述",
  "errors": [
    { "field": "title", "message": "标题不能为空" }
  ],
  "timestamp": "2026-05-19T08:00:00Z"
}
```

---

## 四、HTTP 状态码

| 状态码 | 含义 | 适用场景 |
|--------|------|---------|
| 200 OK | 成功 | GET/PUT 成功 |
| 201 Created | 已创建 | POST 创建工单成功 |
| 400 Bad Request | 请求错误 | 参数校验失败 |
| 401 Unauthorized | 未认证 | Token 无效或过期 |
| 403 Forbidden | 无权限 | 无权操作该工单 |
| 404 Not Found | 资源不存在 | 工单不存在 |
| 500 Internal Server Error | 服务器错误 | 数据库错误等 |

---

## 五、关联 API

| 服务 | 端点 | 用途 |
|------|------|------|
| MasterDataService | GET /api/hierarchy/areas | 区域数据 |
| MasterDataService | GET /api/hierarchy/area-buildings?areaId=X | 楼栋数据（带房号） |
| MasterDataService | GET /api/departments | 部门数据 |
| MasterDataService | GET /api/job-types | 工种数据 |
| MasterDataService | GET /api/field-definitions/by-module/ticket | 字段定义 |

---

**文档版本**：v1.1
**作者**：软件项目负责人
**审核**：软件架构师
**状态**：已确认（反映 2026-05-19 实际实现）
---

## 六、补充接口（2026-05-19 新增）

### 6.1 拒单

```
PUT /api/tenant/tickets/{id}/reject
```

**说明**：工人拒单时调用。拒单超过3次自动转人工处理。

**Request Body**:
```json
{
  "reason": "无法处理，需要专业人员"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 35,
    "rejectCount": 5,
    "dispatchStatus": "manual",
    "status": "Escalated",
    "escalationLevel": 5
  },
  "message": "拒单成功（5/3），超过3次将自动转人工处理"
}
```

| 字段 | 说明 |
|------|------|
| rejectCount | 当前拒单次数，>3次触发人工 |
| dispatchStatus | 超过3次变为 `manual`（需人工指派） |
| status | 超过3次变为 `Escalated` |
| escalationLevel | 等于 rejectCount |

---

### 6.2 重新指派

```
PUT /api/tenant/tickets/{id}/reassign
```

**说明**：调度员将工单重新指派给其他人员。

**Request Body**:
```json
{
  "personId": 28,
  "reason": "原负责人离职，需换人处理"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 37,
    "oldAssigneeId": 33,
    "newAssigneeId": 28,
    "status": "Dispatched",
    "dispatchStatus": "assigned",
    "escalationLevel": 0
  },
  "message": "重新指派成功（33 → 28）：原负责人离职，需换人处理"
}
```

| 字段 | 说明 |
|------|------|
| oldAssigneeId | 原负责人 ID |
| newAssigneeId | 新负责人 ID |
| escalationLevel | 自动重置为 0 |

---

### 6.3 Overdue 工单列表

```
GET /api/tenant/tickets/overdue
```

**说明**：返回需要调度员关注的超时工单（`dispatchStatus=manual` 或 `escalationLevel>0`）。

**Request Headers**:
```
X-Tenant: wo_property
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 35,
        "ticketCode": "REPAIR20260500024",
        "title": "单元门禁系统故障",
        "status": "Escalated",
        "dispatchStatus": "manual",
        "escalationLevel": 5,
        "rejectCount": 5
      }
    ],
    "total": 2
  }
}
```

---

**文档版本**：v1.1 → v1.2
**更新日期**：2026-05-19
**更新内容**：新增第六章「补充接口」— reject / reassign / overdue API
