<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { masterDataApi, type FieldDefinition, type ModuleField } from '../api/masterDataService'

// 可用模块列表
const modules = [
  { value: 'ticket', label: '工单' },
  { value: 'complaint', label: '投诉' },
  { value: 'visitor', label: '访客' },
  { value: 'inspection', label: '巡检' },
  { value: 'device', label: '设备' },
  { value: 'material', label: '物料' },
  { value: 'contract', label: '合同' },
  { value: 'finance', label: '财务' },
  { value: 'key', label: '钥匙' },
  { value: 'parking', label: '车位' }
]

// 当前选中模块
const currentModule = ref('ticket')

// 数据
const availableFields = ref<FieldDefinition[]>([])
const selectedModuleFields = ref<(ModuleField & { fieldDefinition?: FieldDefinition; ownerModule?: string; isEditable?: boolean; alias?: string | null })[]>([])

// 加载状态
const loadingAvailable = ref(false)
const loadingSelected = ref(false)

// 已选字段 ID 集合
const selectedFieldIds = computed(() => {
  return new Set(selectedModuleFields.value.map(mf => mf.fieldDefinitionId))
})

// 未选择的可用字段
const unselectedFields = computed(() => {
  return availableFields.value.filter(f => !selectedFieldIds.value.has(f.id))
})

// 编辑对话框
const dialogVisible = ref(false)
const editingField = ref<{
  id?: number
  ownerModule: string
  isEditable: boolean
  alias: string
}>({
  ownerModule: 'master',
  isEditable: false,
  alias: ''
})

// 加载可用字段
const loadAvailableFields = async () => {
  loadingAvailable.value = true
  try {
    const res = await masterDataApi.getFieldsByModule(currentModule.value)
    availableFields.value = res.data || []
  } catch (error: any) {
    if (error.response) {
      availableFields.value = []
    } else {
      availableFields.value = []
    }
  } finally {
    loadingAvailable.value = false
  }
}

// 加载已选字段
const loadSelectedFields = async () => {
  loadingSelected.value = true
  try {
    const res = await masterDataApi.getModuleFields(currentModule.value)
    // 关联字段详情
    const mfWithDetails = (res.data || []).map(mf => ({
      ...mf,
      fieldDefinition: availableFields.value.find(f => f.id === mf.fieldDefinitionId)
    }))
    selectedModuleFields.value = mfWithDetails
  } catch (error: any) {
    if (error.response) {
      selectedModuleFields.value = []
    } else {
      selectedModuleFields.value = []
    }
  } finally {
    loadingSelected.value = false
  }
}

// 加载当前模块数据
const loadCurrentModule = async () => {
  await loadAvailableFields()
  await loadSelectedFields()
}

// 添加字段到模块
const addField = async (field: FieldDefinition) => {
  try {
    await masterDataApi.addModuleField(currentModule.value, field.id)
    ElMessage.success(`已添加字段 "${field.displayName}"`)
    await loadSelectedFields()
  } catch (error: any) {
    if (error.response) {
      ElMessage.error('添加失败')
    } else {
      // 模拟成功
      ElMessage.success(`已添加字段 "${field.displayName}"（模拟）`)
      await loadSelectedFields()
    }
  }
}

// 移除字段
const removeField = async (mf: ModuleField & { fieldDefinition?: FieldDefinition }) => {
  try {
    await masterDataApi.removeModuleField(mf.id)
    ElMessage.success(`已移除字段 "${mf.fieldDefinition?.displayName || mf.fieldDefinitionId}"`)
    await loadSelectedFields()
  } catch (error: any) {
    if (error.response) {
      ElMessage.error('移除失败')
    } else {
      ElMessage.success(`已移除字段（模拟）`)
      await loadSelectedFields()
    }
  }
}

// 打开编辑对话框
const openEditDialog = (mf: ModuleField & { fieldDefinition?: FieldDefinition }) => {
  editingField.value = {
    id: mf.id,
    ownerModule: (mf as any).ownerModule || 'master',
    isEditable: (mf as any).isEditable ?? false,
    alias: (mf as any).alias || ''
  }
  dialogVisible.value = true
}

// 关闭编辑对话框
const closeEditDialog = () => {
  dialogVisible.value = false
  editingField.value = { ownerModule: 'master', isEditable: false, alias: '' }
}

