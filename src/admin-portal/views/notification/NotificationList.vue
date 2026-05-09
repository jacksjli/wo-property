<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { notificationApi } from '../../api/http'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Setting } from '@element-plus/icons-vue'
import { getActiveFields } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

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

const loading = ref(false)
const notifications = ref<any[]>([])
const pagination = ref({
  page: 1,
  pageSize: 10,
  total: 0
})

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('发送通知')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const viewingNotification = ref<any>(null)
const viewDialogVisible = ref(false)

const form = ref({
  title: '',
  content: '',
  type: 'System',
  priority: 'Medium',
  targetUsers: [] as number[],
  expireHours: 72
})

const rules: FormRules = {
  title: [
    { required: true, message: '请输入通知标题', trigger: 'blur' }
  ],
  content: [
    { required: true, message: '请输入通知内容', trigger: 'blur' }
  ],
  type: [
    { required: true, message: '请选择通知类型', trigger: 'change' }
  ]
}

const typeOptions = [
  { value: 'System', label: '系统通知' },
  { value: 'Notice', label: '公告' },
  { value: 'Alert', label: '警报' },
  { value: 'Reminder', label: '提醒' }
]

const priorityOptions = [
  { value: 'High', label: '高' },
  { value: 'Medium', label: '中' },
  { value: 'Low', label: '低' }
]

const loadData = async () => {
  loading.value = true
  try {
    const response = await notificationApi.get('/api/notifications', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.pageSize
      }
    })
    if (response.success) {
      notifications.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    console.error('加载数据失败:', error)
  }
  loading.value = false
}

const handlePageChange = (page: number) => {
  pagination.value.page = page
  loadData()
}

const getTypeColor = (type: string) => {
  const colors: Record<string, string> = {
    'System': '#409eff',
    'Notice': '#67c23a',
    'Alert': '#f56c6c',
    'Reminder': '#909399'
  }
  return colors[type] || '#409eff'
}

const getTypeText = (type: string) => {
  const texts: Record<string, string> = {
    'System': '系统',
    'Notice': '公告',
    'Alert': '警报',
    'Reminder': '提醒'
  }
  return texts[type] || type
}

const openCreateDialog = () => {
  form.value = {
    title: '',
    content: '',
    type: 'System',
    priority: 'Medium',
    targetUsers: [],
    expireHours: 72
  }
  dialogTitle.value = '发送通知'
  dialogVisible.value = true
}

const handleView = (row: any) => {
  viewingNotification.value = row
  viewDialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    submitting.value = true
    
    await notificationApi.post('/api/notifications', form.value)
    ElMessage.success('通知发送成功')
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) {
      ElMessage.error(error.message || '发送失败')
    }
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除通知"${row.title}"吗？`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    await notificationApi.delete(`/api/notifications/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
    }
  }
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="notification-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>通知管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openCreateDialog">
              <el-icon><Plus /></el-icon> 发送通知
            </el-button>
          </div>
        </div>
      </template>
      
      <el-table :data="notifications" v-loading="loading" stripe>
        <el-table-column prop="title" label="标题" min-width="200" />
        <el-table-column prop="content" label="内容" min-width="300" show-overflow-tooltip />
        <el-table-column prop="type" label="类型" width="100">
          <template #default="{ row }">
            <el-tag :color="getTypeColor(row.type)" style="color: #fff; border: none;">
              {{ getTypeText(row.type) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="priority" label="优先级" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.priority === 'High'" type="danger" size="small">高</el-tag>
            <el-tag v-else-if="row.priority === 'Medium'" type="warning" size="small">中</el-tag>
            <el-tag v-else type="info" size="small">低</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="发布时间" width="180">
          <template #default="{ row }">
            {{ new Date(row.createdAt).toLocaleString('zh-CN') }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleView(row)">查看</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      
      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="pagination.page"
          :page-size="pagination.pageSize"
          :total="pagination.total"
          layout="total, prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>
    
    <!-- 发送通知对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
      >
        <el-form-item label="通知标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入通知标题" maxlength="100" show-word-limit />
        </el-form-item>
        
        <el-form-item label="通知内容" prop="content">
          <el-input 
            v-model="form.content" 
            type="textarea" 
            :rows="4" 
            placeholder="请输入通知内容"
            maxlength="1000"
            show-word-limit
          />
        </el-form-item>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="通知类型" prop="type">
              <el-select v-model="form.type" placeholder="请选择类型" style="width: 100%">
                <el-option
                  v-for="opt in typeOptions"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="优先级">
              <el-select v-model="form.priority" placeholder="请选择优先级" style="width: 100%">
                <el-option
                  v-for="opt in priorityOptions"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-form-item label="过期时间">
          <el-select v-model="form.expireHours" placeholder="选择过期时间" style="width: 100%">
            <el-option :value="24" label="24小时后" />
            <el-option :value="48" label="48小时后" />
            <el-option :value="72" label="72小时后" />
            <el-option :value="168" label="7天后" />
            <el-option :value="0" label="永不过期" />
          </el-select>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          发送
        </el-button>
      </template>
    </el-dialog>
    
    <!-- 查看通知对话框 -->
    <el-dialog
      v-model="viewDialogVisible"
      title="通知详情"
      width="600px"
    >
      <template v-if="viewingNotification">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="标题">{{ viewingNotification.title }}</el-descriptions-item>
          <el-descriptions-item label="类型">
            <el-tag :color="getTypeColor(viewingNotification.type)" style="color: #fff; border: none;">
              {{ getTypeText(viewingNotification.type) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="优先级">
            <el-tag v-if="viewingNotification.priority === 'High'" type="danger" size="small">高</el-tag>
            <el-tag v-else-if="viewingNotification.priority === 'Medium'" type="warning" size="small">中</el-tag>
            <el-tag v-else type="info" size="small">低</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="发布时间">
            {{ new Date(viewingNotification.createdAt).toLocaleString('zh-CN') }}
          </el-descriptions-item>
          <el-descriptions-item label="内容" :span="2">{{ viewingNotification.content }}</el-descriptions-item>
        </el-descriptions>
      </template>
      <template #footer>
        <el-button @click="viewDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="notification" 
      module-name="通知管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.header-actions {
  display: flex;
  gap: 10px;
}

<style scoped>
.notification-list {
  width: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
