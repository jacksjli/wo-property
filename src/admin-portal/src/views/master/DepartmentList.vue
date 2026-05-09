<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { departmentApi } from '@/api/http'

const loading = ref(false)
const treeData = ref<any[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)

const form = ref({
  name: '',
  parentId: null as number | null,
  sort: 0,
})

const allDepts = ref<any[]>([])
const parentOptions = ref<any[]>([])

const loadDepartments = async () => {
  loading.value = true
  try {
    const r: any = await departmentApi.get('/departments')
    if (r.success) {
      treeData.value = r.data || []
      allDepts.value = flattenTree(r.data || [])
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const flattenTree = (list: any[], result: any[] = [], prefix = ''): any[] => {
  list.forEach(d => {
    result.push({ ...d, displayName: prefix + d.name })
    if (d.children?.length) flattenTree(d.children, result, prefix + '  ├─ ')
  })
  return result
}

const openCreate = (parent: any = null) => {
  isEdit.value = false
  form.value = { name: '', parentId: parent?.id || null, sort: 0 }
  parentOptions.value = allDepts.value.filter(d => !parent || d.id !== parent.id)
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { name: row.name, parentId: row.parentId, sort: row.sort || 0 }
  parentOptions.value = allDepts.value.filter(d => !row || d.id !== row.id)
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.name) { ElMessage.warning('请填写部门名称'); return }
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
  if (row.children?.length > 0) {
    ElMessage.warning('该部门有子部门，请先删除子部门')
    return
  }
  try {
    await ElMessageBox.confirm(`删除部门「${row.name}」？`, '确认', { type: 'warning' })
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
        <el-button type="primary" :icon="Plus" @click="openCreate()">新增部门</el-button>
        <el-button @click="loadDepartments" :loading="loading"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
      </div>
    </div>

    <el-card shadow="never">
      <el-alert type="info" :closable="false" style="margin-bottom:16px">
        <template #title>树形结构：支持多级部门，新增时可选择上级部门。</template>
      </el-alert>

      <el-table :data="treeData" v-loading="loading" stripe row-key="id" default-expand-all>
        <el-table-column prop="name" label="部门名称" min-width="200" />
        <el-table-column prop="parentId" label="上级部门" width="150" align="center">
          <template #default="{ row }">
            <span v-if="row.parent">{{ row.parent.name }}</span>
            <span v-else style="color:#999">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="sort" label="排序" width="80" align="center" />
        <el-table-column label="操作" width="200" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openCreate(row)">新增子部门</el-button>
            <el-button link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑部门' : '新增部门'" width="450px" destroy-on-close>
      <el-form :model="form" label-width="80px">
        <el-form-item label="部门名称" required>
          <el-input v-model="form.name" placeholder="如：工程部/客服部/安保部" />
        </el-form-item>
        <el-form-item label="上级部门">
          <el-select v-model="form.parentId" placeholder="无上级（顶级部门）" clearable style="width:100%">
            <el-option v-for="d in parentOptions" :key="d.id" :label="d.displayName" :value="d.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sort" :min="0" />
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
