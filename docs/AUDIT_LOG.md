# 审计日志总表

## 2026-05-11 本周审计

### 本周重点模块
- 字段管理系统（FieldManagementView + 等价标准）
- 外来临时人员统一管理（新建原则）

### 执行时间
2026-05-11 09:00

---

## 模块：字段等价标准

| 检查项 | 文档描述 | 实际代码 | 偏差 | 严重度 |
|--------|---------|---------|------|--------|
| equivalenceGroups 命名 | API fieldKey 为蛇形（snake_case） | 代码中部分字段仍用驼峰（如 assigneeName） | 已修复 | 低 |
| 别名展开功能 | 点击标签应展开显示等价别名列表 | 展开功能有 Bug（.has vs .includes） | 已修复 | 低 |
| API 字段匹配 | 等价组字段应与 API 返回的 fieldKey 一致 | 部分字段不匹配（reporterName vs reporter_name） | 已修复 | 中 |

**评级：🟡 观察**

**原因：** 等价标准刚建立，数据层已对齐但前端展示层刚经历多次修复，需要稳定性验证。

**行动项：**
- [ ] 验证字段管理页面别名展开功能稳定运行
- [ ] 补充缺失的等价字段到 equivalenceGroups（如 complainant_name, recipient_name, handlerName）

---

## 模块：外来临时人员统一管理

| 检查项 | 原则要求 | 执行状态 | 严重度 |
|--------|---------|---------|--------|
| external_persons 表 | 新建统一表存放访客/外卖/快递 | ✅ 已建表 | - |
| API 契约 | 各模块只调用 API，不自己建表写数据 | 🔴 未执行 | 高 |
| 前端行为 | 工单模块调用搜索+选择，不直接填 | 🔴 未执行 | 高 |
| 数据库权限 | VisitorService/DeliveryService 有写权限 | 🔴 未配置 | 高 |

**评级：🔴 整改**

**原因：** 原则刚刚建立，尚未开始执行。需要本周完成 Phase 2-5。

**行动项：**
- [ ] 在 VisitorService (5013) 或新建 ExternalPersonService 加 API
- [ ] 工单模块改造：reporter_name/phone → 调用 external-persons API
- [ ] 数据库权限配置：只给 VisitorService/DeliveryService 写权限
- [ ] 编写审计脚本（每周检查业务表是否有冗余人名/电话字段）

---

## 下周待审模块
- PersonService API 契约一致性
- MasterDataService 字段管理 API

---

_记录人：🪽的芦苇_
_更新周期：每周一_
