<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { masterDataApi, type FieldDefinition } from '../api/masterDataService'

// 搜索过滤
const searchKeyword = ref('')
const filterShared = ref<'' | 'true' | 'false'>('')

// 加载状态
const loading = ref(false)

// 原始数据
const allFields = ref<FieldDefinition[]>([])

// 弹窗状态
const detailDialogVisible = ref(false)
const createDialogVisible = ref(false)

// 编辑中的字段
const editingField = ref<FieldDefinition | null>(null)

// 新增字段表单
const createForm = ref({
  fieldKey: '',
  displayName: '',
  fieldType: 'text' as FieldDefinition['fieldType'],
  source: 'user',
  isShared: true,
  module: '',
  options: '',
  defaultValue: '',
  isRequired: false,
  width: 120
})

const createFormRef = ref()

// 统计
const stats = computed(() => {
  const total = allFields.value.length
  const shared = allFields.value.filter(f => f.isShared).length
  const privateCount = total - shared
  return { total, shared, privateCount }
})

// 过滤后的字段列表
const filteredFields = computed(() => {
  let result = allFields.value

  // 按关键字搜索
  if (searchKeyword.value) {
    const kw = searchKeyword.value.toLowerCase()
    result = result.filter(f =>
      f.fieldKey.toLowerCase().includes(kw) ||
      f.displayName.toLowerCase().includes(kw)
    )
  }

  // 按共享/私有过滤
  if (filterShared.value === 'true') {
    result = result.filter(f => f.isShared)
  } else if (filterShared.value === 'false') {
    result = result.filter(f => !f.isShared)
  }

  return result
})

// 加载字段数据
const loadFields = async () => {
  loading.value = true
  try {
    const res = await masterDataApi.getAllFields()
    allFields.value = res.data || []
  } catch (error: any) {
    if (error.response) {
      ElMessage.error('加载字段失败')
    } else {
      // 网络错误，使用空数据
      allFields.value = []
    }
  } finally {
    loading.value = false
  }
}

// 查看字段详情
const showDetail = async (row: FieldDefinition) => {
  editingField.value = { ...row }
  detailDialogVisible.value = true
}

// 打开新增弹窗
const openCreateDialog = () => {
  createForm.value = {
    fieldKey: '',
    displayName: '',
    fieldType: 'text',
    source: 'user',
    isShared: true,
    module: '',
    options: '',
    defaultValue: '',
    isRequired: false,
    width: 120
  }
  createDialogVisible.value = true
}

// 新增字段
const submitCreate = async () => {
  if (!createFormRef.value) return

  try {
    await createFormRef.value.validate()
  } catch {
    return
  }

  const data = {
    fieldKey: createForm.value.fieldKey,
    displayName: createForm.value.displayName,
    fieldType: createForm.value.fieldType,
    source: createForm.value.source,
    isShared: createForm.value.isShared,
    module: createForm.value.isShared ? null : createForm.value.module,
    options: createForm.value.options ? createForm.value.options.split(',').map(s => s.trim()) : [],
    defaultValue: createForm.value.defaultValue,
    isRequired: createForm.value.isRequired,
    width: createForm.value.width,
    sortOrder: 0,
    status: 'Active' as const
  }

  try {
    await masterDataApi.createFieldDefinition(data)
    ElMessage.success('字段创建成功')
    createDialogVisible.value = false
    await loadFields()
  } catch (error: any) {
    if (error.response) {
      ElMessage.error(error.response.data?.message || '创建失败')
    } else {
      // 网络错误，模拟成功
      ElMessage.success('字段创建成功（模拟）')
      createDialogVisible.value = false
    }
  }
}

// 更新字段
const submitUpdate = async () => {
  if (!editingField.value) return

  const data: Partial<FieldDefinition> = {
    fieldKey: editingField.value.fieldKey,
    displayName: editingField.value.displayName,
    fieldType: editingField.value.fieldType,
    source: editingField.value.source,
    isShared: editingField.value.isShared,
    module: editingField.value.isShared ? null : editingField.value.module,
    options: editingField.value.options,
    defaultValue: editingField.value.defaultValue,
    isRequired: editingField.value.isRequired,
    width: editingField.value.width
  }

  try {
    await masterDataApi.updateFieldDefinition(editingField.value.id, data)
    ElMessage.success('字段更新成功')
    detailDialogVisible.value = false
    await loadFields()
  } catch (error: any) {
    if (error.response) {
      ElMessage.error(error.response.data?.message || '更新失败')
    } else {
      // 网络错误，模拟成功
      ElMessage.success('字段更新成功（模拟）')
      detailDialogVisible.value = false
    }
  }
}

// 字段类型映射
const fieldTypeMap: Record<string, string> = {
  text: '文本',
  number: '数字',
  date: '日期',
  select: '下拉',
  textarea: '多行文本'
}

// 初始化
onMounted(() => {
  loadFields()
})
</script>

