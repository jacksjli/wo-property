<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Refresh, Search, Clock, Check, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

type TakeoutStatus = '待取餐' | '配送中' | '已送达' | '已取消' | '异常'
type TakeoutType = '中餐' | '西餐' | '快餐' | '甜品' | '饮品' | '其他'

interface TakeoutOrder {
  id: number; orderNo: string; restaurantName: string; foodType: TakeoutType;
  residentName: string; roomNo: string; phone: string; deliveryPerson: string;
  deliveryPhone: string; deliveryTime: string; status: TakeoutStatus;
  totalAmount: number; remark?: string; createTime: string; updateTime: string;
}

const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const refreshKey = ref(0)
const refreshFields = () => { refreshKey.value++ }
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) { fieldDialogRef.value?.open() }
}

const orders = ref<TakeoutOrder[]>([])
const loading = ref(false)
const stats = computed(() => ({
  total: orders.value.length,
  pending: orders.value.filter(o => o.status === '待取餐').length,
  delivering: orders.value.filter(o => o.status === '配送中').length,
  delivered: orders.value.filter(o => o.status === '已送达').length
}))

const searchForm = ref({ keyword: '', status: '', foodType: '' })
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const dialogTitle = ref('新增订单')
const editingOrder = ref<TakeoutOrder | null>(null)
const viewingOrder = ref<TakeoutOrder | null>(null)
const submitting = ref(false)
const formRef = ref<FormInstance>()

const formData = ref<TakeoutOrder>({
  id: 0, orderNo: '', restaurantName: '', foodType: '快餐', residentName: '',
  roomNo: '', phone: '', deliveryPerson: '', deliveryPhone: '', deliveryTime: '',
  status: '待取餐', totalAmount: 0, remark: '', createTime: '', updateTime: ''
})

