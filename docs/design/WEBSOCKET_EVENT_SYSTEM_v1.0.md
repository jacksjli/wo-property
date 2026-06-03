# WebSocket 实时事件系统架构

> 版本: 1.1
> 更新日期: 2026-05-30
> 状态: ✅ 已实现（v1.1 更新事件格式兼容）

---

## 📋 概述

WebSocket 实时事件系统为 WO 物业管理软件提供前后端实时数据推送能力，支持工单、派工、通知、支付等模块的即时更新。

### 核心功能

| 功能 | 说明 |
|------|------|
| 实时推送 | 工单状态变更、派工通知等即时推送 |
| 统一事件格式 | 所有模块使用统一的 `module:eventType` 格式 |
| 自动重连 | 前端 WebSocket 断开自动重连 |
| 模块化订阅 | 支持通配符订阅，如 `ticket:*` |

---

## 🏗️ 系统架构

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Gateway (5000)                               │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────────┐  │
│  │ WebSocket    │  │ Event API    │  │ 事件广播 (Broadcast)       │  │
│  │ Manager      │  │ /internal/    │  │ wsManager.BroadcastAsync() │  │
│  │ /ws          │  │ events/publish│  └────────────────────────────┘  │
│  └──────────────┘  └──────────────┘                                  │
└─────────────────────────────────────────────────────────────────────┘
                              ▲
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────┴───────┐   ┌───────┴───────┐   ┌───────┴───────┐
│ TicketService  │   │NotificationSvc│   │ PaymentService│
│ (5102)        │   │ (5105)        │   │ (5109)        │
│ - ticket:created   │ - notification:created│ - payment:created│
│ - ticket:updated   │ - notification:read  │ - payment:status_changed
└───────────────────┘   └───────────────────┘   └───────────────────┘
        │                     │                     │
        └─────────────────────┴─────────────────────┘
                              │
                    HTTP POST /internal/events/publish
```

---

## 🔌 事件发布端点

### 统一事件发布 API

```
POST /internal/events/publish
Content-Type: application/json

{
  "module": "ticket",
  "eventType": "created",
  "data": {
    "id": 123,
    "ticketCode": "WO-20260527-0001",
    "title": "维修报修",
    "status": "New"
  }
}
```

### 响应格式

```json
{
  "success": true,
  "connections": 5
}
```

---

## 📡 事件类型

### 已实现的事件（v1.1 更新）

| 模块 | 事件类型 | 触发时机 | 数据内容 |
|------|----------|----------|----------|
| **ticket** | created | 新建工单 | ticketId, ticketCode, title, status, projectCode, areaId, createdAt |
| **ticket** | statusChanged | 工单状态变更 | ticketId, ticketCode, previousStatus, currentStatus, assigneePersonId, assignedAt |
| **dispatch** | dispatched | 自动派单 | ticketId, ticketCode, previousStatus, currentStatus, assigneePersonId |

### 事件格式（兼容两种）

**Gateway WebSocket 广播格式：**
```json
{
  "type": "ticket:created",
  "data": {
    "ticketCode": "WO-20260530-1125",
    "title": "维修报修",
    "status": "New"
  }
}
```

**API 响应格式（TicketService 发布）：**
```json
{
  "Module": "ticket",
  "EventType": "created",
  "Data": {
    "ticketId": 1125,
    "ticketCode": "WO-20260530-1125",
    "title": "维修报修",
    "status": "New",
    "projectCode": "YGHY001"
  }
}
```

> 前端 `handleMessage` 统一处理两种格式，自动识别 `type` 或 `Module:EventType`

---

## 🎯 前端订阅方式

### 微信小程序端

```javascript
// 引入 WebSocket 管理器
const websocket = require('./utils/websocket.js')

// 初始化连接
websocket.connect()

// 订阅工单更新
websocket.on('ticket:*', (data) => {
  console.log('工单更新:', data)
  // 更新本地工单列表或显示提示
})

// 订阅支付状态变化
websocket.on('payment:status_changed', (data) => {
  console.log('支付状态变化:', data)
})

// 订阅所有通知
websocket.on('notification:*', (data) => {
  console.log('新通知:', data)
})
```

### 管理后台 (admin-portal)

```typescript
import { initWebSocket, useWebSocket } from '@/stores/websocket'

// 在组件中
const { on } = useWebSocket()

onMounted(() => {
  initWebSocket()
  on('ticketUpdated', handleTicketUpdate)
  on('payment:status_changed', handlePaymentUpdate)
})
```

---

## 🔄 自动重连机制

### 微信小程序

```javascript
// 连接失败 5 秒后自动重连
// 最多重试 10 次

