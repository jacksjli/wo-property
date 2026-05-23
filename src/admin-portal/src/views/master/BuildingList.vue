<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Upload, Download } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'
import { toPinyinCode } from '@/utils/pinyin'
import * as XLSX from 'xlsx'

const buildings = ref<any[]>([])
const areas = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const filterArea = ref<string | null>(null)

// 导入相关
const importDialogVisible = ref(false)
const importLoading = ref(false)
const importResult = ref<{ success: number; failed: number; skipped: number; errors: string[] } | null>(null)

const form = ref({
  code: '',
  name: '',
  area: null as string | null,
  totalFloors: null as number | null,
  totalUnits: null as number | null,
  description: '',
  status: 'Active',
})

// 监听名称变化，自动生成编码
watch(() => form.value.name, (newName) => {
  if (newName && !isEdit.value) {
    form.value.code = toPinyinCode(newName)
  }
})

onMounted(() => { loadAreas(); loadData() })

const loadAreas = async () => {
  const res: any = await masterApi.get('/areas')
  if (res.success) areas.value = res.data || []
}

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {}
    if (filterArea.value) params.area = filterArea.value
    const res: any = await masterApi.get('/buildings', { params })
    if (res.success) buildings.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { code: '', name: '', area: null, totalFloors: null, totalUnits: null, description: '', status: 'Active' }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { 
    code: row.code, 
    name: row.name, 
    area: row.area || null, 
    totalFloors: row.totalFloors, 
    totalUnits: row.totalUnits, 
    description: row.description || '', 
    status: row.status || 'Active' 
  }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { ElMessage.warning('请填写编码和名称'); return }
  submitting.value = true
  try {
    const payload = {
      code: form.value.code,
      name: form.value.name,
      area: form.value.area,
      totalFloors: form.value.totalFloors,
      totalUnits: form.value.totalUnits,
      description: form.value.description,
      status: form.value.status
    }
    
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/buildings/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/buildings', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除楼栋「${row.name}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/buildings/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

// 下载导入模板
const handleDownloadTemplate = () => {
  const data = [
    { '楼栋名称': '', '所属区域': '', '楼层数': '', '每层户数': '', '描述': '', '排序': '' },
    { '楼栋名称': '1号楼', '所属区域': 'A区', '楼层数': 6, '每层户数': 4, '描述': '', '排序': 1 },
    { '楼栋名称': '2号楼', '所属区域': 'A区', '楼层数': 6, '每层户数': 4, '描述': '', '排序': 2 },
  ]
  const ws = XLSX.utils.json_to_sheet(data)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '楼栋导入模板')
  XLSX.writeFile(wb, '楼栋导入模板.xlsx')
}

// 处理文件选择
const handleFileChange = async (upload: any) => {
  const file = upload.raw
  if (!file) return

  importLoading.value = true
  importResult.value = null

  try {
    const reader = new FileReader()
    reader.onload = async (e: any) => {
      try {
        const data = new Uint8Array(e.target.result)
        const workbook = XLSX.read(data, { type: 'array', cellDates: true })
        const firstSheet = workbook.Sheets[workbook.SheetNames[0]]
        const jsonData = XLSX.utils.sheet_to_json(firstSheet, { defval: '' }) as any[]

        if (jsonData.length === 0) {
          ElMessage.warning('导入文件为空')
          importLoading.value = false
          return
        }

        // 映射字段（支持中文列名）
        const rows = jsonData.map((row: any) => ({
          name: row['楼栋名称'] || row['Name'] || '',
          area: row['所属区域'] || row['Area'] || '',
          totalFloors: parseInt(row['楼层数'] || row['TotalFloors'] || '0') || null,
          totalUnits: parseInt(row['每层户数'] || row['TotalUnits'] || '0') || null,
          description: row['描述'] || row['Description'] || '',
          sortOrder: parseInt(row['排序'] || row['SortOrder'] || '0') || null,
        }))

        const res: any = await masterApi.post('/buildings/import', { rows })

        importResult.value = {
          success: res.successCount || 0,
          failed: res.failedCount || 0,
          skipped: res.skippedCount || 0,
          errors: res.errors || []
        }

        if (res.success) {
          ElMessage.success(`导入完成：成功 ${res.successCount} 条`)
          importDialogVisible.value = false
          loadData()
        } else {
          ElMessage.warning(`导入完成：成功 ${res.successCount}，失败 ${res.failedCount}，跳过 ${res.skippedCount}`)
        }
      } catch (err: any) {
        ElMessage.error('解析Excel文件失败: ' + (err.message || ''))
      } finally {
        importLoading.value = false
      }
    }
    reader.readAsArrayBuffer(file)
  } catch (err: any) {
    ElMessage.error('读取文件失败: ' + (err.message || ''))
    importLoading.value = false
  }
}

