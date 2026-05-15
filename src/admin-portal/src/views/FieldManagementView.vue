<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { ArrowRight } from '@element-plus/icons-vue'
import { masterDataApi, type FieldDefinition } from '../api/masterDataService'

// 等价组数据（蛇形命名 → 标准名）
const equivalenceGroups: Record<string, string[]> = {
  person_name: ['name', 'visitor_name', 'host_name', 'assigneeName', 'reporter_name', 'complainant_name', 'resident_name', 'cleaner_name', 'inspector_name', 'applicant_name', 'recipient_name', 'handlerName'],
  phone_number: ['phone', 'visitor_phone', 'host_phone', 'resident_phone', 'contact_phone', 'reporter_phone', 'emergency_phone', 'holder_phone', 'applicant_phone', 'recipient_phone', 'complainant_phone', 'delivery_phone'],
  status: ['handleStatus', 'deliveryStatus', 'paymentStatus', 'ticketTypeStatus'],
  contact_name: ['contact_person', 'contactName', 'emergency_contact_name'],
  building_id: ['building_id', 'area_id', 'building']
}

// 别名 → 标准名映射
const aliasToStandard: Record<string, string> = {}
for (const [standard, aliases] of Object.entries(equivalenceGroups)) {
  for (const alias of aliases) {
    aliasToStandard[alias] = standard
  }
}

// 别名来源模块映射
const aliasSourceModules: Record<string, string> = {
  name: 'Personnel', visitor_name: 'visitor', host_name: 'visitor',
  assigneeName: 'ticket', reporter_name: 'ticket', contact_phone: 'ticket', reporter_phone: 'ticket', handlerName: 'ticket',
  resident_name: 'resident', resident_phone: 'resident', emergency_phone: 'resident',
  complainant_name: 'complaints', complainant_phone: 'complaints',
  applicant_name: 'payment', applicant_phone: 'payment',
  recipient_name: 'delivery', recipient_phone: 'delivery', delivery_phone: 'delivery',
  cleaner_name: 'cleaning', inspector_name: 'inspection',
  holder_phone: 'key', contact_person: 'ticket', contactName: 'ticket',
  emergency_contact_name: 'resident',
  building_id: 'building', area_id: 'area', building: 'key'
}

// 模块顺序（工单优先）
const moduleOrder = ['ticket', 'visitor', 'delivery', 'complaints', 'inspection', 'cleaning', 'resident', 'payment', 'key', 'building', 'area', 'Personnel', 'ticketType', 'contract', 'material', 'device', 'finance', 'notification', 'statistics', 'project', 'projectTracking', 'accessControl', 'cleaning', 'community', 'express', 'renovation', 'project_config', 'field', 'fieldDefinition', 'department', 'region', 'room', 'supplier', 'deviceType', 'jobType', 'timeout', 'dispatch', 'parking']
const moduleNameMap: Record<string, string> = {
  ticket: '工单', visitor: '访客', delivery: '配送', complaints: '投诉',
  inspection: '巡检', cleaning: '保洁', resident: '住户', payment: '缴费',
  key: '钥匙', building: '楼栋', area: '区域', Personnel: '人员',
  ticketType: '工单类型', contract: '合同', material: '物料', device: '设备',
  finance: '财务', notification: '消息', statistics: '统计', project: '项目',
  projectTracking: '项目跟踪', accessControl: '权限控制', community: '社区',
  express: '快递', renovation: '装修', project_config: '项目配置',
  field: '字段', fieldDefinition: '字段定义', department: '部门',
  region: '区域', room: '房号', supplier: '供应商', deviceType: '设备类型',
  jobType: '工种', timeout: '超时', dispatch: '派单', parking: '车位'
}

// 字段标准名 → 别名列表
const standardToAliases: Record<string, string[]> = {}
for (const [standard, aliases] of Object.entries(equivalenceGroups)) {
  standardToAliases[standard] = aliases
}

// 搜索过滤
const searchKeyword = ref('')

// 展开状态
const expandedModules = ref<Set<string>>(new Set())

// 加载状态
const loading = ref(false)

// 原始数据
const allFields = ref<FieldDefinition[]>([])

// 弹窗状态
const detailDialogVisible = ref(false)
const createDialogVisible = ref(false)
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

// 别名字段显示
const getAliasDisplay = (fieldKey: string): string => {
  const aliases = standardToAliases[fieldKey] || []
  if (aliases.length === 0) return '-'
  return aliases.join(', ')
}

