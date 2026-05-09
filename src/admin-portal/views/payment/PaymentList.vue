<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Delete, Edit, Search, Refresh, Money, Setting } from '@element-plus/icons-vue'
import { getActiveFields, addField, updateField, deleteField, toggleFieldStatus, autoGenerateKey, fieldConfigs, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

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
  // 强制重新渲染
}

// 模拟数据（根据启用的收费项目动态生成）
const tableData = ref<any[]>([
  { id: 1, paymentNo: 'PAY202604001', roomNo: '1栋101', ownerName: '张三', phone: '13800138001', propertyFee: 350, parkingFee: 500, waterFee: 120, electricFee: 280, total: 1250, status: '已支付', payDate: '2026-04-05', remark: '' },
  { id: 2, paymentNo: 'PAY202604002', roomNo: '1栋102', ownerName: '李四', phone: '13800138002', propertyFee: 350, parkingFee: 480, waterFee: 98, electricFee: 320, total: 1248, status: '待支付', payDate: '', remark: '' },
  { id: 3, paymentNo: 'PAY202604003', roomNo: '2栋201', ownerName: '王五', phone: '13800138003', propertyFee: 350, parkingFee: 0, waterFee: 150, electricFee: 450, total: 950, status: '已支付', payDate: '2026-04-08', remark: '' },
  { id: 4, paymentNo: 'PAY202604004', roomNo: '2栋202', ownerName: '赵六', phone: '13800138004', propertyFee: 350, parkingFee: 350, waterFee: 80, electricFee: 200, total: 980, status: '待支付', payDate: '', remark: '逾期未缴' },
  { id: 5, paymentNo: 'PAY202604005', roomNo: '3栋301', ownerName: '钱七', phone: '13800138005', propertyFee: 350, parkingFee: 500, waterFee: 110, electricFee: 310, total: 1270, status: '已支付', payDate: '2026-04-10', remark: '' },
])

// 搜索表单
const searchForm = ref({
  ownerName: '',
  status: '',
  paymentNo: '',
  roomNo: ''
})

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增缴费记录')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

// 表单数据
const form = ref<any>({})

// 初始化表单
const initForm = () => {
  const data: any = {
    paymentNo: '',
    roomNo: '',
    ownerName: '',
    phone: '',
    total: 0,
    status: '待支付',
    payDate: '',
    remark: ''
  }
  getActiveFeeItems().forEach(f => {
    data[f.key] = f.defaultAmount
  })
  return data
}

form.value = initForm()

// 计算总费用
const calcTotal = () => {
  let total = 0
  getActiveFeeItems().forEach(f => {
    total += form.value[f.key] || 0
  })
  form.value.total = total
}

// 表单验证
const rules: FormRules = {
  ownerName: [{ required: true, message: '请输入业主姓名', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入联系电话', trigger: 'blur' }]
}

// 选项
const statusOptions = [
  { value: '待支付', label: '待支付' },
  { value: '已支付', label: '已支付' },
  { value: '已逾期', label: '已逾期' }
]

// 按状态筛选
const filterByStatus = (status: string) => {
  searchForm.value.status = status
  searchForm.value.paymentNo = ''
  searchForm.value.ownerName = ''
  searchForm.value.roomNo = ''
  ElMessage.success(`已筛选：${status || '全部'}`)
}

// 筛选后的数据
const filteredData = computed(() => {
  let result = [...tableData.value]
  
  if (searchForm.value.paymentNo) {
    result = result.filter(t => t.paymentNo.includes(searchForm.value.paymentNo))
  }
  if (searchForm.value.ownerName) {
    result = result.filter(t => t.ownerName.includes(searchForm.value.ownerName))
  }
  if (searchForm.value.roomNo) {
    result = result.filter(t => t.roomNo.includes(searchForm.value.roomNo))
  }
  if (searchForm.value.status) {
    result = result.filter(t => t.status === searchForm.value.status)
  }
  
  return result
})

// 统计
const stats = computed(() => {
  const total = tableData.value.length
  const paid = tableData.value.filter(t => t.status === '已支付').length
  const unpaid = tableData.value.filter(t => t.status === '待支付').length
  const overdue = tableData.value.filter(t => t.status === '已逾期').length
  const totalAmount = tableData.value.reduce((sum, t) => sum + (t.status === '已支付' ? t.total : 0), 0)
  const pendingAmount = tableData.value.reduce((sum, t) => sum + (t.status !== '已支付' ? t.total : 0), 0)
  return { total, paid, unpaid, overdue, totalAmount, pendingAmount }
})

// 状态颜色
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    '已支付': 'success',
    '待支付': 'warning',
    '已逾期': 'danger'
  }
  return map[status] || 'info'
}

