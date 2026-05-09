# WO-Property 服务状态报告

**更新时间**: 2026-05-09 08:15

## 核心服务状态

| 服务 | 端口 | 状态 | 备注 |
|------|------|------|------|
| API Gateway | 5000 | ✅ 运行中 | 统一入口，所有前端请求经过此端口 |
| MasterDataService | 5019 | ✅ 运行中 | 基础数据管理（楼栋/部门/区域/房间等） |
| PersonService | 5018 | ✅ 运行中 | 人员管理 |
| TicketService | 5002 | ✅ 运行中 | 工单管理 |
| AuthService | 5006 | ✅ 运行中 | 认证服务 |
| admin-portal | 5173 | ✅ 运行中 | Vue3 前端 |

## Gateway 路由配置

### 当前已配置路由

```json
{
  "/api/auth/*"         → AuthService (5006)
  "/api/tickets/*"      → TicketService (5002)
  "/api/master-data/*"  → MasterDataService (5019)
  "/api/persons/*"      → PersonService (5018)
}
```

### 路径转换规则 (PathPattern Transform)

- `/api/master-data/{path}` → `/api/{path}` → MasterDataService
  - 前端: `masterApi.get('/buildings')` → `GET http://localhost:5000/api/master-data/buildings`
  - Gateway 转换为: `GET http://localhost:5019/api/buildings`

## 前端 API 客户端 (http.ts)

```typescript
// 所有服务通过 Gateway
export const masterApi = createHttpClient('http://localhost:5000/api/master-data')
export const ticketApi = createHttpClient('http://localhost:5000/api/tickets')
export const authApi = createHttpClient('http://localhost:5000/api/auth')
export const personApi = createHttpClient('http://localhost:5000/api/persons')
```

## 数据库数据状态

| 表名 | 记录数 | 备注 |
|------|--------|------|
| Buildings | 5 | A栋/B栋/C栋 + 2条测试 |
| Departments | 8 | 工程部/客服部/保安部等 |
| Areas | 5 | 东区/西区/南区/北区/中心区 |
| Rooms | 10 | 分属A/B/C栋 |
| Suppliers | 5 | 设备/保洁/绿化/安保/电梯供应商 |
| DeviceTypes | 8 | 电梯/摄像头/门禁/消防栓等 |
| JobTypes | 8 | 维修/保洁/绿化/安保/电梯维保等 |
| Regions | 12 | 大区/省/市/区层级数据 |

## 已修复的问题

### 2026-05-09

1. **DepartmentList.vue** - API 路径从 `/` 改为 `/departments`
2. **BuildingList.vue** - 表单字段与 API 响应字段对齐:
   - `floorCount` → `totalFloors`
   - `unitCount` → `totalUnits`
   - `remark` → `description`
   - `isActive` → `status`

## Vite 代理配置

```typescript
// vite.config.ts
server: {
  proxy: {
    '/api': { target: 'http://localhost:5000', changeOrigin: true }
  }
}
```

## Docker 服务

Docker 容器当前未运行。如需使用 DispatchService (5003)、MaterialService (5004) 等，需要启动 Docker。

## 已知问题

1. [ ] RegionList.vue 有 `refreshFields` 未定义警告
2. [ ] 前端页面路由 `/region-management` 等需要验证可访问性
3. [ ] 一些 Docker 服务（Dispatch/Material/Notification 等）的 API 还未接入 Gateway