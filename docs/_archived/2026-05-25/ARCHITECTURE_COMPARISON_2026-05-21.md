# 架构对比：用户期望 vs 实际架构

**生成时间**: 2026-05-21 08:22

---

## 一、用户期望的架构

### 层级结构

```
物业公司 (Property Company)
├── 项目 A (Project) - 虹桥机场
│   ├── 模块: 工单, 巡检, 设备, 钥匙, 访客, ...
│   └── 独立数据库: project_hongqiao
├── 项目 B (Project) - 小区住户管理
│   ├── 模块: 住户, 车位, 缴费, 人员, ...
│   └── 独立数据库: project_residential
├── 项目 C (Project) - 商铺管理
│   ├── 模块: 合同, 财务, 缴费, ...
│   └── 独立数据库: project_shop
└── 项目 D (Project) - 酒店管理
    ├── 模块: 配送, 快递, 清洁, 社区, ...
    └── 独立数据库: project_hotel
```

**特点**:
- 一个物业公司可以管理多个不同类型的项目
- 每个项目独立数据库（数据隔离）
- 每个项目按需选择模块（灵活配置）

---

## 二、当前架构

### 数据库层面 (project_center)

```sql
projects 表:
- id, code, name, database_name, status, config, ...
- code: 'jinxiu', 'yanguang', 'xingfuli', ...
- database_name: 'project_jinxiu', 'project_yanguang', ...
```

**问题**: 
- projects 表是**扁平结构**，没有"上级公司"概念
- 一个 project 直接对应一个数据库
- 没有模块选择的概念（modules 字段写了但没用）

### 前端层面

**stores/project.ts**:
```typescript
const projects = ref([
  { id: 1, name: 'WO物业管理系统', code: 'WO-PMS', modules: [...] },
  { id: 2, name: '小区住户管理', code: 'RESIDENT', modules: [...] }
])
```
**问题**: 
- 这是 mock 数据，写死在代码里
- 不是从后端获取
- 不显示真实项目（锦绣花园等）

---

## 三、对比总结

| 维度 | 用户期望 | 当前架构 | 差距 |
|------|---------|---------|------|
| **层级** | 物业公司 → 项目 | 只有项目（扁平） | ❌ 缺少上级概念 |
| **项目来源** | 从后端获取 | mock 数据 | ❌ 前端写死 |
| **模块配置** | 每个项目可选不同模块 | modules 字段未使用 | ❌ 配置被忽略 |
| **数据库** | 每个项目独立数据库 | 每个项目独立数据库 | ✅ 已实现 |
| **数据隔离** | X-Project Header 路由 | X-Project Header 路由 | ✅ 已实现 |

---

## 四、需要补充的设计

### 1. 中心数据库需要新增表

```sql
-- 物业公司表（新增）
CREATE TABLE companies (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,        -- 如：WO物业管理公司
    logo VARCHAR(200),                 -- Logo URL
    status ENUM('active','inactive') DEFAULT 'active',
    config JSON,                       -- 公司配置
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 修改 projects 表，添加外键
ALTER TABLE projects ADD COLUMN company_id BIGINT;
ALTER TABLE projects ADD FOREIGN KEY (company_id) REFERENCES companies(id);
```

### 2. 前端需要修改

**stores/project.ts**:
```typescript
// 修改后的数据结构
const currentCompany = ref<any>(null)  // 当前物业公司
const projects = ref<any[]>([])         // 当前公司下的项目

// 加载公司及其项目
const loadCompanyProjects = async (companyId: number) => {
  // 1. 获取公司信息
  // 2. 获取公司下的项目列表
  // 3. 加载模块配置
}
```

### 3. API 需要调整

| API | 当前 | 需要改成 |
|-----|------|---------|
| GET /api/projects | 获取所有项目 | 改为 GET /api/companies/{id}/projects |
| POST /api/projects | 创建项目 | 改为 POST /api/companies/{id}/projects |
| GET /api/companies | 新增 | 获取公司列表 |
| POST /api/companies | 新增 | 创建公司 |

---

## 五、当前已实现的能力（可复用）

| 能力 | 说明 |
|------|------|
| X-Project 路由 | ✅ 中间件已实现 |
| 数据库隔离 | ✅ 每个项目独立数据库 |
| 模块定义 | ✅ 28个模块已定义 |
| 项目创建 | ✅ ProjectDatabaseService 已实现 |

---

## 六、建议的改造路径

### 阶段 1: 数据结构扩展
1. 新增 companies 表
2. 修改 projects 表添加 company_id
3. 更新 CenterService API

### 阶段 2: 前端改造
1. 创建公司选择器
2. 修改 ProjectList 获取公司下的项目
3. 实现模块配置功能

### 阶段 3: 验证测试
1. 创建测试公司
2. 添加测试项目
3. 验证数据隔离

---

## 七、问题确认

1. **物业公司需要几个？**
   - 一个公司管多个项目？
   - 还是多个公司（每个公司管自己的项目）？

2. **项目类型有哪些？**
   - 机场、校区、住宅、酒店、商场...？

3. **模块配置需要多细？**
   - 每个项目选 28 个模块中的几个？
   - 还是每个项目可以定制自己的模块？

4. **数据共享需求？**
   - 部门、工种等是否所有项目共享？
   - 还是每个项目完全独立？