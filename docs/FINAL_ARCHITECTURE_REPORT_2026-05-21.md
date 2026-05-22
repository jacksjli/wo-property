# WO 物业管理软件 - 最终架构报告

**生成时间**: 2026-05-21 08:34
**版本**: v2.0

---

## 一、业务架构（最终确认）

### 1. 层级结构

```
物业公司（唯一）
├── 项目类型（可配置模板）
│   ├── 工厂园区
│   ├── 高端酒店
│   ├── 交通枢纽
│   ├── 高端住宅
│   ├── 医院后勤
│   ├── 商业综合体
│   ├── 教育系统
│   ├── 企事业单位
│   ├── 市政设施
│   └── （以后可新增）
│
└── 项目实例（具体项目）
    ├── 虹桥机场
    ├── 小区住户管理（住宅）
    ├── 某工厂园区
    ├── 某高端酒店
    └── ...（按需创建）
```

### 2. 核心约束

| 约束 | 说明 |
|------|------|
| 物业公司 | **唯一**，一套软件管一个物业公司 |
| 项目类型 | **可扩展**，以后可新增类型 |
| 项目实例 | **多个**，每个独立数据库 |
| 模块配置 | **按需选择**，与现在一致 |
| 部门数据 | **有默认值**，可按项目修改 |

---

## 二、数据库架构

### 2.1 中心数据库 (project_center)

```sql
-- 1. 物业公司表（唯一实例）
CREATE TABLE company (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,          -- 如：WO物业管理有限公司
    logo VARCHAR(200),
    status ENUM('active') DEFAULT 'active',
    config JSON,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 2. 项目类型表（预定义类型，可扩展）
CREATE TABLE project_types (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,   -- 如: factory, hotel, airport
    name VARCHAR(100) NOT NULL,         -- 如: 工厂园区, 高端酒店
    description TEXT,
    icon VARCHAR(50),                   -- 前端图标
    default_modules JSON,                -- 默认选中的模块
    sort_order INT DEFAULT 0,
    status ENUM('active','inactive') DEFAULT 'active',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 3. 项目表
CREATE TABLE projects (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    company_id BIGINT NOT NULL DEFAULT 1,
    type_id BIGINT,                      -- 关联项目类型
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    database_name VARCHAR(50) NOT NULL UNIQUE,
    status ENUM('active','inactive','archived') DEFAULT 'active',
    config JSON,                        -- 项目特定配置
    description TEXT,
    address VARCHAR(200),
    contact_phone VARCHAR(20),
    area_size DECIMAL(10,2),             -- 面积（平方米）
    staff_count INT,                     -- 员工数量
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES company(id),
    FOREIGN KEY (type_id) REFERENCES project_types(id)
);

-- 4. 用户表（公司员工）
CREATE TABLE users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    company_id BIGINT NOT NULL DEFAULT 1,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(200) NOT NULL,
    display_name VARCHAR(100),
    email VARCHAR(100),
    phone VARCHAR(20),
    role ENUM('admin','manager','staff') DEFAULT 'staff',
    status ENUM('active','inactive') DEFAULT 'active',
    last_login_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES company(id)
);

-- 5. 项目成员（用户-项目关联）
CREATE TABLE project_members (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    project_id BIGINT NOT NULL,
    project_role ENUM('admin','manager','staff') DEFAULT 'staff',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

-- 6. 项目模块配置（每个项目选择了哪些模块）
CREATE TABLE project_modules (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    project_id BIGINT NOT NULL,
    module_key VARCHAR(50) NOT NULL,
    config JSON,                         -- 模块特定配置
    sort_order INT DEFAULT 0,
    status ENUM('active','inactive') DEFAULT 'active',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (project_id) REFERENCES projects(id),
    UNIQUE KEY (project_id, module_key)
);
```

### 2.2 项目数据库 (project_{code})

每个项目有独立数据库，基础表结构：

```sql
-- 基础数据表（可按项目修改）
regions          -- 大区/省市区
areas            -- 区域
buildings        -- 楼栋
rooms            -- 房号
departments      -- 部门（默认值可修改）
job_types        -- 工种（默认值可修改）
personnel        -- 人员
residents         -- 住户

-- 业务表（按模块启用情况创建）
tickets          -- 工单
contracts        -- 合同
materials        -- 物料
devices          -- 设备
...（按需创建）
```

