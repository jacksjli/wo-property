<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ticketApi } from '../../api/http'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Refresh, Search, Clock, Check, Close, Warning, Setting } from '@element-plus/icons-vue'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getTicketFields = () => getActiveFields('ticket')

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

// 状态定义
const loading = ref(false)
const tickets = ref<any[]>([])
const stats = ref({ total: 0, open: 0, processing: 0, resolved: 0, closed: 0 })

// 筛选和分页
const searchQuery = ref('')
const filterStatus = ref('')
const filterPriority = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const total = ref(0)

// 对话框状态
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const dialogTitle = ref('创建工单')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)
const viewingTicket = ref<any>(null)

// 表单数据
const form = ref({
  title: '',
  description: '',
  type: 'Repair',
  priority: 'Normal',
  contactName: '',
  contactPhone: '',
  locationId: null as number | null
})

const rules: FormRules = {
  title: [{ required: true, message: '请输入工单标题', trigger: 'blur' }],
  description: [{ required: true, message: '请输入工单描述', trigger: 'blur' }],
  type: [{ required: true, message: '请选择工单类型', trigger: 'change' }],
  priority: [{ required: true, message: '请选择优先级', trigger: 'change' }]
}

// 选项配置
const typeOptions = [
  { value: 'Repair', label: '维修', icon: 'Tools' },
  { value: 'Access', label: '门禁', icon: 'Key' },
  { value: 'Cleaning', label: '清洁', icon: 'Brush' },
  { value: 'Security', label: '安保', icon: 'Shield' },
  { value: 'Other', label: '其他', icon: 'More' }
]

const priorityOptions = [
  { value: 'High', label: '高', type: 'danger' },
  { value: 'Normal', label: '中', type: 'warning' },
  { value: 'Low', label: '低', type: 'info' }
]

const statusOptions = [
  { value: 'Open', label: '待处理', type: 'primary' },
  { value: 'Processing', label: '处理中', type: 'warning' },
  { value: 'Resolved', label: '已解决', type: 'success' },
  { value: 'Closed', label: '已关闭', type: 'info' }
]

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const params: any = {
      page: currentPage.value,
      pageSize: pageSize.value
    }
    if (searchQuery.value) params.keyword = searchQuery.value
    if (filterStatus.value) params.status = filterStatus.value
    if (filterPriority.value) params.priority = filterPriority.value
    
    const response = await ticketApi.get('/api/tickets', { params })
    if (response.success) {
      tickets.value = response.data || []
      total.value = response.total || 0
    }
  } catch (error: any) {
    // API失败时使用模拟数据
    tickets.value = getMockData()
    total.value = tickets.value.length
    calculateStats(tickets.value)
  }
  loading.value = false
}

// 计算统计数据
const calculateStats = (data: any[]) => {
  stats.value = {
    total: data.length,
    open: data.filter(t => t.status === 'Open').length,
    processing: data.filter(t => t.status === 'Processing').length,
    resolved: data.filter(t => t.status === 'Resolved').length,
    closed: data.filter(t => t.status === 'Closed').length
  }
}

// 模拟数据
const getMockData = () => [
  { id: 1, ticketNumber: 'TK-2026-0001', title: 'A栋电梯故障报修', description: '电梯门无法关闭，存在安全隐患', type: 'Repair', priority: 'High', status: 'Open', creatorName: '王先生', assigneeName: null, createdAt: '2026-04-21 08:30', updatedAt: '2026-04-21 08:30' },
  { id: 2, ticketNumber: 'TK-2026-0002', title: '门禁卡消磁补办', description: '业主反映门禁卡无法刷卡', type: 'Access', priority: 'Normal', status: 'Processing', creatorName: '李女士', assigneeName: '张师傅', createdAt: '2026-04-21 09:15', updatedAt: '2026-04-21 10:00' },
  { id: 3, ticketNumber: 'TK-2026-0003', title: '公共区域路灯不亮', description: '夜间路灯熄灭，影响住户出行', type: 'Repair', priority: 'Low', status: 'Resolved', creatorName: '赵先生', assigneeName: '李师傅', createdAt: '2026-04-20 16:20', updatedAt: '2026-04-20 18:30' },
  { id: 4, ticketNumber: 'TK-2026-0004', title: '水管漏水报修', description: 'B栋走廊水管接头处漏水', type: 'Repair', priority: 'High', status: 'Open', creatorName: '张先生', assigneeName: null, createdAt: '2026-04-21 07:45', updatedAt: '2026-04-21 07:45' },
  { id: 5, ticketNumber: 'TK-2026-0005', title: '监控摄像头遮挡', description: '树枝遮挡了摄像头视野', type: 'Security', priority: 'Normal', status: 'Closed', creatorName: '刘经理', assigneeName: '保安队', createdAt: '2026-04-19 14:00', updatedAt: '2026-04-20 09:00' }
]

// 搜索和筛选
const handleSearch = () => {
  currentPage.value = 1
  loadData()
}

