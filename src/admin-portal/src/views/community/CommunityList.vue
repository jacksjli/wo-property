<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, View, Check, Close, User } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const currentId = ref<number | null>(null)

const activeTab = ref('all')
const statusOptions = [
  { label: '全部', value: 'all' },
  { label: '计划中', value: 'planned' },
  { label: '进行中', value: 'ongoing' },
  { label: '已完成', value: 'completed' },
  { label: '已取消', value: 'cancelled' },
]

const keyword = ref('')
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })
const tableData = ref<any[]>([])
const detailVisible = ref(false)
const detailData = ref<any>(null)

const formRef = ref()
const form = ref({
  activityTitle: '',
  activityType: '',
  description: '',
  organizer: '',
  location: '',
  startTime: '',
  endTime: '',
  maxParticipants: null as number | null,
  remarks: '',
})

const statusFilter = computed(() => activeTab.value === 'all' ? '' : activeTab.value)

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/community-activities', {
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

const handleTabChange = () => { pagination.value.page = 1; loadData() }
const handleSearch = () => { pagination.value.page = 1; loadData() }
const handlePageChange = (page: number) => { pagination.value.page = page; loadData() }

const openCreate = () => {
  isEdit.value = false
  currentId.value = null
  form.value = { activityTitle: '', activityType: '', description: '', organizer: '', location: '', startTime: '', endTime: '', maxParticipants: null, remarks: '' }
  dialogVisible.value = true
}

const handleView = async (row: any) => {
  try {
    const res: any = await masterApi.get(`/community-activities/${row.id}`)
    if (res.success) { detailData.value = res.data; detailVisible.value = true }
  } catch (e: any) { ElMessage.error(e.message || '加载详情失败') }
}