---

## 三、预定义的项目类型

### 3.1 项目类型表（初始化数据）

| code | name | 默认模块 |
|------|------|---------|
| factory | 工厂园区 | 工单, 巡检, 设备, 安全, 环境 |
| hotel | 高端酒店 | 客诉, 配送, 保洁, 设备, 钥匙 |
| airport | 交通枢纽 | 工单, 巡检, 设备, 安保, 清洁 |
| residential | 高端住宅 | 工单, 住户, 车位, 缴费, 访客 |
| hospital | 医院后勤 | 工单, 物资, 设备, 保洁, 运送 |
| mall | 商业综合体 | 工单, 商铺, 缴费, 设备, 客服 |
| school | 教育系统 | 工单, 资产, 设备, 保洁, 安保 |
| enterprise | 企事业单位 | 工单, 资产, 设备, 访客, 会议 |
| municipal | 市政设施 | 工单, 巡检, 设备, 安全, 维修 |
| (custom) | 自定义 | （用户选择模块） |

### 3.2 模块定义（保持现有）

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

## 四、数据共享与隔离策略

### 4.1 默认共享数据（公司级）

| 数据类型 | 共享策略 | 可按项目修改 |
|---------|---------|-------------|
| 部门 (departments) | ✅ 默认共享 | 是，可增删改 |
| 工种 (job_types) | ✅ 默认共享 | 是，可增删改 |
| 区域 (areas) | ✅ 默认共享 | 是，可增删改 |
| 楼栋 (buildings) | ✅ 默认共享 | 是，可增删改 |
| 房号 (rooms) | ✅ 默认共享 | 是，可增删改 |

### 4.2 完全隔离数据（项目级）

| 数据类型 | 隔离策略 | 说明 |
|---------|---------|------|
| 工单 (tickets) | ❌ 完全隔离 | 每个项目独立 |
| 住户 (residents) | ❌ 完全隔离 | 每个项目独立 |
| 设备 (devices) | ❌ 完全隔离 | 每个项目独立 |
| 合同 (contracts) | ❌ 完全隔离 | 每个项目独立 |

### 4.3 X-Project 路由机制

```
请求 Header: X-Project: hongqiao
                          ↓
            TenantRoutingMiddleware
                          ↓
            TenantDbFactory 切换数据库
                          ↓
            project_hongqiao 数据库
```

---

## 五、API 设计

### 5.1 中心服务 API (CenterService: 5016)

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/login | 登录 |
| GET | /api/company | 获取公司信息 |
| PUT | /api/company | 更新公司信息 |
| GET | /api/project-types | 获取项目类型列表 |
| POST | /api/project-types | 创建项目类型 |
| GET | /api/projects | 获取项目列表 |
| POST | /api/projects | 创建项目 |
| GET | /api/projects/{code} | 获取项目详情 |
| PUT | /api/projects/{code} | 更新项目 |
| DELETE | /api/projects/{code} | 删除项目（归档） |
| GET | /api/projects/{code}/modules | 获取项目模块配置 |
| PUT | /api/projects/{code}/modules | 更新项目模块配置 |

### 5.2 业务服务 API

每个业务服务通过 X-Project Header 路由到对应数据库，API 结构与现有一致。

---

## 六、前端页面结构

```
首页 /dashboard
├── 项目选择器 (项目切换下拉)
├── 项目类型概览
└── 快捷入口

项目管理 /projects
├── 项目列表
│   ├── 项目名称
│   ├── 项目类型
│   ├── 状态
│   └── 操作
├── 创建项目
│   ├── 选择项目类型
│   ├── 配置项目信息
│   └── 选择模块
└── 项目详情/编辑

基础数据管理 /master
├── 部门管理（默认共享，可按项目修改）
├── 工种管理
├── 区域·楼栋·房号
├── 供应商管理
└── 设备类型

业务模块（按项目配置显示）
├── 工单管理
├── 设备管理
├── 物料管理
├── ...（根据项目模块配置显示）
```

---

## 七、初始化数据

### 7.1 项目类型（预置）

