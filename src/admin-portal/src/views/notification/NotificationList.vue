<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, Bell, Top, Close, View } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import {
  getAllNotifications,
  getNotificationStats,
  addNotification,
  updateNotification,
  deleteNotification,
  publishNotification,
  cancelNotification,
  togglePinNotification,
  getLevelColor,
  getStatusType,
  notificationTypeLabels,
  notificationLevelLabels,
  notificationStatusLabels,
  sendMethodLabels,
  type Notification,
  type NotificationType,
  type NotificationLevel,
  type NotificationStatus
} from '@/stores/notification'

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getNotificationFields = () => getActiveFields('notification')

// 打开字段配置（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 刷新字段
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

// 数据
const notifications = ref<Notification[]>(getAllNotifications())
const stats = computed(() => getNotificationStats())

// 筛选
const filterStatus = ref<NotificationStatus | ''>('')
const filterType = ref<NotificationType | ''>('')

// 筛选后的通知
const filteredNotifications = computed(() => {
  let result = notifications.value
  
  if (filterStatus.value) {
    result = result.filter(n => n.status === filterStatus.value)
  }
  
  if (filterType.value) {
    result = result.filter(n => n.type === filterType.value)
  }
  
  // 置顶优先，然后按发布时间倒序
  return result.sort((a, b) => {
    if (a.isPinned !== b.isPinned) return b.isPinned ? 1 : -1
    return (b.publishTime || b.createdAt).localeCompare(a.publishTime || a.createdAt)
  })
})

// 对话框状态
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const dialogTitle = ref('新增通知')
const editingId = ref<number | null>(null)
const viewingNotification = ref<Notification | null>(null)

// 表单数据
const form = ref({
  notificationNo: '',
  title: '',
  type: 'notice' as NotificationType,
  level: 'info' as NotificationLevel,
  content: '',
  sendMethod: 'app' as string,
  sendScope: 'all' as string,
  targetBuildings: [] as string[],
  publisher: '',
  startTime: '',
  endTime: '',
  status: 'draft' as NotificationStatus,
  isPinned: false,
  remark: ''
})

// 选项
const typeOptions = Object.entries(notificationTypeLabels).map(([value, label]) => ({ value, label }))
const levelOptions = Object.entries(notificationLevelLabels).map(([value, label]) => ({ value, label }))
const statusOptions = Object.entries(notificationStatusLabels).map(([value, label]) => ({ value, label }))
const methodOptions = Object.entries(sendMethodLabels).map(([value, label]) => ({ value, label }))