// 保存编辑
const saveEdit = async () => {
  if (!editingField.value.id) return
  try {
    await masterDataApi.updateModuleField(editingField.value.id, {
      ownerModule: editingField.value.ownerModule,
      isEditable: editingField.value.isEditable,
      alias: editingField.value.alias || null
    } as any)
    ElMessage.success('保存成功')
    closeEditDialog()
    await loadSelectedFields()
  } catch (error: any) {
    if (!error.response) {
      ElMessage.success('保存成功（模拟）')
      // 更新本地数据
      const idx = selectedModuleFields.value.findIndex(mf => mf.id === editingField.value.id)
      if (idx !== -1) {
        selectedModuleFields.value[idx] = {
          ...selectedModuleFields.value[idx],
          ownerModule: editingField.value.ownerModule,
          isEditable: editingField.value.isEditable,
          alias: editingField.value.alias || null
        }
      }
      closeEditDialog()
    } else {
      ElMessage.error('保存失败')
    }
  }
}

// 上移
const moveUp = (index: number) => {
  if (index === 0) return
  const temp = selectedModuleFields.value[index]
  selectedModuleFields.value[index] = selectedModuleFields.value[index - 1]
  selectedModuleFields.value[index - 1] = temp
}

// 下移
const moveDown = (index: number) => {
  if (index === selectedModuleFields.value.length - 1) return
  const temp = selectedModuleFields.value[index]
  selectedModuleFields.value[index] = selectedModuleFields.value[index + 1]
  selectedModuleFields.value[index + 1] = temp
}

// 字段类型映射
const fieldTypeMap: Record<string, string> = {
  text: '文本',
  number: '数字',
  date: '日期',
  select: '下拉',
  textarea: '多行文本'
}

// 监听模块变化
watch(currentModule, () => {
  loadCurrentModule()
})

// 初始化
onMounted(() => {
  loadCurrentModule()
})
</script>

