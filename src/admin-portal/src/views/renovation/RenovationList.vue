<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, View, Check, Close } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const currentId = ref<number | null>(null)

// Tab filter
const activeTab = ref('all')
const statusOptions = [
  { label: '全部', value: 'all' },
  { label: '待审批', value: 'pending' },
  { label: '已批准', value: 'approved' },
  { label: '已拒绝', value: 'rejected' },
  { label: '已完成', value: 'completed' },
]

// Search
const keyword = ref('')

// Pagination
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })
const tableData = ref<any[]>([])

// Rooms list for dropdown
const rooms = ref<any[]>([])

// Detail dialog
const detailVisible = ref(false)
const detailData = ref<any>(null)

// Form
const formRef = ref()
const form = ref({
  roomId: null as number | null,
  applicantName: '',
  applicantPhone: '',
  description: '',
  startDate: '',
  endDate: '',
  remarks: '',
})

const statusFilter = computed(() => activeTab.value === 'all' ? '' : activeTab.value)

const loadRooms = async () => {
  try {
    const res: any = await masterApi.get('/rooms', { params: { pageSize: 500 } })
    if (res.success) rooms.value = res.data || []
  } catch { /* ignore */ }
}

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/renovation-requests', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.pageSize,
        status: statusFilter.value,
        keyword: keyword.value || undefined,
      }
    })
    if (res.success) {
      tableData.value = res.data || []
      pagination.value = res.pagination || pagination.value
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const handleTabChange = () => {
  pagination.value.page = 1
  loadData()
}

const handleSearch = () => {
  pagination.value.page = 1
  loadData()
}

const handlePageChange = (page: number) => {
  pagination.value.page = page
  loadData()
}

const openCreate = () => {
  isEdit.value = false
  currentId.value = null
  form.value = { roomId: null, applicantName: '', applicantPhone: '', description: '', startDate: '', endDate: '', remarks: '' }
  dialogVisible.value = true
}

const handleView = async (row: any) => {
  try {
    const res: any = await masterApi.get(`/renovation-requests/${row.id}`)
    if (res.success) {
      detailData.value = res.data
      detailVisible.value = true
    }
  } catch (e: any) { ElMessage.error(e.message || '加载详情失败') }
}

const handleApprove = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认批准该装修申请？', '审批确认', { type: 'info' })
    await masterApi.put(`/renovation-requests/${row.id}`, { status: 'approved' })
    ElMessage.success('已批准')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleReject = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认拒绝该装修申请？', '审批确认', { type: 'warning' })
    await masterApi.put(`/renovation-requests/${row.id}`, { status: 'rejected' })
    ElMessage.success('已拒绝')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleComplete = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认标记为已完成？', '确认', { type: 'info' })
    await masterApi.put(`/renovation-requests/${row.id}`, { status: 'completed' })
    ElMessage.success('已标记完成')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除装修申请「${row.applicantName}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/renovation-requests/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const handleSave = async () => {
  if (!form.value.roomId || !form.value.applicantName || !form.value.description) {
    ElMessage.warning('请填写房间、申请人姓名和装修内容')
    return
  }
  submitting.value = true
  try {
    const payload = {
      roomId: form.value.roomId,
      applicantName: form.value.applicantName,
      applicantPhone: form.value.applicantPhone || undefined,
      description: form.value.description,
      startDate: form.value.startDate || undefined,
      endDate: form.value.endDate || undefined,
      remarks: form.value.remarks || undefined,
    }
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/renovation-requests/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/renovation-requests', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const getRoomLabel = (id: number) => {
  const r = rooms.value.find(x => x.id === id)
  return r ? `${r.name} (${r.code})` : `房间${id}`
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { pending: '待审批', approved: '已批准', rejected: '已拒绝', completed: '已完成' }
  return map[status] || status
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = { pending: 'warning', approved: 'success', rejected: 'danger', completed: 'info' }
  return map[status] || 'info'
}

onMounted(() => { loadRooms(); loadData() })
</script>

<template>
  <div class="renovation-list">
    <div class="page-header">
      <h2>装修管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增申请</el-button>
    </div>

    <!-- Tabs -->
    <el-tabs v-model="activeTab" @tab-change="handleTabChange" class="status-tabs">
      <el-tab-pane v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :name="opt.value" />
    </el-tabs>

    <!-- Search -->
    <div class="filter-bar">
      <el-input v-model="keyword" placeholder="搜索申请人/手机号" clearable style="width:220px" @keyup.enter="handleSearch" />
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <!-- Table -->
    <el-card shadow="never">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column label="房间" min-width="120">
          <template #default="{ row }">
            {{ row.buildingName || '' }}{{ row.roomNumber || `房间${row.roomId}` }}
          </template>
        </el-table-column>
        <el-table-column prop="applicantName" label="申请人" width="100" />
        <el-table-column prop="applicantPhone" label="联系方式" width="130" />
        <el-table-column prop="description" label="装修内容" min-width="180" show-overflow-tooltip />
        <el-table-column prop="startDate" label="开始日期" width="110">
          <template #default="{ row }">{{ row.startDate?.split('T')[0] || '-' }}</template>
        </el-table-column>
        <el-table-column prop="endDate" label="结束日期" width="110">
          <template #default="{ row }">{{ row.endDate?.split('T')[0] || '-' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" :icon="View" @click="handleView(row)">详情</el-button>
            <template v-if="row.status === 'pending'">
              <el-button link type="success" size="small" :icon="Check" @click="handleApprove(row)">批准</el-button>
              <el-button link type="danger" size="small" :icon="Close" @click="handleReject(row)">拒绝</el-button>
            </template>
            <template v-else-if="row.status === 'approved'">
              <el-button link type="success" size="small" @click="handleComplete(row)">完成</el-button>
            </template>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- Pagination -->
      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="pagination.page"
          :page-size="pagination.pageSize"
          :total="pagination.totalCount"
          layout="prev, pager, next, total"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- Create/Edit Dialog -->
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑装修申请' : '新增装修申请'" width="550px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-form-item label="房间" required>
          <el-select v-model="form.roomId" placeholder="选择房间" style="width:100%">
            <el-option v-for="r in rooms" :key="r.id" :label="`${r.name} (${r.code})`" :value="r.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="申请人" required>
          <el-input v-model="form.applicantName" placeholder="请输入申请人姓名" />
        </el-form-item>
        <el-form-item label="联系电话">
          <el-input v-model="form.applicantPhone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="装修内容" required>
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="描述装修内容" />
        </el-form-item>
        <el-form-item label="开始日期">
          <el-date-picker v-model="form.startDate" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" style="width:100%" />
        </el-form-item>
        <el-form-item label="结束日期">
          <el-date-picker v-model="form.endDate" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" style="width:100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remarks" type="textarea" :rows="2" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <!-- Detail Dialog -->
    <el-dialog v-model="detailVisible" title="装修申请详情" width="550px" destroy-on-close>
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="房间">
          {{ detailData.buildingName || '' }}{{ detailData.roomNumber || `房间${detailData.roomId}` }}
        </el-descriptions-item>
        <el-descriptions-item label="申请人">{{ detailData.applicantName }}</el-descriptions-item>
        <el-descriptions-item label="联系电话">{{ detailData.applicantPhone || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(detailData.status)" size="small">{{ getStatusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="开始日期" :span="2">{{ detailData.startDate?.split('T')[0] || '-' }}</el-descriptions-item>
        <el-descriptions-item label="结束日期" :span="2">{{ detailData.endDate?.split('T')[0] || '-' }}</el-descriptions-item>
        <el-descriptions-item label="装修内容" :span="2">{{ detailData.description }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remarks || '-' }}</el-descriptions-item>
        <el-descriptions-item label="申请时间" :span="2">{{ detailData.createdAt?.replace('T', ' ').slice(0, 19) }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.status-tabs { margin-bottom: 12px; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 16px; }
.pagination-wrap { display: flex; justify-content: flex-end; margin-top: 16px; }
</style>