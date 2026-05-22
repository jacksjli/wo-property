<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Search, Van, Check, Close } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

// 权限验证
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

// 车位类型
type ParkingSpaceType = '固定' | '临时' | 'VIP'
type ParkingSpaceStatus = '空闲' | '已占用' | '已预约' | '维修中'

// 车位数据接口
interface ParkingSpace {
  id: number
  parkingSpaceNumber: string
  buildingId: number | null
  buildingName: string
  floor: number | null
  spaceType: string
  licensePlate: string
  residentId: number | null
  residentName: string
  startDate: string
  endDate: string
  monthlyFee: number
  status: string
  createdAt: string
}

// 数据状态
const loading = ref(false)
const tableData = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })

// 搜索表单
const searchForm = ref({
  spaceNumber: '',
  location: '',
  type: '',
  status: ''
})

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增车位')
const editingSpace = ref<ParkingSpace | null>(null)
const submitting = ref(false)

// 表单数据
const formData = ref({
  parkingSpaceNumber: '',
  buildingId: null as number | null,
  floor: null as number | null,
  spaceType: 'regular',
  licensePlate: '',
  residentId: null as number | null,
  residentName: '',
  startDate: '',
  endDate: '',
  monthlyFee: 300,
  status: 'available'
})

// 表单验证
const formRules = {
  parkingSpaceNumber: [{ required: true, message: '请输入车位编号', trigger: 'blur' }],
  monthlyFee: [{ required: true, message: '请输入月费', trigger: 'blur' }]
}

// 选项
const typeOptions = [
  { value: 'regular', label: '固定车位' },
  { value: 'temporary', label: '临时车位' },
  { value: 'vip', label: 'VIP车位' }
]

const statusOptions = [
  { value: 'available', label: '空闲' },
  { value: 'occupied', label: '已占用' },
  { value: 'reserved', label: '已预约' },
  { value: 'maintenance', label: '维修中' }
]

// 统计
const stats = computed(() => {
  const total = pagination.value.totalCount
  const occupied = tableData.value.filter(s => s.status === 'occupied').length
  const available = tableData.value.filter(s => s.status === 'available').length
  const maintenance = tableData.value.filter(s => s.status === 'maintenance').length
  return { total, occupied, available, maintenance }
})

// 过滤数据（前端过滤）
const filteredList = computed(() => {
  return tableData.value.filter(space => {
    const matchNumber = !searchForm.value.spaceNumber || space.parkingSpaceNumber?.includes(searchForm.value.spaceNumber)
    const matchStatus = !searchForm.value.status || space.status === searchForm.value.status
    const matchType = !searchForm.value.type || space.spaceType === searchForm.value.type
    return matchNumber && matchStatus && matchType
  })
})

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/parking-records', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.pageSize
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

// 重置表单
const resetForm = () => {
  formData.value = {
    parkingSpaceNumber: '',
    buildingId: null,
    floor: null,
    spaceType: 'regular',
    licensePlate: '',
    residentId: null,
    residentName: '',
    startDate: '',
    endDate: '',
    monthlyFee: 300,
    status: 'available'
  }
}

// 打开新增对话框
const handleAdd = () => {
  resetForm()
  dialogTitle.value = '新增车位'
  editingSpace.value = null
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  formData.value = {
    parkingSpaceNumber: row.parkingSpaceNumber || row.spaceNumber || '',
    buildingId: row.buildingId || null,
    floor: row.floor || null,
    spaceType: row.spaceType || 'regular',
    licensePlate: row.licensePlate || '',
    residentId: row.residentId || null,
    residentName: row.residentName || row.ownerName || '',
    startDate: row.startDate?.split('T')[0] || '',
    endDate: row.endDate?.split('T')[0] || '',
    monthlyFee: row.monthlyFee || 300,
    status: row.status || 'available'
  }
  dialogTitle.value = '编辑车位'
  editingSpace.value = row
  dialogVisible.value = true
}

