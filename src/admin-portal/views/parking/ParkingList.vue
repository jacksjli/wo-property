<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Delete, Edit, Search, Refresh, Van, Setting } from '@element-plus/icons-vue'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getParkingFields = () => getActiveFields('parking')

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

// 模拟数据
const tableData = ref<any[]>([
  { id: 1, spaceNumber: 'A001', floor: '地下一层', type: '固定', status: '已租用', plateNumber: '京A12345', ownerName: '张三', phone: '13800138001', monthlyFee: 500, startDate: '2026-01-01', endDate: '2026-12-31' },
  { id: 2, spaceNumber: 'A002', floor: '地下一层', type: '固定', status: '空闲', plateNumber: '', ownerName: '', phone: '', monthlyFee: 500, startDate: '', endDate: '' },
  { id: 3, spaceNumber: 'A003', floor: '地下一层', type: '固定', status: '已租用', plateNumber: '京B67890', ownerName: '李四', phone: '13800138002', monthlyFee: 480, startDate: '2026-02-15', endDate: '2027-02-14' },
  { id: 4, spaceNumber: 'B001', floor: '地下二层', type: '临时', status: '已占用', plateNumber: '京C11111', ownerName: '王五', phone: '13800138003', monthlyFee: 0, startDate: '', endDate: '' },
  { id: 5, spaceNumber: 'B002', floor: '地下二层', type: '临时', status: '空闲', plateNumber: '', ownerName: '', phone: '', monthlyFee: 0, startDate: '', endDate: '' },
  { id: 6, spaceNumber: 'C001', floor: '地面', type: '固定', status: '已租用', plateNumber: '京D22222', ownerName: '赵六', phone: '13800138004', monthlyFee: 350, startDate: '2026-03-01', endDate: '2027-02-28' },
  { id: 7, spaceNumber: 'C002', floor: '地面', type: '临时', status: '空闲', plateNumber: '', ownerName: '', phone: '', monthlyFee: 0, startDate: '', endDate: '' },
])

// 搜索表单
const searchForm = ref({
  spaceNumber: '',
  status: '',
  type: ''
})

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增车位')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

// 表单数据
const form = ref({
  spaceNumber: '',
  floor: '',
  type: '固定',
  status: '空闲',
  plateNumber: '',
  ownerName: '',
  phone: '',
  monthlyFee: 0,
  startDate: '',
  endDate: ''
})

// 表单验证规则
const rules: FormRules = {
  spaceNumber: [{ required: true, message: '请输入车位编号', trigger: 'blur' }],
  floor: [{ required: true, message: '请选择楼层', trigger: 'change' }],
  type: [{ required: true, message: '请选择类型', trigger: 'change' }]
}

// 选项数据
const statusOptions = [
  { value: '空闲', label: '空闲' },
  { value: '已租用', label: '已租用' },
  { value: '已占用', label: '已占用' },
  { value: '已预定', label: '已预定' }
]

const typeOptions = [
  { value: '固定', label: '固定车位' },
  { value: '临时', label: '临时车位' }
]

const floorOptions = [
  { value: '地面', label: '地面' },
  { value: '地下一层', label: '地下一层' },
  { value: '地下二层', label: '地下二层' },
  { value: '地下三层', label: '地下三层' }
]

// 计算统计数据
const stats = computed(() => {
  const total = tableData.value.length
  const rented = tableData.value.filter(t => t.status === '已租用').length
  const occupied = tableData.value.filter(t => t.status === '已占用').length
  const idle = tableData.value.filter(t => t.status === '空闲').length
  const totalIncome = tableData.value.reduce((sum, t) => sum + (t.monthlyFee || 0), 0)
  return { total, rented, occupied, idle, totalIncome }
})

// 获取状态标签类型
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    '空闲': 'info',
    '已租用': 'success',
    '已占用': 'warning',
    '已预定': 'primary'
  }
  return map[status] || 'info'
}

// 获取类型标签类型
const getTypeType = (type: string) => type === '固定' ? 'success' : 'warning'

// 搜索
const handleSearch = () => {
  ElMessage.success('搜索功能已触发')
}

// 重置搜索
const handleReset = () => {
  searchForm.value = { spaceNumber: '', status: '', type: '' }
  ElMessage.info('已重置搜索条件')
}

