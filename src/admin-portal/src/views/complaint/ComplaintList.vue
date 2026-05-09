<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

const getComplaintFields = () => getActiveFields('complaint')

const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

const refreshKey = ref(0)
const refreshFields = () => { refreshKey.value++ }

const loading = ref(false)
const recordList = ref<any[]>([])
const total = ref(0)

const filterKeyword = ref('')
const filterType = ref('')
const filterPriority = ref('')
const filterStatus = ref('')
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const dialogTitle = ref('新增投诉')
const editingId = ref<number | null>(null)
const viewingRecord = ref<any>(null)

const form = ref({
  complaintNo: '',
  title: '',
  type: 'service',
  source: 'phone',
  priority: 'normal',
  description: '',
  complainantName: '',
  complainantPhone: '',
  complainantRoom: '',
  location: '',
  handlerName: '',
  deadline: '',
  remark: ''
})

const typeOptions = [
  { value: 'service', label: '服务投诉' },
  { value: 'facility', label: '设施问题' },
  { value: 'safety', label: '安全隐患' },
  { value: 'cleaning', label: '清洁问题' },
  { value: 'other', label: '其他' }
]

const sourceOptions = [
  { value: 'phone', label: '电话' },
  { value: 'online', label: '在线' },
  { value: 'in_person', label: '现场' },
  { value: 'other', label: '其他' }
]

const priorityOptions = [
  { value: 'normal', label: '普通' },
  { value: 'urgent', label: '紧急' },
  { value: 'immediate', label: '立即处理' }
]

const statusOptions = [
  { value: 'pending', label: '待处理' },
  { value: 'processing', label: '处理中' },
  { value: 'resolved', label: '已解决' },
  { value: 'closed', label: '已关闭' }
]

const stats = computed(() => ({
  total: recordList.value.length,
  urgent: recordList.value.filter((c: any) => c.Priority === 'urgent').length,
  pending: recordList.value.filter((c: any) => c.HandleStatus === 'pending').length,
  processing: recordList.value.filter((c: any) => c.HandleStatus === 'processing').length,
  resolved: recordList.value.filter((c: any) => c.HandleStatus === 'resolved').length
}))

const filteredList = computed(() => {
  let result = recordList.value
  if (filterKeyword.value) {
    const kw = filterKeyword.value.toLowerCase()
    result = result.filter((r: any) =>
      (r.ComplaintNo?.toLowerCase().includes(kw)) ||
      (r.Title?.toLowerCase().includes(kw)) ||
      (r.ComplainantName?.toLowerCase().includes(kw))
    )
  }
  if (filterType.value) result = result.filter((r: any) => r.Type === filterType.value)
  if (filterPriority.value) result = result.filter((r: any) => r.Priority === filterPriority.value)
  if (filterStatus.value) result = result.filter((r: any) => r.HandleStatus === filterStatus.value)
  return result.sort((a: any, b: any) => b.CreatedAt.localeCompare(a.CreatedAt))
})

const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 200 }
    if (filterKeyword.value) params.keyword = filterKeyword.value
    if (filterType.value) params.type = filterType.value
    if (filterPriority.value) params.priority = filterPriority.value
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await masterApi.get('/complaints', { params })
    recordList.value = res.data || []
    total.value = res.total || 0
  } catch (e: any) {
    console.error('Load data error:', e)
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => loadData())

const handleAdd = () => {
  editingId.value = null
  dialogTitle.value = '新增投诉'
  const today = new Date().toISOString().split('T')[0]
  form.value = {
    complaintNo: 'CPT-' + new Date().getFullYear() + '-' + String(Date.now()).slice(-4),
    title: '',
    type: 'service',
    source: 'phone',
    priority: 'normal',
    description: '',
    complainantName: '',
    complainantPhone: '',
    complainantRoom: '',
    location: '',
    handlerName: '',
    deadline: today,
    remark: ''
  }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  editingId.value = row.Id
  dialogTitle.value = '编辑投诉'
  form.value = {
    complaintNo: row.ComplaintNo || '',
    title: row.Title || '',
    type: row.Type || 'service',
    source: row.Source || 'phone',
    priority: row.Priority || 'normal',
    description: row.Description || '',
    complainantName: row.ComplainantName || '',
    complainantPhone: row.ComplainantPhone || '',
    complainantRoom: row.ComplainantRoom || '',
    location: row.Location || '',
    handlerName: row.HandlerName || '',
    deadline: row.Deadline ? row.Deadline.split('T')[0] : '',
    remark: row.Remark || ''
  }
  dialogVisible.value = true
}

