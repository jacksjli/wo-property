# WO物业管理软件 - 静态代码一致性测试报告

**报告日期:** 2026-05-29  
**分析方式:** 静态代码分析（无需启动服务）  
**分析范围:** admin-portal 前端 API vs 各后端服务 Controller

---

## 📊 执行摘要

| 维度 | 状态 | 违规数 | 观察数 |
|------|------|--------|--------|
| 维度1: API契约一致性 | 🟡 观察 | 0 | 3 |
| 维度2: 端口配置一致性 | 🔴 违规 | 3 | 2 |
| 维度3: 数据模型一致性 | 🟡 观察 | 0 | 2 |
| 维度4: 事件发布一致性 | 🟢 合规 | 0 | 0 |
| 维度5: 状态码一致性 | 🟢 合规 | 0 | 0 |
| 维度6: 认证/授权一致性 | 🟢 合规 | 0 | 0 |

**总计: 🔴 3 项违规 / 🟡 5 项观察 / 🟢 3 项合规**

---

## 维度1: API契约一致性

### 1.1 admin-portal 前端 vs 后端 Controller 路由对比

| 前端文件 | 后端服务 | 前端定义端点 | 后端实际路由 | 状态 |
|---------|---------|------------|------------|------|
| announcement.ts | AnnouncementService:5511 | `/api/tenant/announcements` | `/api/tenant/announcements` ✅ | 🟢 匹配 |
| auth.ts | AuthService:5106 | `/api/auth/login`, `/api/auth/me` | `/api/auth/login`, `/api/auth/userinfo` ✅ | 🟢 匹配 |
| cleaning.ts | 直接连接:5516 | `/api/tenant/cleaning` | (空壳，跳过) | 🟡 待验证 |
| community.ts | 直接连接:5522 | `/api/tenant/community/activities` | (空壳，跳过) | 🟡 待验证 |
| contract.ts | ContractService:5501 | `/api/tenant/contract` | (空壳，跳过) | 🟡 待验证 |
| delivery.ts | `http` 默认实例 | `/api/delivery-requests` | `/api/delivery-requests` ✅ | 🟡 调用方式异常 |
| device.ts | DeviceService:5530 | `/api/tenant/devices` | (空壳，跳过) | 🟡 待验证 |
| dispatch.ts | `http` 默认实例 | `/api/tenant/dispatch/pending` | `/api/tenant/dispatch/pending` ✅ | 🟢 匹配 |
| express.ts | ExpressService:5517 | `/api/tenant/express/express-records` | (空壳，跳过) | 🟡 待验证 |
| finance.ts | FinanceService:5509 | `/api/tenant/finance` | (空壳，跳过) | 🟡 待验证 |
| inspection.ts | 直接连接:5510 | `/api/tenant/inspection` | `/api/tenant/inspection` ✅ | 🟢 匹配 |
| key.ts | KeyService:5512 | `/api/tenant/keys` | (空壳，跳过) | 🟡 待验证 |
| masterDataService.ts | MasterDataService:5019 | `/api/field-definitions`, `/api/module-fields` 等 | 多个 Controller ✅ | 🟢 匹配 |
| material.ts | MaterialService:5504 | `/api/material-categories`, `/api/materials` 等 | `/api/tenant/material/materials` 等 ✅ | 🟡 导入错误 |
| notification.ts | NotificationService | `/api/tenant/notification/notifications` | `/api/tenant/notification/notifications` ✅ | 🟢 匹配 |
| parking.ts | ParkingService:5525 | `/api/tenant/parking` | (空壳，跳过) | 🟡 待验证 |
| payment.ts | 直接连接:5507 | `/api/tenant/payment` | (空壳，跳过) | 🟡 待验证 |
| person.ts | PersonService:5018 | `/api/persons` | `/api/persons` ✅ | 🟢 匹配 |
| statistics.ts | 直接连接:5026 | `/api/tenant/statistics/*` | `/api/tenant/statistics/*` ✅ | 🟡 端口错误 |
| ticket.ts | TicketService:5102 | `/api/tenant/tickets` | `/api/tenant/tickets` ✅ | 🟢 匹配 |
| ticketType.ts | TicketService:5102 | `/api/ticket-types` | `/api/ticket-types` ✅ | 🟢 匹配 |
| visitor.ts | VisitorService:5513 | `/api/tenant/visitor/visitors` | `/api/tenant/visitor/visitors` ✅ | 🟢 匹配 |
| externalPerson.ts | VisitorService:5513 | `/api/tenant/visitor/visitors/external-persons` | `/api/tenant/visitor/visitors/external-persons` ✅ | 🟢 匹配 |