// 打开新增对话框
const handleAdd = () => {
  dialogTitle.value = '新增通知'
  editingId.value = null
  const now = new Date().toISOString()
  const today = now.split('T')[0]
  form.value = {
    notificationNo: 'NOT-' + new Date().getFullYear() + '-' + String(Date.now()).slice(-4),
    title: '',
    type: 'notice',
    level: 'info',
    content: '',
    sendMethod: 'app',
    sendScope: 'all',
    targetBuildings: [],
    publisher: '物业管理员',
    startTime: today,
    endTime: '',
    status: 'draft',
    isPinned: false,
    remark: ''
  }
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: Notification) => {
  dialogTitle.value = '编辑通知'
  editingId.value = row.id
  form.value = {
    notificationNo: row.notificationNo,
    title: row.title,
    type: row.type,
    level: row.level,
    content: row.content,
    sendMethod: row.sendMethod,
    sendScope: row.sendScope,
    targetBuildings: row.targetBuildings || [],
    publisher: row.publisher,
    startTime: row.startTime ? row.startTime.split('T')[0] : '',
    endTime: row.endTime ? row.endTime.split('T')[0] : '',
    status: row.status,
    isPinned: row.isPinned,
    remark: row.remark
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = () => {
  if (!form.value.title.trim()) {
    ElMessage.warning('请输入通知标题')
    return
  }
  if (!form.value.content.trim()) {
    ElMessage.warning('请输入通知内容')
    return
  }

  const data = {
    ...form.value,
    startTime: form.value.startTime ? form.value.startTime + 'T00:00:00' : '',
    endTime: form.value.endTime ? form.value.endTime + 'T23:59:59' : ''
  }

  if (editingId.value) {
    updateNotification(editingId.value, data)
    ElMessage.success('更新成功')
  } else {
    addNotification({
      ...data,
      publishTime: undefined,
      attachments: []
    })
    ElMessage.success('添加成功')
  }

  notifications.value = getAllNotifications()
  dialogVisible.value = false
}

// 删除通知
const handleDelete = async (row: Notification) => {
  try {
    await ElMessageBox.confirm(`确定删除通知 "${row.title}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    deleteNotification(row.id)
    notifications.value = getAllNotifications()
    ElMessage.success('删除成功')
  } catch {
    // 取消
  }
}

// 查看详情
const handleView = (row: Notification) => {
  viewingNotification.value = row
  detailDialogVisible.value = true
}

// 发布
const handlePublish = async (row: Notification) => {
  try {
    await ElMessageBox.confirm(`确定发布通知 "${row.title}" 吗？`, '发布确认', {
      confirmButtonText: '发布',
      cancelButtonText: '取消',
      type: 'info'
    })
    publishNotification(row.id)
    notifications.value = getAllNotifications()
    ElMessage.success('通知已发布')
  } catch {
    // 取消
  }
}

// 撤回
const handleCancel = async (row: Notification) => {
  try {
    await ElMessageBox.confirm(`确定撤回通知 "${row.title}" 吗？`, '撤回确认', {
      confirmButtonText: '撤回',
      cancelButtonText: '取消',
      type: 'warning'
    })
    cancelNotification(row.id)
    notifications.value = getAllNotifications()
    ElMessage.success('通知已撤回')
  } catch {
    // 取消
  }
}

// 置顶/取消置顶
const handleTogglePin = async (row: Notification) => {
  togglePinNotification(row.id)
  notifications.value = getAllNotifications()
  ElMessage.success(row.isPinned ? '已取消置顶' : '已置顶')
}

// 刷新数据
const handleRefresh = () => {
  notifications.value = getAllNotifications()
  ElMessage.success('已刷新')
}

// 格式化内容（换行）
const formatContent = (content: string) => {
  return content.replace(/\n/g, '<br>')
}
</script>

<template>
  <div class="notification-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>消息管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增通知
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="消息管理说明"
        description="管理所有系统通知、公告、提醒等，支持发布、撤回、置顶功能。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value">{{ stats.total }}</div>
            <div class="stat-label">通知总数</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value published">{{ stats.published }}</div>
            <div class="stat-label">已发布</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value draft">{{ stats.draft }}</div>
            <div class="stat-label">草稿</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value pinned">{{ stats.pinned }}</div>
            <div class="stat-label">置顶</div>
          </div>
        </el-card>
      </div>

      <!-- 通知列表 -->
      <el-table :data="filteredNotifications" stripe @row-click="handleView">
        <el-table-column label="" width="40" align="center">
          <template #default="{ row }">
            <el-icon v-if="row.isPinned" color="#E6A23C"><Top /></el-icon>
          </template>
        </el-table-column>
        <el-table-column prop="notificationNo" label="编号" width="120" />
        <el-table-column prop="title" label="通知标题" min-width="200">
          <template #default="{ row }">
            <span style="font-weight: 600;">{{ row.title }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="type" label="类型" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small">{{ notificationTypeLabels[row.type] }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="level" label="级别" width="90" align="center">
          <template #default="{ row }">
            <el-tag :style="{ backgroundColor: getLevelColor(row.level), borderColor: getLevelColor(row.level), color: '#fff' }" size="small">
              {{ notificationLevelLabels[row.level] }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sendMethod" label="发送方式" width="100" align="center">
          <template #default="{ row }">
            {{ sendMethodLabels[row.sendMethod as keyof typeof sendMethodLabels] || row.sendMethod }}
          </template>
        </el-table-column>
        <el-table-column prop="publishTime" label="发布时间" width="150" align="center">
          <template #default="{ row }">
            {{ row.publishTime ? row.publishTime.slice(0, 16).replace('T', ' ') : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ notificationStatusLabels[row.status] }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="250" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="success" size="small" @click.stop="handlePublish(row)" v-if="row.status === 'draft'">
              发布
            </el-button>
            <el-button link type="warning" size="small" @click.stop="handleCancel(row)" v-if="row.status === 'published'">
              <el-icon><Close /></el-icon> 撤回
            </el-button>
            <el-button link type="info" size="small" @click.stop="handleTogglePin(row)">
              <el-icon><Top /></el-icon> {{ row.isPinned ? '取消置顶' : '置顶' }}
            </el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="700px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="通知编号">
              <el-input v-model="form.notificationNo" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="通知标题" required>
              <el-input v-model="form.title" placeholder="请输入通知标题" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="通知类型">
              <el-select v-model="form.type" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="紧急程度">
              <el-select v-model="form.level" style="width: 100%">
                <el-option v-for="opt in levelOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="发送方式">
              <el-select v-model="form.sendMethod" style="width: 100%">
                <el-option v-for="opt in methodOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="通知内容" required>
          <el-input v-model="form.content" type="textarea" rows="5" placeholder="请输入通知内容" />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="发布人">
              <el-input v-model="form.publisher" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="生效时间">
              <el-date-picker v-model="form.startTime" type="date" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="失效时间">
              <el-date-picker v-model="form.endTime" type="date" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="置顶">
          <el-switch v-model="form.isPinned" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 通知详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="通知详情" width="700px">
      <div v-if="viewingNotification" class="notification-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="通知编号">{{ viewingNotification.notificationNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(viewingNotification.status)">
              {{ notificationStatusLabels[viewingNotification.status] }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="通知标题" :span="2">
            <span style="font-weight: 600; font-size: 16px;">{{ viewingNotification.title }}</span>
            <el-icon v-if="viewingNotification.isPinned" color="#E6A23C" style="margin-left: 8px;"><Top /></el-icon>
          </el-descriptions-item>
          <el-descriptions-item label="类型">
            <el-tag size="small">{{ notificationTypeLabels[viewingNotification.type] }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="级别">
            <el-tag :style="{ backgroundColor: getLevelColor(viewingNotification.level), borderColor: getLevelColor(viewingNotification.level), color: '#fff' }" size="small">
              {{ notificationLevelLabels[viewingNotification.level] }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="发送方式">
            {{ sendMethodLabels[viewingNotification.sendMethod as keyof typeof sendMethodLabels] || viewingNotification.sendMethod }}
          </el-descriptions-item>
          <el-descriptions-item label="发布人">{{ viewingNotification.publisher }}</el-descriptions-item>
          <el-descriptions-item label="发布时间">
            {{ viewingNotification.publishTime ? viewingNotification.publishTime.slice(0, 16).replace('T', ' ') : '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="生效时间">
            {{ viewingNotification.startTime ? viewingNotification.startTime.slice(0, 10) : '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="失效时间">
            {{ viewingNotification.endTime ? viewingNotification.endTime.slice(0, 10) : '-' }}
          </el-descriptions-item>
        </el-descriptions>

        <el-card shadow="never" style="margin-top: 20px;">
          <template #header>
            <span>通知内容</span>
          </template>
          <div v-html="formatContent(viewingNotification.content)" style="line-height: 1.8;"></div>
        </el-card>

        <el-alert
          v-if="viewingNotification.remark"
          :title="'备注：' + viewingNotification.remark"
          type="info"
          :closable="false"
          style="margin-top: 15px;"
        />
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="notification"
      module-name="消息管理"
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.notification-page {
  width: 100%;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.header-actions {
  display: flex;
  gap: 10px;
}
.stats-grid {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
}
.stat-card {
  flex: 1;
  cursor: pointer;
  transition: all 0.3s;
}
.stat-card:hover {
  transform: translateY(-2px);
}
.stat-content {
  text-align: center;
}
.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: #409EFF;
}
.stat-value.published { color: #67C23A; }
.stat-value.draft { color: #E6A23C; }
.stat-value.pinned { color: #F56C6C; }
.stat-label {
  font-size: 13px;
  color: #909399;
  margin-top: 5px;
}
.notification-detail {
  padding: 10px;
}
</style>