<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, View, Check, Star } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const currentId = ref<number | null>(null)

const activeTab = ref('all')
const statusOptions = [
  { label: '全部', value: 'all' },
  { label: '待执行', value: 'pending' },
  { label: '执行中', value: 'in_progress' },
  { label: '已完成', value: 'completed' },
  { label: '质量问题', value: 'quality_issue' },
]

const keyword = ref('')
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })
const tableData = ref<any[]>([])
const buildings = ref<any[]>([])
const detailVisible = ref(false)
const detailData = ref<any>(null)

const formRef = ref()
const form = ref({
  buildingId: null as number | null,
  cleaningArea: '',
  cleanerName: '',
  cleaningType: '',
  planDate: '',
  remarks: '',
})

const statusFilter = computed(() => activeTab.value === 'all' ? '' : activeTab.value)

const loadBuildings = async () => {
  try {
    const res: any = await masterApi.get('/buildings', { params: { pageSize: 500 } })
    if (res.success) buildings.value = res.data || []
  } catch { /* ignore */ }
}

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/cleaning-records', {
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
  form.value = { buildingId: null, cleaningArea: '', cleanerName: '', cleaningType: '', planDate: '', remarks: '' }
  dialogVisible.value = true
}

const handleView = async (row: any) => {
  try {
    const res: any = await masterApi.get(`/cleaning-records/${row.id}`)
    if (res.success) { detailData.value = res.data; detailVisible.value = true }
  } catch (e: any) { ElMessage.error(e.message || '加载详情失败') }
}

const handleStart = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认开始执行清洁？', '确认', { type: 'info' })
    await masterApi.put(`/cleaning-records/${row.id}`, { status: 'in_progress' })
    ElMessage.success('已开始执行')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleComplete = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认清洁已完成？', '确认完成', { type: 'success' })
    await masterApi.put(`/cleaning-records/${row.id}`, { status: 'completed', actualDate: new Date().toISOString() })
    ElMessage.success('已标记完成')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleQualityIssue = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认存在质量问题？', '质量问题', { type: 'warning' })
    await masterApi.put(`/cleaning-records/${row.id}`, { status: 'quality_issue' })
    ElMessage.warning('已标记质量问题')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除清洁记录「${row.cleaningArea}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/cleaning-records/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const handleSave = async () => {
  if (!form.value.buildingId || !form.value.cleaningArea) {
    ElMessage.warning('请填写楼栋和清洁区域')
    return
  }
  submitting.value = true
  try {
    const payload = {
      buildingId: form.value.buildingId,
      cleaningArea: form.value.cleaningArea,
      cleanerName: form.value.cleanerName || undefined,
      cleaningType: form.value.cleaningType || undefined,
      planDate: form.value.planDate || undefined,
      remarks: form.value.remarks || undefined,
    }
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/cleaning-records/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/cleaning-records', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { pending: '待执行', in_progress: '执行中', completed: '已完成', quality_issue: '质量问题' }
  return map[status] || status
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = { pending: 'warning', in_progress: 'primary', completed: 'success', quality_issue: 'danger' }
  return map[status] || 'info'
}

const getQualityStars = (level: string) => {
  if (!level) return '-'
  const num = parseInt(level.replace(/[^0-9]/g, '')) || 0
  return '★'.repeat(num) + '☆'.repeat(5 - num)
}

onMounted(() => { loadBuildings(); loadData() })
</script>

<template>
  <div class="cleaning-list">
    <div class="page-header">
      <h2>清洁管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增清洁计划</el-button>
    </div>

    <el-tabs v-model="activeTab" @tab-change="handleTabChange" class="status-tabs">
      <el-tab-pane v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :name="opt.value" />
    </el-tabs>

    <div class="filter-bar">
      <el-input v-model="keyword" placeholder="搜索清洁区域/清洁人员" clearable style="width:240px" @keyup.enter="handleSearch" />
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column prop="buildingName" label="楼栋" min-width="100" />
        <el-table-column prop="cleaningArea" label="清洁区域" min-width="140" show-overflow-tooltip />
        <el-table-column prop="cleanerName" label="清洁人员" width="100" />
        <el-table-column prop="cleaningType" label="清洁类型" width="100" />
        <el-table-column label="计划日期" width="110">
          <template #default="{ row }">{{ row.planDate?.split('T')[0] || '-' }}</template>
        </el-table-column>
        <el-table-column label="实际日期" width="110">
          <template #default="{ row }">{{ row.actualDate?.split('T')[0] || '-' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="质量评分" width="100" align="center">
          <template #default="{ row }">
            <span v-if="row.qualityLevel" class="quality-stars">{{ getQualityStars(row.qualityLevel) }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" :icon="View" @click="handleView(row)">详情</el-button>
            <template v-if="row.status === 'pending'">
              <el-button link type="primary" size="small" @click="handleStart(row)">开始</el-button>
            </template>
            <template v-if="row.status === 'in_progress'">
              <el-button link type="success" size="small" :icon="Check" @click="handleComplete(row)">完成</el-button>
              <el-button link type="danger" size="small" @click="handleQualityIssue(row)">质量问题</el-button>
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
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑清洁计划' : '新增清洁计划'" width="550px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-form-item label="楼栋" required>
          <el-select v-model="form.buildingId" placeholder="选择楼栋" style="width:100%">
            <el-option v-for="b in buildings" :key="b.id" :label="b.name" :value="b.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="清洁区域" required>
          <el-input v-model="form.cleaningArea" placeholder="如：1号楼大厅、2号楼电梯" />
        </el-form-item>
        <el-form-item label="清洁人员">
          <el-input v-model="form.cleanerName" placeholder="请输入清洁人员姓名" />
        </el-form-item>
        <el-form-item label="清洁类型">
          <el-select v-model="form.cleaningType" placeholder="选择类型" style="width:100%">
            <el-option label="日常保洁" value="日常保洁" />
            <el-option label="深度清洁" value="深度清洁" />
            <el-option label="开荒清洁" value="开荒清洁" />
            <el-option label="消杀服务" value="消杀服务" />
          </el-select>
        </el-form-item>
        <el-form-item label="计划日期">
          <el-date-picker v-model="form.planDate" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" style="width:100%" />
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
    <el-dialog v-model="detailVisible" title="清洁详情" width="550px" destroy-on-close>
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="楼栋">{{ detailData.buildingName }}</el-descriptions-item>
        <el-descriptions-item label="清洁人员">{{ detailData.cleanerName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="清洁区域">{{ detailData.cleaningArea }}</el-descriptions-item>
        <el-descriptions-item label="清洁类型">{{ detailData.cleaningType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="计划日期">{{ detailData.planDate?.split('T')[0] || '-' }}</el-descriptions-item>
        <el-descriptions-item label="实际日期">{{ detailData.actualDate?.split('T')[0] || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(detailData.status)" size="small">{{ getStatusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="质量评分">
          <span v-if="detailData.qualityLevel" class="quality-stars">{{ getQualityStars(detailData.qualityLevel) }}</span>
          <span v-else>-</span>
        </el-descriptions-item>
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
.quality-stars { color: #f5a623; letter-spacing: 2px; }
</style>
