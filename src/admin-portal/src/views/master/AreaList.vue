<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Upload, Download } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'
import * as XLSX from 'xlsx';
import { toPinyinCode } from '@/utils/pinyin'

const areas = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)

// 批量导入
const importDialogVisible = ref(false)
const importLoading = ref(false)
const importResult = ref<{ success: number; failed: number; skipped: number; errors: string[] } | null>(null)


const handleDownloadTemplate = () => {
  const templateData = [
    {
      '区域名称': '',
      '上级区域ID': '',
      '描述': '',
      '排序': 0
    }
  ]
  const ws = XLSX.utils.json_to_sheet(templateData)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '区域导入模板')
  XLSX.writeFile(wb, '区域导入模板.xlsx')
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
          if (index === 0 && row['区域名称'] === '区域名称') return null
          if (!row['区域名称']) {
            return { _error: `第${index + 1}行: 区域名称为必填字段`, _row: row }
          }
          return {
            name: row['区域名称'] || '',
            parentId: row['上级区域ID'] || null,
            description: row['描述'] || '',
            sort: row['排序'] || 0,
          }
        }).filter((r: any) => r !== null)
        const res: any = await masterApi.importAreas({ rows })
        if (res.success) {
          importResult.value = res.data
          ElMessage.success(`导入完成: 成功${res.data.success}条, 跳过${res.data.skipped}条, 失败${res.data.failed}条`)
          if (res.data.failed > 0) {
            ElMessage.warning(`失败原因: ${res.data.errors?.join('; ')}`)
          }
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

const openImportDialog = () => {
  importResult.value = null
  importDialogVisible.value = true
}


const form = ref({
  code: '',
  name: '',
  sort: 0,
  isActive: true,
})

// 监听名称变化，自动生成编码
watch(() => form.value.name, (newName) => {
  if (newName && !isEdit.value) {
    form.value.code = toPinyinCode(newName)
  }
})

onMounted(() => { loadData() })

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/areas')
    if (res.success) areas.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { code: '', name: '', sort: 0, isActive: true }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { code: row.code, name: row.name, sort: row.sort, isActive: row.isActive }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) {
    ElMessage.warning('请填写编码和名称')
    return
  }
  submitting.value = true
  try {
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/areas/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/areas', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除区域「${row.name}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/areas/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}
</script>

<template>
  <div class="area-list">
    <div class="page-header">
      <h2>区域管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增区域</el-button>
      <el-button @click="openImportDialog"><el-icon><Upload /></el-icon> 批量导入</el-button>
    </div>

    <div class="filter-bar">
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="areas" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="区域名称" />
        <el-table-column prop="sort" label="排序" width="80" align="center" />
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑区域' : '新增区域'" width="450px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="名称" required>
          <el-input v-model="form.name" placeholder="如：东区" @input="!isEdit && (form.code = toPinyinCode(form.name))" />
        </el-form-item>
        <el-form-item label="编码" required>
          <el-input v-model="form.code" placeholder="自动生成或手动输入" />
        </el-form-item>
        <el-form-item label="排序"><el-input-number v-model="form.sort" :min="0" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="form.isActive" active-value="Active" inactive-value="Inactive" active-text="启用" inactive-text="停用" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <!-- 批量导入区域对话框 -->
    <el-dialog v-model="importDialogVisible" title="批量导入区域" width="600px">
      <div style="margin-bottom: 20px;">
        <h4>导入说明：</h4>
        <ul style="color: #666; font-size: 13px; line-height: 1.8;">
          <li>请先下载导入模板，按模板格式填写数据</li>
          <li>区域名称为必填字段，其他为选填</li>
          <li>名称重复的区域会自动覆盖</li>
          <li>必填字段为空的数据会自动跳过</li>
        </ul>
        <el-button type="primary" link @click="handleDownloadTemplate" style="margin: 10px 0;">
          <el-icon><Download /></el-icon> 下载导入模板
        </el-button>
      </div>
      <el-upload
        ref="uploadRef"
        :auto-upload="false"
        :limit="1"
        accept=".xlsx,.xls"
        :on-change="handleFileChange"
        style="margin-bottom: 20px;">
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