<template>
  <div class="module-fields-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1>模块字段配置</h1>
      <p>配置各功能模块使用的字段</p>
    </div>

    <!-- 主体布局 -->
    <div class="main-layout">
      <!-- 左侧：模块选择 -->
      <div class="sidebar">
        <el-card class="module-selector">
          <template #header>
            <span>选择模块</span>
          </template>
          <el-scrollbar height="calc(100vh - 240px)">
            <el-menu
              :default-active="currentModule"
              class="module-menu"
              @select="(key: string) => currentModule = key"
            >
              <el-menu-item v-for="m in modules" :key="m.value" :index="m.value">
                {{ m.label }}
              </el-menu-item>
            </el-menu>
          </el-scrollbar>
        </el-card>
      </div>

      <!-- 右侧：字段配置 -->
      <div class="content">
        <el-row :gutter="16">
          <!-- 可选字段列表 -->
          <el-col :span="12">
            <el-card class="fields-card">
              <template #header>
                <div class="card-header">
                  <span>可选字段</span>
                  <el-tag size="small">{{ unselectedFields.length }} 个可选</el-tag>
                </div>
              </template>
              <el-scrollbar height="calc(100vh - 320px)">
                <div v-loading="loadingAvailable" class="field-list">
                  <div
                    v-for="field in unselectedFields"
                    :key="field.id"
                    class="field-item"
                  >
                    <div class="field-info">
                      <div class="field-name">{{ field.displayName }}</div>
                      <div class="field-meta">
                        <el-tag size="small">{{ field.fieldKey }}</el-tag>
                        <el-tag size="small" type="info">{{ fieldTypeMap[field.fieldType] }}</el-tag>
                      </div>
                    </div>
                    <el-button
                      type="primary"
                      size="small"
                      text
                      @click="addField(field)"
                    >
                      添加
                    </el-button>
                  </div>
                  <el-empty v-if="!loadingAvailable && unselectedFields.length === 0" description="无可选字段" />
                </div>
              </el-scrollbar>
            </el-card>
          </el-col>

          <!-- 已选字段列表 -->
          <el-col :span="12">
            <el-card class="fields-card">
              <template #header>
                <div class="card-header">
                  <span>已选字段</span>
                  <el-tag size="small" type="success">{{ selectedModuleFields.length }} 个已配置</el-tag>
                </div>
              </template>
              <el-scrollbar height="calc(100vh - 320px)">
                <div v-loading="loadingSelected" class="field-list">
                  <el-table
                    :data="selectedModuleFields"
                    style="width: 100%"
                    :show-header="true"
                    :row-class-name="'field-table-row'"
                    @row-click="(row: any) => openEditDialog(row)"
                  >
                    <el-table-column prop="fieldDefinition?.displayName" label="字段" min-width="100">
                      <template #default="{ row }">
                        <span class="field-name-cell">{{ row.fieldDefinition?.displayName }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column prop="ownerModule" label="Owner" width="80">
                      <template #default="{ row }">{{ row.ownerModule || '-' }}</template>
                    </el-table-column>
                    <el-table-column prop="isEditable" label="可编辑" width="70" align="center">
                      <template #default="{ row }">
                        <el-tag :type="row.isEditable ? 'success' : 'info'" size="small">
                          {{ row.isEditable ? '是' : '否' }}
                        </el-tag>
                      </template>
                    </el-table-column>
                    <el-table-column prop="alias" label="别名" width="120">
                      <template #default="{ row }">
                        <span :class="{ 'alias-active': row.alias }">
                          {{ row.alias || row.fieldDefinition?.displayName }}
                        </span>
                      </template>
                    </el-table-column>
                    <el-table-column label="操作" width="140" fixed="right">
                      <template #default="{ row, $index }">
                        <div class="table-actions" @click.stop>
                          <el-button size="small" text :disabled="$index === 0" @click="moveUp($index)">↑</el-button>
                          <el-button size="small" text :disabled="$index === selectedModuleFields.length - 1" @click="moveDown($index)">↓</el-button>
                          <el-button size="small" text type="danger" @click="removeField(row)">移除</el-button>
                        </div>
                      </template>
                    </el-table-column>
                  </el-table>
                  <el-empty v-if="!loadingSelected && selectedModuleFields.length === 0" description="请从左侧添加字段" />
                </div>
              </el-scrollbar>
            </el-card>
          </el-col>
        </el-row>
      </div>
    </div>

    <!-- 编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      title="编辑字段配置"
      width="400px"
      @close="closeEditDialog"
    >
      <el-form label-width="80px" label-position="left">
        <el-form-item label="Owner">
          <el-input v-model="editingField.ownerModule" placeholder="master" />
        </el-form-item>
        <el-form-item label="可编辑">
          <el-switch v-model="editingField.isEditable" />
        </el-form-item>
        <el-form-item label="别名">
          <el-input v-model="editingField.alias" placeholder="可选，用于覆盖显示名" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="closeEditDialog">取消</el-button>
        <el-button type="primary" @click="saveEdit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.module-fields-view {
  padding: 0;
}

.page-header {
  margin-bottom: 20px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px 0;
}

.page-header p {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

.main-layout {
  display: grid;
  grid-template-columns: 200px 1fr;
  gap: 16px;
}

.sidebar {
  flex-shrink: 0;
}

.module-selector {
  border-radius: 8px;
}

.module-menu {
  border-right: none;
}

.content {
  min-width: 0;
}

.fields-card {
  border-radius: 8px;
  height: calc(100vh - 200px);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.field-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 4px;
}

.field-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  background: #f9fafb;
  transition: background 0.2s;
}

.field-item:hover {
  background: #f3f4f6;
}

.field-info {
  flex: 1;
  min-width: 0;
}

.field-name {
  font-size: 14px;
  font-weight: 500;
  color: #1f2937;
  margin-bottom: 4px;
}

.field-meta {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}

.field-actions {
  display: flex;
  gap: 4px;
  align-items: center;
}

/* 新增样式 */
:deep(.field-table-row) {
  cursor: pointer;
}

:deep(.field-table-row:hover) {
  background-color: #f5f7fa;
}

.field-name-cell {
  font-weight: 500;
  color: #1f2937;
}

.table-actions {
  display: flex;
  gap: 2px;
  align-items: center;
}

.alias-active {
  color: #67C23A;
  font-weight: bold;
}

.alias-fallback {
  color: #909399;
}
</style>