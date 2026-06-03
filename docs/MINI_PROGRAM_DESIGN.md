# WO 物业管理小程序 - 整体开发方案

> 创建日期：2026-05-23
> 版本：v1.2（2026-05-30 更新）
> 更新内容：WebSocket 实时刷新、时区修复、PM2 进程管理

---

## 📋 概述

WO 物业管理小程序面向三类用户：
1. **业主** - 报修、投诉、查询进度
2. **员工**（Matched/Guest） - 根据身份动态显示 TabBar
3. **管理员** - 工单监控、统计报表

---

## 🔐 登录方式

### v1.1 新增：员工身份登录（姓名+电话）

**登录流程：**
```
用户输入 姓名 + 电话
    ↓
POST /api/auth/login-by-person → AuthService (5106)
    ↓
GET /api/tenant/persons/by-name-phone → PersonService (5018)
    ↓
找到了 → employeeType="Matched"
没找到 → employeeType="Guest"
    ↓
返回 JWT Token + 员工信息
    ↓
前端根据 employeeType 决定 TabBar 显示
```

**两种登录模式：**
| 模式 | 说明 |
|------|------|
| 员工登录（默认） | 姓名+电话，匹配 PersonService 人员库 |
| 微信授权登录 | 微信 OAuth 授权（保留原有逻辑）|

**TabBar 动态规则：**
| 员工类型 | 显示 Tab | 说明 |
|----------|---------|------|
| Matched | 报修 / 管理 / 工程师 | 3 tabs，已在人员库 |
| Guest | 报修 / 管理 | 2 tabs，未在人员库 |
| WeChat | 报修 / 管理 / 工程师 | 3 tabs，微信授权 |

**JWT Token Claims：**
```json
{
  "employeeType": "Matched",    // Matched | Guest
  "tenantCode": "wo_property",
  "personId": 26,
  "name": "张工程-modified",
  "role": "supervisor",
  "specialtyIds": "[1]",
  "areaIds": "[1]",
  "buildingIds": ""
}
```

---

## 👥 用户角色与功能

### 角色定义

| 角色 | 使用场景 | 登录方式 | TabBar |
|------|---------|---------|--------|
| 业主/访客 | 报修、评价进度 | 姓名+电话（Guest） | 报修/管理 |
| 员工 | 接单、处理、完工 | 姓名+电话（Matched） | 报修/管理/工程师 |
| 管理员 | 监控、统计、派单 | 微信授权 | 报修/管理/工程师 |

---

## 📱 页面结构

### TabBar 配置（动态）

```javascript
// Matched 员工：3 tabs
[
  { pagePath: 'pages/owner/index', text: '报修' },
  { pagePath: 'pages/admin/dashboard', text: '管理' },
  { pagePath: 'pages/ticket/list', text: '工程师' }   // 仅 Matched
]

// Guest 访客：2 tabs
[
  { pagePath: 'pages/owner/index', text: '报修' },
  { pagePath: 'pages/admin/dashboard', text: '管理' }
]
```

### 页面清单

| 角色 | 页面 | 文件 | 状态 |
|------|------|------|------|
| **通用** | 登录 | pages/login/index.vue | ✅ 已更新 |
| **业主** | 首页（我的工单） | pages/owner/index.vue | ⏳ 待开发 |
| **业主** | 新建报修 | pages/owner/create.vue | ⏳ 待开发 |
| **业主** | 工单详情 | pages/owner/detail.vue | ⏳ 待开发 |
| **业主** | 完工评价 | pages/owner/rate.vue | ⏳ 待开发 |
| **工程师** | 工单列表 | pages/ticket/list.vue | ✅ 已完成 |
| **工程师** | 工单详情 | pages/ticket/detail.vue | ✅ 已完成 |
| **工程师** | 完工评价 | pages/ticket/rate.vue | ✅ 已完成 |
| **工程师** | 转单申请 | pages/ticket/transfer.vue | ✅ 已完成 |
| **管理员** | 运营看板 | pages/admin/dashboard.vue | ✅ 已完成 |
| **管理员** | 工单管理 | pages/admin/tickets.vue | ✅ 已更新 |
| **管理员** | 超时告警 | pages/admin/alerts.vue | ⏳ 待开发 |
| **通用** | 我的 | pages/mine/index.vue | ✅ 已完成 |

---

## 🔌 API 对接清单

### 身份认证 API（v1.1 新增）

| API | 方法 | 用途 |
|-----|------|------|
| `POST /api/auth/login-by-person` | AuthService | 员工姓名+电话登录 |
| `GET /api/tenant/persons/by-name-phone` | PersonService | 查询匹配人员 |

### 业主端

| API | 方法 | 用途 |
|-----|------|------|
| `POST /api/tenant/tickets` | TicketService | 创建工单 |
| `GET /api/tenant/tickets` | TicketService | 我的工单列表（支持 areaId/buildingId/ticketTypeId 筛选）|
| `GET /api/tenant/tickets/{id}` | TicketService | 工单详情 |
| `POST /api/tenant/tickets/{id}/remind` | TicketService | 催单 |
| `GET /api/ticket-types` | MasterDataService | 工单类型 |
| `GET /api/areas` | MasterDataService | 区域列表 |
| `GET /api/buildings` | MasterDataService | 楼栋列表 |

