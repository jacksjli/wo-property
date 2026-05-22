<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { departmentApi } from '@/api/http'
import { toPinyinCode } from '@/utils/pinyin'

const loading = ref(false)
const treeData = ref<any[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)

const form = ref({
  name: '',
  code: '',
  description: '',
  sortOrder: 0,
})

// 监听名称变化，自动生成编码
const generateCode = () => {
  if (form.value.name && !isEdit.value) {
    form.value.code = toPinyinCode(form.value.name)
  }
}

const loadDepartments = async () => {
  loading.value = true
  try {
    const r: any = await departmentApi.get('/departments')
    if (r.success) {
      treeData.value = r.data || []
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { name: '', code: '', description: '', sortOrder: 0 }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { 
    name: row.name, 
    code: row.code || '', 
    description: row.description || '', 
    sortOrder: row.sortOrder || 0 
  }
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { 
    ElMessage.warning('请填写编码和部门名称'); return 
  }
  submitting.value = true
  try {
    if (isEdit.value && currentId.value) {
      await departmentApi.put(`/departments/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await departmentApi.post('/departments', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadDepartments()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除部门「${row.name}」吗？`, '确认', { type: 'warning' })
    await departmentApi.delete(`/departments/${row.id}`)
    ElMessage.success('已删除')
    loadDepartments()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const openFieldConfig = () => { fieldDialogRef.value?.open() }

onMounted(loadDepartments)
</script>

<template>
  <div class="department-list">
    <div class="page-header">
      <h2>部门管理</h2>
      <div style="display:flex;gap:8px">
        <el-button type="primary" :icon="Plus" @click="openCreate">新增部门</el-button>
        <el-button @click="loadDepartments" :loading="loading"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
      </div>
    </div>

    <el-card shadow="never">
      <el-table :data="treeData" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="部门名称" />
        <el-table-column prop="description" label="描述" min-width="200" />
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
        <el-table-column label="操作" width="150" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑部门' : '新增部门'" width="450px" destroy-on-close>
      <el-form :model="form" label-width="80px">
        <el-form-item label="部门名称" required>
          <el-input v-model="form.name" placeholder="如：工程部" @input="generateCode" />
        </el-form-item>
        <el-form-item label="编码" required>
          <el-input v-model="form.code" placeholder="自动生成或手动输入" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="部门职能说明" />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sortOrder" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="department" module-name="部门管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
</style>
