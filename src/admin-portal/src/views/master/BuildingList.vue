<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

const buildings = ref<any[]>([])
const areas = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const filterAreaId = ref<number | null>(null)

const form = ref({
  code: '',
  name: '',
  areaId: null as number | null,
  totalFloors: null as number | null,
  totalUnits: null as number | null,
  description: '',
  status: 'Active',
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
    if (filterAreaId.value) params.areaId = filterAreaId.value
    const res: any = await masterApi.get('/buildings', { params })
    if (res.success) buildings.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { code: '', name: '', areaId: null, totalFloors: null, totalUnits: null, description: '', status: 'Active' }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { code: row.code, name: row.name, areaId: row.areaId, totalFloors: row.totalFloors, totalUnits: row.totalUnits, description: row.description || '', status: row.status || 'Active' }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { ElMessage.warning('请填写编码和名称'); return }
  submitting.value = true
  try {
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/buildings/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/buildings', form.value)
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

const getAreaName = (areaId: number | null) => {
  if (!areaId) return '-'
  const area = areas.value.find(a => a.id === areaId)
  return area?.name || '-'
}
</script>

<template>
  <div class="building-list">
    <div class="page-header">
      <h2>楼栋管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增楼栋</el-button>
    </div>

    <div class="filter-bar">
      <el-select v-model="filterAreaId" placeholder="所属区域" clearable style="width:160px" @change="loadData">
        <el-option v-for="a in areas" :key="a.id" :label="a.name" :value="a.id" />
      </el-select>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="buildings" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="100" />
        <el-table-column prop="name" label="楼栋名称" />
        <el-table-column label="所属区域" width="120">
          <template #default="{ row }">{{ getAreaName(row.areaId) }}</template>
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
        <el-form-item label="编码" required><el-input v-model="form.code" placeholder="如：BLD-01" /></el-form-item>
        <el-form-item label="名称" required><el-input v-model="form.name" placeholder="如：1号楼" /></el-form-item>
        <el-form-item label="所属区域">
          <el-select v-model="form.areaId" placeholder="选择区域" clearable style="width:100%">
            <el-option v-for="a in areas" :key="a.id" :label="a.name" :value="a.id" />
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
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 16px; }
</style>
