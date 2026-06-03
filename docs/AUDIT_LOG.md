# 审计日志总表

## 2026-05-25 本周审计

### 本周重点模块
- 工单模块（TicketService + DispatchService）
- API Gateway 路由配置
- 16个服务完整 API 链路测试

### 执行时间
2026-05-25 09:00

### 审计方法
- API 链路测试：使用 curl + Python 脚本直接调用 16 个服务
- 代码审查：对比 `docs/design/TICKET_SERVICE_ARCHITECTURE_v1.0.md` 与实际代码
- 配置检查：对比 `config/ports.json` 与实际监听端口

### 评级：🟡 观察

**原因：**
- 核心业务逻辑（工单编号、状态流转、派单算法）与设计文档一致 ✅
- 端口配置偏差（DispatchService 文档5003/实际5241）需要更新文档
- JWT tenant_code 为空影响 6 个服务认证，需要修复
- Gateway 部分路由 404，需要排查配置

### 行动项：
- [ ] 更新 `TICKET_SERVICE_ARCHITECTURE_v1.0.md` DispatchService 端口（5003→5241）
- [ ] 排查 API Gateway /api/tickets 路由 404 问题
- [ ] 修复 AuthService JWT tenant_code 写入逻辑
- [ ] 详细报告见：`docs/audit/PRODUCTION_TICKET_AUDIT_2026-05.md`

---

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


---

## 2026-05-25 Schema规范化行动

### 背景
今天修复了三个服务（KeyService, InspectionService, FinanceService）的模型与数据库不匹配问题。
根本原因是：代码设计与数据库实际结构脱节。

### 执行的操作

1. **创建完整数据库 Schema 文档**
   - 文件: `docs/database/SCHEMA_REFERENCE.md`
   - 内容: 所有表结构的列名、类型、映射规则
   - 目的: 建立"数据库是唯一真相来源"的标准

2. **创建 Schema 验证脚本**
   - 文件: `scripts/validate-schema.sh`
   - 功能: 检查 TenantDbContext vs 数据库列名
   - 使用: `./scripts/validate-schema.sh KeyService`

3. **创建 Schema 验证技能**
   - 文件: `skills/schema-validation/SKILL.md`
   - 规范: 设计→实现→验证→文档 的完整流程

### 根因分析
- 代码模型凭空设计，未对照数据库实际结构
- 文档只写"是什么"，不写"数据库实际是什么"
- 缺少代码 vs 数据库验证环节

### 预防措施
- 设计阶段必须先查数据库 Schema
- 实现阶段使用显式 HasColumnName 映射
- 验证阶段运行 validate-schema.sh
- 文档阶段更新 SCHEMA_REFERENCE.md

### 相关文件
- `docs/database/SCHEMA_REFERENCE.md` (新建)
- `scripts/validate-schema.sh` (新建)
- `skills/schema-validation/SKILL.md` (新建)

---

## 2026-05-25 设计文档更新

### 更新/新建的文档

| 文档 | 操作 | 说明 |
|------|------|------|
| `docs/README.md` | 重写 | 完整设计文档，包含架构、服务、数据库、API规范 |
| `docs/SERVICES_STATUS.md` | 重写 | 服务状态表，包含端口、表、状态、备注 |
| `docs/ARCHITECTURE_OVERVIEW.md` | 新建 | 项目架构概览，技术栈、系统架构图、多租户说明 |
| `docs/database/SCHEMA_REFERENCE.md` | 新建 | 完整数据库Schema对照表，68张表结构 |
| `docs/design/SERVICE_REFACTOR_2026-05-25.md` | 新建 | 服务重构记录 |

### 文档结构

```
docs/
├── README.md                    # 主文档（设计文档入口）
├── ARCHITECTURE_OVERVIEW.md     # 项目架构概览
├── SERVICES_STATUS.md           # 服务状态
├── PORTS.md                     # 端口分配
├── API_STANDARD.md              # API规范
├── AUDIT_LOG.md                 # 审计日志
├── database/
│   └── SCHEMA_REFERENCE.md      # 数据库Schema完整参考
└── design/
    ├── TICKET_SERVICE_ARCHITECTURE_v1.0.md
    └── SERVICE_REFACTOR_2026-05-25.md
```

### 核心原则

**数据库是唯一真相来源 (Database is the source of truth)**

所有文档现在都遵循这一原则，代码实现必须与数据库一致。

### 验证流程

1. `docs/database/SCHEMA_REFERENCE.md` - 所有表结构的准确记录
2. `scripts/validate-schema.sh` - Schema验证脚本
3. `skills/schema-validation/SKILL.md` - 验证工作流技能


---

## 2026-05-27 下午 (16:30-16:45)

### WebSocket 实时事件系统实现完成

