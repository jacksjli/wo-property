<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Bell, Top, Close, View } from '@element-plus/icons-vue'
import { getNotifications, createNotification, updateNotification, deleteNotification, markReadNotification } from '@/api/notification'

// 通知类型/级别/状态标签
const typeLabels: Record<string, string> = {
  system: '系统通知',
  notice: '温馨提示',
  alert: '预警通知',
  reminder: '提醒通知',
  announcement: '公告',
  Ticket: '工单通知',
  Announcement: '系统公告'
}

const priorityLabels: Record<string, string> = {
  Normal: '普通',
  High: '重要',
  Urgent: '紧急'
}

const statusLabels: Record<string, string> = {
  published: '已发布',
  draft: '草稿',
  cancelled: '已撤回'
}

// 数据
const notifications = ref<any[]>([])
const loading = ref(false)

// 筛选
const filterType = ref('')
const filterPriority = ref('')

// 筛选后的通知
const filteredNotifications = computed(() => {
  let result = notifications.value
  if (filterType.value) result = result.filter(n => n.type === filterType.value)
  if (filterPriority.value) result = result.filter(n => n.priority === filterPriority.value)
  return result.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
})

// 统计
const stats = computed(() => ({
  total: notifications.value.length,
  unread: notifications.value.filter(n => !n.isRead).length,
  today: notifications.value.filter(n => {
    const today = new Date().toISOString().split('T')[0]
    return n.createdAt?.startsWith(today)
  }).length
}))

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('发送通知')
const editingId = ref<number | null>(null)

const form = ref({
  userId: 0 as number | null,
  title: '',
  content: '',
  type: 'system',
  priority: 'Normal'
})

const typeOptions = Object.entries(typeLabels).map(([v, l]) => ({ value: v, label: l }))
const priorityOptions = Object.entries(priorityLabels).map(([v, l]) => ({ value: v, label: l }))

// 加载数据
const loadingData = async () => {
  loading.value = true
  try {
    const res: any = await getNotifications({ page: 1, pageSize: 200 })
    if (res.success) {
      notifications.value = res.data?.records || res.data || []
    } else {
      ElMessage.error(res.message || '加载失败')
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败')
  } finally {
    loading.value = false
  }
}

// 打开新增
const handleAdd = () => {
  dialogTitle.value = '发送通知'
  editingId.value = null
  form.value = { userId: 0, title: '', content: '', type: 'system', priority: 'Normal' }
  dialogVisible.value = true
}

// 打开编辑
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑通知'
  editingId.value = row.id
  form.value = {
    userId: row.userId,
    title: row.title,
    content: row.content,
    type: row.type,
    priority: row.priority
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!form.value.title.trim()) { ElMessage.warning('请输入标题'); return }
  if (!form.value.content.trim()) { ElMessage.warning('请输入内容'); return }

  try {
    const payload = {
      userId: form.value.userId ?? 0,
      title: form.value.title,
      content: form.value.content,
      type: form.value.type,
      priority: form.value.priority
    }
    if (editingId.value) {
      await updateNotification(editingId.value, payload)
      ElMessage.success('更新成功')
    } else {
      await createNotification(payload)
      ElMessage.success('发送成功')
    }
    dialogVisible.value = false
    await loadingData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除通知「${row.title}」吗？`, '删除确认', { type: 'warning' })
    await deleteNotification(row.id)
    ElMessage.success('删除成功')
    await loadingData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

// 标记已读
const handleMarkRead = async (row: any) => {
  try {
    await markReadNotification(row.id)
    ElMessage.success('已标记为已读')
    await loadingData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

// 获取状态类型（用于表格标签颜色）
const getStatusType = (notification: any) => {
  if (notification.isRead) return 'info'
  if (notification.priority === 'Urgent') return 'danger'
  if (notification.priority === 'High') return 'warning'
  return 'success'
}

const getTypeLabel = (type: string) => typeLabels[type] || type
const getPriorityLabel = (priority: string) => priorityLabels[priority] || priority

onMounted(() => {
  loadingData()
})
</script>

<template>
  <div class="notification-container">
    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :span="8">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon size="32" color="#409EFF"><Bell /></el-icon>
            <div>
              <div class="stat-value">{{ stats.total }}</div>
              <div class="stat-label">通知总数</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon size="32" color="#67C23A"><View /></el-icon>
            <div>
              <div class="stat-value">{{ stats.unread }}</div>
              <div class="stat-label">未读通知</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon size="32" color="#E6A23C"><Top /></el-icon>
            <div>
              <div class="stat-value">{{ stats.today }}</div>
              <div class="stat-label">今日发送</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 操作栏 -->
    <div class="toolbar">
      <div class="filters">
        <el-select v-model="filterType" placeholder="通知类型" clearable style="width: 140px">
          <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterPriority" placeholder="优先级" clearable style="width: 120px">
          <el-option v-for="opt in priorityOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </div>
      <div class="actions">
        <el-button type="primary" :icon="Plus" @click="handleAdd">发送通知</el-button>
        <el-button :icon="Refresh" @click="loadingData">刷新</el-button>
      </div>
    </div>

    <!-- 表格 -->
    <el-table :data="filteredNotifications" v-loading="loading" stripe style="width: 100%">
      <el-table-column prop="id" label="ID" width="60" />
      <el-table-column prop="title" label="标题" min-width="200" show-overflow-tooltip />
      <el-table-column prop="type" label="类型" width="100">
        <template #default="{ row }">{{ getTypeLabel(row.type) }}</template>
      </el-table-column>
      <el-table-column prop="priority" label="优先级" width="90">
        <template #default="{ row }">
          <el-tag :type="row.priority === 'Urgent' ? 'danger' : row.priority === 'High' ? 'warning' : 'info'" size="small">
            {{ getPriorityLabel(row.priority) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="isRead" label="状态" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isRead ? 'info' : 'success'" size="small">{{ row.isRead ? '已读' : '未读' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createdAt" label="发送时间" width="160">
        <template #default="{ row }">{{ row.createdAt?.replace('T', ' ').slice(0, 16) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" size="small" @click="handleMarkRead(row)" :disabled="row.isRead">已读</el-button>
          <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
          <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 发送/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px" destroy-on-close>
      <el-form :model="form" label-width="100px">
        <el-form-item label="目标用户">
          <el-input-number v-model="form.userId" :min="0" placeholder="0表示全体用户" style="width: 200px" />
          <span class="form-tip">（0 = 全体用户）</span>
        </el-form-item>
        <el-form-item label="通知标题" required>
          <el-input v-model="form.title" placeholder="请输入通知标题" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item label="通知内容" required>
          <el-input v-model="form.content" type="textarea" :rows="4" placeholder="请输入通知内容" maxlength="2000" show-word-limit />
        </el-form-item>
        <el-form-item label="通知类型">
          <el-select v-model="form.type" style="width: 200px">
            <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="form.priority" style="width: 200px">
            <el-option v-for="opt in priorityOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.notification-container { padding: 20px; }
.stats-row { margin-bottom: 20px; }
.stat-card { display: flex; align-items: center; gap: 16px; }
.stat-value { font-size: 28px; font-weight: bold; color: #303133; }
.stat-label { font-size: 14px; color: #909399; }
.toolbar { display: flex; justify-content: space-between; margin-bottom: 16px; }
.filters { display: flex; gap: 12px; }
.actions { display: flex; gap: 8px; }
.form-tip { margin-left: 8px; color: #909399; font-size: 12px; }
</style>