const handleView = (row: any) => {
  viewingRecord.value = row
  detailDialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.value.title.trim()) { ElMessage.warning('请输入投诉标题'); return }
  if (!form.value.complainantName.trim()) { ElMessage.warning('请输入投诉人姓名'); return }
  if (!form.value.description.trim()) { ElMessage.warning('请输入投诉内容'); return }

  try {
    if (editingId.value) {
      await masterApi.put(`/complaints/${editingId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/complaints', form.value)
      ElMessage.success('添加成功')
    }
    await loadData()
    dialogVisible.value = false
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除投诉 "${row.Title}" 吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/complaints/${row.Id}`)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

const handleRefresh = async () => {
  refreshFields()
  await loadData()
}

const handleReset = () => {
  filterKeyword.value = ''
  filterType.value = ''
  filterPriority.value = ''
  filterStatus.value = ''
}

const getStatusType = (status: string) => {
  const map: Record<string, string> = { pending: 'warning', processing: '', resolved: 'success', closed: 'info' }
  return map[status] || 'info'
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { pending: '待处理', processing: '处理中', resolved: '已解决', closed: '已关闭' }
  return map[status] || status
}

const getTypeLabel = (t: string) => {
  const map: Record<string, string> = { service: '服务投诉', facility: '设施问题', safety: '安全隐患', cleaning: '清洁问题', other: '其他' }
  return map[t] || t
}

const getPriorityLabel = (p: string) => {
  const map: Record<string, string> = { normal: '普通', urgent: '紧急', immediate: '立即处理' }
  return map[p] || p
}

const getPriorityColor = (p: string) => {
  const map: Record<string, string> = { normal: '#409EFF', urgent: '#F56C6C', immediate: '#E6A23C' }
  return map[p] || '#909399'
}

const formatDate = (d: string | null) => d ? d.replace('T', ' ').slice(0, 16) : '-'
</script>

<template>
  <div class="complaint-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>投诉管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon> 配置字段</el-button>
            <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon> 刷新</el-button>
            <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon> 新增投诉</el-button>
          </div>
        </div>
      </template>

      <el-alert title="投诉管理说明" description="记录所有住户投诉，支持处理流程跟踪和回访记录。" type="info" :closable="false" style="margin-bottom: 20px;" />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">投诉总数</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card urgent" @click="filterPriority = filterPriority === 'urgent' ? '' : 'urgent'">
          <div class="stat-content"><div class="stat-value">{{ stats.urgent }}</div><div class="stat-label">紧急投诉</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card pending" @click="filterStatus = filterStatus === 'pending' ? '' : 'pending'">
          <div class="stat-content"><div class="stat-value">{{ stats.pending }}</div><div class="stat-label">待处理</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card processing" @click="filterStatus = filterStatus === 'processing' ? '' : 'processing'">
          <div class="stat-content"><div class="stat-value">{{ stats.processing }}</div><div class="stat-label">处理中</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card resolved" @click="filterStatus = filterStatus === 'resolved' ? '' : 'resolved'">
          <div class="stat-content"><div class="stat-value">{{ stats.resolved }}</div><div class="stat-label">已解决</div></div>
        </el-card>
      </div>

      <!-- 筛选工具栏 -->
      <div class="filter-toolbar">
        <el-input v-model="filterKeyword" placeholder="关键词搜索" clearable style="width: 200px;" />
        <el-select v-model="filterType" placeholder="类型" clearable style="width: 120px;">
          <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterPriority" placeholder="紧急度" clearable style="width: 120px;">
          <el-option v-for="opt in priorityOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 120px;">
          <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-button @click="handleReset">重置</el-button>
      </div>

      <!-- 数据列表 -->
      <el-table :data="filteredList" stripe v-loading="loading" @row-click="handleView">
        <el-table-column prop="ComplaintNo" label="投诉编号" width="130" />
        <el-table-column prop="Title" label="投诉标题" min-width="180" />
        <el-table-column prop="Type" label="类型" width="100" align="center">
          <template #default="{ row }"><el-tag size="small">{{ getTypeLabel(row.Type) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="Priority" label="紧急度" width="100" align="center">
          <template #default="{ row }">
            <el-tag :style="{ backgroundColor: getPriorityColor(row.Priority), borderColor: getPriorityColor(row.Priority), color: '#fff' }" size="small">
              {{ getPriorityLabel(row.Priority) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="ComplainantName" label="投诉人" width="100" />
        <el-table-column prop="ComplainantRoom" label="房号" width="90" align="center" />
        <el-table-column prop="HandlerName" label="处理人" width="90" align="center" />
        <el-table-column prop="HandleStatus" label="状态" width="100" align="center">
          <template #default="{ row }"><el-tag :type="getStatusType(row.HandleStatus)" size="small">{{ getStatusLabel(row.HandleStatus) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="CreatedAt" label="创建时间" width="160" align="center">
          <template #default="{ row }">{{ formatDate(row.CreatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="650px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="投诉编号" required><el-input v-model="form.complaintNo" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="投诉标题" required><el-input v-model="form.title" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="投诉类型">
              <el-select v-model="form.type" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="紧急程度">
              <el-select v-model="form.priority" style="width: 100%">
                <el-option v-for="opt in priorityOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="来源">
              <el-select v-model="form.source" style="width: 100%">
                <el-option v-for="opt in sourceOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="投诉内容" required><el-input v-model="form.description" type="textarea" rows="3" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="投诉人" required><el-input v-model="form.complainantName" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="联系电话"><el-input v-model="form.complainantPhone" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="房号"><el-input v-model="form.complainantRoom" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="投诉地点"><el-input v-model="form.location" placeholder="请输入投诉地点" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="处理人"><el-input v-model="form.handlerName" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="处理期限"><el-date-picker v-model="form.deadline" type="date" style="width: 100%" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="投诉详情" width="750px">
      <div v-if="viewingRecord" class="complaint-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="投诉编号">{{ viewingRecord.ComplaintNo }}</el-descriptions-item>
          <el-descriptions-item label="状态"><el-tag :type="getStatusType(viewingRecord.HandleStatus)">{{ getStatusLabel(viewingRecord.HandleStatus) }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="投诉标题" :span="2">{{ viewingRecord.Title }}</el-descriptions-item>
          <el-descriptions-item label="投诉类型">{{ getTypeLabel(viewingRecord.Type) }}</el-descriptions-item>
          <el-descriptions-item label="紧急程度"><el-tag :style="{ backgroundColor: getPriorityColor(viewingRecord.Priority), color: '#fff' }">{{ getPriorityLabel(viewingRecord.Priority) }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="投诉人">{{ viewingRecord.ComplainantName }}</el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ viewingRecord.ComplainantPhone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="房号">{{ viewingRecord.ComplainantRoom || '-' }}</el-descriptions-item>
          <el-descriptions-item label="来源">{{ viewingRecord.Source }}</el-descriptions-item>
          <el-descriptions-item label="投诉地点" :span="2">{{ viewingRecord.Location || '-' }}</el-descriptions-item>
          <el-descriptions-item label="投诉内容" :span="2">{{ viewingRecord.Description }}</el-descriptions-item>
          <el-descriptions-item label="处理人">{{ viewingRecord.HandlerName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="处理期限">{{ viewingRecord.Deadline || '-' }}</el-descriptions-item>
          <el-descriptions-item label="创建时间" :span="2">{{ formatDate(viewingRecord.CreatedAt) }}</el-descriptions-item>
        </el-descriptions>
        <div v-if="viewingRecord.Feedback" style="margin-top: 20px;">
          <h4>回访记录</h4>
          <el-alert :title="'满意度评分：' + '⭐'.repeat(viewingRecord.Rating || 0)" type="success" :closable="false" />
          <p style="margin-top: 10px;">{{ viewingRecord.Feedback }}</p>
        </div>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="complaint" module-name="投诉管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.complaint-page { width: 100%; }
.header { display: flex; justify-content: space-between; align-items: center; }
.header-actions { display: flex; gap: 10px; }
.stats-grid { display: flex; gap: 15px; margin-bottom: 20px; }
.stat-card { flex: 1; cursor: pointer; transition: all 0.3s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { text-align: center; }
.stat-value { font-size: 24px; font-weight: bold; color: #409EFF; }
.stat-card.urgent .stat-value { color: #F56C6C; }
.stat-card.pending .stat-value { color: #E6A23C; }
.stat-card.processing .stat-value { color: #909399; }
.stat-card.resolved .stat-value { color: #67C23A; }
.stat-label { font-size: 13px; color: #909399; margin-top: 5px; }
.filter-toolbar { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; align-items: center; }
.complaint-detail { padding: 10px; }
</style>