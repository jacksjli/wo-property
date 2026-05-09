<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance } from 'element-plus'
import { Plus, Delete, Edit, Search, Refresh, Money } from '@element-plus/icons-vue'
import { getActiveFields, addField, updateField, deleteField, toggleFieldStatus, autoGenerateKey, fieldConfigs, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { masterApi } from '@/api/http'

const { verifyAdminPassword } = usePermission()

// 字段配置对话框
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的收费项目
const getActiveFeeItems = () => getActiveFields('payment')

// 打开字段配置（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 刷新字段配置
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

// 数据状态
const loading = ref(false)
const tableData = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })
const searchForm = ref({
  ownerName: '',
  status: '',
  paymentNo: '',
  roomNo: ''
})

// 统计
const stats = computed(() => {
  const total = pagination.value.totalCount
  const paid = tableData.value.filter(r => r.status === 'paid').length
  const unpaid = tableData.value.filter(r => r.status === 'pending' || r.status === 'overdue').length
  const totalAmount = tableData.value.reduce((sum, r) => sum + (r.amount || 0), 0)
  return { total, paid, unpaid, totalAmount }
})

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增缴费记录')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

// 表单数据
const form = ref<any>({
  paymentNumber: '',
  residentId: null,
  residentName: '',
  roomId: null,
  paymentType: 'property_fee',
  amount: 0,
  periodStart: '',
  periodEnd: '',
  dueDate: '',
  remarks: ''
})

// 选项
const paymentTypeOptions = [
  { value: 'property_fee', label: '物业费' },
  { value: 'water_fee', label: '水费' },
  { value: 'electric_fee', label: '电费' },
  { value: 'parking_fee', label: '停车费' },
  { value: 'other', label: '其他' }
]

const statusOptions = [
  { value: 'paid', label: '已支付' },
  { value: 'pending', label: '待支付' },
  { value: 'overdue', label: '逾期' }
]

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/payment-records', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.pageSize,
        status: searchForm.value.status || undefined,
        keyword: searchForm.value.ownerName || undefined
      }
    })
    if (res.success) {
      tableData.value = res.data || []
      pagination.value = res.pagination || pagination.value
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败')
  } finally {
    loading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.value.page = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.value = { ownerName: '', status: '', paymentNo: '', roomNo: '' }
  handleSearch()
}

// 刷新
const handleRefresh = () => {
  loadData()
  ElMessage.success('数据已刷新')
}

// 初始化表单
const initForm = () => {
  const data: any = {
    paymentNumber: '',
    residentId: null,
    residentName: '',
    roomId: null,
    paymentType: 'property_fee',
    amount: 0,
    periodStart: '',
    periodEnd: '',
    dueDate: '',
    remarks: ''
  }
  getActiveFeeItems().forEach(f => {
    data[f.key] = f.defaultAmount
  })
  return data
}

// 打开新增对话框
const handleAdd = () => {
  form.value = initForm()
  editingId.value = null
  dialogTitle.value = '新增缴费记录'
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  editingId.value = row.id
  form.value = {
    paymentNumber: row.paymentNumber || '',
    residentId: row.residentId || null,
    residentName: row.residentName || row.ownerName || '',
    roomId: row.roomId || null,
    paymentType: row.paymentType || 'property_fee',
    amount: row.amount || 0,
    periodStart: row.periodStart?.split('T')[0] || '',
    periodEnd: row.periodEnd?.split('T')[0] || '',
    dueDate: row.dueDate?.split('T')[0] || '',
    remarks: row.remarks || ''
  }
  dialogTitle.value = '编辑缴费记录'
  dialogVisible.value = true
}