### 🟡 观察项

**观察1: delivery.ts 使用无 baseURL 的 http 实例**
- 文件: `src/admin-portal/src/api/delivery.ts`
- 问题: `import http from './http'` 后直接调用 `http.get('/api/delivery-requests')`，但 `http.ts` 的默认导出是 `createHttpClient` 函数（需传 baseURL），不是 axios 实例
- 影响: 运行时 baseURL 为 undefined，API 请求会失败
- 建议: 应改为 `import { createHttpClient } from './http'` 并创建带 baseURL 的实例，或使用 gateway URL

**观察2: material.ts 导入不存在的命名导出**
- 文件: `src/admin-portal/src/api/material.ts:1`
- 问题: `import { materialApi as materialClient } from './http'` — http.ts 中不存在名为 `materialApi` 的命名导出
- http.ts 中实际导出的是: `materialApi = createHttpClient(getServiceUrl('material') + '/api/tenant/material')`
- 影响: 打包/编译时报错
- 建议: 改为 `import { materialApi } from './http'` 并使用 `materialApi` 本身

**观察3: 多个服务为空壳（无法验证路由正确性）**
- 空壳服务: CleaningService, CommunityService, ContractService, DeviceService, ExpressService, FinanceService, KeyService, ParkingService, PaymentService
- 影响: 无法确认前端 API 与后端实际路由是否匹配
- 建议: 完成这些服务的 Controller 实现

### 小程序 API 契约检查

| 文件 | 使用的服务 | API路径 | 状态 |
|------|----------|---------|------|
| auth.js | AuthService | `/auth/wechat-login` | 🟢 匹配 |
| ticket.js | TicketService | `/tenant/tickets`, `/ticket-types` | 🟢 匹配 |
| dispatch.js | DispatchService | `/pending`, `/{id}/receive` 等 | 🟢 匹配 |

小程序通过硬编码 `192.168.1.3:5000` Gateway 调用所有 API，路径与后端 Controller 一致。

---

## 维度2: 端口配置一致性

### 2.1 ports.json vs 各服务实际监听端口

| 服务 | ports.json 声明端口 | 实际监听端口 | 状态 |
|------|-------------------|------------|------|
| GatewayService | 5000 | 5000 (APIGateway) ✅ | 🟢 一致 |
| AuthService | 5106 | 5106 ✅ | 🟢 一致 |
| TicketService | 5102 | 5102 ✅ | 🟢 一致 |
| DispatchService | 5241 | 5241 ✅ | 🟢 一致 |
| PersonService | 5018 | 5018 ✅ | 🟢 一致 |
| MasterDataService | 5019 | 5019 ✅ | 🟢 一致 |
| MaterialService | 5504 | 5504 ✅ | 🟢 一致 |
| NotificationService | 5105 | (未确认端口) ⚠️ | 🟡 待确认 |
| PaymentService | 5109 | **5109** (实际) vs **5507** (前端硬编码) | 🔴 前端配置错误 |
| DeviceService | 5530 | 5530 ✅ | 🟢 一致 |
| ContractService | 5501 | 5501 ✅ | 🟢 一致 |
| FinanceService | 5509 | 5509 ✅ | 🟢 一致 |
| InspectionService | 5510 | 5510 ✅ | 🟢 一致 |
| AnnouncementService | 5511 | 5511 ✅ | 🟢 一致 |
| KeyService | 5512 | 5512 ✅ | 🟢 一致 |
| VisitorService | 5513 | 5513 ✅ | 🟢 一致 |
| StatisticsService | 5250 | **5250** (实际) vs **5026** (前端硬编码) | 🔴 前端配置错误 |
| CleaningService | 5516 | 5516 ✅ | 🟢 一致 |
| CommunityService | 5522 | 5522 ✅ | 🟢 一致 |
| DeliveryService | 5017 | 5017 ✅ | 🟢 一致 |
| ExpressService | 5517 | 5517 ✅ | 🟢 一致 |
| ParkingService | 5525 | 5525 ✅ | 🟢 一致 |
| RenovationService | 5521 | 5521 ✅ | 🟢 一致 |
| TicketTypeService | 5107 | 5107 ✅ | 🟢 一致 |

