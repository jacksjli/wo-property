<template>
  <div class="material-category-list">
    <div class="page-header">
      <h2>物料分类</h2>
      <el-button type="primary" @click="handleAdd">新增分类</el-button>
    </div>
    
    <el-table :data="list" v-loading="loading" border>
      <el-table-column prop="code" label="分类编码" width="120" />
      <el-table-column prop="name" label="分类名称" />
      <el-table-column prop="description" label="描述" />
      <el-table-column prop="createdAt" label="创建时间" width="180">
        <template #default="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <el-button size="small" @click="handleEdit(row)">编辑</el-button>
          <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="分类编码" required>
          <el-input v-model="form.code" placeholder="如：ELECTRIC" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="分类名称" required>
          <el-input v-model="form.name" placeholder="如：电气材料" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" rows="3" placeholder="分类描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { materialCategoryApi } from '@/api/materialCategory'

const list = ref([])
const loading = ref(false)
const dialogVisible = ref(false)
const dialogTitle = ref('新增分类')
const isEdit = ref(false)
const submitting = ref(false)

const form = reactive({
  id: null,
  code: '',
  name: '',
  description: ''
})

const loadList = async () => {
  loading.value = true
  try {
    const res = await materialCategoryApi.getList()
    if (res.success) {
      list.value = res.data || []
    } else {
      ElMessage.error(res.message || '加载失败')
    }
  } catch (e) {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN')
}

const handleAdd = () => {
  dialogTitle.value = '新增分类'
  isEdit.value = false
  form.id = null
  form.code = ''
  form.name = ''
  form.description = ''
  dialogVisible.value = true
}

const handleEdit = (row) => {
  dialogTitle.value = '编辑分类'
  isEdit.value = true
  form.id = row.id
  form.code = row.code
  form.name = row.name
  form.description = row.description || ''
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.code || !form.name) {
    ElMessage.warning('请填写必填项')
    return
  }
  
  submitting.value = true
  try {
    let res
    if (isEdit.value) {
      res = await materialCategoryApi.update(form.id, {
        name: form.name,
        description: form.description
      })
    } else {
      res = await materialCategoryApi.create({
        code: form.code,
        name: form.name,
        description: form.description
      })
    }
    
    if (res.success) {
      ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
      dialogVisible.value = false
      loadList()
    } else {
      ElMessage.error(res.message || '操作失败')
    }
  } catch (e) {
    ElMessage.error('操作失败')
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定删除分类"${row.name}"吗？`, '提示', {
      type: 'warning'
    })
    const res = await materialCategoryApi.delete(row.id)
    if (res.success) {
      ElMessage.success('删除成功')
      loadList()
    } else {
      ElMessage.error(res.message || '删除失败')
    }
  } catch (e) {
    // 用户取消
  }
}

onMounted(() => {
  loadList()
})
</script>

<style scoped>
.material-category-list {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0;
}
</style>