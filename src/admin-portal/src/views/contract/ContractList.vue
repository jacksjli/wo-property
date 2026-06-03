<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, Document, Warning, Money, Clock } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { contractService } from '@/api/contract'

type ContractType = 'rental' | 'service' | 'procurement' | 'construction' | 'maintenance' | 'other'
type ContractStatus = 'draft' | 'active' | 'expired' | 'terminated' | 'renewed'
type PaymentMethod = 'one_time' | 'monthly' | 'quarterly' | 'yearly' | 'custom'

interface PaymentRecord {
  id: number; contractId: number; date: string; amount: number;
  method: PaymentMethod; invoiceNo: string; handler: string; remark: string;
}

interface Contract {
  id: number; contractNo: string; name: string; type: ContractType; partyA: string; partyB: string;
  contactPersonA: string; contactPersonB: string; phoneA: string; phoneB: string;
  addressA: string; addressB: string; amount: number; paymentMethod: PaymentMethod;
  signDate: string; startDate: string; endDate: string; status: ContractStatus;
  autoRenew: boolean; renewalPeriod: number; nextRenewDate: string;
  description: string; remark: string; createdBy: string; createdAt: string;
  paymentRecords: PaymentRecord[];
}

const contractTypeLabels: Record<ContractType, string> = {
  'rental': '租赁合同', 'service': '服务合同', 'procurement': '采购合同',
  'construction': '施工合同', 'maintenance': '维保合同', 'other': '其他合同'
}
const contractStatusLabels: Record<ContractStatus, string> = {
  'draft': '草稿', 'active': '执行中', 'expired': '已到期', 'terminated': '已终止', 'renewed': '已续约'
}
const paymentMethodLabels: Record<PaymentMethod, string> = {
  'one_time': '一次性付款', 'monthly': '月付', 'quarterly': '季付', 'yearly': '年付', 'custom': '自定义'
}
const formatAmount = (amount: number) => `¥${amount.toLocaleString('zh-CN', { minimumFractionDigits: 2 })}`

const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const refreshKey = ref(0)
const refreshFields = () => { refreshKey.value++ }
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) { fieldDialogRef.value?.open() }
}

const contractList = ref<Contract[]>([])
const loading = ref(false)
const stats = computed(() => ({
  total: contractList.value.length,
  active: contractList.value.filter(c => c.status === 'active').length,
  expired: contractList.value.filter(c => c.status === 'expired').length,
  activeAmount: contractList.value.filter(c => c.status === 'active').reduce((s, c) => s + (c.amount || 0), 0)
}))

const expiringContracts = computed(() => {
  const today = new Date()
  const thirtyDaysLater = new Date()
  thirtyDaysLater.setDate(today.getDate() + 30)
  return contractList.value.filter(c => {
    if (c.status !== 'active') return false
    const endDate = new Date(c.endDate)
    return endDate >= today && endDate <= thirtyDaysLater
  })
})

const filterStatus = ref<ContractStatus | ''>('')
const filteredContracts = computed(() => {
  if (!filterStatus.value) return contractList.value
  return contractList.value.filter(c => c.status === filterStatus.value)
})
const handleStatClick = (status: ContractStatus | '') => { filterStatus.value = status }

const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const paymentDialogVisible = ref(false)
const terminateDialogVisible = ref(false)
const renewDialogVisible = ref(false)
const dialogTitle = ref('新增合同')
const editingId = ref<number | null>(null)
const viewingContract = ref<Contract | null>(null)

const form = ref({
  contractNo: '', name: '', type: 'service' as ContractType, partyA: '', partyB: '',
  contactPersonA: '', contactPersonB: '', phoneA: '', phoneB: '',
  addressA: '', addressB: '', amount: 0, paymentMethod: 'monthly' as string,
  signDate: '', startDate: '', endDate: '', autoRenew: false, renewalPeriod: 12,
  description: '', remark: ''
})

const paymentForm = ref({ date: '', amount: 0, method: 'monthly' as string, invoiceNo: '', handler: '', remark: '' })
const terminateReason = ref('')
const renewForm = ref({ newEndDate: '', newAmount: 0 })

const typeOptions = Object.entries(contractTypeLabels).map(([value, label]) => ({ value, label }))
const statusOptions = Object.entries(contractStatusLabels).map(([value, label]) => ({ value, label }))
const paymentMethodOptions = Object.entries(paymentMethodLabels).map(([value, label]) => ({ value, label }))

const getStatusType = (status: ContractStatus) => {
  const map: Record<ContractStatus, string> = { draft: 'info', active: 'success', expired: 'warning', terminated: 'danger', renewed: 'success' }
  return map[status]
}

