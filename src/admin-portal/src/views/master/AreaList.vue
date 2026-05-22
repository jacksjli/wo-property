<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'
import { toPinyinCode } from '@/utils/pinyin'

const areas = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)

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
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 16px; }
</style>