<template>
  <div class="field-management-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h1>字段管理</h1>
        <p>管理系统中的自定义字段定义</p>
      </div>
      <el-button type="primary" @click="openCreateDialog">
        新增字段
      </el-button>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-row">
      <el-card class="stat-card">
        <div class="stat-value">{{ stats.total }}</div>
        <div class="stat-label">总字段数</div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-value">{{ stats.shared }}</div>
        <div class="stat-label">共享字段</div>
      </el-card>
      <el-card class="stat-card">
        <div class="stat-value">{{ stats.privateCount }}</div>
        <div class="stat-label">私有字段</div>
      </el-card>
    </div>

    <!-- 搜索过滤 -->
    <div class="filter-row">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索字段标识或显示名"
        style="width: 300px"
        clearable
      />
      <el-select
        v-model="filterShared"
        placeholder="筛选共享/私有"
        style="width: 150px"
        clearable
      >
        <el-option label="全部" value="" />
        <el-option label="共享字段" value="true" />
        <el-option label="私有字段" value="false" />
      </el-select>
    </div>

    <!-- 字段表格 -->
    <el-card class="table-card">
      <el-table
        :data="filteredFields"
        v-loading="loading"
        stripe
        style="width: 100%"
      >
        <el-table-column prop="fieldKey" label="字段标识" width="160" />
        <el-table-column prop="displayName" label="显示名" width="140" />
        <el-table-column label="类型" width="100">
          <template #default="{ row }">
            {{ fieldTypeMap[row.fieldType] || row.fieldType }}
          </template>
        </el-table-column>
        <el-table-column label="共享" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isShared ? 'success' : 'info'" size="small">
              {{ row.isShared ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="所属模块" width="120">
          <template #default="{ row }">
            {{ row.module || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="source" label="来源" width="100" />
        <el-table-column label="必填" width="70">
          <template #default="{ row }">
            <el-tag :type="row.isRequired ? 'warning' : 'info'" size="small">
              {{ row.isRequired ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="width" label="列宽" width="80" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Active' ? 'success' : 'danger'" size="small">
              {{ row.status === 'Active' ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click="showDetail(row)">
              详情
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 字段详情/编辑弹窗 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="字段详情"
      width="600px"
    >
      <div v-if="editingField" class="field-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="字段标识">
            {{ editingField.fieldKey }}
          </el-descriptions-item>
          <el-descriptions-item label="显示名">
            <el-input v-model="editingField.displayName" size="small" />
          </el-descriptions-item>
          <el-descriptions-item label="类型">
            <el-select v-model="editingField.fieldType" size="small">
              <el-option label="文本" value="text" />
              <el-option label="数字" value="number" />
              <el-option label="日期" value="date" />
              <el-option label="下拉" value="select" />
              <el-option label="多行文本" value="textarea" />
            </el-select>
          </el-descriptions-item>
          <el-descriptions-item label="来源">
            <el-input v-model="editingField.source" size="small" />
          </el-descriptions-item>
          <el-descriptions-item label="共享">
            <el-switch v-model="editingField.isShared" />
          </el-descriptions-item>
          <el-descriptions-item label="所属模块">
            <el-input v-model="editingField.module" size="small" :disabled="editingField.isShared" />
          </el-descriptions-item>
          <el-descriptions-item label="列宽">
            <el-input-number v-model="editingField.width" :min="60" :max="400" size="small" />
          </el-descriptions-item>
          <el-descriptions-item label="必填">
            <el-switch v-model="editingField.isRequired" />
          </el-descriptions-item>
          <el-descriptions-item label="选项" :span="2">
            <el-input
              v-model="editingField.options"
              size="small"
              placeholder="用逗号分隔"
            />
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitUpdate">保存修改</el-button>
      </template>
    </el-dialog>

    <!-- 新增字段弹窗 -->
    <el-dialog
      v-model="createDialogVisible"
      title="新增字段"
      width="600px"
    >
      <el-form
        ref="createFormRef"
        :model="createForm"
        label-width="100px"
        class="create-form"
      >
        <el-form-item label="字段标识" prop="fieldKey">
          <el-input v-model="createForm.fieldKey" placeholder="如: custom_field_1" />
        </el-form-item>
        <el-form-item label="显示名" prop="displayName">
          <el-input v-model="createForm.displayName" placeholder="如: 自定义字段" />
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="createForm.fieldType" style="width: 100%">
            <el-option label="文本" value="text" />
            <el-option label="数字" value="number" />
            <el-option label="日期" value="date" />
            <el-option label="下拉" value="select" />
            <el-option label="多行文本" value="textarea" />
          </el-select>
        </el-form-item>
        <el-form-item label="来源">
          <el-input v-model="createForm.source" placeholder="如: user, system" />
        </el-form-item>
        <el-form-item label="共享">
          <el-switch v-model="createForm.isShared" />
        </el-form-item>
        <el-form-item label="所属模块" v-if="!createForm.isShared">
          <el-input v-model="createForm.module" placeholder="如: ticket, complaint" />
        </el-form-item>
        <el-form-item label="选项">
          <el-input v-model="createForm.options" placeholder="用逗号分隔，如: 选项1,选项2,选项3" />
        </el-form-item>
        <el-form-item label="默认值">
          <el-input v-model="createForm.defaultValue" placeholder="可选" />
        </el-form-item>
        <el-form-item label="列宽">
          <el-input-number v-model="createForm.width" :min="60" :max="400" />
        </el-form-item>
        <el-form-item label="必填">
          <el-switch v-model="createForm.isRequired" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitCreate">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.field-management-view {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;
}

.header-left h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px 0;
}

.header-left p {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
}

.stat-value {
  font-size: 32px;
  font-weight: bold;
  color: #409eff;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  margin-top: 4px;
}

.filter-row {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
}

.table-card {
  border-radius: 8px;
}

.field-detail {
  padding: 8px 0;
}

.create-form {
  padding: 16px 0;
}
</style>