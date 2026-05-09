<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

const suppliers = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)
const filterType = ref('')

const types = ['物料供应商', '维修服务商', '保洁服务商', '安保服务商', '其他']

const form = ref({
  code: '',
  name: '',
  type: '',
  contact: '',
  phone: '',
  address: '',
  remark: '',
  isActive: true,
})

onMounted(() => { loadData() })

const loadData = async () => {
  loading.value = true
  try {
    const params: any = {}
    if (filterType.value) params.type = filterType.value
    const res: any = await masterApi.get('/suppliers', { params })
    if (res.success) suppliers.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const openCreate = () => {
  isEdit.value = false
  form.value = { code: '', name: '', type: '', contact: '', phone: '', address: '', remark: '', isActive: true }
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { code: row.code, name: row.name, type: row.type || '', contact: row.contact || '', phone: row.phone || '', address: row.address || '', remark: row.remark || '', isActive: row.isActive }
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { ElMessage.warning('请填写编码和名称'); return }
  submitting.value = true
  try {
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/suppliers/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/suppliers', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除供应商「${row.name}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/suppliers/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}
</script>

<template>
  <div class="supplier-list">
    <div class="page-header">
      <h2>供应商管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增供应商</el-button>
    </div>

    <div class="filter-bar">
      <el-select v-model="filterType" placeholder="类型筛选" clearable style="width:160px" @change="loadData">
        <el-option v-for="t in types" :key="t" :label="t" :value="t" />
      </el-select>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="suppliers" v-loading="loading" stripe>
        <el-table-column prop="code" label="编码" width="100" />
        <el-table-column prop="name" label="供应商名称" />
        <el-table-column prop="type" label="类型" width="120" align="center" />
        <el-table-column prop="contact" label="联系人" width="100" />
        <el-table-column prop="phone" label="联系电话" width="130" />
        <el-table-column prop="address" label="地址" />
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

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑供应商' : '新增供应商'" width="550px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="编码" required><el-input v-model="form.code" /></el-form-item>
        <el-form-item label="名称" required><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="类型">
          <el-select v-model="form.type" placeholder="选择类型" clearable style="width:100%">
            <el-option v-for="t in types" :key="t" :label="t" :value="t" />
          </el-select>
        </el-form-item>
        <el-form-item label="联系人"><el-input v-model="form.contact" /></el-form-item>
        <el-form-item label="联系电话"><el-input v-model="form.phone" /></el-form-item>
        <el-form-item label="地址"><el-input v-model="form.address" /></el-form-item>
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
