# WO 物业管理软件 — 部署文档

> 最后更新：2026-06-02
> 版本：v1.3

---

## 快速启动

```bash
# 首次部署（或换网络环境）
bash setup.sh

# 启动所有服务
bash start-all.sh

# 停止所有服务
bash stop-all.sh
```

---

## 系统架构

```
┌─────────────────────────────────────────┐
│           手机 / 微信小程序              │
│         (http://本机IP:5173)            │
└────────────────┬──────────────────────┘
                  │ HTTP/WebSocket
┌────────────────▼──────────────────────┐
│          GatewayService :5000          │
│     (路由 + 鉴权 + 多租户过滤)          │
└────┬───┬───┬───┬───┬───┬───┬───┬───┬───┘
     │   │   │   │   │   │   │   │   │
  ┌──▼─┐ ┌▼──┐ ┌▼──┐ ┌▼──┐ ┌▼──┐ ┌▼──┐ ┌▼──┐
  │Auth│ │Tick│ │Disc│ │Pers│ │Mast│ │...│ │...│
  │5106│ │5102│ │5241│ │5018│ │5019│ │...│ │...│
  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘
```

---

## 服务端口一览

| 服务 | 端口 | 说明 |
|------|------|------|
| GatewayService | 5000 | API 网关，统一入口 |
| AuthService | 5106 | 用户认证 |
| TicketService | 5102 | 工单管理 |
| DispatchService | 5241 | 智能派单 |
| PersonService | 5018 | 人员中心 |
| MasterDataService | 5019 | 基础数据+字段管理 |
| MaterialService | 5504 | 物料管理 |
| NotificationService | 5105 | 通知服务 |
| PaymentService | 5109 | 支付服务 |
| DeviceService | 5530 | 设备管理 |
| ContractService | 5501 | 合同管理 |
| FinanceService | 5509 | 财务管理 |
| InspectionService | 5510 | 巡检管理 |
| ComplaintService | 5201 | 投诉管理 |
| KeyService | 5512 | 钥匙管理 |
| VisitorService | 5513 | 访客管理 |
| StatisticsService | 5250 | 统计服务 |
| MobileService | 5526 | 移动端聚合 |
| CommunityService | 5522 | 社区服务 |
| ParkingService | 5525 | 停车服务 |
| RenovationService | 5521 | 装修服务 |
| admin-portal | 5173 | 管理后台前端 |

---

## IP 自动检测机制

**原理：**
- 启动时自动检测本机局域网 IP（`en0` 接口）
- 对比 `.server-ip` 文件记录的上次 IP
- 如果变化了，自动更新所有相关配置文件

**影响的配置文件：**
- `admin-portal/src/api/*.ts` — 管理后台 API 地址
- `admin-portal/src/stores/*.ts` — WebSocket 地址
- `woa-property-mini/src/config/env.js` — 小程序 API 地址
- `woa-property-mini/src/App.vue` — 小程序全局配置
- `src/*/appsettings.json` — 后端 CORS 配置

**无需手动改任何文件** — 换网络后重新运行 `start-all.sh` 即可。

---

## 多 Mac 部署

### 架构
```
Mac A（主机）          Mac B（副机）
├─ Gateway:5000       ├─ TicketService:5102
├─ AuthService:5106   ├─ PersonService:5018
├─ ...                └─ MasterDataService:5019

手机 → Mac A Gateway → 按需路由到 Mac B 的服务
```

### 部署步骤

**Mac A（已有服务）：**
```bash
bash start-all.sh
```

**Mac B（新机器）：**
```bash
git clone <项目>
bash setup.sh    # 输入 Mac A 的 IP
bash start-all.sh # 只启动分配给自己的服务
```

**配置文件 `config/ports.json`：**
```json
{
  "name": "TicketService",
  "port": 5102,
  "hosts": ["mac-a", "mac-b"],   // 哪些机器跑这个服务
  "primary": "mac-a"             // 主实例
}
```

### 服务分组（推荐）
- **核心组**（必须和 Gateway 同机）：AuthService、TicketService、DispatchService、PersonService、MasterDataService
- **业务组**（可分散）：其他 15 个业务服务
- **前端组**：admin-portal（5173）

---

## 数据库

- **MySQL**：`wo_property` 数据库（统一库）
- 位置：本地 `/usr/local/var/mysql/`
- 字符集：`utf8mb4`
- 迁移：`dotnet ef database update` 或手动执行 SQL

---

## 日志

- 位置：`/Users/mac/Projects/WO-Property-Management/logs/`
- 服务独立日志文件
- 健康检查看门狗：`scripts/service-watchdog.sh`（每 5 分钟执行）

---

## 测试账号

| 账号 | 密码 | 角色 |
|------|------|------|
| admin | Admin@123 | 系统管理员 |
| tech | Tech@123 | 技术人员 |
| user | User@123 | 普通用户（业主） |

---

## 微信小程序

- 代码：`src/woa-property-mini/`
- 编译：`npm run build:mp-weixin`
- 产物：`dist/` 目录，用微信开发者工具打开并上传

---

## 健康检查

```bash
# 单次检查
for port in 5000 5106 5102 5241 5018 5019; do
  curl -s http://localhost:$port/health
done

# 看门狗自动重启（已在 cron 中配置）
crontab -l | grep service-watchdog
```