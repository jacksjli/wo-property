<template>
  <div class="external-person-list">
    <div class="page-header">
      <h2>外部人员</h2>
      <el-button type="primary" @click="handleAdd">新增人员</el-button>
    </div>
    
    <el-table :data="list" v-loading="loading" border>
      <el-table-column prop="name" label="姓名" width="120" />
      <el-table-column prop="phone" label="电话" width="150" />
      <el-table-column prop="type" label="类型" width="120">
        <template #default="{ row }">
          <el-tag>{{ row.type || '未分类' }}</el-tag>
        </template>
      </el-table-column>
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

    <!-- 分页 -->
    <div class="pagination-wrapper">
      <el-pagination
        v-model:current-page="page"
        :page-size="pageSize"
        :total="total"
        layout="total, prev, pager, next"
        @current-change="loadList"
      />
    </div>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="姓名" required>
          <el-input v-model="form.name" placeholder="请输入姓名" />
        </el-form-item>
        <el-form-item label="电话" required>
          <el-input v-model="form.phone" placeholder="请输入电话" />
        </el-form-item>
        <el-form-item label="类型" required>
          <el-select v-model="form.type" placeholder="选择类型">
            <el-option label="访客" value="visitor" />
            <el-option label="供应商" value="supplier" />
            <el-option label="外卖员" value="delivery" />
            <el-option label="其他" value="other" />
          </el-select>
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
import { externalPersonApi } from '@/api/externalPerson'

const list = ref([])
const loading = ref(false)
const dialogVisible = ref(false)
const dialogTitle = ref('新增人员')
const isEdit = ref(false)
const submitting = ref(false)
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)

const form = reactive({
  id: null,
  name: '',
  phone: '',
  type: ''
})

const loadList = async () => {
  loading.value = true
  try {
    const res = await externalPersonApi.getList({ page: page.value, pageSize: pageSize.value })
    if (res.success) {
      list.value = res.data || []
      total.value = res.total || 0
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
  dialogTitle.value = '新增人员'
  isEdit.value = false
  form.id = null
  form.name = ''
  form.phone = ''
  form.type = ''
  dialogVisible.value = true
}

const handleEdit = (row) => {
  dialogTitle.value = '编辑人员'
  isEdit.value = true
  form.id = row.id
  form.name = row.name
  form.phone = row.phone
  form.type = row.type || ''
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.name || !form.phone || !form.type) {
    ElMessage.warning('请填写必填项')
    return
  }
  
  submitting.value = true
  try {
    let res
    if (isEdit.value) {
      res = await externalPersonApi.update(form.id, {
        name: form.name,
        phone: form.phone,
        type: form.type
      })
    } else {
      res = await externalPersonApi.create({
        name: form.name,
        phone: form.phone,
        type: form.type
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
    await ElMessageBox.confirm(`确定删除"${row.name}"吗？`, '提示', {
      type: 'warning'
    })
    const res = await externalPersonApi.delete(row.id)
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
.external-person-list {
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

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>