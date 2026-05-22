<template>
  <div class="message-template-list">
    <div class="page-header">
      <h2>消息模板</h2>
      <el-button type="primary" @click="handleAdd">新增模板</el-button>
    </div>
    
    <el-table :data="list" v-loading="loading" border>
      <el-table-column prop="name" label="模板名称" width="150" />
      <el-table-column prop="type" label="类型" width="100" />
      <el-table-column prop="subject" label="主题" />
      <el-table-column prop="variables" label="变量" width="200">
        <template #default="{ row }">
          <span class="variables">{{ row.variables || '-' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="createdAt" label="创建时间" width="160">
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
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="模板名称" required>
          <el-input v-model="form.name" placeholder="如：工单创建通知" />
        </el-form-item>
        <el-form-item label="类型" required>
          <el-select v-model="form.type" placeholder="选择类型">
            <el-option label="工单" value="Ticket" />
            <el-option label="设备" value="Device" />
            <el-option label="物料" value="Material" />
            <el-option label="公告" value="Announcement" />
          </el-select>
        </el-form-item>
        <el-form-item label="主题" required>
          <el-input v-model="form.subject" placeholder="如：【工单通知】新的工单 #{ticketId} 已创建" />
        </el-form-item>
        <el-form-item label="内容" required>
          <el-input v-model="form.content" type="textarea" rows="4" placeholder="模板内容，使用 #{变量名} 占位" />
        </el-form-item>
        <el-form-item label="变量">
          <el-input v-model="form.variables" placeholder="如：ticketId,title,priority（用逗号分隔）" />
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
import { messageTemplateApi } from '@/api/messageTemplate'

const list = ref([])
const loading = ref(false)
const dialogVisible = ref(false)
const dialogTitle = ref('新增模板')
const isEdit = ref(false)
const submitting = ref(false)

const form = reactive({
  id: null,
  name: '',
  type: '',
  subject: '',
  content: '',
  variables: ''
})

const loadList = async () => {
  loading.value = true
  try {
    const res = await messageTemplateApi.getList()
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
  dialogTitle.value = '新增模板'
  isEdit.value = false
  form.id = null
  form.name = ''
  form.type = ''
  form.subject = ''
  form.content = ''
  form.variables = ''
  dialogVisible.value = true
}

const handleEdit = (row) => {
  dialogTitle.value = '编辑模板'
  isEdit.value = true
  form.id = row.id
  form.name = row.name
  form.type = row.type
  form.subject = row.subject
  form.content = row.content
  form.variables = row.variables || ''
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.name || !form.type || !form.subject || !form.content) {
    ElMessage.warning('请填写必填项')
    return
  }
  
  submitting.value = true
  try {
    let res
    if (isEdit.value) {
      res = await messageTemplateApi.update(form.id, {
        name: form.name,
        type: form.type,
        subject: form.subject,
        content: form.content,
        variables: form.variables
      })
    } else {
      res = await messageTemplateApi.create({
        name: form.name,
        type: form.type,
        subject: form.subject,
        content: form.content,
        variables: form.variables
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
    await ElMessageBox.confirm(`确定删除模板"${row.name}"吗？`, '提示', {
      type: 'warning'
    })
    const res = await messageTemplateApi.delete(row.id)
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
.message-template-list {
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

.variables {
  font-size: 12px;
  color: #666;
}
</style>