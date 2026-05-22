<template>
  <div class="finance-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>财务管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增记录
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="财务管理说明"
        description="记录所有收支明细，支持按类型、分类、日期筛选，自动统计收支情况。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计卡片 - with icons -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card income">
          <div class="stat-content">
            <div class="stat-icon"><el-icon><ArrowUp /></el-icon></div>
            <div class="stat-info">
              <div class="stat-value">{{ (stats?.totalIncome ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</div>
              <div class="stat-label">总收入</div>
            </div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card expense">
          <div class="stat-content">
            <div class="stat-icon"><el-icon><ArrowDown /></el-icon></div>
            <div class="stat-info">
              <div class="stat-value">{{ (stats?.totalExpense ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</div>
              <div class="stat-label">总支出</div>
            </div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card balance" :class="{ negative: stats?.balance < 0 }">
          <div class="stat-content">
            <div class="stat-icon"><el-icon><Money /></el-icon></div>
            <div class="stat-info">
              <div class="stat-value">{{ (stats?.balance ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</div>
              <div class="stat-label">结余</div>
            </div>
          </div>
        </el-card>
      </div>

      <!-- 筛选工具栏 -->
      <div class="filter-toolbar">
        <el-select v-model="filterType" placeholder="收支类型" clearable style="width: 120px;">
          <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterCategory" placeholder="分类" clearable style="width: 140px;">
          <el-option v-for="opt in categoryOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-date-picker
          v-model="filterDateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          style="width: 240px;"
        />
        <el-button @click="handleReset">重置</el-button>
      </div>

      <!-- 记录列表 -->
      <el-table :data="filteredList" stripe v-loading="loading">
        <el-table-column prop="recordNumber" label="交易编号" width="130" />
        <el-table-column prop="type" label="类型" width="80" align="center">
          <template #default="{ row }">
            <el-tag :style="{ backgroundColor: getTypeColor(row.type), borderColor: getTypeColor(row.type), color: '#fff' }">
              {{ typeLabels[row.type] }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="category" label="分类" width="100" align="center">
          <template #default="{ row }">
            {{ categoryLabels[row.category] || row.category }}
          </template>
        </el-table-column>
        <el-table-column prop="amount" label="金额" width="120" align="right">
          <template #default="{ row }">
            <span :style="{ color: getTypeColor(row.type), fontWeight: 'bold' }">
              {{ row.type === 'income' ? '+' : '-' }}{{ Number(row.amount).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="paymentMethod" label="支付方式" width="100" align="center">
          <template #default="{ row }">
            {{ paymentMethodLabels[row.paymentMethod] || row.paymentMethod }}
          </template>
        </el-table-column>
        <el-table-column prop="recordDate" label="日期" width="100" align="center" />
        <el-table-column prop="relatedParty" label="对方" width="120" />
        <el-table-column prop="description" label="说明" min-width="150" />
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ statusLabels[row.status] }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="650px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="交易编号" required>
              <el-input v-model="form.transactionNo" placeholder="如：TR-2024-001" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="收支类型" required>
              <el-select v-model="form.type" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="分类" required>
              <el-select v-model="form.category" style="width: 100%">
                <el-option v-for="opt in categoryOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="金额" required>
              <el-input-number v-model="form.amount" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="支付方式">
              <el-select v-model="form.paymentMethod" style="width: 100%">
                <el-option v-for="opt in paymentMethodOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="日期" required>
              <el-date-picker v-model="form.date" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="经手人">
              <el-input v-model="form.handler" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="对方">
              <el-input v-model="form.relatedParty" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="合同编号">
              <el-input v-model="form.contractNo" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="票据编号">
              <el-input v-model="form.billNo" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="说明">
          <el-input v-model="form.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="收据编号">
              <el-input v-model="form.receiptNo" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="finance"
      :refresh-key="refreshKey"
      @updated="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, Money, ArrowDown, ArrowUp } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

const getFinanceFields = () => getActiveFields('finance')

const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open(getFinanceFields())
  }
}

const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

const loading = ref(false)
const transactionList = ref<any[]>([])
const stats = computed(() => {
  if (!transactionList.value || !transactionList.value.length) {
    return { totalIncome: 0, totalExpense: 0, balance: 0 }
  }
  const totalIncome = transactionList.value.filter(t => t.type === 'income').reduce((sum: number, t: any) => sum + Number(t.amount), 0)
  const totalExpense = transactionList.value.filter(t => t.type === 'expense').reduce((sum: number, t: any) => sum + Number(t.amount), 0)
  return {
    totalIncome: totalIncome || 0,
    totalExpense: totalExpense || 0,
    balance: (totalIncome || 0) - (totalExpense || 0)
  }
})

const filterType = ref('')
const filterCategory = ref('')
const filterDateRange = ref<[string, string] | null>(null)

const dialogVisible = ref(false)
const dialogTitle = ref('新增记录')
const editingId = ref<number | null>(null)

const form = ref({
  transactionNo: '',
  type: 'income',
  category: 'property_fee',
  amount: 0,
  paymentMethod: 'transfer',
  date: '',
  handler: '',
  relatedParty: '',
  contractNo: '',
  billNo: '',
  description: '',
  receiptNo: '',
  status: 'completed',
  remark: ''
})

const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 100 }
    if (filterType.value) params.type = filterType.value
    if (filterCategory.value) params.category = filterCategory.value
    const res: any = await masterApi.get('/finance-records', { params })
    transactionList.value = res.data || []
  } catch (e: any) {
    console.error('Finance loadData error:', e)
    ElMessage.error('加载失败: ' + (e?.message || e?.response?.data?.message || '未知错误'))
  } finally {
    loading.value = false
  }
}

onMounted(() => loadData())

const categoryOptions = [
  { value: 'property_fee', label: '物业费' },
  { value: 'parking', label: '停车费' },
  { value: 'advertising', label: '广告收入' },
  { value: 'facility', label: '设施租赁' },
  { value: 'utility', label: '水电费' },
  { value: 'maintenance', label: '维修费' },
  { value: 'cleaning', label: '保洁费' },
  { value: 'other', label: '其他' }
]

const typeOptions = [
  { value: 'income', label: '收入' },
  { value: 'expense', label: '支出' }
]

const paymentMethodOptions = [
  { value: 'transfer', label: '转账' },
  { value: 'wechat', label: '微信' },
  { value: 'alipay', label: '支付宝' },
  { value: 'cash', label: '现金' },
  { value: 'card', label: '刷卡' },
  { value: 'other', label: '其他' }
]

const statusOptions = [
  { value: 'pending', label: '待处理' },
  { value: 'completed', label: '已完成' },
  { value: 'cancelled', label: '已取消' }
]

const filteredList = computed(() => {
  let result = transactionList.value
  if (filterType.value) {
    result = result.filter((t: any) => t.type === filterType.value)
  }
  if (filterCategory.value) {
    result = result.filter((t: any) => t.category === filterCategory.value)
  }
  if (filterDateRange.value) {
    const [start, end] = filterDateRange.value
    result = result.filter((t: any) => t.recordDate >= start && t.recordDate <= end)
  }
  return result.sort((a: any, b: any) => (b.recordDate || '').localeCompare(a.recordDate || ''))
})

const getTypeColor = (type: string) => type === 'income' ? '#67C23A' : '#F56C6C'

const typeLabels: Record<string, string> = {
  income: '收入',
  expense: '支出'
}

const categoryLabels: Record<string, string> = {
  property_fee: '物业费',
  parking: '停车费',
  advertising: '广告收入',
  facility: '设施租赁',
  utility: '水电费',
  maintenance: '维修费',
  cleaning: '保洁费',
  other: '其他'
}

const paymentMethodLabels: Record<string, string> = {
  transfer: '转账',
  wechat: '微信',
  alipay: '支付宝',
  cash: '现金',
  card: '刷卡',
  other: '其他'
}

const statusLabels: Record<string, string> = {
  pending: '待处理',
  completed: '已完成',
  cancelled: '已取消'
}

const getStatusType = (status: string) => {
  const map: Record<string, string> = { pending: 'warning', completed: 'success', cancelled: 'info' }
  return map[status] || 'info'
}

const handleAdd = () => {
  editingId.value = null
  dialogTitle.value = '新增记录'
  form.value = {
    transactionNo: '', type: 'income', category: 'property_fee', amount: 0,
    paymentMethod: 'transfer', date: '', handler: '', relatedParty: '',
    contractNo: '', billNo: '', description: '', receiptNo: '', status: 'completed', remark: ''
  }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  editingId.value = row.id
  dialogTitle.value = '编辑记录'
  form.value = {
    transactionNo: row.recordNumber || '',
    type: row.type || 'income',
    category: row.category || 'property_fee',
    amount: Number(row.amount) || 0,
    paymentMethod: row.paymentMethod || 'transfer',
    date: row.recordDate || '',
    handler: row.handler || '',
    relatedParty: row.relatedParty || '',
    contractNo: row.contractNo || '',
    billNo: row.billNo || '',
    description: row.description || '',
    receiptNo: row.receiptNo || '',
    status: row.status || 'completed',
    remark: row.remarks || ''
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.value.transactionNo.trim()) {
    ElMessage.warning('请输入交易编号')
    return
  }
  if (form.value.amount <= 0) {
    ElMessage.warning('请输入正确的金额')
    return
  }
  if (!form.value.date) {
    ElMessage.warning('请选择日期')
    return
  }
  const payload = {
    RecordNumber: form.value.transactionNo,
    Type: form.value.type,
    Category: form.value.category,
    Amount: form.value.amount,
    PaymentMethod: form.value.paymentMethod,
    RecordDate: form.value.date,
    Handler: form.value.handler,
    RelatedParty: form.value.relatedParty,
    ContractNo: form.value.contractNo,
    BillNo: form.value.billNo,
    Description: form.value.description,
    ReceiptNo: form.value.receiptNo,
    Status: form.value.status,
    Remarks: form.value.remark,
  }
  try {
    if (editingId.value) {
      await masterApi.put(`/finance-records/${editingId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/finance-records', payload)
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
    await ElMessageBox.confirm(`确定删除记录 ${row.recordNumber} 吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/finance-records/${row.id}`)
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
  filterType.value = ''
  filterCategory.value = ''
  filterDateRange.value = null
}
</script>

<style scoped>
.finance-page { padding: 0; }
.header { display: flex; justify-content: space-between; align-items: center; }
.header-actions { display: flex; gap: 8px; }
.stats-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 20px; }
.stat-card.income { border-left: 4px solid #67C23A; }
.stat-card.expense { border-left: 4px solid #F56C6C; }
.stat-card.balance { border-left: 4px solid #409EFF; }
.stat-card.balance.negative { border-left-color: #F56C6C; }
.stat-content { display: flex; align-items: center; gap: 16px; }
.stat-icon { font-size: 32px; color: #909399; }
.stat-info { flex: 1; }
.stat-value { font-size: 20px; font-weight: bold; color: #303133; }
.stat-label { font-size: 14px; color: #909399; margin-top: 4px; }
.filter-toolbar { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; align-items: center; }
</style>
