<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Delete, Edit } from '@element-plus/icons-vue'
import { communityApi } from '@/api/community'

const loading = ref(false)
const list = ref<any[]>([])
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const formRef = ref()
const submitting = ref(false)

const form = ref({
  title: '',
  content: '',
  type: 'activity',
  location: '',
  startTime: '',
  endTime: '',
  maxParticipants: 0,
  status: 'active'
})

const types = [
  { value: 'activity', label: '社区活动' },
  { value: 'fitness', label: '健身运动' },
  { value: 'education', label: '教育培训' },
  { value: 'entertainment', label: '娱乐休闲' },
  { value: 'volunteer', label: '志愿服务' },
  { value: 'other', label: '其他' }
]

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await communityApi.getActivities({ pageSize: 100 })
    if (res.success !== false) {
      list.value = res.data || res
    }
  } catch (error) {
    console.error('加载失败:', error)
  }
  loading.value = false
}

const handleAdd = () => {
  editingId.value = null
  form.value = { title: '', content: '', type: 'activity', location: '', startTime: '', endTime: '', maxParticipants: 0, status: 'active' }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  editingId.value = row.id
  form.value = {
    title: row.title,
    content: row.content,
    type: row.type,
    location: row.location || '',
    startTime: row.startTime || '',
    endTime: row.endTime || '',
    maxParticipants: row.maxParticipants || 0,
    status: row.status
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate()
  submitting.value = true
  try {
    if (editingId.value) {
      await communityApi.update(editingId.value, form.value)
      ElMessage.success('更新成功')
    } else {
      await communityApi.create(form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    ElMessage.error(error.message || '操作失败')
  }
  submitting.value = false
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除活动「${row.title}」吗？`, '确认删除')
    await communityApi.delete(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const getTypeLabel = (type: string) => {
  return types.find(t => t.value === type)?.label || type
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="community-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>社区活动管理</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon> 新增活动
          </el-button>
        </div>
      </template>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="title" label="活动名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="type" label="类型" width="100">
          <template #default="{ row }">
            {{ getTypeLabel(row.type) }}
          </template>
        </el-table-column>
        <el-table-column prop="location" label="地点" width="120" show-overflow-tooltip />
        <el-table-column prop="startTime" label="开始时间" width="160" />
        <el-table-column prop="maxParticipants" label="人数上限" width="90" />
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 'active' ? 'success' : 'info'" size="small">
              {{ row.status === 'active' ? '进行中' : '已结束' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑活动' : '新增活动'" width="600px">
      <el-form ref="formRef" :model="form" label-width="80px">
        <el-form-item label="活动名称" prop="title" required>
          <el-input v-model="form.title" placeholder="请输入活动名称" />
        </el-form-item>
        <el-form-item label="类型" prop="type">
          <el-select v-model="form.type" style="width: 100%">
            <el-option v-for="t in types" :key="t.value" :label="t.label" :value="t.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="地点" prop="location">
          <el-input v-model="form.location" placeholder="请输入活动地点" />
        </el-form-item>
        <el-form-item label="开始时间" prop="startTime">
          <el-date-picker v-model="form.startTime" type="datetime" placeholder="选择开始时间" style="width: 100%" />
        </el-form-item>
        <el-form-item label="结束时间" prop="endTime">
          <el-date-picker v-model="form.endTime" type="datetime" placeholder="选择结束时间" style="width: 100%" />
        </el-form-item>
        <el-form-item label="人数上限" prop="maxParticipants">
          <el-input-number v-model="form.maxParticipants" :min="0" style="width: 100%" />
        </el-form-item>
        <el-form-item label="活动内容" prop="content">
          <el-input v-model="form.content" type="textarea" :rows="3" placeholder="请输入活动内容" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.community-page { padding: 20px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
</style>