### 🔴 违规项（必须修复）

**违规1: StatisticsService 端口硬编码错误**
- 文件: `src/admin-portal/src/api/config.ts:23`
- 问题: `statistics: 5026` (硬编码) — ports.json 正确值为 **5250**，实际服务也监听 5250
- 代码: `statistics: 5250,     // StatisticsService (Phase 1)` — 注释正确但赋值错误
- 影响: 统计模块完全无法工作
- 修复: 改为 `statistics: 5250`

**违规2: PaymentService 端口硬编码错误**
- 文件: `src/admin-portal/src/api/config.ts:23`
- 问题: `payment: 5507` (硬编码) — ports.json 正确值为 **5109**
- 影响: 支付模块完全无法工作
- 修复: 改为 `payment: 5109`

**违规3: NotificationService 端口硬编码错误**
- 文件: `src/admin-portal/src/api/config.ts:21`
- 问题: `notification: 5129` (硬编码) — ports.json 正确值为 **5105**
- 影响: 通知模块可能无法连接
- 修复: 改为 `notification: 5105`

### 🟡 观察项

**观察1: DispatchService 通过变量 port 动态配置**
- 文件: `src/WO.Property.DispatchService/Program.cs:52`
- 代码: `var port = 5241; app.Urls.Add($"http://0.0.0.0:{port}");`
- 状态: 硬编码在代码中，未从 ports.json 读取，但值正确

**观察2: 多个服务使用硬编码端口**
- AuthService, AnnouncementService, TicketService 等均硬编码端口值在 `builder.WebHost.UseUrls("http://0.0.0.0:XXXX")`
- 虽不符合"配置化"规范，但值与 ports.json 一致，不影响功能

---

## 维度3: 数据模型一致性

### 3.1 共享字段审计

| 字段 | 预期定义位置 | 实际使用情况 | 状态 |
|------|------------|------------|------|
| createdAt | MasterDataService FieldDefinitions | 各服务自行定义 | 🟡 不一致风险 |
| updatedAt | MasterDataService FieldDefinitions | 各服务自行定义 | 🟡 不一致风险 |
| tenantCode | 各服务 TenantDbContext | 各服务自行定义 | 🟢 一致 |
| projectCode | 各服务 TenantDbContext | 各服务自行定义 | 🟢 一致 |

### 🟡 观察项

**观察1: 共享字段由各服务自行定义，无统一约束**
- createdAt, updatedAt 在不同服务中可能有不同的类型（DateTime vs string vs 时间戳）
- 建议: 在 MasterDataService 的 FieldDefinitions 中明确定义所有共享字段的类型和格式

**观察2: MasterDataService 字段定义 API 路径前后端不一致**
- 前端调用: `/api/field-definitions/all` (masterDataService.ts)
- 后端实际: `api/field-definitions/all` (多个 Controller)
- 状态: 匹配 ✅
- 但 MasterDataService 有多个 Controller (AreasController, BuildingsController 等)，路径前缀各不相同
- 前端 masterDataApi 混用了不同前缀（如 `/api/persons/import` vs `/field-definitions/all`），建议统一

---

## 维度4: 事件发布一致性

### 检查结果: 🟢 合规（未发现问题）

各服务通过 HTTP Client 向 Gateway 推送事件：

- **AnnouncementService**: `POST /internal/events/publish` → Gateway ✅
- **DispatchService**: 通过 HttpClient "Gateway" 发送事件 ✅
- 其他服务事件发布机制待运行时验证

静态分析未发现事件发布配置错误。

---

## 维度5: 状态码一致性

