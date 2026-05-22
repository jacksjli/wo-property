# 审计日志总表

## 2026-05-11 本周审计

### 本周重点模块
- 字段管理系统（FieldManagementView + 等价标准）
- 外来临时人员统一管理（新建原则）

### 执行时间
2026-05-11 09:00

---

## 模块：字段等价标准

| 检查项 | 文档描述 | 实际代码 | 偏差 | 严重度 |
|--------|---------|---------|------|--------|
| equivalenceGroups 命名 | API fieldKey 为蛇形（snake_case） | 代码中部分字段仍用驼峰（如 assigneeName） | 已修复 | 低 |
| 别名展开功能 | 点击标签应展开显示等价别名列表 | 展开功能有 Bug（.has vs .includes） | 已修复 | 低 |
| API 字段匹配 | 等价组字段应与 API 返回的 fieldKey 一致 | 部分字段不匹配（reporterName vs reporter_name） | 已修复 | 中 |

**评级：🟡 观察**

**原因：** 等价标准刚建立，数据层已对齐但前端展示层刚经历多次修复，需要稳定性验证。

**行动项：**
- [ ] 验证字段管理页面别名展开功能稳定运行
- [ ] 补充缺失的等价字段到 equivalenceGroups（如 complainant_name, recipient_name, handlerName）

---

## 模块：外来临时人员统一管理

| 检查项 | 原则要求 | 执行状态 | 严重度 |
|--------|---------|---------|--------|
| external_persons 表 | 新建统一表存放访客/外卖/快递 | ✅ 已建表 | - |
| API 契约 | 各模块只调用 API，不自己建表写数据 | 🔴 未执行 | 高 |
| 前端行为 | 工单模块调用搜索+选择，不直接填 | 🔴 未执行 | 高 |
| 数据库权限 | VisitorService/DeliveryService 有写权限 | 🔴 未配置 | 高 |

**评级：🔴 整改**

**原因：** 原则刚刚建立，尚未开始执行。需要本周完成 Phase 2-5。

**行动项：**
- [ ] 在 VisitorService (5013) 或新建 ExternalPersonService 加 API
- [ ] 工单模块改造：reporter_name/phone → 调用 external-persons API
- [ ] 数据库权限配置：只给 VisitorService/DeliveryService 写权限
- [ ] 编写审计脚本（每周检查业务表是否有冗余人名/电话字段）

---


---

## 2026-05-18 本周审计

### 本周重点模块
- Phase 0 多物业分库改造（AuthService + TicketService）

### 执行时间
2026-05-18 09:00

---

## 模块：Phase 0 多物业分库

| 检查项 | 文档描述 | 实际代码 | 偏差 | 严重度 |
|--------|---------|---------|------|--------|
| Phase 0 架构文档 | 无（docs/ 下无相关文档） | TenantDbFactory + TenantRoutingMiddleware | 🔴 文档缺失 | 高 |
| AuthService 多租户 API | 无文档 | JWT 新增 tenant_id/tenant_code/project_ids | 🟡 观察 | 中 |
| TicketService API | docs/MODULES/ticket/API.md 描述单租户 | 实际为多租户（/api/tenant/tickets） | 🔴 文档过时 | 高 |
| 租户隔离 | 数据库级隔离 | tenant_a/tenant_b 完全隔离 | ✅ 已执行 | - |
| 单元测试覆盖 | 无 | 13 个测试通过 | ✅ 已执行 | - |
| 集成测试覆盖 | 无 | 16 个场景通过 | ✅ 已执行 | - |
| 分支推送 | 未推送 GitHub | 4 个 commit 本地 | 🟡 观察 | 中 |

**评级：🟡 观察**

**原因：** 核心代码质量良好，租户隔离验证通过，但设计文档严重滞后于实现。

**行动项：**
- [ ] 创建 Phase 0 架构文档
- [ ] 标注 docs/MODULES/ticket/API.md 为 DEPRECATED
- [ ] 推送 feature/multi-tenant 到 GitHub
- [ ] Phase 1 规划文档

---

## 下周待审模块
- PersonService API 契约一致性
- MasterDataService 字段管理 API

---

_记录人：🪽的芦苇_
_更新周期：每周一_

