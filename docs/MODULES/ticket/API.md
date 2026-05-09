# 工单模块 API 定义

> **版本**：v1.0
> **服务**：TicketService (5002)
> **状态**：已确认

---

## 1. 工单 API 列表

### 1.1 工单列表

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

### 1.2 创建工单

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

### 1.3 派单请求

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

### 1.4 接单

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

### 1.5 拒单

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

### 1.6 更新进度

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

### 1.7 完工申请

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

### 1.8 确认完工

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

### 1.9 评价

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

## 2. API 响应状态码

| 状态码 | 含义 | 适用场景 |
|--------|------|---------|
| 200 OK | 成功 | GET/PUT/PATCH 成功 |
| 201 Created | 已创建 | POST 创建工单成功 |
| 400 Bad Request | 请求错误 | 参数校验失败 |
| 401 Unauthorized | 未认证 | Token 无效或过期 |
| 403 Forbidden | 无权限 | 无权操作该工单 |
| 404 Not Found | 资源不存在 | 工单不存在 |

---

## 3. 错误响应格式

```json
{
  "success": false,
  "message": "错误描述",
  "errors": [
    { "field": "title", "message": "标题不能为空" }
  ],
  "timestamp": "2026-05-05T08:00:00Z"
}
```

---

**文档版本**：v1.0
**作者**：后端工程师
**审核**：软件架构师
**状态**：已确认
