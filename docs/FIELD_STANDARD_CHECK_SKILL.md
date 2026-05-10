# SKILL.md - WO-Property 字段标准化规范

## 核心规则（强制）

### 规则1：新模块必须注册
```bash
# 每次新增模块后，运行检查
python3 scripts/field_standard_check.py --fix-missing
```
34个标准模块必须在 FieldDefinitions + ModuleFields 中存在。

### 规则2：等价映射完整性
```bash
python3 scripts/field_standard_check.py --full-report
```
新增字段时，自动提示是否需要加入等价映射组。

### 规则3：MySQL 必须用 utf8mb4
连接串必须包含 `CharSet=utf8mb4;Pooling=false`。

### 规则4：OwnerModule 配置
- 业务输入模块（ticket/visitor/resident 等）→ IsEditable=1
- 基础数据模块（region/area/building 等）→ IsEditable=0

---

## 自动执行清单

### ✅ 已实现自动化

| 检查项 | 执行方式 | 自动化程度 |
|--------|----------|------------|
| 新模块注册 | `field_standard_check.py --fix-missing` | **全自动** |
| OwnerModule 配置 | `field_standard_check.py --full-report` | **全自动** |
| IsEditable 默认值 | 随 ModuleFields 自动设置 | **全自动** |
| 等价映射完整性 | `field_standard_check.py --full-report` | **半自动**（需人工确认分组）|
| MySQL charset 检查 | 构建时检查 | 可集成到 CI |
| 字段名≠列名检查 | 构建时检查 | 可集成到 CI |

---

## 使用场景

### 场景1：新服务上线前
```bash
# 1. 检查所有模块配置
python3 scripts/field_standard_check.py --full-report

# 2. 有缺失则自动补全
python3 scripts/field_standard_check.py --fix-missing
```

### 场景2：新增字段
```bash
# 运行报告，查看等价映射提示
python3 scripts/field_standard_check.py --full-report

# 手动决定等价组后，更新 FieldDefinitionEquivalents
```

### 场景3：集成到 CI/CD
```bash
# scripts/cicd/ 中加入
python3 scripts/field_standard_check.py --full-report || exit 1
```

---

## 数据库结构

### FieldDefinitions
- 标准字段定义，所有模块共享
- IsShared=1 表示全局共享字段

### ModuleFields
- Per-module 配置（visibility / alias / owner / editable）
- **OwnerModule** = 谁能写这条字段
- **IsEditable** = 当前模块是否可输入

### FieldDefinitionEquivalents
- 别名映射（phone_number ← visitorPhone）
- **MasterFieldKey** = 标准名
- **EquivalentFieldKey** = 别名

---

## 关键决策记录

| 日期 | 决策 | 原因 |
|------|------|------|
| 2026-05-10 | phone_number 作为主关联键 | 12个模块共用，最实用 |
| 2026-05-10 | region/area/building 等为基础数据只读 | 用户不在这些模块输入数据 |
| 2026-05-10 | 等价映射用 MasterFieldKey 标识标准名 | 避免歧义，统一主键 |