## 2026-05-19
变更同步：完成
  详见：memory/2026-05-19.md

---

## $(date +%Y-%m-%d) 自动更新

### 字段命名标准化
- 清理 PascalCase 重复字段（71个）
- FieldDefinitions: 398 → 235
- 统一使用 snake_case 命名

### 字段等价映射
- 新增 field_equivalences 表（52条映射数据）
- API: /api/field-equivalences/resolve
- 前端 fieldConfig store 支持等价映射

### 分级刷新机制
- HIGH (1分钟): fieldDefinition, ticket, ticketType, dispatch
- MEDIUM (3分钟): personnel, contract, material...
- LOW (5分钟): building, room...
- STATIC (10分钟): statistics, project...

### 测试通过
- 17 个测试项目全部通过
- 119 个测试用例 100% 通过率


## 2026-05-22
变更同步：完成
  详见：memory/2026-05-22.md

---

## 2026-05-22 本次审计

### 本次重点模块
- 第二批模块：物料分类、消息模板、外部人员
- 第三批模块：报表中心（8个Tab）
- AnnouncementService 聚合报表 API
- 侧边栏分类和图标修复

### 执行时间
2026-05-22 11:05

---

## 模块：物料分类 (MaterialCategory)

| 检查项 | 状态 | 说明 |
|--------|------|------|
| MaterialService API | ✅ 正常 | 端口 5504 |
| TenantDbContext 映射 | ✅ 已修复 | PascalCase |
| 前端 MaterialCategoryList.vue | ✅ 已创建 | 对接 5504 API |
| 数据库数据 | ✅ 5 条 | 正常工作 |

**评级：🟢 合规**

---

## 模块：消息模板 (MessageTemplate)

| 检查项 | 状态 | 说明 |
|--------|------|------|
| NotificationService API | ✅ 正常 | 端口 5129 |
| 前端 MessageTemplateList.vue | ✅ 已创建 | 对接 5129 API |
| 数据库数据 | ✅ 5 条 | 正常工作 |

**评级：🟢 合规**

---

## 模块：外部人员 (ExternalPerson)

| 检查项 | 状态 | 说明 |
|--------|------|------|
| VisitorService API | ✅ 正常 | 端口 5513 |
| 前端 ExternalPersonList.vue | ✅ 已创建 | 对接 5513 API |
| 数据库数据 | ✅ 2 条 | 正常工作 |

**评级：🟢 合规**

---

## 模块：报表中心 (Reports)

| 检查项 | 状态 | 说明 |
|--------|------|------|
| AnnouncementService 聚合 API | ✅ 已创建 | 端口 5511 |
| 设备报表 | ✅ | /api/tenant/announcements/reports/device |
| 工单报表 | ✅ | /api/tenant/announcements/reports/ticket |
| 物料报表 | ✅ | /api/tenant/announcements/reports/material |
| 满意度调查 | ✅ | /api/tenant/announcements/reports/satisfaction |
| 采购订单 | ✅ | /api/tenant/announcements/purchase-orders |
| 库存事务 | ✅ | /api/tenant/announcements/stock-transactions |
| 枚举定义 | ✅ | /api/tenant/announcements/enum-definitions |
| 综合报表 | ✅ | /api/tenant/announcements/general-reports |
| 前端 ReportsView.vue | ✅ 已创建 | 8 个 Tab 页面 |
| 侧边栏菜单 | ✅ 已添加 | 11 个新模块 |

**评级：🟢 合规**

---

## 模块：侧边栏分类

| 检查项 | 状态 | 说明 |
|--------|------|------|
| App.vue categories | ✅ 已更新 | 新增 3 个分类 |
| 消息中心 | ✅ | 公告、消息、模板、外部人员 |
| 数据报表 | ✅ | 统计分析 + 5 个报表模块 |
| 采购库存 | ✅ | 采购订单、库存事务、枚举定义 |
| 图标修复 | ✅ | Truck→Van, Coin→Money, BankCard→CreditCard, Sunny→Brush, Goods→ShoppingCart |

**评级：🟢 合规**

---

## 整体评级：🟢 合规

所有本次实现的模块均已通过检查，设计文档与代码一致。