// 打开导入弹窗
const openImportDialog = () => {
  importResult.value = null
  importDialogVisible.value = true
}
</script>

<template>
  <div class="building-list">
    <div class="page-header">
      <h2>楼栋管理</h2>
      <div style="display:flex;gap:8px">
        <el-button :icon="Upload" @click="openImportDialog">批量导入</el-button>
        <el-button type="primary" :icon="Plus" @click="openCreate">新增楼栋</el-button>
      </div>
    </div>

    <div class="filter-bar">
      <el-select v-model="filterArea" placeholder="所属区域" clearable style="width:160px" @change="loadData">
        <el-option v-for="a in areas" :key="a.id" :label="a.name" :value="a.name" />
      </el-select>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="buildings" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="100" />
        <el-table-column prop="name" label="楼栋名称" />
        <el-table-column prop="area" label="所属区域" width="120">
          <template #default="{ row }">{{ row.area || '-' }}</template>
        </el-table-column>
        <el-table-column prop="totalFloors" label="层数" width="80" align="center" />
        <el-table-column prop="totalUnits" label="单元数" width="80" align="center" />
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑楼栋' : '新增楼栋'" width="500px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="名称" required>
          <el-input v-model="form.name" placeholder="如：1号楼" @input="!isEdit && (form.code = toPinyinCode(form.name))" />
        </el-form-item>
        <el-form-item label="编码" required>
          <el-input v-model="form.code" placeholder="自动生成或手动输入" />
        </el-form-item>
        <el-form-item label="所属区域">
          <el-select v-model="form.area" placeholder="选择区域" clearable style="width:100%">
            <el-option v-for="a in areas" :key="a.id" :label="a.name" :value="a.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="总层数"><el-input-number v-model="form.totalFloors" :min="1" /></el-form-item>
        <el-form-item label="单元数"><el-input-number v-model="form.totalUnits" :min="1" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="form.description" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="form.status" active-value="Active" inactive-value="Inactive" active-text="启用" inactive-text="停用" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <!-- 批量导入弹窗 -->
    <el-dialog v-model="importDialogVisible" title="批量导入楼栋" width="600px" destroy-on-close>
      <div class="import-tips">
        <p>📋 <strong>导入说明</strong></p>
        <ul>
          <li>楼栋名称为必填项，其他字段可选</li>
          <li>名称重复时将覆盖已有记录</li>
          <li>必填字段为空则跳过该行</li>
        </ul>
        <el-button type="primary" link :icon="Download" @click="handleDownloadTemplate" style="margin-top:8px">
          下载导入模板
        </el-button>
      </div>

      <el-divider />

      <el-upload
        ref="uploadRef"
        class="upload-demo"
        drag
        :auto-upload="false"
        accept=".xlsx,.xls"
        :on-change="handleFileChange"
        :limit="1"
      >
        <el-icon class="el-icon--upload"><upload-filled /></el-icon>
        <div class="el-upload__text">拖拽文件到此处，或 <em>点击上传</em></div>
        <template #tip>
          <div class="el-upload__tip">支持 .xlsx 和 .xls 文件</div>
        </template>
      </el-upload>

      <div v-if="importResult" class="import-result">
        <el-alert :title="`导入完成`" type="info" :closable="false" style="margin-top:16px">
          <template #default>
            <div>成功: <strong>{{ importResult.success }}</strong> 条 | 失败: <strong>{{ importResult.failed }}</strong> 条 | 跳过: <strong>{{ importResult.skipped }}</strong> 条</div>
            <div v-if="importResult.errors.length > 0" style="margin-top:8px;max-height:120px;overflow-y:auto">
              <p v-for="(err, i) in importResult.errors.slice(0, 10)" :key="i" style="color:#e6a23c;font-size:12px">{{ err }}</p>
              <p v-if="importResult.errors.length > 10" style="color:#909399;font-size:12px">...还有 {{ importResult.errors.length - 10 }} 条错误</p>
            </div>
          </template>
        </el-alert>
      </div>

      <template #footer>
        <el-button @click="importDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 16px; }
.import-tips { background: #f5f7fa; padding: 12px 16px; border-radius: 6px; font-size: 14px; }
.import-tips ul { margin: 6px 0 0 20px; color: #606266; }
.import-tips li { margin-bottom: 4px; }
.import-result { margin-top: 16px; }
</style>