// 搜索
const handleSearch = () => {
  ElMessage.success('搜索功能已触发')
}

// 重置
const handleReset = () => {
  searchForm.value = { ownerName: '', status: '', paymentNo: '' }
  ElMessage.info('已重置搜索条件')
}

// 新增
const handleAdd = () => {
  dialogTitle.value = '新增缴费记录'
  editingId.value = null
  form.value = initForm()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑缴费记录'
  editingId.value = row.id
  form.value = { ...row }
  dialogVisible.value = true
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    calcTotal()
    submitting.value = true
    
    if (editingId.value) {
      const index = tableData.value.findIndex(t => t.id === editingId.value)
      if (index !== -1) {
        tableData.value[index] = { ...form.value, id: editingId.value }
      }
      ElMessage.success('缴费记录更新成功')
    } else {
      const newId = Math.max(...tableData.value.map(t => t.id), 0) + 1
      const paymentNo = `PAY${new Date().getFullYear()}${String(new Date().getMonth() + 1).padStart(2, '0')}${String(newId).padStart(3, '0')}`
      tableData.value.unshift({ ...form.value, id: newId, paymentNo })
      ElMessage.success('缴费记录新增成功')
    }
    
    dialogVisible.value = false
  } catch (error) {
    // 验证失败
  } finally {
    submitting.value = false
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除缴费记录「${row.paymentNo}」吗？`,
      '删除确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' }
    )
    
    const index = tableData.value.findIndex(t => t.id === row.id)
    if (index !== -1) {
      tableData.value.splice(index, 1)
    }
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 确认缴费
const handleConfirmPay = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认业主「${row.ownerName}」已完成缴费 ¥${row.total}？`,
      '确认缴费',
      { confirmButtonText: '确认', cancelButtonText: '取消', type: 'success' }
    )
    
    row.status = '已支付'
    row.payDate = new Date().toLocaleDateString('zh-CN')
    ElMessage.success('缴费已确认')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('操作失败')
    }
  }
}
</script>

