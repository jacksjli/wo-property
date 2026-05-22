<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { masterApi } from '@/api/http'
import { toPinyinCode } from '@/utils/pinyin'

const loading = ref(false)
const tableData = ref<any[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)

const form = ref({
  code: '',
  name: '',
  description: '',
  sortOrder: 0,
  status: 'Active',
})

const statusOptions = [
  { label: '启用', value: 'Active' },
  { label: '停用', value: 'Inactive' },
]

const loadRegions = async () => {
  loading.value = true
  try {
    const r: any = await masterApi.get('/regions')
    if (r.success) tableData.value = r.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { code: '', name: '', description: '', sortOrder: 0, status: 'Active' }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { 
    code: row.code, 
    name: row.name, 
    description: row.description || '', 
    sortOrder: row.sortOrder || 0, 
    status: row.status || 'Active' 
  }
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { 
    ElMessage.warning('请填写编码和名称'); return 
  }
  submitting.value = true
  try {
    const payload = { 
      code: form.value.code, 
      name: form.value.name, 
      description: form.value.description, 
      sortOrder: form.value.sortOrder, 
      status: form.value.status 
    }
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/regions/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/regions', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadRegions()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除区域「${row.name}」（${row.code}）吗？`, '确认', { type: 'warning' })
    await masterApi.delete(`/regions/${row.id}`)
    ElMessage.success('已删除')
    loadRegions()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const openFieldConfig = () => { fieldDialogRef.value?.open() }

onMounted(loadRegions)
</script>

<template>
  <div class="region-list">
    <div class="page-header">
      <h2>大区/省市区管理</h2>
      <div style="display:flex;gap:8px">
        <el-button type="primary" :icon="Plus" @click="openCreate">新增区域</el-button>
        <el-button @click="loadRegions" :loading="loading"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
      </div>
    </div>

    <el-card shadow="never">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="区域名称" min-width="200" />
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑区域' : '新增区域'" width="500px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="区域名称" required>
          <el-input v-model="form.name" placeholder="如：华东区域" />
        </el-form-item>
        <el-form-item label="编码" required>
          <el-input v-model="form.code" placeholder="如：HD（华北）/ZJ（浙江）" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="区域说明" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sortOrder" :min="0" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" style="width:100%">
            <el-option v-for="s in statusOptions" :key="s.value" :label="s.label" :value="s.value" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="region" module-name="大区管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
</style>
