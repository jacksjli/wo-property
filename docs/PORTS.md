# WO Property 物业管理软件 - 端口分配表

> 最后更新: **2026-05-27**
> 说明: 所有服务端口统一从 `config/ports.json` 配置，禁止在代码中硬编码

---

## 📋 端口配置概览

### 网关和认证

| 端口 | 服务 | 功能 | 配置化 |
|------|------|------|--------|
| 5000 | GatewayService | API网关，统一入口 + WebSocket | ✅ |
| 5106 | AuthService | 用户认证服务 | ✅ |

### 核心业务服务

| 端口 | 服务 | 功能 | 配置化 |
|------|------|------|--------|
| 5102 | TicketService | 工单管理 + 事件发布 | ✅ |
| 5241 | DispatchService | 智能派单 + 事件发布 | ✅ |
| 5501 | ContractService | 合同管理 | ✅ |
| 5504 | MaterialService | 物料管理 | ✅ |
| 5530 | DeviceService | 设备管理 | ✅ |

### 公共服务

| 端口 | 服务 | 功能 | 配置化 |
|------|------|------|--------|
| 5018 | PersonService | 人员中心 | ✅ |
| 5019 | MasterDataService | 基础数据+字段管理 | ✅ |

### 事件系统服务

| 端口 | 服务 | 功能 | 配置化 |
|------|------|------|--------|
| 5105 | NotificationService | 通知服务 + 事件发布 | ✅ |
| 5109 | PaymentService | 支付服务 + 事件发布 | ✅ |
| 5201 | ComplaintService | 投诉管理 + 事件发布 | ✅ |
| 5510 | InspectionService | 巡检管理 + 事件发布 | ✅ |
| 5511 | AnnouncementService | 公告服务 + 事件发布 | ✅ |

### 其他业务服务

| 端口 | 服务 | 功能 | 配置化 |
|------|------|------|--------|
| 5006 | AccessControlService | 门禁服务 | ✅ |
| 5509 | FinanceService | 财务管理 | ✅ |
| 5512 | KeyService | 钥匙管理 | ✅ |
| 5513 | VisitorService | 访客管理 | ✅ |
| 5516 | CleaningService | 保洁服务 | ✅ |
| 5517 | ExpressService | 快递服务 | ✅ |
| 5521 | RenovationService | 装修服务 | ✅ |
| 5522 | CommunityService | 社区服务 | ✅ |
| 5525 | ParkingService | 停车服务 | ✅ |
| 5526 | MobileService | 移动端服务 | ✅ |
| 5250 | StatisticsService | 统计服务 | ✅ |
| 5017 | DeliveryService | 配送服务 | ✅ |
| 5107 | TicketTypeService | 工单类型服务 | ✅ |

### 管理后台

| 端口 | 服务 | 功能 |
|------|------|------|
| 5173 | admin-portal | 管理后台 (Vue3 + PM2) |

---

## 🔌 WebSocket 事件系统

### 架构

```
┌─────────────┐     WebSocket      ┌─────────────┐
│ admin-portal│◄────────────────►│   Gateway   │
│  (5173)     │   ws://:5000/ws  │   (5000)    │
└─────────────┘                   └─────────────┘
        │                                 ▲
        │ HTTP POST                       │
        │ /internal/events/publish        │
        ▼                                 │
┌─────────────┐                   ┌───────┴───────┐
│ TicketService                     │ 事件广播     │
│ (5102)    │──────┐         ┌────┴───────────┐  │
└─────────────┘      │         │               │  │
┌─────────────┐      └────────►│               │  │
│Notification │              │   Gateway      │  │
│ Service     │──────────────►│   (5000)       │  │
│ (5105)     │              │               │  │
└─────────────┘              └───────────────┘  │
```

### 事件格式

```javascript
// 发布事件
{
  module: "ticket",           // 模块名
  eventType: "created",        // 事件类型
  data: {
    id: 123,
    ticketCode: "WO-20260527-0001",
    title: "维修报修",
    status: "New"
  }
}

// 前端订阅示例
websocket.on('ticket:*', callback)           // 所有工单事件
websocket.on('payment:status_changed', cb)   // 支付状态变化
websocket.on('notification:*', cb)            // 所有通知事件
```

### 支持的事件类型

| 模块 | 事件 | 说明 |
|------|------|------|
| ticket | created, updated | 工单创建/更新 |
| dispatch | dispatched, received, completed | 派工状态变化 |
| notification | created, read | 通知创建/已读 |
| payment | created, status_changed | 支付创建/状态变化 |
| complaint | created, status_changed | 投诉创建/状态变化 |
| inspection | created, updated | 巡检创建/更新 |
| announcement | created | 公告创建 |

---

## 🔧 配置化端口使用

### 配置文件位置

```
/Users/mac/Projects/WO-Property-Management/config/ports.json
```

### 代码使用示例

```csharp
// 使用 ServiceRunner (推荐)
ServiceRunner.ConfigurePort(builder, "AuthService", 5106);

// 或直接使用 PortConfig
var port = PortConfig.GetPortOrDefault("TicketService", 5102);
```

### PortConfig API

```csharp
PortConfig.GetPort("ServiceName")          // 返回 int? (可空)
PortConfig.GetPortOrDefault("Service", 5000) // 返回 int，默认值
PortConfig.ClearCache()                   // 清除缓存，重新加载
```

---

## ✅ 测试验证

```bash
# 测试所有服务健康检查
for port in 5000 5106 5102 5241 5018 5019 5105 5109 5510 5511; do
  echo -n "Port $port: "
  curl -s --max-time 2 "http://localhost:$port/health" | python3 -c \
    "import sys,json; d=json.load(sys.stdin); print(d.get('service','?'))" 2>/dev/null || echo "❌"
done

# 测试事件发布
curl -X POST http://localhost:5000/internal/events/publish \
  -H "Content-Type: application/json" \
  -d '{"module":"test","eventType":"unit_test","data":{"id":1}}'
```

---

## 📝 相关文档

- 服务状态: [SERVICES_STATUS.md](./SERVICES_STATUS.md)
- 数据库Schema: [database/SCHEMA_REFERENCE.md](./database/SCHEMA_REFERENCE.md)
- 设计文档: [design/](./design/)
- 主文档: [README.md](./README.md)
- API规范: [API_STANDARD.md](./API_STANDARD.md)