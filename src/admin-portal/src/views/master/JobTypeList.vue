<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, Upload, Download } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { masterDataApi } from '@/api/http'
import { ticketTypeApi } from '@/api/ticketType'
import * as XLSX from 'xlsx'

const jobTypes = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const openFieldConfig = () => { fieldDialogRef.value?.open() }
const filterCategory = ref('')

// 批量导入
const importDialogVisible = ref(false)
const importLoading = ref(false)
const importResult = ref<{ success: number; failed: number; skipped: number; errors: string[] } | null>(null)

const handleDownloadTemplate = () => {
  const templateData = [{
    '工种名称': '',
    '工种分类': '维修/保安/咨询/应急/保洁/投诉/装修',
    '工单类型ID': '',
    '描述': '',
    '排序': 0,
  }]
  const ws = XLSX.utils.json_to_sheet(templateData)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '工种导入模板')
  XLSX.writeFile(wb, '工种导入模板.xlsx')
}

const handleFileChange = async (file: any) => {
  if (!file) return
  importLoading.value = true
  importResult.value = null
  try {
    const reader = new FileReader()
    reader.onload = async (e) => {
      try {
        const data = new Uint8Array(reader.result as ArrayBuffer)
        const workbook = XLSX.read(data, { type: 'array' })
        const sheetName = workbook.SheetNames[0]
        const worksheet = workbook.Sheets[sheetName]
        const jsonData = XLSX.utils.sheet_to_json(worksheet, { defval: '' })
        const rows = jsonData.map((row: any, index: number) => {
          if (index === 0 && row['工种名称'] === '工种名称') return null
          if (!row['工种名称']) return { _error: `第${index + 1}行: 工种名称为必填字段`, _row: row }
          return {
            name: row['工种名称'] || '',
            category: row['工种分类'] || '',
            ticketTypeId: row['工单类型ID'] || null,
            description: row['描述'] || '',
            sortOrder: row['排序'] || 0,
          }
        }).filter((r: any) => r !== null)
        const res: any = await masterDataApi.importJobTypes({ rows })
        if (res.success) {
          importResult.value = res.data
          ElMessage.success(`导入完成: 成功${res.data.success}条, 跳过${res.data.skipped}条, 失败${res.data.failed}条`)
          if (res.data.failed > 0 && res.data.errors?.length) ElMessage.warning(`失败原因: ${res.data.errors.join('; ')}`)
          loadData()
        } else {
          ElMessage.error(res.message || '导入失败')
        }
      } catch (err: any) { ElMessage.error('解析Excel失败: ' + err.message) }
      finally { importLoading.value = false }
    }
    reader.readAsArrayBuffer(file.raw || file)
  } catch (err: any) { ElMessage.error('读取文件失败: ' + err.message); importLoading.value = false }
}

const openImportDialog = () => { importResult.value = null; importDialogVisible.value = true }

const categories = ref<string[]>([])

const form = ref({
  code: '',
  name: '',
  category: '',
  description: '',
  sortOrder: 0,
  status: 'Active',
})

onMounted(async () => { 
  loadData()
  await loadCategories()
})

const loadCategories = async () => {
  try {
    const r: any = await ticketTypeApi.getAll()
    if (r.success && r.data) {
      categories.value = r.data.map((t: any) => t.name)
    }
  } catch (e) {
    console.error('加载工单类型失败', e)
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {}
    if (filterCategory.value) params.category = filterCategory.value
    const res: any = await masterDataApi.get('/job-types', { params })
    if (res.success) jobTypes.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { code: '', name: '', category: '', description: '', sortOrder: 0, status: 'Active' }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { code: row.code, name: row.name, category: row.category || '', description: row.description || '', sortOrder: row.sortOrder, status: row.status || 'Active' }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { ElMessage.warning('请填写编码和名称'); return }
  submitting.value = true
  try {
    const payload = { ...form.value }
    if (isEdit.value && currentId.value) {
      await masterDataApi.put(`/job-types/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterDataApi.post('/job-types', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除工种「${row.name}」吗？`, '提示', { type: 'warning' })
    await masterDataApi.delete(`/job-types/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}
</script>

<template>
  <div class="jobtype-list">
    <div class="page-header">
      <h2>工种管理</h2>
      <div style="display:flex;gap:8px">
        <el-button @click="openImportDialog"><el-icon><Upload /></el-icon> 批量导入</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">新增工种</el-button>
        <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
      </div>
    </div>

    <div class="filter-bar">
      <el-select v-model="filterCategory" placeholder="分类筛选" clearable style="width:140px" @change="loadData">
        <el-option v-for="c in categories" :key="c" :label="c" :value="c" />
      </el-select>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
      <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="jobTypes" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="工种名称" />
        <el-table-column prop="category" label="分类" width="120" align="center" />
        <el-table-column prop="description" label="描述" />
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Active' ? 'success' : 'info'" size="small">{{ row.status === 'Active' ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑工种' : '新增工种'" width="500px" destroy-on-close>
      <el-form :model="form" label-width="80px">
        <el-form-item label="编码" required><el-input v-model="form.code" placeholder="如：plumber" /></el-form-item>
        <el-form-item label="名称" required><el-input v-model="form.name" placeholder="如：水管工" /></el-form-item>
        <el-form-item label="分类">
          <el-select v-model="form.category" placeholder="选择分类" clearable style="width:100%">
            <el-option v-for="c in categories" :key="c" :label="c" :value="c" />
          </el-select>
        </el-form-item>
        <el-form-item label="描述"><el-input v-model="form.description" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="form.sortOrder" :min="0" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="form.status" active-value="Active" inactive-value="Inactive" active-text="启用" inactive-text="停用" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="jobtype" module-name="工种管理" @update="refreshFields" />

    <el-dialog v-model="importDialogVisible" title="批量导入工种" width="600px">
      <div style="margin-bottom: 20px;">
        <h4>导入说明：</h4>
        <ul style="color: #666; font-size: 13px; line-height: 1.8;">
          <li>请先下载导入模板，按模板格式填写数据</li>
          <li>工种名称和工种分类为必填字段</li>
          <li>工种名称重复的数据会自动覆盖</li>
          <li>必填字段为空的数据会自动跳过</li>
        </ul>
        <el-button type="primary" link @click="handleDownloadTemplate">
          <el-icon><Download /></el-icon> 下载导入模板
        </el-button>
      </div>
      <el-upload :auto-upload="false" :limit="1" accept=".xlsx,.xls" :on-change="handleFileChange" style="margin-bottom: 20px;">
        <el-button type="default">选择Excel文件</el-button>
      </el-upload>
      <el-divider v-if="importResult" />
      <div v-if="importResult" style="background: #f5f7fa; padding: 15px; border-radius: 4px;">
        <h4>导入结果：</h4>
        <el-descriptions :column="1" border size="small">
          <el-descriptions-item label="成功">{{ importResult.success }} 条</el-descriptions-item>
          <el-descriptions-item label="跳过">{{ importResult.skipped }} 条</el-descriptions-item>
          <el-descriptions-item label="失败">{{ importResult.failed }} 条</el-descriptions-item>
        </el-descriptions>
        <div v-if="importResult.errors?.length" style="margin-top: 10px;">
          <el-alert type="error" :closable="false">
            <template #title>失败原因：{{ importResult.errors.join('; ') }}</template>
          </el-alert>
        </div>
      </div>
      <template #footer><el-button @click="importDialogVisible = false">关闭</el-button></template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 16px; }
</style>
