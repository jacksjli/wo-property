# WO 物业管理软件 - 项目架构分析报告

**生成时间**: 2026-05-21 08:10
**调查范围**: 前后端项目列表、项目数据来源、数据库架构

---

## 一、现状发现的问题

### ⚠️ 重大发现：前端项目列表存在两套数据源

#### 1. stores/project.ts 中的 mock 数据（当前实际使用）

```typescript
const projects = ref<any[]>([
  {
    id: 1,
    name: 'WO物业管理系统',
    code: 'WO-PMS',
    status: 'Active',
    modules: ['工单管理', '设备管理', '物料管理', ...]
  },
  {
    id: 2,
    name: '小区住户管理',
    code: 'RESIDENT',
    status: 'Active',
    modules: ['住户管理', '车位管理', ...]
  }
])
```

**问题**: 这不是从后端获取的，而是写死在代码里的 mock 数据！

#### 2. ProjectList.vue 中调用后端 API

```typescript
// 第 204 行
const response = await fetch('http://localhost:5016/api/projects', {
  method: 'POST',  // 创建新项目时调用
  ...
})
```

**问题**: 
- 仅在创建新项目时调用 `POST /api/projects`
- **从未调用** `GET /api/projects` 获取项目列表
- 项目列表使用的是 `stores/project.ts` 中的 mock 数据

---

## 二、项目数据结构

### 1. 数据库层面 (project_center.projects)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | bigint | 主键 |
| code | varchar(50) | 项目代码（唯一） |
| name | varchar(100) | 项目名称 |
| database_name | varchar(50) | 对应的数据库名（唯一） |
| status | enum | active/inactive/archived |
| config | json | 配置信息 |
| description | text | 描述 |
| address | varchar(200) | 地址 |
| contact_phone | varchar(20) | 联系电话 |
| created_at | datetime | 创建时间 |
| updated_at | datetime | 更新时间 |

### 2. 项目数据库映射

| code | name | database_name | 状态 |
|------|------|--------------|------|
| jinxiu | 锦绣花园 | project_jinxiu | active |
| yanguang | 阳光小区 | project_yanguang | active |
| xingfuli | 幸福里 | project_xingfuli | active |
| test001 | 测试项目 | project_test001 | active |
| test002 | 测试项目2 | project_test002 | active |
| demo | 演示项目 | project_demo | active |
| newproject | 新建项目 | project_newproject | active |

### 3. 存在的数据库

```
project_center     # 中心数据库
project_jinxiu     # 锦绣花园数据
project_yanguang   # 阳光小区数据
project_yangguang  # 错误: 多了个 u
project_xingfuli   # 幸福里数据
project_test001    # 测试项目
project_demo       # 演示项目
project_newproject # 新建项目
```

---

## 三、前后端数据流分析

### 当前数据流（有问题）

```
┌─────────────────────────────────────────────────────────────┐
│  stores/project.ts                                          │
│  ├── projects = [mock数据]  ← 直接写死，不从后端获取         │
│  ├── allModules = [28个模块] ← 也写死                       │
│  └── currentProject = null                                   │
└─────────────────────────────────────────────────────────────┘
                              ↑
                              │ 从不使用
                              │
                    ┌─────────┴──────────┐
                    │  CenterService     │
                    │  GET /api/projects │
                    │  (被忽略)          │
                    └─────────┬──────────┘
                              ↓
                    ┌─────────────────┐
                    │ project_center  │
                    │ projects 表     │
                    └─────────────────┘
```

### 创建项目时的数据流

```
ProjectList.vue
    ↓ POST /api/projects (创建时调用)
CenterService (5016)
    ↓
ProjectDatabaseService (异步创建)
    ↓
创建新数据库 project_xxx
```

---

## 四、X-Project 数据库隔离机制

### 已实现但存在问题的机制

1. **TenantConfigLoader** 读取 `project-mapping.json`
2. **TenantRoutingMiddleware** 解析 `X-Project` Header
3. **TenantDbFactory** 切换数据库连接

### 配置文件存在不一致

| 来源 | 项目代码 | 数据库名 |
|------|---------|---------|
| project_center | yanguang | project_yanguang |
| project-mapping.json | yangguang | project_yangguang |
| 实际数据库 | - | project_yanguang |

**问题**: 多了个 `project_yangguang` 数据库，是错误的配置

---

## 五、模块配置

前端项目有 28 个模块可选：

```
工单管理, 设备管理, 物料管理, 合同管理, 财务管理,
巡检管理, 钥匙管理, 访客管理, 消息管理, 统计分析,
住户管理, 车位管理, 缴费管理, 人员管理, 派单规则,
超时设置, 工单类型, 项目跟踪, 权限控制, 清洁管理,
社区管理, 配送管理, 快递管理, 装修管理, 项目配置,
字段管理, 部门管理, 大区省市区, 区域·楼栋·房号,
供应商管理, 设备类型
```

---

## 六、问题汇总

| # | 问题 | 严重程度 | 说明 |
|---|------|---------|------|
| 1 | 项目列表使用 mock 数据 | 🔴 高 | 前端不从后端获取项目列表 |
| 2 | yanguang/yangguang 拼写不一致 | 🟡 中 | 配置文件中不一致 |
| 3 | 项目列表不显示真实项目 | 🔴 高 | 用户看到的是 mock 项目名 |
| 4 | 无项目删除功能 | 🟡 中 | 只能创建，不能删除 |
| 5 | 项目模块配置未被使用 | 🟡 中 | 后端不使用 modules 字段 |

---

## 七、需要确认的问题

1. **项目数据应该从哪里来？**
   - 后端 `project_center.projects` 表？
   - 还是前端配置？

2. **锦绣花园、阳光小区等是真实项目还是演示项目？**
   - 如果是演示项目，为什么配置这么复杂？
   - 如果是真实项目，为什么用 mock 数据？

3. **是否需要支持多租户（每个项目完全隔离）？**
   - 当前架构支持，但配置有问题

4. **虹桥机场、欢朋喜来登这两个项目在哪里配置？**
   - 目前在 project_center 中没有找到

---

## 八、建议的架构（待确认）

```
┌─────────────────────────────────────────────────────────┐
│  admin-portal (5173)                                    │
│  ├── 登录 → CenterService (5016)                        │
│  ├── 获取项目列表 → GET /api/projects                   │
│  ├── 选择项目 → 保存到 currentProject + localStorage    │
│  └── 业务操作 → 带上 X-Project Header                    │
└─────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────┐
│  CenterService (5016)                                  │
│  ├── 认证 (JWT)                                         │
│  ├── 项目管理 (projects 表)                             │
│  └── 不处理业务数据                                     │
└─────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────┐
│  业务服务 (Ticket/Delivery/...)                         │
│  ├── 读取 X-Project Header                              │
│  ├── TenantDbFactory 切换数据库                         │
│  └── 每个项目独立的数据库                               │
└─────────────────────────────────────────────────────────┘
```

---

**下一步**: 请确认以上分析是否正确，特别是：
1. 项目数据应该从后端获取还是前端配置？
2. 锦绣花园等项目的定位是什么？
3. 虹桥机场、欢朋喜来登是否需要加入系统？