const handleFilterChange = () => {
  currentPage.value = 1
  loadData()
}

const handlePageChange = (page: number) => {
  currentPage.value = page
  loadData()
}

const handleSizeChange = (size: number) => {
  pageSize.value = size
  loadData()
}

// 打开创建对话框
const openCreateDialog = () => {
  dialogTitle.value = '创建工单'
  editingId.value = null
  form.value = {
    title: '',
    description: '',
    type: 'Repair',
    priority: 'Normal',
    contactName: '',
    contactPhone: '',
    locationId: null
  }
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑工单'
  editingId.value = row.id
  form.value = {
    title: row.title,
    description: row.description,
    type: row.type,
    priority: row.priority,
    contactName: row.contactName || '',
    contactPhone: row.contactPhone || '',
    locationId: row.locationId
  }
  dialogVisible.value = true
}

// 查看详情
const handleView = (row: any) => {
  viewingTicket.value = row
  detailDialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    submitting.value = true
    
    if (editingId.value) {
      await ticketApi.put(`/api/tickets/${editingId.value}`, form.value)
      ElMessage.success('工单更新成功')
    } else {
      await ticketApi.post('/api/tickets', form.value)
      ElMessage.success('工单创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) ElMessage.error(error.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

// 处理工单
const handleProcess = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要开始处理工单"${row.title}"吗？`, '处理确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })
    await ticketApi.put(`/api/tickets/${row.id}/status`, { status: 'Processing' })
    ElMessage.success('工单已开始处理')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '操作失败')
  }
}

// 完成工单
const handleResolve = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要完成工单"${row.title}"吗？`, '完成确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'success'
    })
    await ticketApi.put(`/api/tickets/${row.id}/status`, { status: 'Resolved' })
    ElMessage.success('工单已完成')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '操作失败')
  }
}

// 关闭工单
const handleClose = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要关闭工单"${row.title}"吗？`, '关闭确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await ticketApi.put(`/api/tickets/${row.id}/status`, { status: 'Closed' })
    ElMessage.success('工单已关闭')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '操作失败')
  }
}

// 删除工单
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除工单"${row.title}"吗？此操作不可恢复！`, '删除确认', {
      confirmButtonText: '确定删除',
      cancelButtonText: '取消',
      type: 'error'
    })
    await ticketApi.delete(`/api/tickets/${row.id}`)
    ElMessage.success('工单已删除')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

// 格式化时间
const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

// 获取状态配置
const getStatusConfig = (status: string) => {
  const config = statusOptions.find(s => s.value === status)
  return config || { label: status, type: 'info' }
}

// 获取优先级配置
const getPriorityConfig = (priority: string) => {
  const config = priorityOptions.find(p => p.value === priority)
  return config || { label: priority, type: 'info' }
}