const rules: FormRules = {
  restaurantName: [{ required: true, message: '请输入餐厅名称', trigger: 'blur' }],
  foodType: [{ required: true, message: '请选择餐食类型', trigger: 'change' }],
  residentName: [{ required: true, message: '请输入住户姓名', trigger: 'blur' }],
  roomNo: [{ required: true, message: '请输入房号', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入联系电话', trigger: 'blur' }],
  status: [{ required: true, message: '请选择订单状态', trigger: 'change' }],
  totalAmount: [{ required: true, message: '请输入总金额', trigger: 'blur' }]
}

const typeOptions = [
  { value: '中餐', label: '中餐' }, { value: '西餐', label: '西餐' }, { value: '快餐', label: '快餐' },
  { value: '甜品', label: '甜品' }, { value: '饮品', label: '饮品' }, { value: '其他', label: '其他' }
]
const statusOptions = [
  { value: '待取餐', label: '待取餐' }, { value: '配送中', label: '配送中' },
  { value: '已送达', label: '已送达' }, { value: '已取消', label: '已取消' }, { value: '异常', label: '异常' }
]

const filteredOrders = computed(() => {
  let result = orders.value
  if (searchForm.value.keyword) {
    const kw = searchForm.value.keyword.toLowerCase()
    result = result.filter(o =>
      o.orderNo.toLowerCase().includes(kw) || o.residentName.toLowerCase().includes(kw) ||
      o.restaurantName.toLowerCase().includes(kw)
    )
  }
  if (searchForm.value.status) result = result.filter(o => o.status === searchForm.value.status)
  if (searchForm.value.foodType) result = result.filter(o => o.foodType === searchForm.value.foodType)
  return result
})

const getStatusType = (status: TakeoutStatus) => {
  const map: Record<string, string> = { '待取餐': 'warning', '配送中': 'primary', '已送达': 'success', '已取消': 'info', '异常': 'danger' }
  return map[status] || 'info'
}
const getTypeTag = (type: TakeoutType) => {
  const map: Record<string, string> = { '中餐': '', '西餐': 'success', '快餐': 'warning', '甜品': 'danger', '饮品': 'info', '其他': '' }
  return map[type] || ''
}

const getTakeoutFields = () => getActiveFields('takeout')

const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 100 }
    if (searchForm.value.status) params.status = searchForm.value.status
    if (searchForm.value.foodType) params.foodType = searchForm.value.foodType
    if (searchForm.value.keyword) params.keyword = searchForm.value.keyword
    const res: any = await masterApi.get('/takeout-orders', { params })
    if (res.success) orders.value = res.data || []
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const handleRefresh = () => { loadData(); ElMessage.success('已刷新') }
const handleSearch = () => { loadData() }
const handleFilterChange = () => { loadData() }
const handleStatClick = (status: string) => { searchForm.value.status = status; loadData() }

const openCreateDialog = () => {
  dialogTitle.value = '新增订单'
  editingOrder.value = null
  formData.value = {
    id: 0, orderNo: '', restaurantName: '', foodType: '快餐', residentName: '',
    roomNo: '', phone: '', deliveryPerson: '', deliveryPhone: '', deliveryTime: '',
    status: '待取餐', totalAmount: 0, remark: '', createTime: '', updateTime: ''
  }
  dialogVisible.value = true
}

const handleView = (row: TakeoutOrder) => { viewingOrder.value = row; detailDialogVisible.value = true }

const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    submitting.value = true
    const payload = { ...formData.value }
    if (editingOrder.value) {
      await masterApi.put(`/takeout-orders/${editingOrder.value.id}`, payload)
      ElMessage.success('订单更新成功')
    } else {
      await masterApi.post('/takeout-orders', payload)
      ElMessage.success('订单创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { if (e !== false) ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleUpdateStatus = async (row: TakeoutOrder, status: TakeoutStatus) => {
  try {
    await ElMessageBox.confirm(`确定要将订单"${row.orderNo}"状态更新为"${status}"吗？`, '状态更新',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'info' })
    await masterApi.put(`/takeout-orders/${row.id}`, { status })
    ElMessage.success('状态更新成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '操作失败') }
}

const handleDelete = async (row: TakeoutOrder) => {
  try {
    await ElMessageBox.confirm(`确定要删除订单"${row.orderNo}"吗？此操作不可恢复！`, '删除确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'error' })
    await masterApi.delete(`/takeout-orders/${row.id}`)
    ElMessage.success('订单已删除')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="takeout-list">
    <div class="page-header">
      <div class="header-left">
        <h1>外卖管理</h1>
        <p>管理所有外卖配送订单</p>
      </div>
      <div class="header-right">
        <el-button type="default" @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
        <el-button type="primary" @click="openCreateDialog"><el-icon><Plus /></el-icon>新增订单</el-button>
        <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon>刷新</el-button>
      </div>
    </div>

    <div class="stats-grid">
      <el-card shadow="hover" class="stat-card" @click="handleStatClick('')">
        <div class="stat-content">
          <div class="stat-icon total-icon"><el-icon><Clock /></el-icon></div>
          <div class="stat-info"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">订单总数</div></div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card" @click="handleStatClick('待取餐')">
        <div class="stat-content">
          <div class="stat-icon pending-icon"><el-icon><Clock /></el-icon></div>
          <div class="stat-info"><div class="stat-value">{{ stats.pending }}</div><div class="stat-label">待取餐</div></div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card" @click="handleStatClick('配送中')">
        <div class="stat-content">
          <div class="stat-icon delivering-icon"><el-icon><Refresh /></el-icon></div>
          <div class="stat-info"><div class="stat-value">{{ stats.delivering }}</div><div class="stat-label">配送中</div></div>
        </div>
      </el-card>
      <el-card shadow="hover" class="stat-card" @click="handleStatClick('已送达')">
        <div class="stat-content">
          <div class="stat-icon delivered-icon"><el-icon><Check /></el-icon></div>
          <div class="stat-info"><div class="stat-value">{{ stats.delivered }}</div><div class="stat-label">已送达</div></div>
        </div>
      </el-card>
    </div>

    <el-card shadow="never" class="filter-card">
      <el-form :inline="true" class="filter-form">
        <el-form-item label="订单状态">
          <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px" @change="handleFilterChange">
            <el-option label="全部" value="" /><el-option label="待取餐" value="待取餐" /><el-option label="配送中" value="配送中" />
            <el-option label="已送达" value="已送达" /><el-option label="已取消" value="已取消" /><el-option label="异常" value="异常" />
          </el-select>
        </el-form-item>
        <el-form-item label="餐食类型">
          <el-select v-model="searchForm.foodType" placeholder="全部" clearable style="width: 100px" @change="handleFilterChange">
            <el-option label="全部" value="" /><el-option label="中餐" value="中餐" /><el-option label="西餐" value="西餐" />
            <el-option label="快餐" value="快餐" /><el-option label="甜品" value="甜品" /><el-option label="饮品" value="饮品" /><el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="关键词">
          <el-input v-model="searchForm.keyword" placeholder="订单号/住户/餐厅" clearable style="width: 180px" @keyup.enter="handleSearch" />
        </el-form-item>
        <el-form-item><el-button type="primary" @click="handleSearch"><el-icon><Search /></el-icon>搜索</el-button></el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="filteredOrders" stripe v-loading="loading" @row-click="handleView">
        <el-table-column v-for="field in getTakeoutFields()" :key="'col-' + refreshKey + '-' + field.id"
          :prop="field.key" :label="field.name" :width="field.width" :align="field.align || 'center'">
          <template #default="{ row }">
            <span v-if="field.key === 'orderNo'" class="order-number">{{ row.orderNo }}</span>
            <span v-else-if="field.key === 'foodType'"><el-tag :type="getTypeTag(row.foodType)" size="small">{{ row.foodType }}</el-tag></span>
            <span v-else-if="field.key === 'status'"><el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag></span>
            <span v-else-if="field.key === 'totalAmount'">¥{{ row.totalAmount.toFixed(2) }}</span>
            <span v-else-if="field.key === 'createTime'">{{ row.createTime }}</span>
            <span v-else-if="field.key === 'updateTime'">{{ row.updateTime }}</span>
            <span v-else>{{ row[field.key] || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleView(row)">详情</el-button>
            <el-button link type="warning" size="small" v-if="row.status === '待取餐'" @click.stop="handleUpdateStatus(row, '配送中')">开始配送</el-button>
            <el-button link type="success" size="small" v-if="row.status === '配送中'" @click.stop="handleUpdateStatus(row, '已送达')">确认送达</el-button>
            <el-button link type="danger" size="small" v-if="row.status !== '已送达' && row.status !== '已取消'" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 创建/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px">
        <el-form-item label="餐厅名称" prop="restaurantName"><el-input v-model="formData.restaurantName" placeholder="请输入餐厅名称" /></el-form-item>
        <el-form-item label="餐食类型" prop="foodType">
          <el-select v-model="formData.foodType" placeholder="请选择餐食类型" style="width: 100%">
            <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="住户姓名" prop="residentName"><el-input v-model="formData.residentName" placeholder="请输入住户姓名" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="房号" prop="roomNo"><el-input v-model="formData.roomNo" placeholder="请输入房号" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="联系电话" prop="phone"><el-input v-model="formData.phone" placeholder="请输入联系电话" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="配送员"><el-input v-model="formData.deliveryPerson" placeholder="请输入配送员姓名" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="配送员电话"><el-input v-model="formData.deliveryPhone" placeholder="请输入配送员电话" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="送达时间"><el-date-picker v-model="formData.deliveryTime" type="datetime" placeholder="选择送达时间" style="width: 100%" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="订单状态" prop="status">
            <el-select v-model="formData.status" placeholder="请选择订单状态" style="width: 100%">
              <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
            </el-select>
          </el-form-item></el-col>
        </el-row>
        <el-form-item label="总金额" prop="totalAmount"><el-input-number v-model="formData.totalAmount" :min="0" :precision="2" style="width: 100%" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="订单详情" width="700px">
      <template v-if="viewingOrder">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="订单编号">{{ viewingOrder.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="状态"><el-tag :type="getStatusType(viewingOrder.status)" size="small">{{ viewingOrder.status }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="餐厅名称">{{ viewingOrder.restaurantName }}</el-descriptions-item>
          <el-descriptions-item label="餐食类型"><el-tag :type="getTypeTag(viewingOrder.foodType)" size="small">{{ viewingOrder.foodType }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="住户姓名">{{ viewingOrder.residentName }}</el-descriptions-item>
          <el-descriptions-item label="房号">{{ viewingOrder.roomNo }}</el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ viewingOrder.phone }}</el-descriptions-item>
          <el-descriptions-item label="总金额">¥{{ viewingOrder.totalAmount.toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="配送员">{{ viewingOrder.deliveryPerson || '-' }}</el-descriptions-item>
          <el-descriptions-item label="配送员电话">{{ viewingOrder.deliveryPhone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="送达时间">{{ viewingOrder.deliveryTime || '-' }}</el-descriptions-item>
          <el-descriptions-item label="下单时间">{{ viewingOrder.createTime }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ viewingOrder.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </template>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <el-button type="primary" v-if="viewingOrder?.status === '待取餐'" @click="handleUpdateStatus(viewingOrder!, '配送中'); detailDialogVisible = false">开始配送</el-button>
        <el-button type="success" v-if="viewingOrder?.status === '配送中'" @click="handleUpdateStatus(viewingOrder!, '已送达'); detailDialogVisible = false">确认送达</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="takeout" module-name="外卖管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.takeout-list { width: 100%; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
.header-left h1 { font-size: 24px; font-weight: 600; color: #1f2937; margin: 0 0 4px 0; }
.header-left p { font-size: 14px; color: #6b7280; margin: 0; }
.header-right { display: flex; gap: 12px; }
.stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 16px; }
.stat-card { cursor: pointer; transition: transform 0.2s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { display: flex; align-items: center; gap: 16px; }
.stat-icon { width: 48px; height: 48px; border-radius: 12px; display: flex; align-items: center; justify-content: center; font-size: 24px; }
.total-icon { background: rgba(59, 130, 246, 0.1); color: #3b82f6; }
.pending-icon { background: rgba(245, 158, 11, 0.1); color: #f59e0b; }
.delivering-icon { background: rgba(59, 130, 246, 0.1); color: #3b82f6; }
.delivered-icon { background: rgba(16, 185, 129, 0.1); color: #10b981; }
.stat-value { font-size: 28px; font-weight: 700; color: #1f2937; }
.stat-label { font-size: 14px; color: #6b7280; }
.filter-card { margin-bottom: 16px; }
.filter-form { margin-bottom: 0; }
.table-card { border-radius: 8px; }
.order-number { font-family: Monaco, Menlo, monospace; font-weight: 500; color: #374151; }
</style>