// 提交
const handleSubmit = async () => {
  if (!form.value.amount) {
    ElMessage.warning('请输入缴费金额')
    return
  }

  submitting.value = true
  try {
    if (editingId.value) {
      await masterApi.put(`/payment-records/${editingId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/payment-records', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除缴费记录「${row.paymentNumber}」吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await masterApi.delete(`/payment-records/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

// 标记支付
const handleMarkPaid = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认住户「${row.residentName}」已付款？`, '确认支付', { type: 'info' })
    await masterApi.put(`/payment-records/${row.id}`, {
      status: 'paid',
      paidDate: new Date().toISOString(),
      paymentMethod: 'offline'
    })
    ElMessage.success('已标记为已支付')
    loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '操作失败')
  }
}

// 状态颜色
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    paid: 'success',
    pending: 'warning',
    overdue: 'danger'
  }
  return map[status] || 'info'
}

// 状态标签
const getStatusLabel = (status: string) => {
  const map: Record<string, string> = {
    paid: '已支付',
    pending: '待支付',
    overdue: '逾期'
  }
  return map[status] || status
}

// 格式化日期
const formatDate = (date: string) => {
  if (!date) return '-'
  return date.split('T')[0]
}

// 分页
const handlePageChange = (page: number) => {
  pagination.value.page = page
  loadData()
}

// 初始化
onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="payment-page">
    <!-- 统计卡片 -->
    <div class="stats-container">
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon total-icon"><Money /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.total }}</span>
            <span class="stat-label">缴费记录总数</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon paid-icon"><Money /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.paid }}</span>
            <span class="stat-label">已支付</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon unpaid-icon"><Money /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.unpaid }}</span>
            <span class="stat-label">待支付</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon amount-icon"><Money /></el-icon>
          <div class="stat-info">
            <span class="stat-value">¥{{ stats.totalAmount.toLocaleString() }}</span>
            <span class="stat-label">总金额</span>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 操作栏 -->
    <el-card shadow="never" class="操作栏">
      <el-form :model="searchForm" inline>
        <el-form-item label="住户姓名">
          <el-input v-model="searchForm.ownerName" placeholder="请输入住户姓名" clearable style="width: 150px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
            <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch"><el-icon><Search /></el-icon>搜索</el-button>
          <el-button @click="handleReset"><el-icon><Refresh /></el-icon>重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 表格 -->
    <el-card shadow="never">
      <div class="操作按钮">
        <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon>新增缴费记录</el-button>
        <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Edit /></el-icon>配置收费项目</el-button>
      </div>

      <el-table :data="tableData" stripe style="width: 100%" :max-height="600" v-loading="loading">
        <el-table-column prop="paymentNumber" label="缴费单号" width="150" />
        <el-table-column prop="residentName" label="住户姓名" width="100" align="center" />
        <el-table-column prop="roomNumber" label="房号" width="80" align="center" />
        <el-table-column prop="buildingName" label="楼栋" width="80" align="center" />
        <el-table-column prop="paymentType" label="缴费类型" width="100">
          <template #default="{ row }">
            {{ row.paymentType === 'property_fee' ? '物业费' : row.paymentType === 'water_fee' ? '水费' : row.paymentType === 'electric_fee' ? '电费' : row.paymentType === 'parking_fee' ? '停车费' : row.paymentType }}
          </template>
        </el-table-column>
        <el-table-column prop="amount" label="金额(元)" width="100" align="center">
          <template #default="{ row }">
            <span style="color: #409EFF; font-weight: bold;">¥{{ row.amount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="periodStart" label="费用周期" width="200">
          <template #default="{ row }">
            {{ formatDate(row.periodStart) }} ~ {{ formatDate(row.periodEnd) }}
          </template>
        </el-table-column>
        <el-table-column prop="dueDate" label="到期日期" width="110">
          <template #default="{ row }">
            {{ formatDate(row.dueDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="paidDate" label="支付日期" width="110">
          <template #default="{ row }">
            {{ formatDate(row.paidDate) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status !== 'paid'" type="success" link size="small" @click="handleMarkPaid(row)">确认支付</el-button>
            <el-button type="primary" link size="small" @click="handleEdit(row)"><el-icon><Edit /></el-icon>编辑</el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)"><el-icon><Delete /></el-icon>删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.page"
          :page-size="pagination.pageSize"
          :total="pagination.totalCount"
          layout="total, prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px" destroy-on-close>
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="住户姓名" required>
              <el-input v-model="form.residentName" placeholder="请输入住户姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="缴费类型">
              <el-select v-model="form.paymentType" style="width: 100%">
                <el-option v-for="item in paymentTypeOptions" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="缴费金额" required>
              <el-input-number v-model="form.amount" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="到期日期">
              <el-date-picker v-model="form.dueDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="周期开始">
              <el-date-picker v-model="form.periodStart" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="周期结束">
              <el-date-picker v-model="form.periodEnd" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注">
          <el-input v-model="form.remarks" type="textarea" placeholder="备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="payment"
      module-name="缴费管理"
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.payment-page {
  width: 100%;
}

.stats-container {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
}

.stat-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
}

.stat-icon {
  padding: 10px;
  border-radius: 8px;
}

.total-icon {
  color: #409eff;
  background: #ecf5ff;
}

.paid-icon {
  color: #67c23a;
  background: #f0f9eb;
}

.unpaid-icon {
  color: #e6a23c;
  background: #fdf6ec;
}

.amount-icon {
  color: #f56c6c;
  background: #fef0f0;
}

:deep(.el-card__body) {
  padding: 16px;
}

.操作按钮 {
  margin-bottom: 16px;
  display: flex;
  gap: 10px;
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>