# 数据库结构设计方案（草案）

> 基于三个目标：1）34模块全字段统计 2）标准名+别名体系 3）标准模块输入/别名字段只读

---

## 一、现状分析

### 当前元数据规模

| 表 | 记录数 | 说明 |
|---|--------|------|
| FieldDefinitions | 221条 | 30个模块（缺5个模块未配置） |
| ModuleFields | 192条 | 28个模块（缺5个模块未配置） |
| FieldDefinitionEquivalents | 28条 | 5个等价组（phone_number/person_name/contact_name/building_id/status） |

### 当前问题

**问题1：5个模块缺失字段配置**
```
fieldDefinition / projectTracking / notification / statistics / project
→ 这5个模块没有任何字段定义，无法管理
```

**问题2：OwnerModule 只配置了"辅助模块"**
```
目前配置了 OwnerModule 的都是 region/area/department 等基础数据模块
而 visitor/ticket/resident/complaint 等业务表反而没有配置 OwnerModule
→ 缺少对"谁可以输入数据"的控制
```

**问题3：等价格系不完整**
```
person_name 等价组：已有12条，但 ReporterName/AssigneeName/HostName 等未纳入
phone_number 等价组：已有12条，但 ComplainantPhone/RecipientPhone 等未纳入
→ 跨模块关联查询会漏数据
```

---

## 二、目标拆解与设计方案

### 目标1：34模块所有字段全部统计出来

**方案：** 补全缺失的5个模块字段配置

```
FieldDefinitions 表补充：
- fieldDefinition: 字段定义自身管理（字段名/类型/选项）
- notification: 通知记录（模板/渠道/接收人/状态）
- statistics: 统计报表配置（报表类型/维度/指标）
- projectTracking: 项目进度跟踪（项目名/阶段/完成度）
- project: 项目基本信息（项目名/时间/预算/负责人）
```

**数据量预估：**
- 补全后 FieldDefinitions 预计达到 **260+字段**
- 补全后 ModuleFields 预计达到 **230+条**

---

### 目标2：字段名不同但数据来源一致 → 标准名 + 别名

**方案：建立"等价字段三层映射"**

```
Layer 1: 标准名（FieldKey）
         ↓ 由以下模块共享（FieldDefinitionEquivalents 建立映射）
Layer 2: 别名（Alias）- 每个模块可定义自己的别名
         ↓ 最终落地到
Layer 3: 数据库列名（物理列名）
```

**等价组完整清单（补全后）：**

| 等价组 | 标准名 | 别名列表 | 用途 |
|--------|--------|---------|------|
| phone_number | phone | ContactPhone, VisitorPhone, ApplicantPhone, ReporterPhone... | 12个模块 |
| person_name | personName | ReporterName, AssigneeName, HostName, ApplicantName, ResidentName... | 11个模块 |
| contact_name | contactName | EmergencyContactName, HandlerName, CleanerName... | 4个模块 |
| building_id | buildingId | BuildingId, Building, AreaId | 3个模块 |
| status | status | TicketStatus, HandleStatus, PaymentStatus | 3个模块 |

**等价映射表（FieldDefinitionEquivalents）需要新增：**
```sql
-- person_name 等价组需要补全
person_name ← reporter_name
person_name ← assignee_name
person_name ← host_name
person_name ← complainant_name
person_name ← recipient_name

-- phone_number 等价组需要补全
phone_number ← complainant_phone
phone_number ← recipient_phone
phone_number ← delivery_phone
phone_number ← contact_phone
phone_number ← holder_phone
```

---

### 目标3：标准名模块输入数据，别名字段只读

**这是最关键的设计。**

**核心规则：**
1. 每个数据库列只属于一个"OwnerModule"（标准名模块）
2. OwnerModule 可以读写这条数据
3. 其他模块只能引用这条数据（只读）
4. 通过 `ModuleFields.OwnerModule` 字段控制

**举例：**

```
phone_number 字段链：
  FieldDefinitions.FieldKey = "phone_number"  （标准名）
  FieldDefinitionEquivalents: phone_number ← resident_phone, visitor_phone, applicant_phone...
  ModuleFields.OwnerModule = "resident"     （resident 是标准名模块）

resident 表（Phone 列）：
  - ✅ resident 模块：可读写 Phone 列（OwnerModule = resident）
  - ❌ visitor 模块：只读 Phone 列（IsEditable = false）
  - ❌ ticket 模块：只读 Phone 列（IsEditable = false）
  - ❌ renovation 模块：只读 Phone 列（IsEditable = false）
```

---

## 三、推荐数据库结构

### 方案A：纯应用层控制（当前结构扩展）⭐推荐

