# 审计报告 — 工单模块 — 2026-06-01

## 审计概要

| 项目 | 值 |
|------|---|
| 审计日期 | 2026-06-01 |
| 审计范围 | 工单模块（TicketService + 多项目支持） |
| 评级 | 🟡 观察 |
| 严重偏差 | 1 个（工单编号格式文档未更新） |
| 中等偏差 | 2 个（多项目设计文档缺失、DispatchService 端口文档过时） |

---

## 1. API契约检查

### 1.1 工单编号格式

| 检查项 | 设计文档描述 | 实际行为 | 偏差 | 严重度 |
|--------|------------|---------|------|--------|
| 工单编号格式 | `WO-YYYYMM-NNNNN`（DESIGN.md v1.0） | `YGHY001-WO-YYYYMM-NNNNN`（含项目前缀） | 文档未反映多项目架构下的编号规则 | 🔴 高 |
| 编号前缀 | 固定 `WO-` | 按项目前缀变化（`YGHY001-`, `YGXY001-`） | 同上 | 🔴 高 |
| 序号位数 | 5位（10001起） | 5位（10001起），每月重置 | ✅ 一致 | - |

### 1.2 创建工单 API

| 检查项 | 预期 | 实际 | 结果 |
|--------|------|------|------|
| `POST /api/tickets` 成功 | `success: true` + `ticketCode` | ✅ 成功返回 `YGHY001-WO-202606-10014` | 🟢 合规 |
| `X-Project-Code` 头过滤 | 按项目代码过滤工单 | ✅ `YGXY001` 项目查询返回 0 条（无该项目的工单） | 🟢 合规 |
| 返回数据格式 | `success + data + total + page` | ✅ 格式完全一致 | 🟢 合规 |

---

## 2. 设计文档 vs 实际代码偏差

| # | 问题描述 | 设计文档位置 | 实际代码 | 严重度 | 修复建议 |
|---|---------|------------|---------|--------|---------|
| 1 | 工单编号格式未反映多项目前缀 | `docs/MODULES/ticket/DESIGN.md` 第2章 | `TenantTicketController.CreateTicket` | 🔴 高 | 更新 DESIGN.md 第2章，补充多项目编号规范 |
| 2 | 多项目设计完全缺失 | `DESIGN.md` | `ProjectTabBar.vue` + `currentProject` | 🔴 高 | 新增「多项目支持」章节，记录设计决策 |
| 3 | DispatchService 端口未更新 | `DESIGN.md` 注 5003 | 实际运行在 5241 | 🟡 中 | 更新 DESIGN.md 中 DispatchService 端口说明 |

---

## 3. 决策追溯

### 已实施的决策（无文档记录）

| 决策 | 实施时间 | 状态 |
|------|---------|------|
| 工单编号增加项目前缀 `{ProjectCode}-WO-{YYYYMM}-{NNNNN}` | 2026-06-01（本周） | ✅ 已实施，待更新文档 |
| 多小区用户通过 `X-Project-Code` header 切换项目上下文 | 2026-06-01（本周） | ✅ 已实施，待更新文档 |
| 工单编号序列号按项目 + 月重置（每项目独立序号） | 2026-06-01（本周） | ✅ 已实施，待更新文档 |

**问题**：以上决策无设计文档记录，不符合「审计要求决策可追溯」原则。

---

## 4. 安全与配置检查

| 检查项 | 预期 | 实际 | 结果 |
|--------|------|------|------|
| JWT 认证 | Bearer Token | ✅ 正常验证 | 🟢 合规 |
| Tenant-Code | Header | ✅ 正常传递 | 🟢 合规 |
| X-Project-Code | Header | ✅ 正常过滤 | 🟢 合规 |
| TicketCode 唯一约束 | MySQL UNIQUE | ✅ 数据库层有 UNIQUE 约束 | 🟢 合规 |

---

## 5. 下周行动项

- [ ] **高优先级**：更新 `docs/MODULES/ticket/DESIGN.md` 第2章（工单编号规范），补充多项目格式 `{ProjectCode}-WO-YYYYMM-NNNNN`
- [ ] **高优先级**：更新 `docs/MODULES/ticket/DESIGN.md` 新增「多项目支持」章节
- [ ] **中优先级**：更新 `DESIGN.md` 中 DispatchService 端口（5003 → 5241）
- [ ] **低优先级**：将本次审计报告存入 `docs/audit/PRODUCTION_TICKET_AUDIT_2026-06.md`

---

## 6. 本周代码变更摘要

```
2026-06-01 完成的代码变更：
✅ PersonService/AuthService login-by-person 返回 projects 数组（多小区支持）
✅ ProjectTabBar 组件（底部项目切换栏）
✅ X-Project-Code header 过滤工单
✅ 工单编号格式升级为 YGHY001-WO-YYYYMM-NNNNN（含项目前缀）
✅ DispatchService 重启至 5241 端口（健康）
✅ admin/tickets.vue 修复 refreshTimer 未定义 Bug
✅ 新增 dispatch.vue / ticket-detail.vue 页面
✅ IP 配置 192.168.1.3 → 192.168.0.76（Mac IP 变更）
✅ TicketNumber 列宽 ALTER TABLE（20 → 50）
✅ API 测试脚本 test-api.js 完成，22 项全通过
```

---

_审计人：🪽的芦苇_
_下次审计：2026-06-08_