// 统计
const stats = computed(() => {
  const total = allFields.value.length
  const shared = allFields.value.filter(f => f.isShared).length
  const privateCount = total - shared
  return { total, shared, privateCount }
})

// 按 module 分组（module 有值则按模块展示，无值则为全局共享）
const groupedFields = computed(() => {
  const groups: Record<string, FieldDefinition[]> = {}
  const noModule: FieldDefinition[] = []

  for (const field of allFields.value) {
    if (field.module) {
      if (!groups[field.module]) groups[field.module] = []
      groups[field.module].push(field)
    } else {
      noModule.push(field)
    }
  }

  const sorted: { module: string; fields: FieldDefinition[] }[] = []
  for (const mod of moduleOrder) {
    if (groups[mod]) {
      sorted.push({ module: mod, fields: groups[mod] })
      delete groups[mod]
    }
  }
  for (const mod of Object.keys(groups).sort()) {
    sorted.push({ module: mod, fields: groups[mod] })
  }
  return { sorted, noModule }
})

const sharedFields = computed(() => allFields.value)

// 过滤关键字（搜索所有字段）
const filteredAllFields = computed(() => {
  if (!searchKeyword.value) return allFields.value
  const kw = searchKeyword.value.toLowerCase()
  return allFields.value.filter(f =>
    f.fieldKey.toLowerCase().includes(kw) ||
    f.displayName.toLowerCase().includes(kw) ||
    (standardToAliases[f.fieldKey] || []).some(a => a.toLowerCase().includes(kw)) ||
    (f.module && f.module.toLowerCase().includes(kw))
  )
})

// 过滤后的分组（搜索模式下使用）
const filteredGrouped = computed(() => {
  if (!searchKeyword.value) return []
  const kw = searchKeyword.value.toLowerCase()
  const matched = filteredAllFields.value.filter(f => f.module)
  const groups: Record<string, FieldDefinition[]> = {}
  for (const f of matched) {
    if (!groups[f.module!]) groups[f.module!] = []
    groups[f.module!].push(f)
  }
  const sorted: { module: string; fields: FieldDefinition[] }[] = []
  for (const mod of moduleOrder) {
    if (groups[mod]) { sorted.push({ module: mod, fields: groups[mod] }); delete groups[mod] }
  }
  for (const mod of Object.keys(groups).sort()) {
    sorted.push({ module: mod, fields: groups[mod] })
  }
  return sorted
})

// 搜索模式标志
const isSearchMode = computed(() => !!searchKeyword.value)

// 加载字段数据
const loadFields = async () => {
  loading.value = true
  try {
    const res = await masterDataApi.getAllFieldsNoPagination()
    allFields.value = res.data || []
  } catch {
    allFields.value = []
  } finally {
    loading.value = false
  }
}

// 查看/编辑字段详情
const showDetail = (row: FieldDefinition) => {
  editingField.value = { ...row }
  detailDialogVisible.value = true
}

// 打开新增弹窗
const openCreateDialog = () => {
  createForm.value = {
    fieldKey: '', displayName: '', fieldType: 'text', source: 'user',
    isShared: true, module: '', options: '', defaultValue: '', isRequired: false, width: 120
  }
  createDialogVisible.value = true
}

// 新增字段
const submitCreate = async () => {
  if (!createFormRef.value) return
  try { await createFormRef.value.validate() } catch { return }

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
  } catch {
    ElMessage.error('创建失败')
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
  } catch {
    ElMessage.error('更新失败')
  }
}

// 获取模块中文名
const getModuleName = (mod: string): string => {
  return moduleNameMap[mod] || mod
}

const toggleModule = (mod: string) => {
  if (expandedModules.value.has(mod)) {
    expandedModules.value.delete(mod)
  } else {
    expandedModules.value.add(mod)
  }
}

const isModuleExpanded = (mod: string) => expandedModules.value.has(mod)

onMounted(() => {
  loadFields()
  // 默认全部展开
  expandedModules.value.add('__shared__')
  allFields.value.forEach(f => {
    if (f.module) expandedModules.value.add(f.module)
  })
})
</script>

