# 标准字段管理制度

## 核心原则

**所有标准字段都是全局字段**，只有两种状态：
1. **已有别名** - 已在等价组中配置
2. **暂无别名** - 现在没有，以后可能有

---

## 字段结构

| 层级 | 字段 | 说明 |
|------|------|------|
| **标准字段** | FieldKey（英文） | 全局唯一，如 `phone_number` |
| **别名** | EquivalentFieldKey | 各模块自己的叫法，如 `visitorPhone` |
| **等价组** | MasterFieldKey | 标准字段作为主键 |

---

## 标准字段（MasterFieldKey）完整列表

### 姓名类 - person_name
| 别名 | 来源模块 |
|------|----------|
| name | Personnel |
| visitorName | visitor |
| hostName | visitor |
| assigneeName | ticket |
| reporterName | ticket |
| complainantName | complaints |
| residentName | resident |
| cleanerName | cleaning |
| inspectorName | inspection |
| applicantName | payment/renovation |
| recipientName | delivery |
| handlerName | ticket |

### 电话类 - phone_number
| 别名 | 来源模块 |
|------|----------|
| phone | Personnel |
| visitorPhone | visitor |
| hostPhone | visitor |
| residentPhone | resident |
| contactPhone | ticket |
| reporterPhone | ticket |
| emergencyPhone | Personnel/resident |
| holderPhone | key |
| applicantPhone | payment |
| recipientPhone | delivery |
| complainantPhone | complaints |
| deliveryPhone | delivery |

### 状态类 - status
| 别名 | 来源模块 |
|------|----------|
| handleStatus | ticket/complaints |
| deliveryStatus | delivery |
| paymentStatus | payment |
| ticketTypeStatus | ticketType |

### 联系人 - contact_name
| 别名 | 来源模块 |
|------|----------|
| contactPerson | ticket |
| contactName | ticket |
| emergencyContactName | resident |
| emergency_contact_name | Personnel |

### 楼栋 - building_id
| 别名 | 来源模块 |
|------|----------|
| buildingId | building |
| areaId | area |
| building | keys |

### 暂无别名的标准字段
```
name / phone / email / idCard / remark / createdAt / updatedAt /
department / role / gender / joinDate / emergencyContact /
emergencyPhone / roomNo / floor / location /
type / priority / category / level / unit / cycle /
```

---

## 字段管理三大规则

### 规则1：标准字段只有全局字段
- FieldDefinitions 表中 **所有记录都是全局字段**
- Module = NULL 表示全局标准字段
- 所有标准字段都可以被任何模块引用

### 规则2：别名只能关联，不能新建
- 通过 FieldDefinitionEquivalents 表建立别名关系
- 各业务模块只能定义别名，不能定义标准字段
- 别名必须有对应的 SourceModule

### 规则3：字段定义列表有，等价组关系
- FieldDefinitionList 显示标准名字段
- 每行列出该字段的等价别名数量
- 点击展开查看所有别名

---

## 前端页面职责

| 页面 | 路由 | 职责 |
|------|------|------|
| FieldDefinitionList | /master/field-definition | 新增/删除标准字段 |
| ModuleFieldsView | /master/module-fields | 添加字段 + 定义别名 |
| 业务模块表单 | /tickets 等 | 只读/使用，不能新建 |

---

## 数据库结构

### FieldDefinitions（标准字段）
- 所有字段都是 `IsShared=1`
- Module = NULL 或 = 'master'
- 用于全局引用

### ModuleFields（模块字段配置）
- 某模块引用某标准字段
- alias = 该模块给它的别名
- isEditable = 该模块是否可写

### FieldDefinitionEquivalents（等价别名）
- MasterFieldKey = 标准名
- EquivalentFieldKey = 别名
- SourceModule = 来源模块

---

## 字段管理操作规范

### 新增标准字段（在 FieldDefinitionList 页面）
1. FieldDefinitions 表插入新记录
2. IsShared=1，Module=NULL
3. 等待有人需要时再建立等价别名关系

### 新增别名（在 ModuleFieldsView 页面）
1. 该模块已通过 ModuleFields 引用该标准字段
2. 在 FieldDefinitionEquivalents 中建立别名关系
3. 等价组自动生成

### 删除别名
- 直接从 FieldDefinitionEquivalents 删除
- 标准字段保留，不受影响

### 删除标准字段
- 先检查是否有 ModuleFields 引用
- 再检查是否有别名
- 确认无引用后才能删除
