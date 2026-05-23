<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Upload, Download } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'
import * as XLSX from 'xlsx'

const rooms = ref<any[]>([])
const buildings = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const filterBuildingId = ref<number | null>(null)

// 导入相关
const importDialogVisible = ref(false)
const importLoading = ref(false)
const importResult = ref<{ success: number; failed: number; skipped: number; errors: string[] } | null>(null)

const form = ref({
  buildingId: null as number | null,
  roomNumber: '',   // 数据库: RoomNumber
  roomType: '',     // 数据库: RoomType
  floor: '',
  unit: '',
  area: null as number | null,
  status: 'Active', // 数据库: Status ('Active' / 'Inactive')
})

onMounted(() => { loadBuildings(); loadData() })

const loadBuildings = async () => {
  const res: any = await masterApi.get('/buildings')
  if (res.success) buildings.value = res.data || []
}

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {}
    if (filterBuildingId.value) params.buildingId = filterBuildingId.value
    const res: any = await masterApi.get('/rooms', { params })
    if (res.success) rooms.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

watch(filterBuildingId, () => { loadData() })

const openCreate = () => {
  isEdit.value = false
  form.value = { buildingId: filterBuildingId.value, roomNumber: '', roomType: '', floor: '', unit: '', area: null, status: 'Active' }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { 
    buildingId: row.buildingId, 
    roomNumber: row.roomNumber || '',   // 数据库: RoomNumber
    roomType: row.roomType || '',       // 数据库: RoomType
    floor: row.floor || '', 
    unit: row.unit || '', 
    area: row.area, 
    status: row.status || 'Active'      // 数据库: Status
  }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.buildingId || !form.value.roomNumber) {
    ElMessage.warning('请选择楼栋并填写房号')
    return
  }
  submitting.value = true
  try {
    // 构建符合数据库字段的 payload
    const payload = {
      buildingId: form.value.buildingId,
      roomNumber: form.value.roomNumber,
      roomType: form.value.roomType,
      floor: form.value.floor,
      unit: form.value.unit,
      area: form.value.area,
      status: form.value.status
    }
    
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/rooms/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/rooms', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除房号「${row.roomNumber}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/rooms/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const getBuildingName = (id: number) => {
  const b = buildings.value.find(b => b.id === id)
  return b?.name || '-'
}

const getStatusType = (status: string) => {
  return status === 'Active' ? 'success' : 'info'
}

const getStatusText = (status: string) => {
  return status === 'Active' ? '启用' : '停用'
}

// 下载导入模板
const handleDownloadTemplate = () => {
  const data = [
    { '房号名称（必填）': '', '所属楼栋ID': '', '楼层': '', '单元': '', '建筑面积': '', '使用面积': '', '描述': '' },
    { '房号名称（必填）': '101', '所属楼栋ID': '1', '楼层': '1', '单元': '1', '建筑面积': '120', '使用面积': '100', '描述': '示例房号' }
  ]
  const ws = XLSX.utils.json_to_sheet(data)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '房号导入模板')
  XLSX.writeFile(wb, '房号导入模板.xlsx')
}

// 打开导入弹窗
const openImportDialog = () => {
  importResult.value = null
  importDialogVisible.value = true
}

// 文件选择
const handleFileChange = async (uploadFile: any) => {
  const file = uploadFile.raw
  if (!file) return
  importLoading.value = true
  importResult.value = null
  try {
    const arrayBuffer = await file.arrayBuffer()
    const workbook = XLSX.read(arrayBuffer, { type: 'array' })
    const sheetName = workbook.SheetNames[0]
    const sheet = workbook.Sheets[sheetName]
    const rows: any[] = XLSX.utils.sheet_to_json(sheet)

    const mappedRows = rows.map((r: any) => ({
      roomNumber: r['房号名称（必填）'] || '',
      buildingId: r['所属楼栋ID'] ? Number(r['所属楼栋ID']) : null,
      floor: r['楼层'] || '',
      unit: r['单元'] || '',
      area: r['建筑面积'] ? Number(r['建筑面积']) : null,
      useArea: r['使用面积'] ? Number(r['使用面积']) : null,
      description: r['描述'] || ''
    }))

    const res: any = await masterApi.importRooms({ rows: mappedRows })
    if (res.success) {
      importResult.value = res.data
      ElMessage.success(`导入完成：成功 ${res.data.success}，失败 ${res.data.failed}，跳过 ${res.data.skipped}`)
    } else {
      ElMessage.error(res.message || '导入失败')
    }
  } catch (e: any) {
    ElMessage.error(e.message || '解析文件失败')
  } finally {
    importLoading.value = false
  }
}
</script>