```sql
INSERT INTO project_types (code, name, default_modules, sort_order) VALUES
('factory', '工厂园区', '["工单管理","巡检管理","设备管理","安全管理","环境管理"]', 1),
('hotel', '高端酒店', '["客诉管理","配送管理","保洁管理","设备管理","钥匙管理"]', 2),
('airport', '交通枢纽', '["工单管理","巡检管理","设备管理","安保管理","清洁管理"]', 3),
('residential', '高端住宅', '["工单管理","住户管理","车位管理","缴费管理","访客管理"]', 4),
('hospital', '医院后勤', '["工单管理","物资管理","设备管理","保洁管理","运送管理"]', 5),
('mall', '商业综合体', '["工单管理","商铺管理","缴费管理","设备管理","客服管理"]', 6),
('school', '教育系统', '["工单管理","资产管理","设备管理","保洁管理","安保管理"]', 7),
('enterprise', '企事业单位', '["工单管理","资产管理","设备管理","访客管理","会议管理"]', 8),
('municipal', '市政设施', '["工单管理","巡检管理","设备管理","安全管理","维修管理"]', 9);
```

### 7.2 默认部门（公司级）

```sql
INSERT INTO shared_departments (code, name, sort_order) VALUES
('DEPT001', '物业部', 1),
('DEPT002', '工程部', 2),
('DEPT003', '客服部', 3),
('DEPT004', '保安部', 4),
('DEPT005', '保洁部', 5),
('DEPT006', '绿化部', 6),
('DEPT007', '财务部', 7),
('DEPT008', '行政部', 8);
```

### 7.3 默认工种（公司级）

```sql
INSERT INTO shared_job_types (code, name, sort_order) VALUES
('JOB001', '维修工', 1),
('JOB002', '电工', 2),
('JOB003', '水工', 3),
('JOB004', '保安', 4),
('JOB005', '保洁员', 5),
('JOB006', '绿化工', 6),
('JOB007', '客服', 7),
('JOB008', '管理员', 8);
```

---

## 八、部署架构

```
┌─────────────────────────────────────────────────────────┐
│  admin-portal (5173) - Vue3 前端                         │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│  CenterService (5016) - 中心服务                        │
│  ├── 用户认证 (JWT)                                    │
│  ├── 项目管理 (projects 表)                            │
│  ├── 项目类型管理 (project_types 表)                  │
│  └── 公司配置 (company 表)                             │
└─────────────────────────────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ TicketSvc │   │ DeliverySvc│  │ 其他服务 │
    │ (5102)   │   │ (5017)   │   │          │
    └──────────┘   └──────────┘   └──────────┘
          │               │               │
          ▼               ▼               ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ project_  │   │ project_  │   │ project_  │
    │ jinxiu   │   │ hongqiao │   │ residential│
    └──────────┘   └──────────┘   └──────────┘

┌─────────────────────────────────────────────────────────┐
│  MySQL (127.0.0.1:3306)                                 │
│  ├── project_center (中心数据库)                      │
│  │   ├── company                                      │
│  │   ├── project_types                                 │
│  │   ├── projects                                      │
│  │   ├── users                                         │
│  │   └── shared_* (默认共享数据)                       │
│  ├── project_jinxiu (项目数据库)                       │
│  ├── project_hongqiao                                 │
│  ├── project_residential                              │
│  └── ...                                               │
└─────────────────────────────────────────────────────────┘
```

---

## 九、改造计划

### 阶段 1: 数据库扩展

1. 创建 `company` 表（唯一实例）
2. 创建 `project_types` 表
3. 修改 `projects` 表添加 type_id, company_id
4. 创建 `project_modules` 表
5. 初始化项目类型数据

### 阶段 2: 后端改造

1. CenterService 添加项目类型 API
2. 添加公司管理 API
3. 修改项目创建逻辑（关联项目类型）
4. 实现模块配置功能

### 阶段 3: 前端改造

1. 添加项目类型选择器
2. 修改项目列表显示项目类型
3. 实现模块配置界面
4. 添加公司信息页面

---

## 十、确认事项

请确认以下内容：

1. **公司名称**：如 "WO物业管理有限公司"
2. **初始项目**：需要创建哪些示例项目？
3. **默认模块**：每个项目类型默认选哪些模块？
4. **共享数据**：部门、工种等是否作为默认值提供？