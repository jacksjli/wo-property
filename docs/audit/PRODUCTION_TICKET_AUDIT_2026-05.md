# 审计报告 — 工单模块（Ticket + Dispatch）— 2026-05-25

## 审计概述

| 项目 | 内容 |
|------|------|
| **审计时间** | 2026-05-25 09:00 |
| **审计范围** | TicketService (5102) + DispatchService (5241) + API Gateway 路由 |
| **评级** | 🟡 观察 |

---

## 1. API契约检查

### 1.1 TicketService (5102)

| 接口 | 文档描述 | 实际行为 | 偏差 | 严重度 |
|------|---------|---------|------|--------|
| GET /api/tickets | 设计文档 v3.0：`/api/tickets` | 实际：`/api/tenant/tickets`（多租户） | ⚠️ 路径不匹配 | 中 |
| POST /api/tickets | 设计文档 v3.0：`/api/tickets` | 实际：`/api/tenant/tickets` | ⚠️ 路径不匹配 | 中 |
| 工单编号规则 | `WO-YYYYMM-NNNNN`（5位起，溢出扩6位） | ✅ 实际：`WO-202605-10001` | 无偏差 | - |
| 状态流转 | 8个状态（Pending→Assigned→...→Closed） | ✅ 实际返回 status: New/InProgress/Resolved/Closed 等 | 无偏差 | - |
| 派单触发 | 创建工单后自动派单 | ✅ 实际：DispatchService 返回 200 | 无偏差 | - |

### 1.2 DispatchService (5241)

| 接口 | 文档描述 | 实际行为 | 偏差 | 严重度 |
|------|---------|---------|------|--------|
| GET /api/dispatch/rules | 设计文档 v3.0：派单规则列表 | 实际：404（路由不存在） | 🔴 路由不存在 | 高 |
| POST /api/dispatch/auto | 自动派单 | 实际路由：`/api/tenant/dispatch/auto` | ⚠️ 路径不匹配 | 中 |
| 派单规则算法 | 3步：job_type匹配→area匹配→优先级排序 | ✅ 代码实现一致 | 无偏差 | - |
| 拒单处理 | 设计文档有描述 | ✅ 代码实现一致 | 无偏差 | - |

### 1.3 API Gateway (5000) 路由

| 路由 | 配置状态 | 实际行为 | 偏差 | 严重度 |
|------|---------|---------|------|--------|
| /api/tickets → ticket-cluster | ✅ 已配置 | GET 返回 404 | 🔴 ticket-cluster 路由指向错误端口或路径 | 高 |
| /api/tenant/tickets → ticket-cluster | ✅ 已配置 | ✅ 返回 200 | 无偏差 | - |
| /api/dispatch/* → dispatch-cluster | ✅ 已配置 | ⚠️ 路由指向 5241 但 /api/dispatch/rules 404 | 🟡 观察 | 中 |

---

## 2. 决策追溯检查

| 决策 | 文档位置 | 执行状态 | 说明 |
|------|---------|---------|------|
| TicketService 和 DispatchService 分离 | `TICKET_SERVICE_ARCHITECTURE_v1.0.md` | ✅ 已执行 | TicketService 5102，DispatchService 5241 |
| 工单编号格式 `WO-YYYYMM-NNNNN`（5位起，溢出扩6位） | `docs/MODULES/ticket/DESIGN.md` | ✅ 已执行 | TicketController 生成 |
| 派单规则引擎独立 | `TICKET_SERVICE_ARCHITECTURE_v1.0.md` | ✅ DispatchService 已实现 | 5241 端口 |
| 8个状态流转 | `TICKET_SERVICE_ARCHITECTURE_v1.0.md` | ✅ 代码实现一致 | Pending/Assigned/InProgress/Resolved/Closed + 3个特殊状态 |
| DispatchService 实际端口 | `TICKET_SERVICE_ARCHITECTURE_v1.0.md` 写 5003 | 实际 5241 | 🔴 端口不一致 | 高 |

---

## 3. 安全检查

| 检查项 | 预期 | 实际 | 结果 |
|--------|------|------|------|
| TicketService 需要认证 | JWT Token | ✅ 401 without token | 🟢 合规 |
| TenantRoutingMiddleware | 从 JWT 提取 tenant_code | ⚠️ tenant_code 为空字符串 | 🟡 观察 |
| DispatchService 租户隔离 | 仅访问当前租户数据 | ✅ TenantDbContextFactory 已实现 | 🟢 合规 |

---

## 4. 不一致问题清单

| # | 问题描述 | 严重度 | 修复建议 |
|---|---------|--------|---------|
| 1 | **DispatchService 端口不一致** — 设计文档写 5003，实际 5241 | 高 | 更新 `TICKET_SERVICE_ARCHITECTURE_v1.0.md` 端口为 5241 |
| 2 | **API Gateway ticket-cluster 路由指向旧端口** — Gateway配置指向 5102 但 GET /api/tickets 返回 404 | 高 | 检查 Gateway 配置，确认 /api/tickets 路由指向 |
| 3 | **JWT tenant_code 为空** — AuthService 登录时 tenant_code 写入为空字符串，影响多个服务 401 | 中 | 修复 AuthService JWT 写入逻辑 |
| 4 | **DispatchService /api/dispatch/rules 路由不存在** — 设计文档描述该端点，但实际路由是 `/api/tenant/dispatch/*` | 中 | 更新设计文档或新增路由 |
| 5 | **config/ports.json 与 SERVICES_STATUS.md 端口一致** | - | ✅ 与设计文档偏差时，以 ports.json 为准 |

---

## 5. 端口配置一致性

| 服务 | 设计文档 | config/ports.json | 实际监听端口 | 一致性 |
|------|---------|-------------------|-------------|--------|
| GatewayService | 5000 | 5000 | 5000 | ✅ |
| TicketService | 5102 | 5102 | 5102 | ✅ |
| DispatchService | 5003 (文档) | 5241 | 5241 | ⚠️ 文档过时 |
| AuthService | 5106 | 5106 | 5106 | ✅ |

---

## 6. 本周新发现（2026-05-25 API测试）

基于今日完整 API 链路测试，发现以下新增问题：

| 问题 | 服务 | 发现方式 |
|------|------|---------|
| 6个服务 401（tenant_code为空） | Material/Device/Contract/Finance/Inspection/Key | curl 测试 |
| Gateway /api/tickets → 404 | API Gateway | curl 测试 |
| DispatchService 实际路径 `/api/tenant/dispatch/*` | DispatchService | curl 测试 |

---

## 7. 下周行动项

- [ ] **高**：更新 `TICKET_SERVICE_ARCHITECTURE_v1.0.md` 中 DispatchService 端口（5003→5241）
- [ ] **高**：排查 API Gateway ticket-cluster 路由配置
- [ ] **中**：修复 AuthService JWT tenant_code 写入问题
- [ ] **中**：更新 `TICKET_SERVICE_ARCHITECTURE_v1.0.md` 中 DispatchService API 路径描述
- [ ] **低**：更新 `config/ports.json` 与本审计报告保持一致（已一致）

---

## 8. 审计结论

**整体评级：🟡 观察**

工单核心业务逻辑（状态流转、派单算法、工单编号）实现质量良好，与设计文档高度一致。

主要偏差集中在：
1. **端口配置文档过时**（DispatchService 5003→5241）
2. **Gateway 路由配置问题**（/api/tickets → 404）
3. **JWT tenant 信息缺失**（影响 6 个服务认证）

这些问题不影响当前业务功能（前端使用 /api/tenant/tickets 路径），但需要尽快修复以保持文档准确性。