const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 100 }
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await contractService.getContracts(params)
    if (res.success) {
      contractList.value = res.data?.list || res.data || []
    } else if (res.data) {
      contractList.value = res.data.list || res.data || []
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const getContractFields = () => getActiveFields('contract')

const handleAdd = () => {
  dialogTitle.value = '新增合同'
  editingId.value = null
  const today = new Date().toISOString().split('T')[0]
  form.value = {
    contractNo: 'CT-' + new Date().getFullYear() + '-' + String(Date.now()).slice(-3),
    name: '', type: 'service', partyA: '', partyB: '', contactPersonA: '', contactPersonB: '',
    phoneA: '', phoneB: '', addressA: '', addressB: '', amount: 0, paymentMethod: 'monthly',
    signDate: today, startDate: today, endDate: '', autoRenew: false, renewalPeriod: 12, description: '', remark: ''
  }
  dialogVisible.value = true
}

const handleEdit = (row: Contract) => {
  dialogTitle.value = '编辑合同'
  editingId.value = row.id
  form.value = {
    contractNo: row.contractNo, name: row.name, type: row.type, partyA: row.partyA, partyB: row.partyB,
    contactPersonA: row.contactPersonA || '', contactPersonB: row.contactPersonB || '',
    phoneA: row.phoneA || '', phoneB: row.phoneB || '', addressA: row.addressA || '', addressB: row.addressB || '',
    amount: row.amount, paymentMethod: row.paymentMethod, signDate: row.signDate, startDate: row.startDate,
    endDate: row.endDate, autoRenew: row.autoRenew, renewalPeriod: row.renewalPeriod,
    description: row.description || '', remark: row.remark || ''
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.value.contractNo.trim()) { ElMessage.warning('请输入合同编号'); return }
  if (!form.value.name.trim()) { ElMessage.warning('请输入合同名称'); return }
  if (!form.value.partyA.trim()) { ElMessage.warning('请输入甲方'); return }
  if (!form.value.partyB.trim()) { ElMessage.warning('请输入乙方'); return }
  if (form.value.amount <= 0) { ElMessage.warning('请输入正确的合同金额'); return }

  try {
    const payload = {
      contractNumber: form.value.contractNo,
      contractName: form.value.name,
      contractType: form.value.type,
      partyA: form.value.partyA,
      partyB: form.value.partyB,
      signedDate: form.value.signDate || null,
      startDate: form.value.startDate || null,
      endDate: form.value.endDate || null,
      amount: form.value.amount,
      remarks: form.value.remark || null,
    }
    if (editingId.value) {
      await contractService.updateContract(editingId.value, payload)
      ElMessage.success('更新成功')
    } else {
      await contractService.createContract(payload)
      ElMessage.success('添加成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: Contract) => {
  try {
    await ElMessageBox.confirm(`确定删除合同 "${row.name}" 吗？`, '删除确认',
      { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning' })
    await contractService.deleteContract(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const handleView = (row: Contract) => { viewingContract.value = row; detailDialogVisible.value = true }

const openTerminateDialog = (row: Contract) => { viewingContract.value = row; terminateReason.value = ''; terminateDialogVisible.value = true }

const handleTerminate = async () => {
  if (!terminateReason.value.trim()) { ElMessage.warning('请输入终止原因'); return }
  try {
    await contractService.terminateContract(viewingContract.value!.id, terminateReason.value)
    contractList.value = contractList.value.map(c => c.id === viewingContract.value!.id ? { ...c, status: 'terminated' as ContractStatus } : c)
    terminateDialogVisible.value = false
    ElMessage.success('合同已终止')
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
}

const openRenewDialog = (row: Contract) => {
  viewingContract.value = row
  renewForm.value = { newEndDate: '', newAmount: row.amount }
  renewDialogVisible.value = true
}

const handleRenew = async () => {
  if (!renewForm.value.newEndDate) { ElMessage.warning('请选择新的到期日期'); return }
  try {
    await contractService.renewContract(viewingContract.value!.id, {
      newEndDate: renewForm.value.newEndDate,
      newAmount: renewForm.value.newAmount,
    })
    renewDialogVisible.value = false
    ElMessage.success('合同续约成功')
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
}

const openPaymentDialog = (row: Contract) => {
  viewingContract.value = row
  paymentForm.value = { date: new Date().toISOString().split('T')[0], amount: 0, method: row.paymentMethod, invoiceNo: '', handler: '', remark: '' }
  paymentDialogVisible.value = true
}

const handleAddPayment = () => {
  if (paymentForm.value.amount <= 0) { ElMessage.warning('请输入正确的付款金额'); return }
  // Payment records are tracked separately; for now just close dialog
  paymentDialogVisible.value = false
  ElMessage.success('付款记录已添加（实际业务需扩展API）')
}

const handleRefresh = () => { loadData(); ElMessage.success('已刷新') }

onMounted(() => { loadData() })
</script>

<template>
  <div class="contract-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>合同管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon> 配置字段</el-button>
            <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon> 刷新</el-button>
            <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon> 新增合同</el-button>
          </div>
        </div>
      </template>

      <el-alert title="合同管理说明" description="合同管理包含合同信息、付款记录、续约提醒等功能。" type="info" :closable="false" style="margin-bottom: 20px;" />

      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('')">
          <div class="stat-content"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">合同总数</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('active')">
          <div class="stat-content"><div class="stat-value normal">{{ stats.active }}</div><div class="stat-label">执行中</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('expired')">
          <div class="stat-content"><div class="stat-value warning">{{ stats.expired }}</div><div class="stat-label">已到期</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content"><div class="stat-value info">{{ formatAmount(stats.activeAmount) }}</div><div class="stat-label">执行中金额</div></div>
        </el-card>
      </div>

      <el-alert v-if="expiringContracts.length > 0" title="即将到期提醒" type="warning" :closable="true" style="margin-bottom: 20px;">
        有 {{ expiringContracts.length }} 份合同将在30天内到期，请及时处理续约事宜。
      </el-alert>

      <el-table :data="filteredContracts" stripe v-loading="loading" @row-click="handleView">
        <el-table-column prop="contractNo" label="合同编号" width="120" />
        <el-table-column prop="contractName || name" label="合同名称" min-width="150">
          <template #default="{ row }">{{ row.contractName || row.name }}</template>
        </el-table-column>
        <el-table-column prop="contractType || type" label="类型" width="100" align="center">
          <template #default="{ row }"><el-tag size="small">{{ contractTypeLabels[row.contractType || row.type] }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="partyA" label="甲方" width="150" />
        <el-table-column prop="partyB" label="乙方" width="150" />
        <el-table-column prop="amount" label="金额" width="120" align="right">
          <template #default="{ row }">{{ formatAmount(row.amount || 0) }}</template>
        </el-table-column>
        <el-table-column prop="endDate" label="到期日期" width="110" align="center" />
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ contractStatusLabels[row.status] }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="success" size="small" @click.stop="openPaymentDialog(row)"><el-icon><Money /></el-icon> 付款</el-button>
            <el-button link type="warning" size="small" @click.stop="openRenewDialog(row)" v-if="row.status === 'active'"><el-icon><Clock /></el-icon> 续约</el-button>
            <el-button link type="danger" size="small" @click.stop="openTerminateDialog(row)" v-if="row.status === 'active'">终止</el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="750px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="合同编号" required><el-input v-model="form.contractNo" placeholder="如：CT-2024-001" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="合同类型"><el-select v-model="form.type" style="width: 100%"><el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item></el-col>
        </el-row>
        <el-form-item label="合同名称" required><el-input v-model="form.name" placeholder="请输入合同名称" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="甲方" required><el-input v-model="form.partyA" placeholder="请输入甲方" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="乙方" required><el-input v-model="form.partyB" placeholder="请输入乙方" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="甲方联系人"><el-input v-model="form.contactPersonA" placeholder="甲方联系人" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="乙方联系人"><el-input v-model="form.contactPersonB" placeholder="乙方联系人" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="甲方电话"><el-input v-model="form.phoneA" placeholder="甲方电话" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="乙方电话"><el-input v-model="form.phoneB" placeholder="乙方电话" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="合同金额" required><el-input-number v-model="form.amount" :min="0" :precision="2" style="width: 100%" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="付款方式"><el-select v-model="form.paymentMethod" style="width: 100%"><el-option v-for="opt in paymentMethodOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="签订日期"><el-date-picker v-model="form.signDate" type="date" style="width: 100%" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="开始日期"><el-date-picker v-model="form.startDate" type="date" style="width: 100%" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="结束日期" required><el-date-picker v-model="form.endDate" type="date" style="width: 100%" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="自动续约"><el-switch v-model="form.autoRenew" /></el-form-item></el-col>
          <el-col :span="12" v-if="form.autoRenew"><el-form-item label="续约周期"><el-input-number v-model="form.renewalPeriod" :min="1" :max="36" /> 月</el-form-item></el-col>
        </el-row>
        <el-form-item label="合同描述"><el-input v-model="form.description" type="textarea" placeholder="请输入合同描述" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 合同详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="合同详情" width="800px">
      <div v-if="viewingContract" class="contract-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="合同编号">{{ viewingContract.contractNo }}</el-descriptions-item>
          <el-descriptions-item label="合同类型"><el-tag size="small">{{ contractTypeLabels[viewingContract.type] }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="合同名称" :span="2">{{ viewingContract.name }}</el-descriptions-item>
          <el-descriptions-item label="甲方">{{ viewingContract.partyA }}</el-descriptions-item>
          <el-descriptions-item label="乙方">{{ viewingContract.partyB }}</el-descriptions-item>
          <el-descriptions-item label="甲方联系人">{{ viewingContract.contactPersonA || '-' }}</el-descriptions-item>
          <el-descriptions-item label="乙方联系人">{{ viewingContract.contactPersonB || '-' }}</el-descriptions-item>
          <el-descriptions-item label="甲方电话">{{ viewingContract.phoneA || '-' }}</el-descriptions-item>
          <el-descriptions-item label="乙方电话">{{ viewingContract.phoneB || '-' }}</el-descriptions-item>
          <el-descriptions-item label="合同金额"><span style="color: #409EFF; font-weight: bold;">{{ formatAmount(viewingContract.amount) }}</span></el-descriptions-item>
          <el-descriptions-item label="付款方式">{{ paymentMethodLabels[viewingContract.paymentMethod] }}</el-descriptions-item>
          <el-descriptions-item label="签订日期">{{ viewingContract.signDate }}</el-descriptions-item>
          <el-descriptions-item label="开始日期">{{ viewingContract.startDate }}</el-descriptions-item>
          <el-descriptions-item label="结束日期">{{ viewingContract.endDate }}</el-descriptions-item>
          <el-descriptions-item label="合同状态"><el-tag :type="getStatusType(viewingContract.status)">{{ contractStatusLabels[viewingContract.status] }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="自动续约">{{ viewingContract.autoRenew ? '是' : '否' }}</el-descriptions-item>
          <el-descriptions-item label="下次续约">{{ viewingContract.nextRenewDate || '-' }}</el-descriptions-item>
          <el-descriptions-item label="合同描述" :span="2">{{ viewingContract.description || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer><el-button @click="detailDialogVisible = false">关闭</el-button></template>
    </el-dialog>

    <!-- 付款记录对话框 -->
    <el-dialog v-model="paymentDialogVisible" title="添加付款记录" width="500px">
      <el-form label-width="80px">
        <el-form-item label="付款日期" required><el-date-picker v-model="paymentForm.date" type="date" style="width: 100%" /></el-form-item>
        <el-form-item label="付款金额" required><el-input-number v-model="paymentForm.amount" :min="0" :precision="2" style="width: 100%" /></el-form-item>
        <el-form-item label="付款方式"><el-select v-model="paymentForm.method" style="width: 100%"><el-option v-for="opt in paymentMethodOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item>
        <el-form-item label="发票号"><el-input v-model="paymentForm.invoiceNo" placeholder="请输入发票号" /></el-form-item>
        <el-form-item label="经办人"><el-input v-model="paymentForm.handler" placeholder="请输入经办人" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="paymentForm.remark" type="textarea" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="paymentDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleAddPayment">确定</el-button>
      </template>
    </el-dialog>

    <!-- 终止合同对话框 -->
    <el-dialog v-model="terminateDialogVisible" title="终止合同" width="500px">
      <el-form label-width="80px">
        <el-form-item label="终止原因" required><el-input v-model="terminateReason" type="textarea" placeholder="请输入终止原因" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="terminateDialogVisible = false">取消</el-button>
        <el-button type="danger" @click="handleTerminate">确认终止</el-button>
      </template>
    </el-dialog>

    <!-- 续约合同对话框 -->
    <el-dialog v-model="renewDialogVisible" title="合同续约" width="500px">
      <el-form label-width="100px">
        <el-form-item label="原合同信息">
          <div>{{ viewingContract?.name }}</div>
          <div>原到期日：{{ viewingContract?.endDate }}</div>
          <div>原金额：{{ viewingContract ? formatAmount(viewingContract.amount) : '' }}</div>
        </el-form-item>
        <el-form-item label="新到期日期" required><el-date-picker v-model="renewForm.newEndDate" type="date" style="width: 100%" /></el-form-item>
        <el-form-item label="新合同金额"><el-input-number v-model="renewForm.newAmount" :min="0" :precision="2" style="width: 100%" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="renewDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleRenew">确认续约</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog ref="fieldDialogRef" module="contract" module-name="合同管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.contract-page { width: 100%; }
.header { display: flex; justify-content: space-between; align-items: center; }
.header-actions { display: flex; gap: 10px; }
.stats-grid { display: flex; gap: 15px; margin-bottom: 20px; flex-wrap: wrap; }
.stat-card { flex: 1; min-width: 100px; cursor: pointer; transition: all 0.3s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { text-align: center; }
.stat-value { font-size: 24px; font-weight: bold; color: #409EFF; }
.stat-value.normal { color: #67C23A; }
.stat-value.warning { color: #E6A23C; }
.stat-value.danger { color: #F56C6C; }
.stat-value.info { color: #909399; }
.stat-label { font-size: 13px; color: #909399; margin-top: 5px; }
.contract-detail { padding: 10px; }
</style>