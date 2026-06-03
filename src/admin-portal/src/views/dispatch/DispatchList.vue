<template>
  <div class="dispatch-list">
    <!-- 标签页 -->
    <el-tabs v-model="activeTab" @tab-change="handleTabChange">
      <el-tab-pane label="待处理" name="pending">
        <template #label>
          <span>待处理 <el-badge :value="pendingCount" :hidden="pendingCount === 0" type="warning" /></span>
        </template>
      </el-tab-pane>
      <el-tab-pane label="已接收" name="received" />
      <el-tab-pane label="已完工" name="completed" />
      <el-tab-pane label="超时告警" name="alerts">
        <template #label>
          <span>超时告警 <el-badge :value="alertCount" :hidden="alertCount === 0" type="danger" /></span>
        </template>
      </el-tab-pane>
      <el-tab-pane label="转单审批" name="transfers">
        <template #label>
          <span>转单审批 <el-badge :value="transferCount" :hidden="transferCount === 0" type="info" /></span>
        </template>
      </el-tab-pane>
    </el-tabs>

    <!-- 操作栏 -->
    <div class="toolbar">
      <el-button :icon="Refresh" @click="loadData">刷新</el-button>
      <span class="total">共 {{ total }} 条</span>
    </div>

    <!-- 列表 -->
    <el-table :data="tableData" v-loading="loading" stripe>
      <el-table-column prop="ticketCode" label="工单号" width="160" />
      <el-table-column prop="toPersonName" label="执行人" width="100" />
      <el-table-column prop="dispatchTime" label="派单时间" width="160">
        <template #default="{ row }">
          {{ formatTime(row.dispatchTime) }}
        </template>
      </el-table-column>
      <el-table-column prop="status" label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="getStatusType(row.status)">{{ getStatusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="source" label="来源" width="80">
        <template #default="{ row }">
          <span v-if="row.source === 'Escalation'" class="escalation-tag">升级</span>
          <span v-else>{{ row.source }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="escalationLevel" label="级别" width="70" align="center">
        <template #default="{ row }">
          <el-tag v-if="row.escalationLevel" :type="getEscalationTagType(row.escalationLevel)" size="small">
            {{ row.escalationLevel }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" fixed="right" width="200">
        <template #default="{ row }">
          <template v-if="activeTab === 'pending'">
            <el-button size="small" type="success" @click="handleReceive(row)">接单</el-button>
            <el-button size="small" type="warning" @click="handleTransfer(row)">转单</el-button>
          </template>
          <template v-else-if="activeTab === 'received'">
            <el-button size="small" type="primary" @click="handleComplete(row)">完工</el-button>
          </template>
          <template v-else-if="activeTab === 'transfers'">
            <el-button size="small" type="success" @click="handleApprove(row)" :disabled="row.status !== 'Pending'">同意</el-button>
            <el-button size="small" type="danger" @click="handleReject(row)" :disabled="row.status !== 'Pending'">拒绝</el-button>
          </template>
          <el-button size="small" @click="handleDetail(row)">详情</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <el-pagination
      v-model:current-page="page"
      v-model:page-size="pageSize"
      :total="total"
      :page-sizes="[10, 20, 50]"
      layout="total, sizes, prev, pager, next"
      @change="loadData"
      style="margin-top: 16px"
    />

    <!-- 转单弹窗 -->
    <el-dialog v-model="transferDialogVisible" title="申请转单" width="500px">
      <el-form :model="transferForm" label-width="80px">
        <el-form-item label="原执行人">
          <el-input v-model="transferForm.fromPersonName" disabled />
        </el-form-item>
        <el-form-item label="目标人员" required>
          <el-select v-model="transferForm.toPersonId" placeholder="请选择" filterable>
            <el-option v-for="p in personList" :key="p.id" :label="p.name" :value="p.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="转单原因" required>
          <el-input v-model="transferForm.reason" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="transferDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmTransfer">确认转单</el-button>
      </template>
    </el-dialog>

    <!-- 完工弹窗 -->
    <el-dialog v-model="completeDialogVisible" title="完工提交" width="500px">
      <el-form :model="completeForm" label-width="100px">
        <el-form-item label="完工备注">
          <el-input v-model="completeForm.completionRemark" type="textarea" rows="4" placeholder="请描述完成情况" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="completeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmComplete">确认完工</el-button>
      </template>
    </el-dialog>

    <!-- 评价弹窗 -->
    <el-dialog v-model="ratingDialogVisible" title="确认完工并评价" width="500px">
      <el-form :model="ratingForm" label-width="100px">
        <el-form-item label="服务质量">
          <el-rate v-model="ratingForm.qualityScore" />
        </el-form-item>
        <el-form-item label="服务态度">
          <el-rate v-model="ratingForm.attitudeScore" />
        </el-form-item>
        <el-form-item label="及时性">
          <el-rate v-model="ratingForm.timelinessScore" />
        </el-form-item>
        <el-form-item label="总体评价">
          <el-rate v-model="ratingForm.overallScore" />
        </el-form-item>
        <el-form-item label="评价备注">
          <el-input v-model="ratingForm.comment" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="ratingDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmRating">确认评价</el-button>
      </template>
    </el-dialog>

    <!-- 详情弹窗 -->
    <el-dialog v-model="detailDialogVisible" title="派单详情" width="700px">
      <el-descriptions :column="2" border v-if="currentRecord">
        <el-descriptions-item label="工单号">{{ currentRecord.ticketCode }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(currentRecord.status)">{{ getStatusText(currentRecord.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="派单人">{{ currentRecord.fromPersonName }}</el-descriptions-item>
        <el-descriptions-item label="执行人">{{ currentRecord.toPersonName }}</el-descriptions-item>
        <el-descriptions-item label="派单时间">{{ formatTime(currentRecord.dispatchTime) }}</el-descriptions-item>
        <el-descriptions-item label="来源">{{ currentRecord.source }}</el-descriptions-item>
        <el-descriptions-item label="位置" :span="2">
          {{ [currentRecord.areaName, currentRecord.buildingName, currentRecord.roomName].filter(Boolean).join(' / ') || '-' }}
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import dispatchApi from '@/api/dispatch'
import { personApi } from '@/api/person'

const activeTab = ref('pending')
const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(10)
const pendingCount = ref(0)
const alertCount = ref(0)
const transferCount = ref(0)

const transferDialogVisible = ref(false)
const completeDialogVisible = ref(false)
const ratingDialogVisible = ref(false)
const detailDialogVisible = ref(false)
const currentRecord = ref(null)

const personList = ref([])

const transferForm = reactive({
  dispatchRecordId: 0,
  fromPersonName: '',
  toPersonId: null,
  reason: ''
})

const completeForm = reactive({
  dispatchRecordId: 0,
  completionRemark: ''
})

const ratingForm = reactive({
  raterId: 1,
  raterName: '管理员',
  qualityScore: 5,
  attitudeScore: 5,
  timelinessScore: 5,
  overallScore: 5,
  comment: ''
})

const statusMap = {
  Pending: { text: '待接收', type: 'warning' },
  WReceived: { text: '已接收', type: 'primary' },
  Completed: { text: '已完工', type: 'success' },
  Confirmed: { text: '已确认', type: 'info' },
  Transferred: { text: '已转单', type: 'warning' },
  Cancelled: { text: '已取消', type: 'danger' },
  Escalated: { text: '已升级', type: 'danger' }
}

function getStatusText(status) {
  return statusMap[status]?.text || status
}

function getStatusType(status) {
  return statusMap[status]?.type || 'info'
}

function getEscalationTagType(level) {
  const map = { 'L1': 'warning', 'L2': 'danger', 'L3': 'danger', 'L4': 'danger' }
  return map[level] || 'info'
}

function formatTime(time) {
  if (!time) return '-'
  return new Date(time).toLocaleString('zh-CN', { hour12: false })
}

async function loadData() {
  loading.value = true
  try {
    if (activeTab.value === 'pending') {
      const res = await dispatchApi.getPending(page.value, pageSize.value)
      if (res.data.success) {
        tableData.value = res.data.data
        total.value = res.data.total
        pendingCount.value = res.data.total
      }
    } else if (activeTab.value === 'received') {
      const res = await dispatchApi.getPending(page.value, pageSize.value)
      if (res.data.success) {
        tableData.value = res.data.data.filter(d => d.status === 'WReceived')
        total.value = tableData.value.length
      }
    } else if (activeTab.value === 'completed') {
      const res = await dispatchApi.getPending(page.value, pageSize.value)
      if (res.data.success) {
        tableData.value = res.data.data.filter(d => d.status === 'Completed')
        total.value = tableData.value.length
      }
    } else if (activeTab.value === 'alerts') {
      const res = await dispatchApi.getAlerts(page.value, pageSize.value)
      if (res.data.success) {
        tableData.value = res.data.data
        total.value = res.data.total
        alertCount.value = res.data.total
      }
    } else if (activeTab.value === 'transfers') {
      const res = await dispatchApi.getPendingTransfers(page.value, pageSize.value)
      if (res.data.success) {
        tableData.value = res.data.data
        total.value = res.data.total
        transferCount.value = res.data.total
      }
    }
  } catch (err) {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

function handleTabChange() {
  page.value = 1
  loadData()
}

function handleReceive(row) {
  ElMessageBox.confirm(`确认接收工单 ${row.ticketCode}？`, '接收确认').then(async () => {
    const res = await dispatchApi.receive(row.id)
    if (res.data.success) {
      ElMessage.success('接单成功')
      loadData()
    } else {
      ElMessage.error(res.data.message || '接单失败')
    }
  }).catch(() => {})
}

function handleTransfer(row) {
  currentRecord.value = row
  transferForm.dispatchRecordId = row.id
  transferForm.fromPersonName = row.toPersonName
  transferForm.toPersonId = null
  transferForm.reason = ''
  transferDialogVisible.value = true
  loadPersons()
}

async function loadPersons() {
  try {
    const res = await personApi.getList(1, 100)
    if (res.data.success) {
      personList.value = res.data.data.filter(p => p.status === 'Active')
    }
  } catch {}
}

async function confirmTransfer() {
  if (!transferForm.toPersonId) {
    ElMessage.warning('请选择目标人员')
    return
  }
  const person = personList.value.find(p => p.id === transferForm.toPersonId)
  const res = await dispatchApi.transfer({
    dispatchRecordId: transferForm.dispatchRecordId,
    toPersonId: transferForm.toPersonId,
    toPersonName: person?.name || '',
    reason: transferForm.reason
  })
  if (res.data.success) {
    ElMessage.success('转单申请已提交')
    transferDialogVisible.value = false
    loadData()
  } else {
    ElMessage.error(res.data.message || '转单失败')
  }
}

function handleComplete(row) {
  currentRecord.value = row
  completeForm.dispatchRecordId = row.id
  completeForm.completionRemark = ''
  completeDialogVisible.value = true
}

async function confirmComplete() {
  const res = await dispatchApi.complete(completeForm.dispatchRecordId, {
    completionRemark: completeForm.completionRemark
  })
  if (res.data.success) {
    ElMessage.success('完工提交成功')
    completeDialogVisible.value = false
    ratingDialogVisible.value = true
  } else {
    ElMessage.error(res.data.message || '完工失败')
  }
}

async function confirmRating() {
  const res = await dispatchApi.confirm(completeForm.dispatchRecordId, ratingForm)
  if (res.data.success) {
    ElMessage.success('确认成功，感谢评价')
    ratingDialogVisible.value = false
    completeDialogVisible.value = false
    loadData()
  } else {
    ElMessage.error(res.data.message || '确认失败')
  }
}

function handleApprove(row) {
  ElMessageBox.confirm(`确认同意转单？`, '审批确认').then(async () => {
    const res = await dispatchApi.approveTransfer(row.id, 'approve')
    if (res.data.success) {
      ElMessage.success('已同意转单')
      loadData()
    } else {
      ElMessage.error(res.data.message || '操作失败')
    }
  }).catch(() => {})
}

function handleReject(row) {
  ElMessageBox.confirm(`确认拒绝转单？`, '审批确认').then(async () => {
    const res = await dispatchApi.approveTransfer(row.id, 'reject')
    if (res.data.success) {
      ElMessage.success('已拒绝转单')
      loadData()
    } else {
      ElMessage.error(res.data.message || '操作失败')
    }
  }).catch(() => {})
}

function handleDetail(row) {
  currentRecord.value = row
  detailDialogVisible.value = true
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.dispatch-list {
  padding: 16px;
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}
.escalation-tag {
  color: #E6A23C;
  font-weight: bold;
}
  gap: 12px;
  margin-bottom: 16px;
}
.toolbar .total {
  color: #666;
  font-size: 14px;
}
</style>