### 工程师端

| API | 方法 | 用途 |
|-----|------|------|
| `GET /api/tenant/dispatch/pending?personId=X` | DispatchService | 待处理工单（支持 personId 筛选）|
| `POST /api/tenant/dispatch/{id}/receive` | DispatchService | 接单 |
| `POST /api/tenant/dispatch/{id}/complete` | DispatchService | 完工提交 |
| `POST /api/tenant/dispatch/{id}/confirm` | DispatchService | 确认评价 |
| `POST /api/tenant/dispatch/transfer` | DispatchService | 转单申请 |
| `GET /api/tenant/dispatch/rating/stats` | DispatchService | 评价统计 |

### 管理员端

| API | 方法 | 用途 |
|-----|------|------|
| `GET /api/tenant/dispatch/stats` | DispatchService | 工单统计 |
| `GET /api/tenant/dispatch/alerts` | DispatchService | 超时告警列表 |
| `GET /api/tenant/tickets?areaId=X&buildingId=Y&ticketTypeId=Z` | TicketService | 工单列表（多条件筛选）|

---

## 📊 管理页面筛选逻辑（v1.1 更新）

### 工单管理页面 - 筛选器

| 筛选条件 | 必填 | 说明 |
|----------|------|------|
| 区域 (areaId) | ✅ 必选 | 从 MasterDataService 获取区域列表 |
| 楼栋 (buildingId) | ❌ 可选 | 选"全部楼栋"时不传 |
| 工单类型 (ticketTypeId) | ❌ 可选 | 选"全部类型"时不传 |

### 筛选器UI

```
[选择区域 ▼] [全部楼栋 ▼] [全部类型 ▼]
```

- 区域 picker：必选，显示"选择区域"作为默认空白选项
- 楼栋 picker：可选，显示"全部楼栋"作为默认选项
- 工单类型 picker：可选，显示"全部类型"作为默认选项

---

## 🎨 UI 设计规范

### 主题色

| 用途 | 色值 |
|------|------|
| 主色 | #409EFF（蓝色） |
| 成功 | #52C41A（绿色） |
| 警告 | #FAAD14（橙色） |
| 危险 | #F56C6C（红色） |
| 背景 | #F5F5F5（灰白） |

### 字体

| 用途 | 大小 |
|------|------|
| 标题 | 18px |
| 正文 | 14px |
| 辅助 | 12px |

---

## 📁 项目结构

```
src/woa-property-mini/
├── src/
│   ├── api/
│   │   ├── auth.js           # 微信授权登录
│   │   ├── personAuth.js     # ✅ 员工身份登录（v1.1 新增）
│   │   ├── dispatch.js       # 派单 API（支持 personId 筛选）
│   │   ├── ticket.js         # 工单 API（支持多条件筛选）
│   │   └── user.ts           # 用户 API
│   ├── components/
│   │   └── custom-tabbar.vue # ✅ 动态 TabBar（v1.1 新增）
│   ├── pages/
│   │   ├── login/index.vue   # ✅ 更新：员工登录+微信授权切换
│   │   ├── owner/            # 业主端页面
│   │   ├── ticket/           # 工程师端页面
│   │   ├── admin/            # 管理员端页面
│   │   └── mine/             # 通用页面
│   ├── static/               # 静态资源
│   ├── pages.json            # 路由配置
│   ├── App.vue               # 应用入口
│   └── main.js
└── package.json
```

---

## ⚠️ 注意事项

1. **数据权限**：员工只能看派给自己的工单（personId 筛选）
2. **区域筛选**：管理页面必须选择区域（areaId 必填）
3. **图片上传**：需要配置对象存储（OSS/COS）
4. **消息推送**：需要配置微信消息模板
5. **HTTPS**：生产环境必须使用 HTTPS

---

## 📅 开发计划

### 第一期：基础功能

| 阶段 | 功能 | 状态 |
|------|------|------|
| 1.0 | 员工身份登录（姓名+电话） | ✅ 已完成 |
| 1.1 | 工程师端核心功能 | ✅ 已完成 |
| 1.2 | 管理页面多条件筛选 | ✅ 已完成 |

### 第二期：实时刷新（v1.2 新增）

| 阶段 | 功能 | 状态 | 说明 |
|------|------|------|------|
| 2.1 | WebSocket 实时工单刷新 | ✅ 已完成 | 工单创建/状态变更实时推送 |
| 2.2 | 时区修复 | ✅ 已完成 | UTC→北京时间（+8小时） |
| 2.3 | PM2 进程管理 | ✅ 已完成 | 开机自启动 + 崩溃自动重启 |

### 第三期：完善功能

| 阶段 | 功能 | 状态 |
|------|------|------|
| 3.1 | 业主端报修表单 | ⏳ 规划中 |
| 3.2 | 业主端工单查询 | ⏳ 规划中 |
| 3.3 | 统计报表 | ⏳ 规划中 |
| 3.4 | 消息通知 | ⏳ 规划中 |