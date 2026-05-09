# WO物业管理软件 - API接口文档

## 概述

本文档描述WO物业管理软件各微服务的RESTful API接口。

**基础URL**: `http://localhost:{端口}/api`

**认证方式**: JWT Bearer Token

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**通用响应格式**:

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "total": 100,
  "page": 1,
  "pageSize": 20
}
```

**错误响应**:

```json
{
  "success": false,
  "message": "错误信息",
  "error": "详细错误"
}
```

---

## 1. 认证服务 (端口: 5006)

### 1.1 用户登录
```
POST /api/auth/login
Content-Type: application/json

Request:
{
  "username": "admin",
  "password": "Admin@123"
}

Response:
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "user": {
      "id": 1,
      "username": "admin",
      "name": "管理员",
      "role": "Administrator"
    }
  }
}
```

### 1.2 获取用户列表
```
GET /api/auth/users
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": [
    { "id": 1, "username": "admin", "name": "管理员", "role": "Administrator" },
    { "id": 2, "username": "tech", "name": "技术人员", "role": "Technician" }
  ]
}
```

### 1.3 健康检查
```
GET /health

Response:
{
  "status": "healthy",
  "service": "AuthService",
  "timestamp": "2026-04-21T01:00:00Z"
}
```

---

## 2. 物料管理服务 (端口: 5004)

### 2.1 获取物料列表
```
GET /api/materials?page=1&pageSize=20&keyword=灯泡

Response:
{
  "success": true,
  "total": 50,
  "page": 1,
  "pageSize": 20,
  "data": [
    {
      "id": 1,
      "materialNumber": "MAT-001",
      "name": "LED灯泡",
      "category": "照明设备",
      "specification": "9W E27",
      "unit": "个",
      "quantity": 100,
      "unitPrice": 15.00,
      "status": "InStock"
    }
  ]
}
```

### 2.2 获取单个物料
```
GET /api/materials/{id}

Response:
{
  "success": true,
  "data": { ... }
}
```

### 2.3 创建物料
```
POST /api/materials
Authorization: Bearer {token}

Request:
{
  "name": "新物料",
  "category": "分类",
  "specification": "规格",
  "unit": "个",
  "quantity": 100,
  "unitPrice": 50.00
}

Response:
{
  "success": true,
  "message": "物料创建成功",
  "data": { ... }
}
```

### 2.4 更新物料
```
PUT /api/materials/{id}

Request:
{
  "name": "更新后的名称",
  "quantity": 80
}
```

### 2.5 删除物料
```
DELETE /api/materials/{id}

Response:
{
  "success": true,
  "message": "物料已删除"
}
```

### 2.6 物料统计
```
GET /api/materials/stats

Response:
{
  "success": true,
  "data": {
    "totalMaterials": 100,
    "totalValue": 50000.00,
    "lowStockItems": 5,
    "outOfStockItems": 2
  }
}
```

---

## 3. 通知服务 (端口: 5005)

### 3.1 获取通知列表
```
GET /api/notifications?page=1&pageSize=20

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "title": "系统维护通知",
      "content": "将于今晚10点进行系统维护...",
      "type": "System",
      "priority": "High",
      "createdAt": "2026-04-20T10:00:00Z"
    }
  ]
}
```

### 3.2 发送通知
```
POST /api/notifications
Authorization: Bearer {token}

Request:
{
  "title": "新通知",
  "content": "通知内容",
  "type": "System",
  "priority": "Normal"
}
```

### 3.3 获取公告
```
GET /api/announcements

Response:
{
  "success": true,
  "data": [ ... ]
}
```

---

## 4. 合同管理服务 (端口: 5008)

### 4.1 获取合同列表
```
GET /api/contracts?page=1&pageSize=20&status=Active

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "contractNumber": "CON-2026-0001",
      "title": "物业服务合同",
      "type": "PropertyService",
      "partyA": "物业管理公司",
      "partyB": "业主委员会",
      "signDate": "2026-01-01",
      "startDate": "2026-01-01",
      "endDate": "2026-12-31",
      "amount": 500000.00,
      "status": "Active"
    }
  ]
}
```

### 4.2 获取合同详情
```
GET /api/contracts/{id}

Response:
{
  "success": true,
  "data": {
    "contract": { ... },
    "payments": [ ... ]
  }
}
```

### 4.3 创建合同
```
POST /api/contracts
Authorization: Bearer {token}

Request:
{
  "title": "新合同",
  "type": "PropertyService",
  "partyA": "甲方",
  "partyB": "乙方",
  "signDate": "2026-04-21",
  "startDate": "2026-05-01",
  "endDate": "2027-04-30",
  "amount": 100000.00
}
```

### 4.4 合同统计
```
GET /api/contracts/stats

Response:
{
  "success": true,
  "data": {
    "total": 20,
    "active": 15,
    "expiringSoon": 3,
    "expired": 2,
    "totalAmount": 5000000.00
  }
}
```

---

## 5. 财务管理服务 (端口: 5009)

### 5.1 获取账单列表
```
GET /api/bills?page=1&pageSize=20&status=Pending

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "billNumber": "BILL-2026-0001",
      "title": "2026年4月物业费",
      "amount": 500.00,
      "dueDate": "2026-04-30",
      "status": "Pending",
      "propertyUnit": "A栋101"
    }
  ]
}
```

### 5.2 账单统计
```
GET /api/bills/stats

