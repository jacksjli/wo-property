<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, View, Bell, Check } from '@element-plus/icons-vue'
import { expressApi } from '@/api/express'
import { masterApi } from '@/api/http'
import { currentProject } from '@/stores/project'

const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const currentId = ref<number | null>(null)

const activeTab = ref('all')
const statusOptions = [
  { label: '全部', value: 'all' },
  { label: '待取件', value: 'pending' },
  { label: '已通知', value: 'informed' },
  { label: '已取件', value: 'picked' },
  { label: '已退回', value: 'returned' },
]

const keyword = ref('')
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })
const tableData = ref<any[]>([])
const rooms = ref<any[]>([])
const detailVisible = ref(false)
const detailData = ref<any>(null)

const formRef = ref()
const form = ref({
  roomId: null as number | null,
  recipientName: '',
  recipientPhone: '',
  courierCompany: '',
  trackingNumber: '',
  pickupCode: '',
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
    const res: any = await expressApi.getRecords({
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
      status: statusFilter.value || undefined,
      keyword: keyword.value || undefined,
    })
    if (res.success) {
      tableData.value = res.data || []
      pagination.value.total = res.total || 0
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
  form.value = { roomId: null, recipientName: '', recipientPhone: '', courierCompany: '', trackingNumber: '', pickupCode: '', remarks: '' }
  dialogVisible.value = true
}

const handleView = async (row: any) => {
  try {
    const res: any = await expressApi.getRecord(row.id)
    if (res.success) { detailData.value = res.data; detailVisible.value = true }
  } catch (e: any) { ElMessage.error(e.message || '加载详情失败') }
}

const handleInform = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认通知住户「${row.recipientName}」取件？`, '通知确认', { type: 'info' })
    await expressApi.updateRecord(row.id, { status: 'informed' })
    ElMessage.success('已通知住户')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handlePickup = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认该快递已取件？', '确认取件', { type: 'info' })
    await expressApi.pickupRecord(row.id)
    ElMessage.success('已确认取件')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleReturn = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认该快递已退回？', '确认退回', { type: 'warning' })
    await expressApi.updateRecord(row.id, { status: 'returned' })
    ElMessage.success('已标记退回')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除快递记录「${row.recipientName}」吗？`, '提示', { type: 'warning' })
    await expressApi.deleteRecord(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const handleSave = async () => {
  if (!form.value.roomId || !form.value.recipientName) {
    ElMessage.warning('请填写房间和收件人')
    return
  }
  submitting.value = true
  try {
    const payload = {
      roomId: form.value.roomId,
      recipientName: form.value.recipientName,
      recipientPhone: form.value.recipientPhone || undefined,
      courierCompany: form.value.courierCompany || undefined,
      trackingNumber: form.value.trackingNumber || undefined,
      pickupCode: form.value.pickupCode || undefined,
      remarks: form.value.remarks || undefined,
      ProjectCode: currentProject.value?.code || '',
    }
    if (isEdit.value && currentId.value) {
      await expressApi.updateRecord(currentId.value, { remarks: form.value.remarks })
      ElMessage.success('更新成功')
    } else {
      await expressApi.createRecord(payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const getRoomLabel = (id: number) => {
  const r = rooms.value.find(x => x.id === id)
  return r ? `${r.buildingName || ''}${r.roomNumber || `房间${id}`}` : `房间${id}`
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { pending: '待取件', informed: '已通知', picked: '已取件', returned: '已退回' }
  return map[status] || status
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = { pending: 'warning', informed: 'primary', picked: 'success', returned: 'info' }
  return map[status] || 'info'
}

onMounted(() => { loadRooms(); loadData() })
</script>

<template>
  <div class="express-list">
    <div class="page-header">
      <h2>快递管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增登记</el-button>
    </div>

    <el-tabs v-model="activeTab" @tab-change="handleTabChange" class="status-tabs">
      <el-tab-pane v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :name="opt.value" />
    </el-tabs>

    <div class="filter-bar">
      <el-input v-model="keyword" placeholder="搜索收件人/手机号/快递单号" clearable style="width:260px" @keyup.enter="handleSearch" />
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column label="房间" min-width="130">
          <template #default="{ row }">{{ row.buildingName || '' }}{{ row.roomNumber || `房间${row.roomId}` }}</template>
        </el-table-column>
        <el-table-column prop="recipientName" label="收件人" width="100" />
        <el-table-column prop="recipientPhone" label="联系电话" width="130" />
        <el-table-column prop="courierCompany" label="快递公司" width="110" />
        <el-table-column prop="trackingNumber" label="快递单号" min-width="150" show-overflow-tooltip />
        <el-table-column prop="pickupCode" label="取件码" width="90" />
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" :icon="View" @click="handleView(row)">详情</el-button>
            <template v-if="row.status === 'pending'">
              <el-button link type="warning" size="small" :icon="Bell" @click="handleInform(row)">通知</el-button>
            </template>
            <template v-if="row.status === 'informed'">
              <el-button link type="success" size="small" :icon="Check" @click="handlePickup(row)">确认取件</el-button>
            </template>
            <el-button link type="info" size="small" @click="handleReturn(row)">退回</el-button>
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
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑快递' : '新增快递登记'" width="550px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-form-item label="房间" required>
          <el-select v-model="form.roomId" placeholder="选择房间" style="width:100%">
            <el-option v-for="r in rooms" :key="r.id" :label="`${r.buildingName || ''}${r.roomNumber}`" :value="r.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="收件人" required>
          <el-input v-model="form.recipientName" placeholder="请输入收件人姓名" />
        </el-form-item>
        <el-form-item label="联系电话">
          <el-input v-model="form.recipientPhone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="快递公司">
          <el-input v-model="form.courierCompany" placeholder="如：顺丰、圆通、中通" />
        </el-form-item>
        <el-form-item label="快递单号">
          <el-input v-model="form.trackingNumber" placeholder="请输入快递单号" />
        </el-form-item>
        <el-form-item label="取件码">
          <el-input v-model="form.pickupCode" placeholder="快递柜取件码" />
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
    <el-dialog v-model="detailVisible" title="快递详情" width="550px" destroy-on-close>
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="房间">
          {{ detailData.buildingName || '' }}{{ detailData.roomNumber || `房间${detailData.roomId}` }}
        </el-descriptions-item>
        <el-descriptions-item label="收件人">{{ detailData.recipientName }}</el-descriptions-item>
        <el-descriptions-item label="联系电话">{{ detailData.recipientPhone || '-' }}</el-descriptions-item>
        <el-descriptions-item label="快递公司">{{ detailData.courierCompany || '-' }}</el-descriptions-item>
        <el-descriptions-item label="快递单号">{{ detailData.trackingNumber || '-' }}</el-descriptions-item>
        <el-descriptions-item label="取件码">{{ detailData.pickupCode || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(detailData.status)" size="small">{{ getStatusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="取件时间">{{ detailData.pickupTime?.replace('T', ' ').slice(0, 19) || '-' }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remarks || '-' }}</el-descriptions-item>
        <el-descriptions-item label="登记时间" :span="2">{{ detailData.createdAt?.replace('T', ' ').slice(0, 19) }}</el-descriptions-item>
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