// 新增
const handleAdd = () => {
  dialogTitle.value = '新增车位'
  editingId.value = null
  form.value = {
    spaceNumber: '',
    floor: '地下一层',
    type: '固定',
    status: '空闲',
    plateNumber: '',
    ownerName: '',
    phone: '',
    monthlyFee: 0,
    startDate: '',
    endDate: ''
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑车位'
  editingId.value = row.id
  form.value = { ...row }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    submitting.value = true
    
    if (editingId.value) {
      // 更新
      const index = tableData.value.findIndex(t => t.id === editingId.value)
      if (index !== -1) {
        tableData.value[index] = { ...form.value, id: editingId.value }
      }
      ElMessage.success('车位信息更新成功')
    } else {
      // 新增
      const newId = Math.max(...tableData.value.map(t => t.id), 0) + 1
      tableData.value.unshift({ ...form.value, id: newId })
      ElMessage.success('车位新增成功')
    }
    
    dialogVisible.value = false
  } catch (error) {
    // 表单验证失败
  } finally {
    submitting.value = false
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除车位「${row.spaceNumber}」吗？此操作不可恢复！`,
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
</script>

<template>
  <div class="parking-page">
    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon"><Van /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.total }}</span>
              <span class="stat-label">总车位数</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #67c23a;"><Van /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.rented }}</span>
              <span class="stat-label">已租用</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #e6a23c;"><Van /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.occupied }}</span>
              <span class="stat-label">已占用</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #909399;"><Van /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.idle }}</span>
              <span class="stat-label">空闲车位</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏 -->
    <el-card shadow="never" class="search-card">
      <el-form :model="searchForm" inline>
        <el-form-item label="车位编号">
          <el-input v-model="searchForm.spaceNumber" placeholder="请输入车位编号" clearable style="width: 150px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择" clearable style="width: 120px">
            <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="searchForm.type" placeholder="请选择" clearable style="width: 120px">
            <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
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
      <el-button type="default" @click="openFieldConfig">
        <el-icon><Setting /></el-icon>配置字段
      </el-button>
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>新增车位
      </el-button>
    </div>

    <!-- 数据表格 -->
    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" stripe style="width: 100%" v-loading="false">
        <el-table-column 
          v-for="field in getParkingFields()" 
          :key="'col-' + refreshKey + '-' + field.id"
          :prop="field.key" 
          :label="field.name" 
          :width="field.width"
          :align="field.align || 'center'"
        >
          <template #default="{ row }">
            <span v-if="field.key === 'type'">
              <el-tag :type="getTypeType(row.type)" size="small">{{ row.type }}</el-tag>
            </span>
            <span v-else-if="field.key === 'status'">
              <el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag>
            </span>
            <span v-else-if="field.key === 'monthlyFee'">
              <span v-if="row.monthlyFee > 0" style="color: #67c23a;">¥{{ row.monthlyFee }}</span>
              <span v-else style="color: #909399;">-</span>
            </span>
            <span v-else>{{ row[field.key] || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">
              <el-icon><Delete /></el-icon>删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="650px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="车位编号" prop="spaceNumber">
              <el-input v-model="form.spaceNumber" placeholder="如：A001" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="楼层" prop="floor">
              <el-select v-model="form.floor" placeholder="请选择" style="width: 100%">
                <el-option v-for="opt in floorOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="类型" prop="type">
              <el-select v-model="form.type" placeholder="请选择" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态" prop="status">
              <el-select v-model="form.status" placeholder="请选择" style="width: 100%">
                <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-divider content-position="left">车主信息</el-divider>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="车牌号">
              <el-input v-model="form.plateNumber" placeholder="如：京A12345" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="车主姓名">
              <el-input v-model="form.ownerName" placeholder="请输入车主姓名" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="联系电话">
              <el-input v-model="form.phone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="月租费">
              <el-input-number v-model="form.monthlyFee" :min="0" :precision="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="租期开始">
              <el-date-picker v-model="form.startDate" type="date" placeholder="选择日期" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="租期结束">
              <el-date-picker v-model="form.endDate" type="date" placeholder="选择日期" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </el-form-item>
          </el-col>
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
      module="parking" 
      module-name="车位管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.parking-page {
  width: 100%;
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
</style>
