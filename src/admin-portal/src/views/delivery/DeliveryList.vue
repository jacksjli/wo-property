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

const activeTab = ref('all')
const statusOptions = [
  { label: '全部', value: 'all' },
  { label: '待配送', value: 'pending' },
  { label: '配送中', value: 'in_transit' },
  { label: '已送达', value: 'delivered' },
  { label: '配送失败', value: 'failed' },
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
  residentName: '',
  residentPhone: '',
  deliveryCompany: '',
  deliveryType: '',
  itemDescription: '',
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
    const res: any = await masterApi.get('/delivery-requests', {
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
  form.value = { roomId: null, residentName: '', residentPhone: '', deliveryCompany: '', deliveryType: '', itemDescription: '', remarks: '' }
  dialogVisible.value = true
}

const handleView = async (row: any) => {
  try {
    const res: any = await masterApi.get(`/delivery-requests/${row.id}`)
    if (res.success) { detailData.value = res.data; detailVisible.value = true }
  } catch (e: any) { ElMessage.error(e.message || '加载详情失败') }
}

const handleStart = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认开始配送？', '确认', { type: 'info' })
    await masterApi.put(`/delivery-requests/${row.id}`, { status: 'in_transit' })
    ElMessage.success('已开始配送')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleDelivered = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认已送达？', '确认送达', { type: 'success' })
    await masterApi.put(`/delivery-requests/${row.id}`, { status: 'delivered', deliveryTime: new Date().toISOString() })
    ElMessage.success('已标记送达')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleFailed = async (row: any) => {
  try {
    await ElMessageBox.confirm('确认配送失败？', '配送失败', { type: 'warning' })
    await masterApi.put(`/delivery-requests/${row.id}`, { status: 'failed' })
    ElMessage.warning('已标记配送失败')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除配送请求「${row.itemDescription}」吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/delivery-requests/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const handleSave = async () => {
  if (!form.value.roomId || !form.value.residentName) {
    ElMessage.warning('请填写房间和住户姓名')
    return
  }
  submitting.value = true
  try {
    const payload = {
      roomId: form.value.roomId,
      residentName: form.value.residentName,
      residentPhone: form.value.residentPhone || undefined,
      deliveryCompany: form.value.deliveryCompany || undefined,
      deliveryType: form.value.deliveryType || undefined,
      itemDescription: form.value.itemDescription || undefined,
      remarks: form.value.remarks || undefined,
    }
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/delivery-requests/${currentId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/delivery-requests', payload)
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
  const map: Record<string, string> = { pending: '待配送', in_transit: '配送中', delivered: '已送达', failed: '配送失败' }
  return map[status] || status
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = { pending: 'warning', in_transit: 'primary', delivered: 'success', failed: 'danger' }
  return map[status] || 'info'
}

onMounted(() => { loadRooms(); loadData() })
</script>

<template>
  <div class="delivery-list">
    <div class="page-header">
      <h2>配送管理</h2>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增配送</el-button>
    </div>

    <el-tabs v-model="activeTab" @tab-change="handleTabChange" class="status-tabs">
      <el-tab-pane v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :name="opt.value" />
    </el-tabs>

    <div class="filter-bar">
      <el-input v-model="keyword" placeholder="搜索住户姓名/联系电话" clearable style="width:240px" @keyup.enter="handleSearch" />
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
    </div>

    <el-card shadow="never">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column label="房间" min-width="130">
          <template #default="{ row }">{{ row.buildingName || '' }}{{ row.roomNumber || `房间${row.roomId}` }}</template>
        </el-table-column>
        <el-table-column prop="residentName" label="住户姓名" width="100" />
        <el-table-column prop="residentPhone" label="联系电话" width="130" />
        <el-table-column prop="deliveryCompany" label="配送公司" width="110" />
        <el-table-column prop="deliveryType" label="配送类型" width="100" />
        <el-table-column prop="itemDescription" label="物品描述" min-width="160" show-overflow-tooltip />
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="配送时间" width="160">
          <template #default="{ row }">{{ row.deliveryTime?.replace('T', ' ').slice(0, 19) || '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="280" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" :icon="View" @click="handleView(row)">详情</el-button>
            <template v-if="row.status === 'pending'">
              <el-button link type="primary" size="small" @click="handleStart(row)">开始配送</el-button>
            </template>
            <template v-if="row.status === 'in_transit'">
              <el-button link type="success" size="small" :icon="Check" @click="handleDelivered(row)">确认送达</el-button>
              <el-button link type="danger" size="small" :icon="Close" @click="handleFailed(row)">配送失败</el-button>
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
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑配送' : '新增配送'" width="550px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-form-item label="房间" required>
          <el-select v-model="form.roomId" placeholder="选择房间" style="width:100%">
            <el-option v-for="r in rooms" :key="r.id" :label="`${r.buildingName || ''}${r.roomNumber}`" :value="r.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="住户姓名" required>
          <el-input v-model="form.residentName" placeholder="请输入住户姓名" />
        </el-form-item>
        <el-form-item label="联系电话">
          <el-input v-model="form.residentPhone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="配送公司">
          <el-input v-model="form.deliveryCompany" placeholder="如：美团、饿了么、京东" />
        </el-form-item>
        <el-form-item label="配送类型">
          <el-select v-model="form.deliveryType" placeholder="选择类型" style="width:100%">
            <el-option label="外卖配送" value="外卖配送" />
            <el-option label="快递配送" value="快递配送" />
            <el-option label="生鲜配送" value="生鲜配送" />
            <el-option label="药品配送" value="药品配送" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="物品描述">
          <el-input v-model="form.itemDescription" type="textarea" :rows="2" placeholder="请输入配送物品描述" />
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
    <el-dialog v-model="detailVisible" title="配送详情" width="550px" destroy-on-close>
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="房间">
          {{ detailData.buildingName || '' }}{{ detailData.roomNumber || `房间${detailData.roomId}` }}
        </el-descriptions-item>
        <el-descriptions-item label="住户姓名">{{ detailData.residentName }}</el-descriptions-item>
        <el-descriptions-item label="联系电话">{{ detailData.residentPhone || '-' }}</el-descriptions-item>
        <el-descriptions-item label="配送公司">{{ detailData.deliveryCompany || '-' }}</el-descriptions-item>
        <el-descriptions-item label="配送类型">{{ detailData.deliveryType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(detailData.status)" size="small">{{ getStatusLabel(detailData.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="物品描述" :span="2">{{ detailData.itemDescription || '-' }}</el-descriptions-item>
        <el-descriptions-item label="配送时间">{{ detailData.deliveryTime?.replace('T', ' ').slice(0, 19) || '-' }}</el-descriptions-item>
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