const handleStatusChange = async (row: any, newStatus: string, label: string) => {
  try {
    await ElMessageBox.confirm(`确认将活动状态改为「${label}」？`, '状态变更', { type: 'info' })
    await masterApi.put(`/community-activities/${row.id}`, { status: newStatus })
    ElMessage.success('状态已更新')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleParticipantUpdate = async (row: any, delta: number) => {
  try {
    await masterApi.put(`/community-activities/${row.id}`, { participantCount: Math.max(0, row.participantCount + delta) })
    ElMessage.success('报名人数已更新')
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除活动「${row.activityTitle}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/community-activities/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const handleSave = async () => {
  if (!form.value.activityTitle) {
    ElMessage.warning('请填写活动标题')
    return
  }
  submitting.value = true
  try {
    const payload = {
      activityTitle: form.value.activityTitle,
      activityType: form.value.activityType || undefined,
      description: form.value.description || undefined,
      organizer: form.value.organizer || undefined,
      location: form.value.location || undefined,
      startTime: form.value.startTime ? new Date(form.value.startTime).toISOString() : undefined,
      endTime: form.value.endTime ? new Date(form.value.endTime).toISOString() : undefined,
      maxParticipants: form.value.maxParticipants || undefined,
      remarks: form.value.remarks || undefined,
    }
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/community-activities/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/community-activities', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { planned: '计划中', ongoing: '进行中', completed: '已完成', cancelled: '已取消' }
  return map[status] || status
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = { planned: 'warning', ongoing: 'primary', completed: 'success', cancelled: 'info' }
  return map[status] || 'info'
}

const formatTime = (time: string | null) => {
  if (!time) return '-'
  return time.replace('T', ' ').slice(0, 19)
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="community-list">
    <div class="page-header">
      <h2>社区活动管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增活动</el-button>
    </div>

    <el-tabs v-model="activeTab" @tab-change="handleTabChange" class="status-tabs">
      <el-tab-pane v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :name="opt.value" />
    </el-tabs>

    <div class="filter-bar">
      <el-input v-model="keyword" placeholder="搜索活动标题/组织者" clearable style="width:240px" @keyup.enter="handleSearch" />
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column prop="activityTitle" label="活动标题" min-width="180" show-overflow-tooltip />
        <el-table-column prop="activityType" label="活动类型" width="100" />
        <el-table-column prop="organizer" label="组织者" width="100" />
        <el-table-column prop="location" label="活动地点" min-width="120" show-overflow-tooltip />
        <el-table-column label="开始时间" width="160">
          <template #default="{ row }">{{ formatTime(row.startTime) }}</template>
        </el-table-column>
        <el-table-column label="结束时间" width="160">
          <template #default="{ row }">{{ formatTime(row.endTime) }}</template>
        </el-table-column>
        <el-table-column label="报名人数" width="110" align="center">
          <template #default="{ row }">
            <span>{{ row.participantCount }}{{ row.maxParticipants ? `/${row.maxParticipants}` : '' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="320" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" :icon="View" @click="handleView(row)">详情</el-button>
            <el-button link type="success" size="small" :icon="User" @click="handleParticipantUpdate(row, 1)">报名+1</el-button>
            <el-button link type="warning" size="small" @click="handleParticipantUpdate(row, -1)">取消-1</el-button>
            <template v-if="row.status === 'planned'">
              <el-button link type="primary" size="small" @click="handleStatusChange(row, 'ongoing', '进行中')">开始</el-button>
              <el-button link type="info" size="small" @click="handleStatusChange(row, 'cancelled', '已取消')">取消</el-button>
            </template>
            <template v-if="row.status === 'ongoing'">
              <el-button link type="success" size="small" @click="handleStatusChange(row, 'completed', '已完成')">完成</el-button>
            </template>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

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
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑活动' : '新增社区活动'" width="600px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-form-item label="活动标题" required>
          <el-input v-model="form.activityTitle" placeholder="请输入活动标题" />
        </el-form-item>
        <el-form-item label="活动类型">
          <el-select v-model="form.activityType" placeholder="选择类型" style="width:100%">
            <el-option label="文体活动" value="文体活动" />
            <el-option label="公益活动" value="公益活动" />
            <el-option label="节日庆祝" value="节日庆祝" />
            <el-option label="教育培训" value="教育培训" />
            <el-option label="健康讲座" value="健康讲座" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="组织者">
          <el-input v-model="form.organizer" placeholder="请输入组织者" />
        </el-form-item>
        <el-form-item label="活动地点">
          <el-input v-model="form.location" placeholder="请输入活动地点" />
        </el-form-item>
        <el-form-item label="开始时间">
          <el-date-picker v-model="form.startTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss" placeholder="选择开始时间" style="width:100%" />
        </el-form-item>
        <el-form-item label="结束时间">
          <el-date-picker v-model="form.endTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss" placeholder="选择结束时间" style="width:100%" />
        </el-form-item>
        <el-form-item label="最大人数">
          <el-input-number v-model="form.maxParticipants" :min="1" :max="10000" placeholder="最大参与人数" style="width:100%" />
        </el-form-item>
        <el-form-item label="活动描述">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入活动描述" />
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
    <el-dialog v-model="detailVisible" title="活动详情" width="600px" destroy-on-close>
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="活动标题" :span="2">{{ detailData.activityTitle }}</el-descriptions-item>
        <el-descriptions-item label="活动类型">{{ detailData.activityType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="组织者">{{ detailData.organizer || '-' }}</el-descriptions-item>
        <el-descriptions-item label="活动地点">{{ detailData.location || '-' }}</el-descriptions-item>
        <el-descriptions-item label="开始时间">{{ formatTime(detailData.startTime) }}</el-descriptions-item>
        <el-descriptions-item label="结束时间">{{ formatTime(detailData.endTime) }}</el-descriptions-item>
        <el-descriptions-item label="报名人数">
          {{ detailData.participantCount }}{{ detailData.maxParticipants ? `/${detailData.maxParticipants}` : '' }} 人
        </el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(detailData.status)" size="small">{{ getStatusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="活动描述" :span="2">{{ detailData.description || '-' }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remarks || '-' }}</el-descriptions-item>
        <el-descriptions-item label="创建时间" :span="2">{{ detailData.createdAt?.replace('T', ' ').slice(0, 19) }}</el-descriptions-item>
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