<template>
  <div class="field-management-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h1>字段管理</h1>
        <p>按模块统一管理字段定义</p>
      </div>
      <el-button type="primary" @click="openCreateDialog">新增字段</el-button>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-row">
      <el-card class="stat-card"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">总字段数</div></el-card>
      <el-card class="stat-card"><div class="stat-value">{{ stats.shared }}</div><div class="stat-label">共享字段</div></el-card>
      <el-card class="stat-card"><div class="stat-value">{{ stats.privateCount }}</div><div class="stat-label">私有字段</div></el-card>
    </div>

    <!-- 搜索 -->
    <div class="filter-row">
      <el-input v-model="searchKeyword" placeholder="搜索字段标识、显示名、别名或模块" style="width: 300px" clearable />
    </div>

    <!-- 搜索模式 -->
    <template v-if="isSearchMode">
      <el-card class="table-card">
        <el-table :data="filteredGrouped.flatMap(g => g.fields)" v-loading="loading" stripe empty-text="无匹配字段">
          <el-table-column label="模块" width="120">
            <template #default="{ row }">
              <el-tag size="small" type="warning">{{ getModuleName(row.module) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="displayName" label="显示名" width="140" />
          <el-table-column prop="fieldKey" label="标准名" width="180">
            <template #default="{ row }"><code style="font-size:13px">{{ row.fieldKey }}</code></template>
          </el-table-column>
          <el-table-column label="别名" min-width="200">
            <template #default="{ row }"><span style="color:#909399;font-size:13px">{{ getAliasDisplay(row.fieldKey) }}</span></template>
          </el-table-column>
          <el-table-column label="共享" width="70" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isShared ? 'success' : 'warning'" size="small">{{ row.isShared ? '是' : '否' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="必填" width="70" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isRequired ? 'danger' : 'info'" size="small">{{ row.isRequired ? '是' : '否' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100" fixed="right">
            <template #default="{ row }">
              <el-button size="small" text type="primary" @click="showDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </template>

    <!-- 正常模式：按模块分组 -->
    <template v-else>
      <!-- 共享字段（所有标准名） -->
      <el-card class="table-card" style="margin-bottom: 16px;">
        <template #header>
          <div class="section-header" style="cursor:pointer" @click="toggleModule('__shared__')">
            <el-icon :class="{ 'rotate-icon': isModuleExpanded('__shared__') }" style="transition: transform 0.3s; margin-right: 6px;">
              <ArrowRight />
            </el-icon>
            <span class="section-title">标准名字段</span>
            <span class="section-desc">以下列出所有标准名字段（可录入/删除数据的字段）</span>
            <el-tag type="success" size="small">{{ sharedFields.length }}</el-tag>
          </div>
        </template>
        <el-table v-show="isModuleExpanded('__shared__')" :data="sharedFields" v-loading="loading" stripe empty-text="暂无字段">
          <el-table-column prop="displayName" label="中文名" width="140" />
          <el-table-column prop="fieldKey" label="标准名" width="180">
            <template #default="{ row }"><code style="font-size:13px">{{ row.fieldKey }}</code></template>
          </el-table-column>
          <el-table-column label="所属模块" width="120">
            <template #default="{ row }">
              <el-tag size="small" type="warning">{{ getModuleName(row.module) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="必填" width="70" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isRequired ? 'danger' : 'info'" size="small">{{ row.isRequired ? '是' : '否' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100" fixed="right">
            <template #default="{ row }">
              <el-button size="small" text type="primary" @click="showDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <!-- 所有模块字段（标准名） -->
      <el-card v-for="group in groupedFields.sorted" :key="group.module" class="table-card module-section">
        <template #header>
          <div class="section-header" style="cursor:pointer" @click="toggleModule(group.module)">
            <el-icon :class="{ 'rotate-icon': isModuleExpanded(group.module) }" style="transition: transform 0.3s; margin-right: 6px;">
              <ArrowRight />
            </el-icon>
            <span class="section-title">{{ getModuleName(group.module) }}模块</span>
            <el-tag type="warning" size="small">{{ group.fields.length }}</el-tag>
          </div>
        </template>
        <el-table v-show="isModuleExpanded(group.module)" :data="group.fields" v-loading="loading" stripe empty-text="暂无字段">
          <el-table-column prop="displayName" label="显示名" width="140" />
          <el-table-column prop="fieldKey" label="标准名" width="180">
            <template #default="{ row }"><code style="font-size:13px">{{ row.fieldKey }}</code></template>
          </el-table-column>
          <el-table-column label="别名" min-width="220">
            <template #default="{ row }"><span style="color:#909399;font-size:13px">{{ getAliasDisplay(row.fieldKey) }}</span></template>
          </el-table-column>
          <el-table-column label="类型" width="70" align="center">
            <template #default="{ row }">
              <el-tag type="success" size="small">标准名</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="必填" width="70" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isRequired ? 'danger' : 'info'" size="small">{{ row.isRequired ? '是' : '否' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100" fixed="right">
            <template #default="{ row }">
              <el-button size="small" text type="primary" @click="showDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </template>

    <!-- 字段详情/编辑弹窗 -->
    <el-dialog v-model="detailDialogVisible" title="字段详情" width="560px">
      <div v-if="editingField" class="field-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="标准名"><code style="font-size:13px">{{ editingField.fieldKey }}</code></el-descriptions-item>
          <el-descriptions-item label="显示名"><el-input v-model="editingField.displayName" size="small" /></el-descriptions-item>
          <el-descriptions-item label="字段类型">
            <el-select v-model="editingField.fieldType" size="small">
              <el-option label="文本" value="text" /><el-option label="数字" value="number" />
              <el-option label="日期" value="date" /><el-option label="下拉" value="select" />
              <el-option label="多行文本" value="textarea" />
            </el-select>
          </el-descriptions-item>
          <el-descriptions-item label="共享"><el-switch v-model="editingField.isShared" /></el-descriptions-item>
          <el-descriptions-item label="所属模块"><el-input v-model="editingField.module" size="small" :disabled="editingField.isShared" /></el-descriptions-item>
          <el-descriptions-item label="必填"><el-switch v-model="editingField.isRequired" /></el-descriptions-item>
          <el-descriptions-item label="列宽"><el-input-number v-model="editingField.width" :min="60" :max="400" size="small" /></el-descriptions-item>
          <el-descriptions-item label="选项" :span="2"><el-input v-model="editingField.options" size="small" placeholder="用逗号分隔" /></el-descriptions-item>
        </el-descriptions>
        <div v-if="getAliasDisplay(editingField.fieldKey) !== '-'" style="margin-top:16px">
          <strong>等价别名：</strong>
          <div style="margin-top:8px;color:#606266">{{ getAliasDisplay(editingField.fieldKey) }}</div>
        </div>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitUpdate">保存修改</el-button>
      </template>
    </el-dialog>

    <!-- 新增字段弹窗 -->
    <el-dialog v-model="createDialogVisible" title="新增字段" width="560px">
      <el-form ref="createFormRef" :model="createForm" label-width="100px">
        <el-form-item label="字段标识" prop="fieldKey"><el-input v-model="createForm.fieldKey" placeholder="英文唯一标识" /></el-form-item>
        <el-form-item label="显示名" prop="displayName"><el-input v-model="createForm.displayName" placeholder="中文显示名" /></el-form-item>
        <el-form-item label="字段类型">
          <el-select v-model="createForm.fieldType" style="width:100%">
            <el-option label="文本" value="text" /><el-option label="数字" value="number" />
            <el-option label="日期" value="date" /><el-option label="下拉" value="select" />
            <el-option label="多行文本" value="textarea" />
          </el-select>
        </el-form-item>
        <el-form-item label="共享"><el-switch v-model="createForm.isShared" /></el-form-item>
        <el-form-item label="所属模块" v-if="!createForm.isShared">
          <el-input v-model="createForm.module" placeholder="如：ticket, resident, payment" />
        </el-form-item>
        <el-form-item label="选项"><el-input v-model="createForm.options" placeholder="用逗号分隔" /></el-form-item>
        <el-form-item label="必填"><el-switch v-model="createForm.isRequired" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitCreate">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.field-management-view { padding: 0; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
.header-left h1 { font-size: 24px; font-weight: 600; color: #1f2937; margin: 0 0 4px 0; }
.header-left p { font-size: 14px; color: #6b7280; margin: 0; }
.stats-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 20px; }
.stat-card { text-align: center; }
.stat-value { font-size: 32px; font-weight: bold; color: #409eff; }
.stat-label { font-size: 14px; color: #6b7280; margin-top: 4px; }
.filter-row { display: flex; gap: 12px; margin-bottom: 16px; }
.table-card { border-radius: 8px; margin-bottom: 12px; }
.module-section { margin-bottom: 12px; }
.section-header { display: flex; align-items: center; gap: 8px; }
.section-title { font-weight: 600; font-size: 15px; }
.section-desc { font-size: 12px; color: #909399; }
.rotate-icon { transform: rotate(90deg); }
.field-detail { padding: 8px 0; }
</style>