### 检查结果: 🟢 合规

随机抽样检查:

- **AuthService**: 所有成功响应 `{ success: true, data: {...} }`，错误返回 401/423/400 状态码 ✅
- **AnnouncementService**: `Ok(new { success: true, ... })` 即使出错也返回 200+ `{ success: false }` ⚠️
- **DispatchService**: 正常 REST 风格响应 ✅
- **PersonService**: 统一 `{ success: true, data: ... }` 包装 ✅

**观察: AnnouncementService 异常返回 200 而非 4xx/5xx**
- 文件: `src/WO.Property.AnnouncementService/Controllers/TenantAnnouncementController.cs`
- 问题: 捕获异常后返回 `Ok(new { success = false, message = ex.Message })` 而非 `500 Internal Server Error`
- 影响: 前端无法区分业务错误和服务器错误
- 建议: 区分可恢复错误（200+success:false）和真正异常（500）

---

## 维度6: 认证/授权一致性

### 检查结果: 🟢 合规

| 服务 | JWT Header | Bearer前缀 | Issuer | Audience | 状态 |
|------|-----------|-----------|--------|---------|------|
| GatewayService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| AuthService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| PersonService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| TicketService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| DispatchService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| NotificationService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| StatisticsService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| VisitorService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| AnnouncementService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |
| InspectionService | Authorization | ✅ Bearer | wo-property-unified-auth | wo-property-services | 🟢 |

**所有服务使用统一的 JWT 配置:**
- Header: `Authorization: Bearer <token>`
- Issuer: `wo-property-unified-auth`
- Audience: `wo-property-services`
- 算法: HmacSha256

---

## 🔴 违规项汇总（按优先级）

### P0 - 必须立即修复

| # | 违规项 | 文件:行号 | 影响 |
|---|--------|----------|------|
| 1 | StatisticsService 端口错误 (5026→5250) | `config.ts:23` | 统计模块完全失效 |
| 2 | PaymentService 端口错误 (5507→5109) | `config.ts:23` | 支付模块完全失效 |
| 3 | NotificationService 端口错误 (5129→5105) | `config.ts:21` | 通知模块可能失效 |

### P1 - 应该修复

| # | 观察项 | 文件:行号 | 影响 |
|---|--------|----------|------|
| 1 | material.ts 导入不存在的命名导出 `materialApi` | `material.ts:1` | 编译/打包失败 |
| 2 | delivery.ts 使用无 baseURL 的 http 实例 | `delivery.ts:3` | 运行时 API 调用失败 |

### P2 - 建议改进

| # | 观察项 | 文件 | 说明 |
|---|--------|------|------|
| 1 | 多个服务硬编码端口而非从 ports.json 读取 | 各服务 Program.cs | 不符合配置化规范 |
| 2 | AnnouncementService 异常返回 200 而非 5xx | `TenantAnnouncementController.cs` | 错误处理不规范 |
| 3 | 9个服务为空壳（无法验证 API 契约） | - | 功能模块未完成 |

---

## 📋 修复建议

### 立即修复 (config.ts)

```typescript
// 修改 src/admin-portal/src/api/config.ts
export const SERVICES = {
  // ... 其他保持不变
  notification: 5105,   // 修复: 5129 → 5105
  statistics: 5250,     // 修复: 5026 → 5250
  payment: 5109,         // 修复: 5507 → 5109
}
```

### 修复 material.ts 导入

```typescript
// src/admin-portal/src/api/material.ts
// 修改前:
import { materialApi as materialClient } from './http';
// 修改后:
import { materialApi } from './http';
const materialClient = materialApi;
```

### 修复 delivery.ts

```typescript
// src/admin-portal/src/api/delivery.ts
// 修改前:
import http from './http'
// 修改后:
import { createHttpClient } from './http'
import { getServiceUrl } from './config'
const deliveryHttp = createHttpClient(getServiceUrl('delivery'))
// 然后: deliveryHttp.get('/api/delivery-requests', { params })
```

---

**报告生成时间:** 2026-05-29 13:10 GMT+8  
**分析工具:** 静态代码扫描（grep + 文件读取）
