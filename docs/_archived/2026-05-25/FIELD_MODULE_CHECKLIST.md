# 字段管理模块验收检查清单

> ⚠️ 本清单是 WO-Property 项目的核心质量门槛。任何字段管理相关功能上线前必须通过本清单。

---

## 🔴 必要条件（不满足 = 拒绝上线）

### 1. Alias 支持

- [ ] `ModuleFields` 表有 `Alias` 字段（或等效覆盖机制）
- [ ] 前端可以编辑每个字段在当前模块的 Alias
- [ ] API 返回字段时，`Alias` 优先级高于 `FieldDefinition.DisplayName`
- [ ] 未配置 Alias 时，fallback 正确（不报错、不显示空值）

### 2. OwnerModule / IsEditable 权限控制

- [ ] `ModuleFields` 表有 `OwnerModule` 字段
- [ ] `ModuleFields` 表有 `IsEditable` 字段
- [ ] Owner 模块的 IsEditable = true
- [ ] 其他模块的 IsEditable = false（或按业务需要）
- [ ] 前端根据 IsEditable 禁用/启用输入框
- [ ] API 提交时校验 IsEditable，无权限返回 403

### 3. 两层抽象完整性

- [ ] `FieldDefinitions` 表存在，存储全局字段定义
- [ ] `ModuleFields` 表存在，存储模块级配置
- [ ] 字段在模块间隔离（改 A 模块字段不影响 B 模块）

### 3. 字段生命周期

- [ ] 支持新增字段（系统管理员操作）
- [ ] 支持禁用字段（Status = Disabled，不删除）
- [ ] 禁用后该字段在所有模块不可见
- [ ] 字段变更有审计记录（CreatedAt / UpdatedAt / ModifiedBy）

### 4. 字段约束

- [ ] 数据类型定义（string / int / decimal / date / enum / image）
- [ ] 必填/可选控制（IsRequired）
- [ ] 枚举选项（Options字段，或独立表）
- [ ] 默认值（DefaultValue）

### 5. 权限控制

- [ ] 新增字段定义：仅系统管理员
- [ ] 修改字段定义：仅系统管理员
- [ ] 模块字段配置：模块管理员可操作
- [ ] 查看字段定义：所有登录用户

---

## 🟡 建议条件（不满足 = 观察）

- [ ] 支持字段分组（基本信息 / 联系方式 / 财务信息）
- [ ] 支持正则校验（手机号、身份证、邮箱）
- [ ] 支持 Min/Max 范围（数值字段）
- [ ] 前端按组渲染表单，组可折叠

---

## ✅ 验收执行

每次字段管理相关功能开发完成后，由 QA 或架构师执行：

```
1. 读取数据库 Schema（DESCRIBE FieldDefinitions; DESCRIBE ModuleFields;）
2. 验证 Alias 字段存在
3. 调用 API，确认返回数据中 Alias 覆盖 DisplayName
4. 前端编辑 Alias，保存后刷新，确认显示名变化
5. 禁用一个字段，确认所有模块不再显示该字段
6. 尝试删除有历史数据的字段（应被阻止）
```

---

## ❌ 常见错误（必须避免）

| 错误 | 后果 |
|------|------|
| ModuleFields 没有 Alias 字段 | 字段别名无法实现 |
| Alias 写死在 FieldDefinitions | 所有模块共用，无法区分 |
| 前端直接用 DisplayName 不查 ModuleField | Alias 配置不生效 |
| 删除字段而不是禁用 | 历史数据丢失/关联错误 |
| 字段变更没有 CreatedAt/UpdatedAt | 审计线索断裂 |

---

_本清单与 wo-property SKILL.md 配合使用_
_触发场景：新建字段管理模块 / 修改字段相关功能 / Code Review_
---

### 6. 字段等价检查

- [ ] 新模块接入时运行等价检查（查询 FieldDefinitionEquivalents）
- [ ] 等价字段对已写入 FieldDefinitionEquivalents 表（不是只写在文档）
- [ ] Alias 优先级正确：ModuleFields.Alias > FieldDefinition.DisplayName