<template>
  <div class="payment-page">
    <!-- 统计卡片（点击筛选） -->
    <el-row :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card clickable" @click="filterByStatus('')">
          <div class="stat-content">
            <el-icon class="stat-icon"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.total }}</span>
              <span class="stat-label">全部记录</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card clickable" @click="filterByStatus('已支付')">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #67c23a;"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.paid }}</span>
              <span class="stat-label clickable-label">已支付</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card clickable" @click="filterByStatus('待支付')">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #e6a23c;"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.unpaid }}</span>
              <span class="stat-label clickable-label">待支付</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card clickable" @click="filterByStatus('已逾期')">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #f56c6c;"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.overdue }}</span>
              <span class="stat-label clickable-label">已逾期</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 金额统计 -->
    <el-row :gutter="20" class="stats-row">
      <el-col :span="12">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #67c23a;"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">¥{{ stats.totalAmount }}</span>
              <span class="stat-label">已收金额</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #e6a23c;"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">¥{{ stats.pendingAmount }}</span>
              <span class="stat-label">待收金额</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏 -->
    <el-card shadow="never" class="search-card">
      <el-form :model="searchForm" inline>
        <el-form-item label="业主姓名">
          <el-input v-model="searchForm.ownerName" placeholder="请输入业主姓名" clearable style="width: 150px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择" clearable style="width: 120px">
            <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="缴费单号">
          <el-input v-model="searchForm.paymentNo" placeholder="请输入单号" clearable style="width: 150px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>搜索
          </el-button>
          <el-button @click="handleReset">
            <el-icon><Refresh /></el-icon>重置
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 操作栏 -->
    <div class="toolbar">
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>新增缴费
      </el-button>
      <el-button type="default" @click="openFieldConfig">
        <el-icon><Setting /></el-icon>配置字段
      </el-button>
    </div>

    <!-- 数据表格 -->
    <el-card shadow="never" class="table-card">
      <el-table 
        :data="filteredData" 
        stripe 
        style="width: 100%"
        :scroll-x="true"
        :scrollbar-always-on="true"
      >
        <el-table-column prop="paymentNo" label="单号" width="120" />
        <el-table-column prop="roomNo" label="房间" width="100" />
        <el-table-column prop="ownerName" label="业主" width="80" />
        <el-table-column prop="phone" label="电话" width="120" />
        <el-table-column 
          v-for="item in getActiveFeeItems()" 
          :key="'col-' + refreshKey + '-' + item.key" 
          :prop="item.key" 
          :label="item.name" 
          width="110" 
          align="right"
        >
          <template #default="{ row }">
            <span :style="{ color: item.required ? '#67c23a' : '#606266' }">¥{{ row[item.key] || 0 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="total" label="合计" width="120" align="right">
          <template #default="{ row }">
            <span style="color: #409eff; font-weight: bold;">¥{{ row.total }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="payDate" label="日期" width="100" />
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <div class="action-buttons">
              <el-button v-if="row.status !== '已支付'" link type="success" size="small" @click="handleConfirmPay(row)">确认</el-button>
              <el-button link type="primary" size="small" @click="handleEdit(row)"><el-icon><Edit /></el-icon></el-button>
              <el-button link type="danger" size="small" @click="handleDelete(row)"><el-icon><Delete /></el-icon></el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="650px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="房间号" prop="roomNo">
              <el-input v-model="form.roomNo" placeholder="如：1栋101" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="业主姓名" prop="ownerName">
              <el-input v-model="form.ownerName" placeholder="请输入业主姓名" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="联系电话" prop="phone">
              <el-input v-model="form.phone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-divider content-position="left">费用明细</el-divider>
        
        <el-row :gutter="20">
          <el-col :span="12" v-for="item in getActiveFeeItems()" :key="'form-' + refreshKey + '-' + item.key">
            <el-form-item :label="item.name">
              <el-input-number v-model="form[item.key]" :min="0" :precision="0" style="width: 100%" @change="calcTotal" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="合计">
              <el-input v-model="form.total" disabled style="color: #409eff; font-weight: bold;">
                <template #append>元</template>
              </el-input>
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
          <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="请输入备注信息" />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
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

.stat-card {
  cursor: pointer;
  transition: all 0.3s;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.stat-card.clickable {
  border: 2px solid transparent;
}

.stat-card.clickable:hover {
  border-color: #409eff;
}

.stat-label.clickable-label {
  text-decoration: underline;
  color: #409eff;
}

.stats-row {
  margin-bottom: 20px;
}

.stat-card :deep(.el-card__body) {
  padding: 20px;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  font-size: 40px;
  color: #409eff;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
}

.search-card {
  margin-bottom: 20px;
}

.toolbar {
  margin-bottom: 16px;
}

.table-card {
  margin-bottom: 20px;
}

.table-card :deep(.el-table__body-wrapper) {
  overflow-x: auto;
}

.table-card :deep(.el-table__body-wrapper::-webkit-scrollbar) {
  height: 20px;
}

.table-card :deep(.el-table__body-wrapper::-webkit-scrollbar-track) {
  background: #e8e8e8;
  border-radius: 10px;
}

.table-card :deep(.el-table__body-wrapper::-webkit-scrollbar-thumb) {
  background: #a0a0a0;
  border-radius: 10px;
  border: 3px solid #e8e8e8;
}

.table-card :deep(.el-table__body-wrapper::-webkit-scrollbar-thumb:hover) {
  background: #707070;
}

.action-buttons {
  display: flex;
  gap: 4px;
  align-items: center;
}

.action-buttons .el-button {
  padding: 4px 8px;
}

/* 收费项目配置样式 */
.fee-list {
  margin-bottom: 20px;
}

.fee-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: #f5f7fa;
  border-radius: 8px;
  margin-bottom: 10px;
}

.fee-item-info {
  flex: 1;
}

.fee-item-name {
  font-weight: 600;
  color: #303133;
  margin-bottom: 4px;
}

.fee-item-meta {
  font-size: 12px;
  color: #909399;
}

.fee-item-amount {
  font-weight: 600;
  color: #409eff;
  margin-right: 20px;
}

.fee-item-status {
  margin-right: 15px;
}
</style>
