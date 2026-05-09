# WO物业管理系统 - 模块开发规范

## 新增/修改模块的完整检查清单

### 1. 前端视图开发
- [ ] 在 `src/views/{module}/` 目录下创建或修改 Vue 组件
- [ ] 确保组件有完整的 `<template>` 和 `<script>` 结构（不能是空文件或只有注释）
- [ ] 使用存在的 Element Plus 图标（参考 https://element-plus.org/zh-CN/component/icon.html）
- [ ] 检查所有 import 的组件、store、工具是否正确导出

### 2. 路由配置
- [ ] 在 `src/router/index.ts` 中添加路由
```typescript
{
  path: '/{path}',
  name: '{Name}',
  component: () => import('../views/{path}/{Component}.vue'),
  meta: { title: '{中文名称}' }
}
```

### 3. 模块注册 (project.ts) - ⚠️ 容易遗漏
- [ ] 在 `allModules` 数组中添加模块定义
```typescript
{ name: '{中文名称}', icon: '{Icon}', key: '{key}', path: '/{path}' }
```
- [ ] 在对应项目的 `modules` 数组中添加模块名称（两个项目都要添加！）
```typescript
// 项目1
modules: [..., '{中文名称}']
// 项目2
modules: [..., '{中文名称}']
```

### 4. 字段配置 (可选)
- [ ] 如需可配置字段，在 `src/stores/fieldConfig.ts` 中添加默认字段

### 5. Store/类型定义
- [ ] 确保所有导出的函数、变量、类型在 store 文件中正确定义
- [ ] 组件 import 的内容必须与 store 导出的名称完全一致

### 6. 验证步骤（必须全部执行）
- [ ] 运行 `npm run build` 检查编译错误（不能有 ERROR）
- [ ] 重启前端开发服务器 `npm run dev`
- [ ] 检查浏览器控制台是否有错误
- [ ] 进入项目，确认模块菜单显示
- [ ] 点击模块，确认页面正常显示

## 模型使用规则

### 绝对规定 (2026-04-22 确立)
**规则：** 除非用户明确要求使用其他模型，所有应用的大模型必须是 minimax，决不允许有例外
**性质：** 这是绝对规定，没有任何例外情况
**优先级：** 此规则优先于所有其他配置、默认设置和系统偏好
**适用范围：** 所有应用、编程、代码生成、调试、系统开发等一切技术工作
**执行要求：** 必须严格遵守，不得以任何理由使用其他模型

## 经验总结 (2026-04-22)

### 问题1：空文件导致构建失败
**现象：** `DispatchList.vue` 是空文件，只有注释
**影响：** 整个应用无法构建
**教训：** 新增路由对应的组件必须是完整有效的 Vue 组件

### 问题2：图标不存在
**现象：** 使用了 `ToggleOn`/`ToggleOff`/`Car` 等不存在的图标
**影响：** 构建失败
**教训：** Element Plus 图标有限，使用前先确认存在，推荐用 `Check`/`Close`/`Van` 等常见图标

### 问题3：Store 缺少导出
**现象：** 组件 import 了 `timeoutColorLabels`/`getAllTimeoutRules` 等，但 store 未导出
**影响：** 构建失败
**教训：** 每添加一个组件的 import，必须同步检查 store 是否正确导出

### 问题4：模块未加入项目列表
**现象：** 路由和 allModules 都配置了，但菜单不显示
**影响：** 模块无法访问
**教训：** 必须同时在 `projects[].modules` 中添加模块名称，两个项目都要添加

### 问题5：构建成功不等于功能正常
**现象：** dev 模式运行，但 build 失败
**影响：** 应用实际无法正常使用
**教训：** 每次修改后必须运行 `npm run build` 确认无错误

### 问题6：字段配置不显示（2026-04-22 新增）
**现象：** 字段配置对话框显示"暂无字段配置"，但实际有字段数据
**原因：** 使用了 `getModuleFields` 而非 `getActiveFields`，或字段状态为 'Inactive'
**教训：** 必须使用 `getActiveFields` 获取启用的字段，确保字段状态为 'Active'

### 问题7：字段配置代码结构不一致
**现象：** 不同模块的字段配置代码结构不同，导致维护困难
**教训：** 所有模块必须使用统一的代码结构

## 字段配置模块开发规范（2026-04-22确立）

### 一、代码结构规范（必须严格遵守）

#### 1. 导入语句
```typescript
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
```

#### 2. 组件引用和函数定义
```typescript
// 权限验证
const { verifyAdminPassword } = usePermission()

// 字段配置对话框引用（必须使用 ref）
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段（必须使用 getActiveFields，不能用 getModuleFields）
const get{Module}Fields = () => getActiveFields('{module}')

// 打开字段配置对话框（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 刷新字段
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}
```

#### 3. 模板中使用
```html
<!-- 按钮 -->
<el-button @click="openFieldConfig">
  <el-icon><Edit /></el-icon> 配置字段
</el-button>

<!-- 字段配置对话框（必须使用 ref，不能用 v-model） -->
<FieldConfigDialog
  ref="fieldDialogRef"
  module="{module}"
  module-name="{模块中文名称}"
  @update="refreshFields"
/>
```

### 二、默认字段配置规范

#### 1. 在 fieldConfig.ts 中定义默认字段
```typescript
// {Module}模块的默认字段配置
const {MODULE}_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: '{module}', name: '字段1', key: 'field1', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120 },
  { id: 2, module: '{module}', name: '字段2', key: 'field2', type: 'select', defaultValue: '', required: false, status: 'Active', width: 100, options: ['选项1', '选项2'] },
  // ...其他字段
]
```

#### 2. 初始化逻辑（强制重置）
```typescript
// 强制重置字段配置（确保所有字段状态为 Active）
const forceResetConfig = (): Record<string, FieldConfig[]> => {
  const loadedConfigs = loadFromStorage()
  
  // 强制设置模块配置
  loadedConfigs.{module} = {MODULE}_DEFAULT_FIELDS
  
  // 立即保存
  saveToStorage(loadedConfigs)
  
  return loadedConfigs
}

const fieldConfigs = ref<Record<string, FieldConfig[]>>(forceResetConfig())
```

### 三、字段类型说明
- **text**：文本输入
- **number**：数字输入
- **date**：日期选择
- **select**：下拉选择（需要提供 options）
- **textarea**：多行文本

### 四、必记要点

1. **必须使用 `getActiveFields`**：不能使用 `getModuleFields`
2. **必须使用 `ref` 控制对话框**：不能使用 `v-model`
3. **必须使用 `open()` 方法**：对话框有 open 方法
4. **必须使用 `@update` 事件**：对话框内容变化时刷新
5. **必须强制设置 `status: 'Active'`**：所有默认字段必须启用
6. **必须立即保存到 localStorage**：确保数据持久化

### 五、验证步骤

1. [ ] 运行 `npm run build` 检查编译错误
2. [ ] 重启前端开发服务器
3. [ ] 进入对应模块，点击"配置字段"按钮
4. [ ] 验证显示所有默认字段
5. [ ] 验证可以拖拽排序
6. [ ] 验证可以修改字段
7. [ ] 验证可以删除字段（包括必填字段）
8. [ ] 验证字段配置正确保存和加载