// 获取类型标签
const getTypeLabel = (type: string) => {
  const config = typeOptions.find(t => t.value === type)
  return config ? config.label : type
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="ticket-list">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h1>工单管理</h1>
        <p>管理所有维修请求和服务工单</p>
      </div>
      <div class="header-right">
        <el-button type="default" @click="openFieldConfig">
          <el-icon><Setting /></el-icon>
          配置字段
        </el-button>
        <el-button type="primary" @click="openCreateDialog">
          <el-icon><Plus /></el-icon>
          创建工单
        </el-button>
        <el-button @click="loadData">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-grid">
      <el-card shadow="hover" class="stat-card" @click="filterStatus = ''; handleSearch()">
        <div class="stat-content">
          <div class="stat-icon total-icon"><el-icon><Clock /></el-icon></div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.total }}</div>
            <div class="stat-label">工单总数</div>
          </div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card" @click="filterStatus = 'Open'; handleSearch()">
        <div class="stat-content">
          <div class="stat-icon open-icon"><el-icon><Warning /></el-icon></div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.open }}</div>
            <div class="stat-label">待处理</div>
          </div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card" @click="filterStatus = 'Processing'; handleSearch()">
        <div class="stat-content">
          <div class="stat-icon processing-icon"><el-icon><Refresh /></el-icon></div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.processing }}</div>
            <div class="stat-label">处理中</div>
          </div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card" @click="filterStatus = 'Resolved'; handleSearch()">
        <div class="stat-content">
          <div class="stat-icon resolved-icon"><el-icon><Check /></el-icon></div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.resolved }}</div>
            <div class="stat-label">已解决</div>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 筛选工具栏 -->
    <el-card shadow="never" class="filter-card">
      <div class="filter-toolbar">
        <el-input
          v-model="searchQuery"
          placeholder="搜索工单编号、标题..."
          style="width: 280px;"
          clearable
          @keyup.enter="handleSearch"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select v-model="filterStatus" placeholder="状态筛选" clearable style="width: 130px;" @change="handleFilterChange">
          <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterPriority" placeholder="优先级筛选" clearable style="width: 130px;" @change="handleFilterChange">
          <el-option v-for="opt in priorityOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-button type="primary" @click="handleSearch"><el-icon><Search /></el-icon>搜索</el-button>
      </div>
    </el-card>

    <!-- 工单表格 -->
    <el-card shadow="never" class="table-card">
      <el-table :data="tickets" v-loading="loading" stripe @row-click="handleView">
        <el-table-column 
          v-for="field in getTicketFields()" 
          :key="'col-' + refreshKey + '-' + field.id"
          :prop="field.key" 
          :label="field.name" 
          :width="field.width"
          :align="field.align || 'center'"
        >
          <template #default="{ row }">
            <span v-if="field.key === 'ticketNo'" class="ticket-number">{{ row.ticketNumber }}</span>
            <span v-else-if="field.key === 'type'">{{ getTypeLabel(row.type) }}</span>
            <span v-else-if="field.key === 'status'">
              <el-tag :type="getStatusConfig(row.status).type" size="small">
                {{ getStatusConfig(row.status).label }}
              </el-tag>
            </span>
            <span v-else-if="field.key === 'priority'">
              <el-tag :type="getPriorityConfig(row.priority).type" size="small">
                {{ getPriorityConfig(row.priority).label }}
              </el-tag>
            </span>
            <span v-else-if="field.key === 'createTime'">{{ formatDateTime(row.createdAt) }}</span>
            <span v-else>{{ row[field.key] || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleView(row)">详情</el-button>
            <el-button link type="warning" size="small" v-if="row.status === 'Open'" @click.stop="handleProcess(row)">处理</el-button>
            <el-button link type="success" size="small" v-if="row.status === 'Processing'" @click.stop="handleResolve(row)">完成</el-button>
            <el-button link type="danger" size="small" v-if="row.status !== 'Closed'" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="handlePageChange"
          @size-change="handleSizeChange"
        />
      </div>
    </el-card>

    <!-- 创建/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="工单标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入工单标题" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="工单描述" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="4" placeholder="请详细描述工单内容" maxlength="500" show-word-limit />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="工单类型" prop="type">
              <el-select v-model="form.type" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="优先级" prop="priority">
              <el-select v-model="form.priority" style="width: 100%">
                <el-option v-for="opt in priorityOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="联系人">
              <el-input v-model="form.contactName" placeholder="请输入联系人姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系电话">
              <el-input v-model="form.contactPhone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 工单详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="工单详情" width="700px">
      <template v-if="viewingTicket">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="工单编号">{{ viewingTicket.ticketNumber }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusConfig(viewingTicket.status).type" size="small">
              {{ getStatusConfig(viewingTicket.status).label }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="工单标题" :span="2">{{ viewingTicket.title }}</el-descriptions-item>
          <el-descriptions-item label="类型">{{ getTypeLabel(viewingTicket.type) }}</el-descriptions-item>
          <el-descriptions-item label="优先级">
            <el-tag :type="getPriorityConfig(viewingTicket.priority).type" size="small">
              {{ getPriorityConfig(viewingTicket.priority).label }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="创建人">{{ viewingTicket.creatorName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDateTime(viewingTicket.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="2">{{ viewingTicket.description }}</el-descriptions-item>
          <el-descriptions-item label="联系人">{{ viewingTicket.contactName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ viewingTicket.contactPhone || '-' }}</el-descriptions-item>
        </el-descriptions>
      </template>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <el-button type="primary" v-if="viewingTicket?.status === 'Open'" @click="handleProcess(viewingTicket); detailDialogVisible = false">开始处理</el-button>
        <el-button type="success" v-if="viewingTicket?.status === 'Processing'" @click="handleResolve(viewingTicket); detailDialogVisible = false">完成工单</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="ticket" 
      module-name="工单管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.ticket-list { width: 100%; }

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;
}
.header-left h1 { font-size: 24px; font-weight: 600; color: #1f2937; margin: 0 0 4px 0; }
.header-left p { font-size: 14px; color: #6b7280; margin: 0; }
.header-right { display: flex; gap: 12px; }

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 16px;
}
.stat-card { cursor: pointer; transition: transform 0.2s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { display: flex; align-items: center; gap: 16px; }
.stat-icon { width: 48px; height: 48px; border-radius: 12px; display: flex; align-items: center; justify-content: center; font-size: 24px; }
.total-icon { background: rgba(59, 130, 246, 0.1); color: #3b82f6; }
.open-icon { background: rgba(239, 68, 68, 0.1); color: #ef4444; }
.processing-icon { background: rgba(245, 158, 11, 0.1); color: #f59e0b; }
.resolved-icon { background: rgba(16, 185, 129, 0.1); color: #10b981; }
.stat-value { font-size: 28px; font-weight: 700; color: #1f2937; }
.stat-label { font-size: 14px; color: #6b7280; }

.filter-card { margin-bottom: 16px; }
.filter-toolbar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }

.table-card { border-radius: 8px; }
.ticket-number { font-family: Monaco, Menlo, monospace; font-weight: 500; color: #374151; }

.pagination-wrapper { display: flex; justify-content: center; margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e7eb; }
</style>