#### 完成的工作

1. **Gateway WebSocket 支持**
   - 文件: `src/WO.Property.GatewayService/WebSocketManager.cs`
   - 端点: `/ws` (WebSocket), `/internal/events/publish` (HTTP)

2. **事件发布实现**
   - TicketService: ticket:created, ticket:updated
   - DispatchService: dispatch:dispatched, received, completed
   - NotificationService: notification:created, read
   - PaymentService: payment:created, status_changed
   - ComplaintService: complaint:created, status_changed
   - InspectionService: inspection:created, updated
   - AnnouncementService: announcement:created

3. **前端 WebSocket 客户端**
   - admin-portal: `src/admin-portal/src/stores/websocket.ts`
   - 小程序: `src/woa-property-mini/src/utils/websocket.js`

#### 统一事件格式

```javascript
{
  module: "ticket",
  eventType: "created",
  data: { id, ticketCode, title, status }
}
```

### 端口配置化完成

#### 完成的工作

1. **更新 ports.json**
   - 添加 27 个服务端口配置
   - 统一从 `config/ports.json` 读取

2. **创建配置组件**
   - `src/WO.Shared/Configuration/PortConfig.cs` - 端口配置读取器
   - `src/WO.Shared/Configuration/ServiceRunner.cs` - 服务启动助手

3. **修改所有服务**
   - 移除硬编码端口
   - 改用 `ServiceRunner.ConfigurePort(builder, "ServiceName", defaultPort)`

#### 受影响的服务 (25个)

```
✅ AuthService        ✅ TicketService      ✅ DispatchService
✅ MasterDataService  ✅ NotificationService ✅ PaymentService
✅ ComplaintService   ✅ InspectionService  ✅ AnnouncementService
✅ MaterialService    ✅ DeviceService      ✅ ContractService
✅ FinanceService     ✅ KeyService         ✅ VisitorService
✅ StatisticsService  ✅ MobileService      ✅ AccessControlService
✅ CleaningService    ✅ CommunityService   ✅ DeliveryService
✅ ExpressService     ✅ ParkingService     ✅ RenovationService
✅ TicketTypeService
```

### 文档更新

| 文档 | 说明 |
|------|------|
| `docs/PORTS.md` | 更新端口配置 + WebSocket 事件系统说明 |
| `docs/design/WEBSOCKET_EVENT_SYSTEM_v1.0.md` | 新建 WebSocket 架构文档 |
| `docs/SERVICES_STATUS.md` | 更新服务状态报告 |

### 下一步

- [ ] 启动所有待启动的服务
- [ ] 测试完整的事件发布链路
- [ ] 前端页面集成 WebSocket 实时更新

## 2026-05-28 超时自动升级系统实施

### 概述
实施工单超时自动升级功能，支持根据角色从 timeout_rules 表动态读取超时时间，超时后自动升级给更高级别角色。

### 修改范围
- DispatchService：超时监控 + 升级逻辑 + API
- admin-portal：派单列表 + 工单详情
- woa-property-mini：工单列表 + 工单详情

### 技术决策
1. **超时配置**：从 timeout_rules 表读取（支持按 color + role）
2. **升级路径**：operator → supervisor → manager → department_head → company_head
3. **升级记录**：新建 timeout_escalations 表存储升级历史
4. **派单关联**：parent_dispatch_id 关联母派单，escalation_level 记录级别

### 新增文件
- docs/design/ESCALATION_SYSTEM_v1.0.md

### 新增数据库表
- timeout_escalations

### 新增数据库字段
- dispatch_records.parent_dispatch_id
- dispatch_records.escalation_level

### API 变更
- 新增 GET /api/tenant/dispatch/escalations/{ticketId}

### 状态
✅ 已实施完成

---

## 2026-06-01 本周审计

### 本周重点模块
- 工单模块（TicketService + 多项目支持）
- API 契约一致性（工单编号格式）
- 后端服务端口配置（DispatchService 5241）

### 执行时间
2026-06-01 09:00

### 评级：🟡 观察

**原因：**
- 工单编号格式已升级（`YGHY001-WO-YYYYMM-NNNNN`），但设计文档 `DESIGN.md` 未更新
- 多项目支持（`X-Project-Code` header 过滤）已实施，但无设计文档记录
- DispatchService 端口 5241（文档写 5003）偏差已确认

### 行动项：
- [x] 更新 `docs/MODULES/ticket/DESIGN.md` 第2章（工单编号多项目格式）
- [x] 新增「多项目支持」章节至 `DESIGN.md`
- [x] 更新 `DESIGN.md` DispatchService 端口（5003→5241）
- [ ] 详细报告见：`docs/audit/PRODUCTION_TICKET_AUDIT_2026-06.md`

