<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

const rooms = ref<any[]>([])
const buildings = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const filterBuildingId = ref<number | null>(null)

const form = ref({
  buildingId: null as number | null,
  code: '',
  name: '',
  floor: null as number | null,
  unit: '',
  ownerName: '',
  ownerPhone: '',
  remark: '',
  isActive: true,
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
  form.value = { buildingId: filterBuildingId.value, code: '', name: '', floor: null, unit: '', ownerName: '', ownerPhone: '', remark: '', isActive: true }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { buildingId: row.buildingId, code: row.code, name: row.name, floor: row.floor, unit: row.unit || '', ownerName: row.ownerName || '', ownerPhone: row.ownerPhone || '', remark: row.remark || '', isActive: row.isActive }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.buildingId || !form.value.code || !form.value.name) {
    ElMessage.warning('请填写楼栋、编码和名称')
    return
  }
  submitting.value = true
  try {
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/rooms/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/rooms', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除房号「${row.name}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/rooms/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const getBuildingName = (id: number) => {
  const b = buildings.value.find(b => b.id === id)
  return b?.name || '-'
}
</script>

<template>
  <div class="room-list">
    <div class="page-header">
      <h2>房号管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增房号</el-button>
    </div>

    <div class="filter-bar">
      <el-select v-model="filterBuildingId" placeholder="所属楼栋" clearable style="width:180px" @change="loadData">
        <el-option v-for="b in buildings" :key="b.id" :label="b.name" :value="b.id" />
      </el-select>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="rooms" v-loading="loading" stripe>
        <el-table-column prop="code" label="房号编码" width="100" />
        <el-table-column prop="name" label="显示名称" />
        <el-table-column label="所属楼栋" width="120">
          <template #default="{ row }">{{ getBuildingName(row.buildingId) }}</template>
        </el-table-column>
        <el-table-column prop="floor" label="楼层" width="80" align="center" />
        <el-table-column prop="unit" label="单元" width="80" align="center" />
        <el-table-column prop="ownerName" label="业主姓名" width="100" />
        <el-table-column prop="ownerPhone" label="业主电话" width="130" />
        <el-table-column prop="isActive" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small">{{ row.isActive ? '启用' : '停用' }}</el-tag>
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑房号' : '新增房号'" width="550px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="所属楼栋" required>
          <el-select v-model="form.buildingId" placeholder="选择楼栋" style="width:100%">
            <el-option v-for="b in buildings" :key="b.id" :label="b.name" :value="b.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="编码" required><el-input v-model="form.code" placeholder="如：101" /></el-form-item>
        <el-form-item label="显示名称" required><el-input v-model="form.name" placeholder="如：101室" /></el-form-item>
        <el-form-item label="楼层"><el-input-number v-model="form.floor" :min="1" /></el-form-item>
        <el-form-item label="单元"><el-input v-model="form.unit" placeholder="如：1单元" /></el-form-item>
        <el-form-item label="业主姓名"><el-input v-model="form.ownerName" /></el-form-item>
        <el-form-item label="业主电话"><el-input v-model="form.ownerPhone" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="2" /></el-form-item>
        <el-form-item label="状态"><el-switch v-model="form.isActive" active-text="启用" inactive-text="停用" /></el-form-item>
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
