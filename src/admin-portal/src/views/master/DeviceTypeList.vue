<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { masterApi } from '@/api/http'

const deviceTypes = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const openFieldConfig = () => { fieldDialogRef.value?.open() }
const filterCategory = ref('')

const categories = ['机电类', '消防类', '安防类', '给排水类', '电梯类', '其他']

const form = ref({
  code: '',
  name: '',
  category: '',
  description: '',
  sortOrder: 0,
  status: 'Active',
})

onMounted(() => { loadData() })

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {}
    if (filterCategory.value) params.category = filterCategory.value
    const res: any = await masterApi.get('/device-types', { params })
    if (res.success) deviceTypes.value = res.data || []
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
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/device-types/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/device-types', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除设备类型「${row.name}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/device-types/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}
</script>

<template>
  <div class="devicetype-list">
    <div class="page-header">
      <h2>设备类型管理</h2>
      <div style="display:flex;gap:8px">
        <el-button type="primary" :icon="Plus" @click="openCreate">新增设备类型</el-button>
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
      <el-table :data="deviceTypes" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="设备类型" />
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑设备类型' : '新增设备类型'" width="500px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="编码" required><el-input v-model="form.code" placeholder="如：elevator" /></el-form-item>
        <el-form-item label="名称" required><el-input v-model="form.name" placeholder="如：电梯" /></el-form-item>
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

    <FieldConfigDialog ref="fieldDialogRef" module="devicetype" module-name="设备类型" @update="refreshFields" />
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 16px; }
</style>
