<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { currentProject } from '@/stores/project'
import { materialApi } from '@/api/http'
import { useFieldConfig } from '@/composables/useFieldConfig'

const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

const getMaterialFields = () => getActiveFields('material')

// 字段配置（alias 优先的 label + isEditable 控制）
const { fetchFieldConfig } = useFieldConfig()
const materialLabels = ref<Record<string, any>>({})

// 获取某字段是否可编辑
const isFieldEditable = (fieldKey: string): boolean => {
  return materialLabels.value[fieldKey]?.isEditable ?? true
}

const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

const refreshKey = ref(0)
const refreshFields = () => { refreshKey.value++ }

const loading = ref(false)
const recordList = ref<any[]>([])
const total = ref(0)

const filterKeyword = ref('')
const filterCategory = ref('')
const filterStatus = ref('')
const dialogVisible = ref(false)
const dialogTitle = ref('新增物料')
const editingId = ref<number | null>(null)

const form = ref({
  materialNo: '',
  name: '',
  category: 'repair_parts',
  spec: '',
  unit: '个',
  quantity: 0,
  minQuantity: 5,
  price: 0,
  location: '',
  status: 'normal',
  supplier: '',
  remark: ''
})

const categoryOptions = [
  { value: 'repair_parts', label: '维修配件' },
  { value: 'cleaning', label: '清洁用品' },
  { value: 'security', label: '安保器材' },
  { value: 'office', label: '办公用品' },
  { value: 'other', label: '其他' }
]

const statusOptions = [
  { value: 'normal', label: '正常' },
  { value: 'low_stock', label: '库存不足' },
  { value: 'out_of_stock', label: '已用完' },
  { value: 'expired', label: '已过期' },
  { value: 'disabled', label: '已停用' }
]

const stats = computed(() => ({
  total: recordList.value.length,
  normal: recordList.value.filter(m => m.Status === 'normal').length,
  lowStock: recordList.value.filter(m => m.Status === 'low_stock').length,
  outOfStock: recordList.value.filter(m => m.Status === 'out_of_stock').length
}))

const filteredList = computed(() => {
  let result = recordList.value
  if (filterKeyword.value) {
    const kw = filterKeyword.value.toLowerCase()
    result = result.filter((r: any) =>
      (r.MaterialNo?.toLowerCase().includes(kw)) ||
      (r.Name?.toLowerCase().includes(kw)) ||
      (r.Supplier?.toLowerCase().includes(kw))
    )
  }
  if (filterCategory.value) {
    result = result.filter((r: any) => r.Category === filterCategory.value)
  }
  if (filterStatus.value) {
    result = result.filter((r: any) => r.Status === filterStatus.value)
  }
  return result
})

const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 200 }
    if (filterKeyword.value) params.keyword = filterKeyword.value
    if (filterCategory.value) params.category = filterCategory.value
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await materialApi.get('/materials', { params })
    recordList.value = res.data || []
    total.value = res.total || 0
  } catch (e: any) {
    console.error('Load data error:', e)
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadData()
  // 加载字段配置（alias 优先的 label + isEditable 控制）
  const config = await fetchFieldConfig('material')
  if (config) materialLabels.value = config
})

