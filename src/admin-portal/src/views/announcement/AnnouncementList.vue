<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Delete, Edit, View } from '@element-plus/icons-vue'
import { getAnnouncementList, createAnnouncement, updateAnnouncement, deleteAnnouncement, publishAnnouncement } from '@/api/announcement'
import { getServiceUrl } from '@/api/config'

const loading = ref(false)
const list = ref<any[]>([])
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const formRef = ref()
const submitting = ref(false)
const publishing = ref<number | null>(null)

const form = ref({
  title: '',
  content: '',
  category: 'property',
  level: 'Normal',
  isPinned: false,
  status: 'draft'
})

const categories = [
  { value: 'property', label: '物业通知' },
  { value: 'security', label: '安全公告' },
  { value: 'maintenance', label: '设施维护' },
  { value: 'activity', label: '社区活动' },
  { value: 'emergency', label: '紧急通知' },
  { value: 'other', label: '其他' }
]

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getAnnouncementList({ pageSize: 100 })
    list.value = res.data?.records || res.data || []
  } catch (error) {
    console.error('加载失败:', error)
  }
  loading.value = false
}

const handleAdd = () => {
  editingId.value = null
  form.value = { title: '', content: '', category: 'property', level: 'Normal', isPinned: false, status: 'draft' }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  editingId.value = row.id
  form.value = {
    title: row.title,
    content: row.content,
    category: row.category,
    level: row.level,
    isPinned: row.isPinned,
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
      await updateAnnouncement(editingId.value, form.value)
      ElMessage.success('更新成功')
    } else {
      await createAnnouncement(form.value)
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
    await ElMessageBox.confirm(`确定删除公告「${row.title}」吗？`, '确认删除')
    await deleteAnnouncement(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handlePublish = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定发布公告「${row.title}」吗？`, '确认发布')
    publishing.value = row.id
    await publishAnnouncement(row.id)
    ElMessage.success('发布成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('发布失败')
    }
  } finally {
    publishing.value = null
  }
}

const getCategoryLabel = (cat: string) => {
  return categories.find(c => c.value === cat)?.label || cat
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="announcement-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>公告管理</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon> 新增公告
          </el-button>
        </div>
      </template>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="title" label="标题" min-width="200" show-overflow-tooltip />
        <el-table-column prop="category" label="分类" width="100">
          <template #default="{ row }">
            {{ getCategoryLabel(row.category) }}
          </template>
        </el-table-column>
        <el-table-column prop="level" label="级别" width="80" />
        <el-table-column prop="isPinned" label="置顶" width="60">
          <template #default="{ row }">
            <el-tag v-if="row.isPinned" type="warning" size="small">置顶</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 'published' ? 'success' : 'info'" size="small">
              {{ row.status === 'published' ? '已发布' : '草稿' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="publishTime" label="发布时间" width="160" />
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="success" @click="handlePublish(row)" :loading="publishing === row.id" v-if="row.status !== 'Published'">发布</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑公告' : '新增公告'" width="600px">
      <el-form ref="formRef" :model="form" label-width="80px">
        <el-form-item label="标题" prop="title" required>
          <el-input v-model="form.title" placeholder="请输入公告标题" />
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-select v-model="form.category" style="width: 100%">
            <el-option v-for="c in categories" :key="c.value" :label="c.label" :value="c.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="级别" prop="level">
          <el-input v-model="form.level" placeholder="如: Normal, Important, Urgent" />
        </el-form-item>
        <el-form-item label="内容" prop="content" required>
          <el-input v-model="form.content" type="textarea" :rows="4" placeholder="请输入公告内容" />
        </el-form-item>
        <el-form-item label="置顶">
          <el-switch v-model="form.isPinned" />
        </el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="form.status">
            <el-radio label="draft">草稿</el-radio>
            <el-radio label="published">发布</el-radio>
          </el-radio-group>
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
.announcement-page { padding: 20px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
</style>