const RECONNECT_INTERVAL = 5000
const MAX_RECONNECT_ATTEMPTS = 10

// 页面隐藏断开，页面显示重连
onHide(() => websocket.disconnect())
onShow(() => websocket.connect())
```

### 管理后台

```typescript
// 断开后 5 秒自动重连
socket.onclose = () => {
  setTimeout(() => connect(), 5000)
}
```

---

## 📝 代码实现

### 后端 - Gateway WebSocketManager

```csharp
// 文件: src/WO.Property.GatewayService/WebSocketManager.cs

public async Task BroadcastAsync(string eventType, object data)
{
    var message = JsonSerializer.Serialize(new { type = eventType, data });
    var bytes = Encoding.UTF8.GetBytes(message);

    foreach (var socket in _sockets.Values.ToList())
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(...);
        }
    }
}
```

### 后端 - 服务事件发布

```csharp
// TicketService/Controllers/TenantTicketController.cs
// 工单创建时广播事件
try
{
    var gatewayClient = _httpClientFactory.CreateClient("Gateway");
    await gatewayClient.PostAsJsonAsync("/internal/events/publish", new
    {
        module = "ticket",
        eventType = "created",
        data = new { ticketId, ticketCode, title, status, projectCode, createdAt }
    });
}
catch (Exception ex) { _logger.LogWarning(ex, "Failed to broadcast"); }

// 工单状态变更时广播事件
try
{
    var gatewayClient = _httpClientFactory.CreateClient("Gateway");
    await gatewayClient.PostAsJsonAsync("/internal/events/publish", new
    {
        module = "ticket",
        eventType = "statusChanged",
        data = new { ticketId, ticketCode, previousStatus, currentStatus, assigneePersonId }
    });
}
catch (Exception ex) { _logger.LogWarning(ex, "Failed to broadcast"); }
```

### 前端 - 微信小程序 WebSocket 客户端

```javascript
// src/utils/websocket.js
// 支持两种事件格式兼容
function handleMessage(message) {
  const type = message.type || (message.Module && message.EventType ? `${message.Module}:${message.EventType}` : null)
  const data = message.data || message.Data
  // ...
}

// 连接时自动订阅工单事件
connect({
  onTicketCreated: (data) => {
    uni.showToast({ title: `新工单: ${data.ticketCode}`, icon: 'none' })
    refresh()
  },
  onTicketStatusChanged: (data) => {
    // 更新列表中对应工单状态
    updateTicketStatus(data.ticketId, data.currentStatus)
  }
})
```

---

## 🧪 测试方法

### 1. 测试 WebSocket 连接

```bash
# Mac/Linux
websocat ws://localhost:5000/ws

# 或使用 curl (测试握手)
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Version: 13" \
  -H "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" \
  http://localhost:5000/ws
```

### 2. 测试事件发布

```bash
curl -X POST http://localhost:5000/internal/events/publish \
  -H "Content-Type: application/json" \
  -d '{
    "module": "ticket",
    "eventType": "test",
    "data": {
      "id": 999,
      "ticketCode": "WO-TEST-001",
      "title": "测试工单",
      "status": "New"
    }
  }'
```

### 3. 预期响应

```json
{"success": true, "connections": 0}
```

---

## 📊 性能指标

| 指标 | 目标值 | 说明 |
|------|--------|------|
| 并发连接数 | 100+ | Gateway 支持的 WebSocket 并发数 |
| 消息延迟 | < 100ms | 事件发布到前端接收 |
| 重连时间 | < 5s | 断开后自动重连 |
| 消息大小 | < 1KB | 单条事件数据 |

---

## 🔒 安全考虑

1. **认证**: WebSocket 连接使用与 HTTP 相同的 JWT Token
2. **CORS**: 仅允许配置的来源连接
3. **限流**: WebSocket 消息受全局限流保护
4. **隔离**: 多租户数据通过 Header 隔离

---

## 📁 相关文件

| 文件 | 说明 |
|------|------|
| `src/WO.Property.GatewayService/WebSocketManager.cs` | WebSocket 管理器 |
| `src/WO.Shared/Configuration/PortConfig.cs` | 端口配置读取器 |
| `src/admin-portal/src/stores/websocket.ts` | 前端 WebSocket 客户端 |
| `src/woa-property-mini/src/utils/websocket.js` | 小程序 WebSocket 客户端 |
| `config/ports.json` | 服务端口配置 |

---

## 🚀 后续扩展

- [ ] 添加 Redis pub/sub 支持分布式部署
- [ ] 添加消息持久化 (离线用户)
- [ ] 添加消息历史记录查询 API
- [ ] 支持 WebSocket SSL (wss://)