<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { financeApi } from '../../api/http'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Setting } from '@element-plus/icons-vue'
import { getActiveFields } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 打开字段配置（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 刷新字段
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

const loading = ref(false)
const bills = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 10, total: 0 })

const dialogVisible = ref(false)
const dialogTitle = ref('新增账单')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

const form = ref({
  billNumber: '',
  title: '',
  type: 'Expense',
  category: '',
  amount: 0,
  date: '',
  status: 'Pending',
  description: ''
})

const rules: FormRules = {
  billNumber: [{ required: true, message: '请输入账单编号', trigger: 'blur' }],
  title: [{ required: true, message: '请输入账单名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择类型', trigger: 'change' }],
  amount: [{ required: true, message: '请输入金额', trigger: 'blur' }],
  date: [{ required: true, message: '请选择日期', trigger: 'change' }]
}

const loadData = async () => {
  loading.value = true
  try {
    const response = await financeApi.get('/api/bills', {
      params: { page: pagination.value.page, pageSize: pagination.value.pageSize }
    })
    if (response.success) {
      bills.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    bills.value = getMockData()
    pagination.value.total = 5
  }
  loading.value = false
}

const getMockData = () => [
  { id: 1, billNumber: 'FK-2026-001', title: '水电费支出', type: 'Expense', category: '水电费', amount: 12500, date: '2026-04-15', status: 'Paid' },
  { id: 2, billNumber: 'FK-2026-002', title: '物业费收入', type: 'Income', category: '物业费', amount: 45600, date: '2026-04-10', status: 'Received' },
  { id: 3, billNumber: 'FK-2026-003', title: '维修材料费', type: 'Expense', category: '维修费', amount: 3200, date: '2026-04-08', status: 'Pending' },
]

const getTypeTag = (type: string) => type === 'Income' ? 'success' : 'danger'
const getTypeText = (type: string) => type === 'Income' ? '收入' : '支出'
const formatMoney = (amount: number) => `¥${amount.toLocaleString()}`
const getStatusText = (status: string) => status === 'Paid' || status === 'Received' ? '已结清' : '待处理'

const openCreateDialog = () => {
  dialogTitle.value = '新增账单'
  editingId.value = null
  form.value = { billNumber: '', title: '', type: 'Expense', category: '', amount: 0, date: '', status: 'Pending', description: '' }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  dialogTitle.value = '编辑账单'
  editingId.value = row.id
  form.value = { ...row }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    submitting.value = true
    if (editingId.value) {
      await financeApi.put(`/api/bills/${editingId.value}`, form.value)
      ElMessage.success('账单更新成功')
    } else {
      await financeApi.post('/api/bills', form.value)
      ElMessage.success('账单创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) ElMessage.error(error.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除账单"${row.title}"吗？`, '删除确认', { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' })
    await financeApi.delete(`/api/bills/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="finance-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>财务管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openCreateDialog"><el-icon><Plus /></el-icon> 新增账单</el-button>
          </div>
        </div>
      </template>
      <el-table :data="bills" v-loading="loading" stripe>
        <el-table-column prop="billNumber" label="账单编号" width="130" />
        <el-table-column prop="title" label="账单名称" min-width="180" />
        <el-table-column prop="category" label="类别" width="100" />
        <el-table-column prop="type" label="类型" width="80">
          <template #default="{ row }"><el-tag :type="getTypeTag(row.type)" size="small">{{ getTypeText(row.type) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="amount" label="金额" width="120">
          <template #default="{ row }"><span :style="{ color: row.type === 'Income' ? '#67c23a' : '#f56c6c' }">{{ formatMoney(row.amount) }}</span></template>
        </el-table-column>
        <el-table-column prop="date" label="日期" width="110" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }"><el-tag size="small">{{ getStatusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="loadData" /></div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="账单编号" prop="billNumber"><el-input v-model="form.billNumber" placeholder="如：FK-2026-001" /></el-form-item>
        <el-form-item label="账单名称" prop="title"><el-input v-model="form.title" placeholder="请输入账单名称" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="类型" prop="type"><el-select v-model="form.type" style="width:100%"><el-option value="Income" label="收入" /><el-option value="Expense" label="支出" /></el-select></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="金额" prop="amount"><el-input-number v-model="form.amount" :min="0" :precision="2" style="width:100%" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="类别" prop="category"><el-input v-model="form.category" placeholder="如：水电费" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="日期" prop="date"><el-date-picker v-model="form.date" type="date" style="width:100%" /></el-form-item></el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="finance" 
      module-name="财务管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.header-actions {
  display: flex;
  gap: 10px;
}

<style scoped>
.finance-list { width: 100%; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.pagination { margin-top: 20px; display: flex; justify-content: flex-end; }
</style>