// 保存
const handleSave = async () => {
  submitting.value = true
  try {
    const payload = {
      ParkingSpaceNumber: formData.value.parkingSpaceNumber,
      BuildingId: formData.value.buildingId,
      Floor: formData.value.floor,
      SpaceType: formData.value.spaceType,
      LicensePlate: formData.value.licensePlate || null,
      ResidentId: formData.value.residentId,
      StartDate: formData.value.startDate || null,
      EndDate: formData.value.endDate || null,
      MonthlyFee: formData.value.monthlyFee,
      Status: formData.value.status,
    }
    if (editingSpace.value) {
      await masterApi.put(`/parking-records/${editingSpace.value.id}`, payload)
      ElMessage.success('车位信息更新成功')
    } else {
      await masterApi.post('/parking-records', payload)
      ElMessage.success('车位新增成功')
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
    await ElMessageBox.confirm(`确定要删除车位「${row.parkingSpaceNumber}」吗？`, '删除确认', {
      confirmButtonText: '确定删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await masterApi.delete(`/parking-records/${row.id}`)
    ElMessage.success('车位已删除')
    loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

// 搜索
const handleSearch = () => {
  pagination.value.page = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.value = { spaceNumber: '', location: '', type: '', status: '' }
  handleSearch()
}

// 刷新
const handleRefresh = () => {
  loadData()
  ElMessage.success('数据已刷新')
}

// 状态颜色
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    available: 'success',
    occupied: 'primary',
    reserved: 'warning',
    maintenance: 'info'
  }
  return map[status] || 'info'
}

// 车位类型颜色
const getTypeTag = (type: string) => {
  const map: Record<string, string> = {
    regular: '',
    temporary: 'warning',
    vip: 'danger'
  }
  return map[type] || ''
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
  <div class="parking-page">
    <!-- 统计卡片 -->
    <div class="stats-container">
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon total-icon"><Van /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.total }}</span>
            <span class="stat-label">总车位数</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon occupied-icon"><Check /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.occupied }}</span>
            <span class="stat-label">已占用</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon available-icon"><Close /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.available }}</span>
            <span class="stat-label">空闲车位</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon maintenance-icon"><Refresh /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.maintenance }}</span>
            <span class="stat-label">维修中</span>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 操作栏 -->
    <el-card shadow="never" class="操作栏">
      <el-form :model="searchForm" inline>
        <el-form-item label="车位编号">
          <el-input v-model="searchForm.spaceNumber" placeholder="请输入车位编号" clearable style="width: 150px" />
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="searchForm.type" placeholder="全部" clearable style="width: 120px">
            <el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
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
        <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon>新增车位</el-button>
        <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Edit /></el-icon>配置字段</el-button>
      </div>

      <el-table :data="filteredList" stripe style="width: 100%" :max-height="600" v-loading="loading">
        <el-table-column prop="parkingSpaceNumber" label="车位编号" width="100" fixed />
        <el-table-column prop="buildingName" label="楼栋" width="100" align="center" />
        <el-table-column prop="floor" label="楼层" width="80" align="center">
          <template #default="{ row }">{{ row.floor ?? '-' }}</template>
        </el-table-column>
        <el-table-column prop="spaceType" label="类型" width="100">
          <template #default="{ row }">
            <el-tag :type="getTypeTag(row.spaceType)" size="small">
              {{ row.spaceType === 'regular' ? '固定' : row.spaceType === 'temporary' ? '临时' : row.spaceType === 'vip' ? 'VIP' : row.spaceType }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ row.status === 'available' ? '空闲' : row.status === 'occupied' ? '已占用' : row.status === 'reserved' ? '已预约' : row.status === 'maintenance' ? '维修中' : row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="licensePlate" label="车牌号" width="120" />
        <el-table-column prop="residentName" label="车主姓名" width="100" />
        <el-table-column prop="startDate" label="启用日期" width="110">
          <template #default="{ row }">{{ formatDate(row.startDate) }}</template>
        </el-table-column>
        <el-table-column prop="endDate" label="到期日期" width="110">
          <template #default="{ row }">{{ formatDate(row.endDate) }}</template>
        </el-table-column>
        <el-table-column prop="monthlyFee" label="月费(元)" width="100">
          <template #default="{ row }">¥{{ row.monthlyFee }}</template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
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
      <el-form :model="formData" :rules="formRules" label-width="100px">
        <el-form-item label="车位编号" prop="parkingSpaceNumber">
          <el-input v-model="formData.parkingSpaceNumber" placeholder="如：A001" />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="类型" prop="spaceType">
              <el-select v-model="formData.spaceType" style="width: 100%">
                <el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态" prop="status">
              <el-select v-model="formData.status" style="width: 100%">
                <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="车牌号">
              <el-input v-model="formData.licensePlate" placeholder="如：京A12345" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="车主姓名">
              <el-input v-model="formData.residentName" placeholder="请输入车主姓名" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="月费(元)" prop="monthlyFee">
              <el-input-number v-model="formData.monthlyFee" :min="0" :step="50" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用日期">
              <el-date-picker v-model="formData.startDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" placeholder="选择日期" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="到期日期">
              <el-date-picker v-model="formData.endDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" placeholder="选择日期" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSave">确定</el-button>
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

.occupied-icon {
  color: #67c23a;
  background: #f0f9eb;
}

.available-icon {
  color: #909399;
  background: #f4f4f5;
}

.maintenance-icon {
  color: #e6a23c;
  background: #fdf6ec;
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