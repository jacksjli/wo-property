# 前端模块修复文档
**日期：** 2026-05-21  
**审计类型：** 日常修复  
**评级：** 🟡 观察  

---

## 修复清单

### 1. MasterDataService Bug 修复

#### 问题
- AreasController Update 方法中 MySQL DataReader 没有关闭就执行下一个命令
- BuildingsController `AreaId` 字段类型错误（应该是 `Area` string）
- HierarchyController 返回的楼栋数据缺少 `area` 字段

#### 修复
- **10 个 Controller** 的 Update 方法添加 `reader.Close()`
- `BuildingItem.AreaId` → `BuildingItem.Area` (string)
- UPDATE SQL 使用正确的 `Area` 列名
- HierarchyController 添加楼栋 `area` 字段返回

#### 验证
```bash
curl -X PUT .../api/areas/1 -d '{"code":"EAST","name":"东区（修改）",...}'
# 返回: {"success":true,"message":"区域更新成功"}
```

---

### 2. 区域-楼栋-房号层级管理重构 (AreaBuildingView.vue)

#### 问题
- 楼栋管理：编辑/删除函数内联在模板中，无法做复杂校验
- 房号管理：既不能编辑也不能删除
- 楼栋编辑时无法显示实际所属区域

#### 修复
| 函数 | 功能 |
|------|------|
| `openBuildingEdit(building)` | 打开编辑对话框 |
| `handleBuildingDelete(building)` | 删除前检查房号 |
| `openRoomCreate(buildingId)` | 打开新增房号 |
| `openRoomEdit(room)` | 打开编辑房号 |
| `handleRoomDelete(room)` | 删除房号（带确认） |

#### 验证
- 楼栋编辑：所属区域正确显示 `building.area`
- 房号编辑/删除：正常工作
- 删除校验：有房号的楼栋无法删除

---

### 3. 工种管理模块 (JobTypeList.vue)

#### 问题
- 分类下拉是硬编码的 `['维修类', '安保类', ...]`
- 启用/停用状态无法保存（isActive ↔ status 不匹配）
- 表格状态列使用 `row.isActive`（undefined），永远显示"停用"

#### 修复
1. **分类数据源：** 从工单类型 API 获取
   ```javascript
   const loadCategories = async () => {
     const r = await ticketTypeApi.getAll()
     categories.value = r.data.map(t => t.name)
   }
   ```

2. **状态保存：** `isActive` → `status: 'Active' / 'Inactive'`

3. **表格显示：** `row.status === 'Active'` 替代 `row.isActive`

#### 验证
```bash
curl -X PUT .../api/job-types/9 -d '{"isActive":false}'
# 数据库 status 更新为 "Inactive" ✅
```

---

### 4. 工单类型模块 (TicketTypeList.vue + ticketType.ts)

#### 问题
- add/update/delete/toggle 函数只保存到 localStorage
- 页面刷新后数据丢失
- `convertFromApi` 忽略 API 返回的 icon 和 color，使用硬编码 Map

#### 修复
| 函数 | 修改 |
|------|------|
| `addTicketType` | 调用 `ticketTypeApi.create()` |
| `updateTicketType` | 调用 `ticketTypeApi.update()` |
| `deleteTicketType` | 调用 `ticketTypeApi.delete()` |
| `toggleTicketTypeStatus` | 调用 API 更新状态 |

**convertFromApi 简化：**
```javascript
// 修复后 - 使用 API 返回值
return {
  id: apiType.id,
  name: apiType.name,
  icon: apiType.icon || 'Document',   // ✅ 使用 API 返回值
  color: apiType.color || '#909399', // ✅ 使用 API 返回值
  status: apiType.status === 'Active' ? 'Active' : 'Inactive'
}
```

#### 验证
```bash
# 创建 → API 保存 → 刷新后数据存在 ✅
# 修改颜色 → 刷新后颜色正确显示 ✅
```

---

## API 变更摘要

### MasterDataService (5019)
- `PUT /api/buildings/{id}` - 添加 Area 字段更新
- `PUT /api/hierarchy/rooms/{id}` - **新增** 更新房号
- `GET /api/hierarchy/area-buildings` - 返回楼栋的 area 字段

### TicketService (5102)
- 工单类型 CRUD 已实现（之前已确认）

---

## 备份记录

| 时间 | 操作 |
|------|------|
| 2026-05-21 15:54 | 文档创建 + 备份 |

---

## 下次检查关注点

1. **工种分类** - 分类和工单类型的联动是否需要双向同步？
2. **房号编辑** - 编辑房号时当前所属楼栋是否正确显示？
3. **删除校验** - 楼栋删除时检查房号是否生效？

---

_文档生成时间：2026-05-21 15:54 GMT+8_