<template>
  <div class="room-list">
    <div class="page-header">
      <h2>房号管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增房号</el-button>
      <el-button :icon="Upload" @click="openImportDialog">批量导入</el-button>
    </div>

    <div class="filter-bar">
      <el-select v-model="filterBuildingId" placeholder="所属楼栋" clearable style="width:180px" @change="loadData">
        <el-option v-for="b in buildings" :key="b.id" :label="b.name" :value="b.id" />
      </el-select>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="rooms" v-loading="loading" stripe>
        <el-table-column prop="roomNumber" label="房号" width="100" />
        <el-table-column prop="roomType" label="类型" />
        <el-table-column label="所属楼栋" width="120">
          <template #default="{ row }">{{ getBuildingName(row.buildingId) }}</template>
        </el-table-column>
        <el-table-column prop="floor" label="楼层" width="80" align="center" />
        <el-table-column prop="unit" label="单元" width="80" align="center" />
        <el-table-column prop="area" label="面积(㎡)" width="100" align="center" />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag>
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑房号' : '新增房号'" width="500px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="所属楼栋" required>
          <el-select v-model="form.buildingId" placeholder="选择楼栋" style="width:100%">
            <el-option v-for="b in buildings" :key="b.id" :label="b.name" :value="b.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="房号" required><el-input v-model="form.roomNumber" placeholder="如：101" /></el-form-item>
        <el-form-item label="类型"><el-input v-model="form.roomType" placeholder="如：住宅、商铺" /></el-form-item>
        <el-form-item label="楼层"><el-input v-model="form.floor" placeholder="如：1" /></el-form-item>
        <el-form-item label="单元"><el-input v-model="form.unit" placeholder="如：1" /></el-form-item>
        <el-form-item label="面积(㎡)"><el-input-number v-model="form.area" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" style="width:100%">
            <el-option label="启用" value="Active" />
            <el-option label="停用" value="Inactive" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="importDialogVisible" title="批量导入房号" width="600px" destroy-on-close>
      <div style="margin-bottom:12px">
        <el-button type="success" :icon="Download" @click="handleDownloadTemplate">下载模板</el-button>
        <span style="margin-left:12px;color:#999;font-size:12px">支持 .xlsx/.xls 文件，必填：房号名称</span>
      </div>
      <el-upload
        action="#"
        :auto-upload="false"
        :show-file-list="true"
        :on-change="handleFileChange"
        accept=".xlsx,.xls"
        style="margin-bottom:12px"
      >
        <el-button type="primary" :icon="Upload">选择Excel文件</el-button>
      </el-upload>
      <el-empty v-if="!importResult" description="请上传Excel文件进行导入" />
      <div v-else>
        <el-alert v-if="importResult.errors.length > 0" type="warning" :title="`${importResult.errors.length} 条错误`" style="margin-bottom:8px" />
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="成功"><el-tag type="success">{{ importResult.success }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="失败"><el-tag type="danger">{{ importResult.failed }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="跳过"><el-tag type="info">{{ importResult.skipped }}</el-tag></el-descriptions-item>
        </el-descriptions>
        <div v-if="importResult.errors.length > 0" style="margin-top:8px;max-height:150px;overflow-y:auto">
          <div v-for="(err, idx) in importResult.errors" :key="idx" style="color:#e6a23c;font-size:12px">{{ idx+1 }}. {{ err }}</div>
        </div>
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
</style>