```
┌──────────────────────────────────────────────────────┐
│  FieldDefinitions (221条)                             │
│  - FieldKey: 标准名                                    │
│  - DisplayName: 默认显示名                              │
│  - DataType / IsRequired / Options / Width            │
└────────────────┬───────────────────────────────────────┘
                 │
┌────────────────▼───────────────────────────────────────┐
│  FieldDefinitionEquivalents (28条，扩展到60+条)           │
│  - MasterFieldKey: 标准名（phone_number）                │
│  - EquivalentFieldKey: 别名（resident_phone/visitor...）  │
│  - SourceModule: 来源模块（resident/visitor/ticket）      │
│  - SourceTable: 数据库表名（Residents/Visitors/Tickets）  │
└────────────────┬───────────────────────────────────────┘
                 │
┌────────────────▼───────────────────────────────────────┐
│  ModuleFields (192条，扩展到230+条)                      │
│  - Module: 当前模块                                      │
│  - FieldDefinitionId → FieldDefinitions.Id              │
│  - OwnerModule: 所属标准名模块（谁可以输入）               │
│  - IsEditable: 当前模块是否可以输入                       │
│  - IsVisible: 当前模块是否显示                           │
│  - Alias: 当前模块的显示名                                │
└────────────────┬───────────────────────────────────────┘
                 │
┌────────────────▼───────────────────────────────────────┐
│  各业务表（物理存储）                                     │
│ Residents(Id, Name, Phone, ...)        ← Owner=resident │
│  Visitors(Id, VisitorName, VisitorPhone, ...) ← Owner=visitor │
│  Tickets(Id, Title, ReporterPhone, ...)    ← Owner=ticket  │
└───────────────────────────────────────────────────────┘
```

**优点：** 不改数据库，改应用层逻辑即可
**控制点：** `IsEditable = (Module == OwnerModule)` 规则在 API 层强制

---

### 方案B：数据库层强制（建立跨模块视图 + INSTEAD OF 触发器）

```
对每个等价字段建立只读视图：
  CREATE VIEW v_resident_phone AS
  SELECT Id, Name, Phone FROM Residents
  UNION ALL
  SELECT Id, Name, Phone FROM Visitors WHERE VisitorPhone IS NOT NULL;

其他模块通过视图查询（只能SELECT，不能UPDATE/INSERT）。
```

**优点：** 数据库层硬性保护，无法绕过
**缺点：** MySQL 触发器能力有限，维护成本高

---

### 方案C：统一中间表 + 冗余存储

```
┌──────────────────────┐
│  CrossModuleData     │  ← 统一存储所有等价字段
│  - Id                │
│  - StandardFieldKey  │  ← 标准名（如 phone_number）
│  - StandardValue     │  ← 标准值（如 13800138001）
│  - SourceModule      │
│  - SourceTable       │
│  - SourceRecordId    │
└──────────────────────┘
```

**优点：** 跨模块查询极快
**缺点：** 数据同步维护成本高，冗余存储浪费空间

---

## 四、推荐方案及实施路径

### 选择：方案A（应用层控制）

**原因：**
1. 当前架构已经是 ModuleFields + FieldDefinitions + FieldDefinitionEquivalents 三层
2. 只需补全数据 + 在 API 层增加 IsEditable 校验
3. 不改数据库结构，风险低
4. 方案B/C 需要重建数据层，工作量巨大

### 实施步骤

**Step 1：补全字段配置（目标1）**
- [ ] fieldDefinition 模块字段配置（预计8字段）
- [ ] notification 模块字段配置（预计6字段）
- [ ] statistics 模块字段配置（预计5字段）
- [ ] projectTracking 模块字段配置（预计8字段）
- [ ] project 模块字段配置（预计10字段）

**Step 2：补全等价映射（目标2）**
- [ ] person_name 等价组补全（+5条）
- [ ] phone_number 等价组补全（+5条）
- [ ] contact_name 等价组补全（+2条）
- [ ] building_id 等价组补全（+1条）

**Step 3：配置 OwnerModule（目标3）**
- [ ] 每个 ModuleFields 记录配置 OwnerModule
- [ ] OwnerModule = 该字段的"标准名模块"
- [ ] API 层强制：`IsEditable = (currentModule == OwnerModule)`

**Step 4：API 层校验**
```csharp
// API 输入校验伪代码
bool CanEditField(string module, string fieldKey) {
    var mf = ModuleFields.First(m => m.Module == module && m.FieldKey == fieldKey);
    return mf.OwnerModule == module;
}
```

---

## 五、数据库结构汇总

| 表名 | 作用 | 现状 |
|------|------|------|
| FieldDefinitions | 全局字段定义（标准名） | 221条，30模块 |
| FieldDefinitionEquivalents | 别名→标准名映射 | 28条，5组 |
| ModuleFields | 模块级字段配置（含OwnerModule） | 192条，28模块 |
| 各业务表 | 物理存储（列名≠FieldKey） | 28张业务表 |

**不需要新建表**，只需补全数据 + 正确配置 OwnerModule。

---

_2026-05-10 草案_