const handleAdd = () => {
  editingId.value = null
  dialogTitle.value = '新增物料'
  form.value = {
    materialNo: 'MAT-' + Date.now().toString().slice(-6),
    name: '',
    category: 'repair_parts',
    spec: '',
    unit: '个',
    quantity: 0,
    minQuantity: 5,
    price: 0,
    location: '',
    status: 'normal',
    supplier: '',
    remark: ''
  }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  editingId.value = row.Id
  dialogTitle.value = '编辑物料'
  form.value = {
    materialNo: row.MaterialNo || '',
    name: row.Name || '',
    category: row.Category || 'repair_parts',
    spec: row.Spec || '',
    unit: row.Unit || '个',
    quantity: row.Quantity || 0,
    minQuantity: row.MinQuantity || 0,
    price: row.Price || 0,
    location: row.Location || '',
    status: row.Status || 'normal',
    supplier: row.Supplier || '',
    remark: row.Remark || ''
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.value.materialNo.trim()) { ElMessage.warning('请输入物料编号'); return }
  if (!form.value.name.trim()) { ElMessage.warning('请输入物料名称'); return }

  try {
    // 构建 PascalCase payload（与后端 MaterialService API 一致）
    const payload: any = {
      MaterialNo: form.value.materialNo,
      Name: form.value.name,
      Category: form.value.category || '',
      Spec: form.value.spec || '',
      Unit: form.value.unit || '',
      Quantity: form.value.quantity || 0,
      MinQuantity: form.value.minQuantity || 0,
      Price: form.value.price || 0,
      Location: form.value.location || '',
      Status: form.value.status || 'normal',
      Supplier: form.value.supplier || '',
      Remark: form.value.remark || '',
      ProjectCode: currentProject.value?.code || '',
    }

    if (editingId.value) {
      await materialApi.put(`/materials/${editingId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await materialApi.post('/materials', payload)
      ElMessage.success('添加成功')
    }
    await loadData()
    dialogVisible.value = false
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除物料 "${row.Name}" 吗？`, '提示', { type: 'warning' })
    await materialApi.delete(`/materials/${row.Id}`)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

const handleRefresh = async () => {
  refreshFields()
  await loadData()
}

const handleReset = () => {
  filterKeyword.value = ''
  filterCategory.value = ''
  filterStatus.value = ''
}

const getStatusType = (status: string) => {
  const map: Record<string, string> = { normal: 'success', low_stock: 'warning', out_of_stock: 'danger', expired: 'warning', disabled: 'info' }
  return map[status] || 'info'
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { normal: '正常', low_stock: '库存不足', out_of_stock: '已用完', expired: '已过期', disabled: '已停用' }
  return map[status] || status
}

const getCategoryLabel = (cat: string) => {
  const map: Record<string, string> = { repair_parts: '维修配件', cleaning: '清洁用品', security: '安保器材', office: '办公用品', other: '其他' }
  return map[cat] || cat
}

const formatPrice = (p: number) => `¥${(p || 0).toFixed(2)}`
</script>

<template>
  <div class="material-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>物料管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon> 配置字段</el-button>
            <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon> 刷新</el-button>
            <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon> 新增物料</el-button>
          </div>
        </div>
      </template>

      <el-alert title="物料管理说明" description="物料管理包含入库、出库、库存跟踪功能。" type="info" :closable="false" style="margin-bottom: 20px;" />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">物料总数</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="filterStatus = filterStatus === 'normal' ? '' : 'normal'">
          <div class="stat-content"><div class="stat-value normal">{{ stats.normal }}</div><div class="stat-label">正常</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="filterStatus = filterStatus === 'low_stock' ? '' : 'low_stock'">
          <div class="stat-content"><div class="stat-value warning">{{ stats.lowStock }}</div><div class="stat-label">库存不足</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="filterStatus = filterStatus === 'out_of_stock' ? '' : 'out_of_stock'">
          <div class="stat-content"><div class="stat-value danger">{{ stats.outOfStock }}</div><div class="stat-label">已用完</div></div>
        </el-card>
      </div>

      <!-- 筛选工具栏 -->
      <div class="filter-toolbar">
        <el-input v-model="filterKeyword" placeholder="关键词搜索" clearable style="width: 200px;" />
        <el-select v-model="filterCategory" placeholder="分类" clearable style="width: 130px;">
          <el-option v-for="opt in categoryOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 120px;">
          <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-button @click="handleReset">重置</el-button>
      </div>

      <!-- 数据列表 -->
      <el-table :data="filteredList" stripe v-loading="loading">
        <el-table-column prop="MaterialNo" :label="materialLabels.materialNo?.label || materialLabels.MaterialNo?.label || '物料编号'" width="110" />
        <el-table-column prop="Name" :label="materialLabels.name?.label || materialLabels.Name?.label || '物料名称'" min-width="120" />
        <el-table-column prop="Category" :label="materialLabels.category?.label || materialLabels.Category?.label || '分类'" width="100" align="center">
          <template #default="{ row }"><el-tag size="small">{{ getCategoryLabel(row.Category) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="Spec" :label="materialLabels.spec?.label || materialLabels.Spec?.label || '规格'" width="120" />
        <el-table-column prop="Quantity" label="库存" width="80" align="center">
          <template #default="{ row }">
            <span :style="{ color: row.Quantity != null && row.MinQuantity != null && row.Quantity <= row.MinQuantity ? '#F56C6C' : '#67C23A' }">{{ row.Quantity }} {{ row.Unit }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="MinQuantity" label="最低库存" width="90" align="center">
          <template #default="{ row }">{{ row.MinQuantity }} {{ row.Unit }}</template>
        </el-table-column>
        <el-table-column prop="Price" label="单价" width="80" align="center">
          <template #default="{ row }">{{ formatPrice(row.Price) }}</template>
        </el-table-column>
        <el-table-column prop="Status" label="状态" width="90" align="center">
          <template #default="{ row }"><el-tag :type="getStatusType(row.Status)" size="small">{{ getStatusLabel(row.Status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="650px">
      <el-form label-width="100px">
        <el-form-item :label="materialLabels.materialNo?.label || materialLabels.MaterialNo?.label || '物料编号'" required><el-input v-model="form.materialNo" placeholder="如：MAT-001" :disabled="!isFieldEditable('materialNo') && !isFieldEditable('MaterialNo')" /></el-form-item>
        <el-form-item :label="materialLabels.name?.label || materialLabels.Name?.label || '物料名称'" required><el-input v-model="form.name" placeholder="请输入物料名称" :disabled="!isFieldEditable('name') && !isFieldEditable('Name')" /></el-form-item>
        <el-form-item :label="materialLabels.category?.label || materialLabels.Category?.label || '分类'">
          <el-select v-model="form.category" style="width: 100%" :disabled="!isFieldEditable('category') && !isFieldEditable('Category')">
            <el-option v-for="opt in categoryOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="materialLabels.spec?.label || materialLabels.Spec?.label || '规格型号'"><el-input v-model="form.spec" placeholder="请输入规格型号" :disabled="!isFieldEditable('spec') && !isFieldEditable('Spec')" /></el-form-item>
        <el-form-item label="单位">
          <el-select v-model="form.unit" style="width: 100%" :disabled="!isFieldEditable('unit')">
            <el-option v-for="u in ['个','件','套','米','升','公斤','卷','盒','箱']" :key="u" :label="u" :value="u" />
          </el-select>
        </el-form-item>
        <el-form-item label="当前库存"><el-input-number v-model="form.quantity" :min="0" style="width: 100%" :disabled="!isFieldEditable('quantity')" /></el-form-item>
        <el-form-item label="最低库存"><el-input-number v-model="form.minQuantity" :min="0" style="width: 100%" :disabled="!isFieldEditable('minQuantity')" /></el-form-item>
        <el-form-item label="单价"><el-input-number v-model="form.price" :min="0" :precision="2" style="width: 100%" :disabled="!isFieldEditable('price')" /></el-form-item>
        <el-form-item :label="materialLabels.location?.label || materialLabels.Location?.label || '存放位置'"><el-input v-model="form.location" placeholder="如：仓库A区" :disabled="!isFieldEditable('location') && !isFieldEditable('Location')" /></el-form-item>
        <el-form-item :label="materialLabels.supplier?.label || materialLabels.Supplier?.label || '供应商'"><el-input v-model="form.supplier" placeholder="请输入供应商" :disabled="!isFieldEditable('supplier') && !isFieldEditable('Supplier')" /></el-form-item>
        <el-form-item :label="materialLabels.remark?.label || materialLabels.Remark?.label || '备注'"><el-input v-model="form.remark" type="textarea" placeholder="请输入备注" :disabled="!isFieldEditable('remark') && !isFieldEditable('Remark')" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="material" module-name="物料管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.material-page { width: 100%; }
.header { display: flex; justify-content: space-between; align-items: center; }
.header-actions { display: flex; gap: 10px; }
.stats-grid { display: flex; gap: 15px; margin-bottom: 20px; flex-wrap: wrap; }
.stat-card { flex: 1; min-width: 100px; cursor: pointer; transition: all 0.3s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { text-align: center; }
.stat-value { font-size: 24px; font-weight: bold; color: #409EFF; }
.stat-value.normal { color: #67C23A; }
.stat-value.warning { color: #E6A23C; }
.stat-value.danger { color: #F56C6C; }
.stat-label { font-size: 13px; color: #909399; margin-top: 5px; }
.filter-toolbar { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; align-items: center; }
</style>