Response:
{
  "success": true,
  "data": {
    "totalBills": 100,
    "pendingAmount": 50000.00,
    "paidAmount": 200000.00,
    "overdueAmount": 5000.00,
    "collectionRate": 95.5
  }
}
```

### 5.3 付款记录
```
GET /api/payments?page=1&pageSize=20

Response:
{
  "success": true,
  "data": [ ... ]
}
```

---

## 6. 巡检管理服务 (端口: 5010)

### 6.1 获取巡检计划
```
GET /api/plans?page=1&pageSize=20

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "planNumber": "PLAN-2026-001",
      "title": "月度消防巡检",
      "type": "FireSafety",
      "frequency": "Monthly",
      "startDate": "2026-04-01",
      "endDate": "2026-04-30",
      "status": "Active"
    }
  ]
}
```

### 6.2 获取巡检任务
```
GET /api/tasks?status=Pending

Response:
{
  "success": true,
  "data": [ ... ]
}
```

### 6.3 执行巡检
```
POST /api/records
Authorization: Bearer {token}

Request:
{
  "taskId": 1,
  "location": "A栋1楼",
  "result": "Pass",
  "remarks": "一切正常",
  "issues": []
}
```

### 6.4 巡检统计
```
GET /api/plans/stats

Response:
{
  "success": true,
  "data": {
    "totalPlans": 10,
    "activePlans": 8,
    "totalTasks": 50,
    "completedTasks": 45,
    "issuesFound": 5
  }
}
```

---

## 7. 投诉建议服务 (端口: 5011)

### 7.1 获取投诉列表
```
GET /api/complaints?type=Complaint&status=Submitted&page=1&pageSize=20

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "complaintNumber": "CMP-2026-0001",
      "title": "电梯故障",
      "type": "Complaint",
      "priority": "High",
      "status": "Processing",
      "submitterName": "王先生",
      "createdAt": "2026-04-20T10:00:00Z"
    }
  ]
}
```

### 7.2 提交投诉
```
POST /api/complaints
Authorization: Bearer {token}

Request:
{
  "title": "投诉标题",
  "content": "投诉内容",
  "type": "Complaint",
  "priority": "High",
  "submitterName": "张三",
  "submitterPhone": "138-0000-0001",
  "roomNumber": "A栋101"
}
```

### 7.3 处理投诉
```
PUT /api/complaints/{id}/process

Response:
{
  "success": true,
  "message": "处理中"
}
```

### 7.4 解决投诉
```
PUT /api/complaints/{id}/resolve
Authorization: Bearer {token}

Request:
{
  "remarks": "已维修完成",
  "rating": 5
}
```

### 7.5 投诉统计
```
GET /api/complaints/stats

Response:
{
  "success": true,
  "data": {
    "total": 50,
    "submitted": 5,
    "processing": 10,
    "resolved": 30,
    "todaySubmitted": 3,
    "avgRating": 4.5
  }
}
```

---

## 8. 钥匙管理服务 (端口: 5012)

### 8.1 获取钥匙列表
```
GET /api/keys?status=Available

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "keyNumber": "KEY-A101",
      "name": "A栋101办公室钥匙",
      "location": "A栋101办公室",
      "status": "Available",
      "totalCopies": 2,
      "availableCopies": 2
    }
  ]
}
```

### 8.2 借用钥匙
```
POST /api/borrows
Authorization: Bearer {token}

Request:
{
  "keyId": 1,
  "borrowerName": "李四",
  "borrowerPhone": "139-0000-0002",
  "borrowDate": "2026-04-21",
  "expectedReturnDate": "2026-04-22",
  "purpose": "取文件"
}
```

### 8.3 批准借用
```
PUT /api/borrows/{id}/approve
Authorization: Bearer {token}

Request:
{
  "approver": "张物业",
  "remarks": "同意借用"
}
```

### 8.4 归还钥匙
```
PUT /api/borrows/{id}/return
Authorization: Bearer {token}

Request:
{
  "receiver": "张物业",
  "conditionOk": true,
  "remarks": "钥匙完好"
}
```

### 8.5 钥匙统计
```
GET /api/keys/stats

Response:
{
  "success": true,
  "data": {
    "totalKeys": 50,
    "availableKeys": 45,
    "borrowedKeys": 5,
    "activeBorrows": 3
  }
}
```

---

## 9. 访客管理服务 (端口: 5013)

### 9.1 获取访客列表
```
GET /api/visitors?status=Approved&page=1&pageSize=20

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "visitorNumber": "VIS-2026-0001",
      "visitorName": "张先生",
      "visitorPhone": "138-0001-0001",
      "type": "Business",
      "hostName": "王经理",
      "visitLocation": "A栋2楼会议室",
      "scheduledDate": "2026-04-21",
      "status": "Approved",
      "accessCode": "V20260001"
    }
  ]
}
```

### 9.2 创建访客预约
```
POST /api/visitors
Authorization: Bearer {token}

Request:
{
  "visitorName": "访客姓名",
  "visitorPhone": "138-0000-0001",
  "type": "Business",
  "hostName": "被访问人",
  "visitLocation": "访问地点",
  "scheduledDate": "2026-04-21",
  "purpose": "商务洽谈"
}
```

### 9.3 访客签到
```
PUT /api/visitors/{id}/checkin
Authorization: Bearer {token}

Request:
{
  "gateDevice": "大门1号机",
  "temperature": 36.5
}
```

### 9.4 访客签离
```
PUT /api/visitors/{id}/checkout
Authorization: Bearer {token}
```

### 9.5 今日访客
```
GET /api/visitors/today

Response:
{
  "success": true,
  "data": [ ... ]
}
```

### 9.6 访客统计
```
GET /api/visitors/stats

Response:
{
  "success": true,
  "data": {
    "todayTotal": 10,
    "todayCheckedIn": 5,
    "activeVisitors": 3,
    "weekTotal": 50
  }
}
```

---

## 10. 统计分析服务 (端口: 5014)

### 10.1 获取指标概览
```
GET /api/metrics/indicators

Response:
{
  "success": true,
  "data": {
    "operation": {
      "totalProperties": 5,
      "totalUnits": 500,
      "occupiedUnits": 450,
      "occupancyRate": 90.0
    },
    "financial": {
      "totalRevenue": 1500000.00,
      "collectionRate": 92.5
    },
    "customer": {
      "totalComplaints": 25,
      "resolvedComplaints": 23,
      "complaintResolveRate": 92.0
    }
  }
}
```

### 10.2 获取报表列表
```
GET /api/reports?type=Daily&page=1&pageSize=20

Response:
{
  "success": true,
  "data": [ ... ]
}
```

### 10.3 生成报表
```
POST /api/reports
Authorization: Bearer {token}

Request:
{
  "title": "日报标题",
  "type": "Daily",
  "category": "Comprehensive",
  "startDate": "2026-04-21",
  "endDate": "2026-04-21"
}
```

### 10.4 获取趋势数据
```
GET /api/metrics/trends?metricName=OccupancyRate&days=30

Response:
{
  "success": true,
  "data": [
    { "metricName": "OccupancyRate", "recordDate": "2026-04-01", "value": 88.0 },
    { "metricName": "OccupancyRate", "recordDate": "2026-04-02", "value": 88.5 }
  ]
}
```

### 10.5 仪表盘数据
```
GET /api/dashboard/summary

Response:
{
  "success": true,
  "data": {
    "latestSnapshot": { ... },
    "changes": {
      "occupancyRate": 0.5,
      "collectionRate": 1.2
    }
  }
}
```

---

## 11. 移动端服务 (端口: 5015)

### 11.1 获取首页数据
```
GET /api/mobile/home

Response:
{
  "success": true,
  "data": {
    "quickEntries": [
      { "code": "payment", "name": "物业缴费", "icon": "icon-payment" },
      { "code": "ticket", "name": "我要报修", "icon": "icon-repair" }
    ],
    "recentNotifications": [ ... ],
    "stats": {
      "unpaidBills": 1,
      "activeTickets": 2
    }
  }
}
```

### 11.2 获取快捷入口
```
GET /api/mobile/quick-entries?category=payment

Response:
{
  "success": true,
  "data": [ ... ]
}
```

### 11.3 注册设备
```
POST /api/mobile/devices/register
Authorization: Bearer {token}

Request:
{
  "deviceId": "设备唯一ID",
  "deviceType": "Android",
  "deviceName": "小米12",
  "pushToken": "推送令牌",
  "notificationsEnabled": true
}
```

### 11.4 发送推送通知
```
POST /api/mobile/notifications/send
Authorization: Bearer {token}

Request:
{
  "userId": "admin",
  "type": "System",
  "title": "通知标题",
  "content": "通知内容"
}
```

### 11.5 获取未读通知数
```
GET /api/mobile/notifications/unread-count?userId=admin

Response:
{
  "success": true,
  "data": { "count": 5 }
}
```

### 11.6 二维码登录 - 生成
```
POST /api/mobile/qr/generate?userId=admin

Response:
{
  "success": true,
  "data": {
    "sessionId": "QR-ABCD1234...",
    "qrContent": "wo-property://login?session=QR-ABCD1234...",
    "expiresAt": "2026-04-21T01:05:00Z"
  }
}
```

### 11.7 二维码登录 - 扫码
```
POST /api/mobile/qr/scan

Request:
{
  "sessionId": "QR-ABCD1234..."
}
```

---

## 错误码说明

| 错误码 | 说明 |
|--------|------|
| 400 | 请求参数错误 |
| 401 | 未授权/登录过期 |
| 403 | 权限不足 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

---

## 版本信息

- API版本: v1.0
- 更新日期: 2026-04-21
- 认证: JWT Bearer Token
