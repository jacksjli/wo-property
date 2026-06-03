<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, Tools, Document } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import { getDevices, createDevice, updateDevice, deleteDevice, getDeviceStatistics, submitDeviceRepair } from '@/api/device'
import { useFieldConfig } from '@/composables/useFieldConfig'

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getDeviceFields = () => getActiveFields('device')

// 字段配置（alias 优先的 label + isEditable 控制）
const { fetchFieldConfig } = useFieldConfig()
const deviceLabels = ref<Record<string, any>>({})

// 获取某字段是否可编辑
const isFieldEditable = (fieldKey: string): boolean => {
  return deviceLabels.value[fieldKey]?.isEditable ?? true
}

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

// 设备类型/状态标签
const deviceTypeLabels: Record<string, string> = {
  fire_protection: '消防设备',
  surveillance: '监控设备',
  access_control: '门禁设备',
  elevator: '电梯设备',
  parking: '停车设备',
  other: '其他设备'
}

const deviceStatusLabels: Record<string, string> = {
  normal: '正常',
  maintenance: '维修中',
  fault: '故障',
  disabled: '停用'
}

const inspectionCycleLabels: Record<string, string> = {
  daily: '每日',
  weekly: '每周',
  monthly: '每月',
  quarterly: '每季度',
  yearly: '每年'
}

// 设备列表数据
const deviceList = ref<any[]>([])
const stats = computed(() => ({
  total: deviceList.value.length,
  normal: deviceList.value.filter((d: any) => d.status === 'normal').length,
  maintenance: deviceList.value.filter((d: any) => d.status === 'maintenance').length,
  fault: deviceList.value.filter((d: any) => d.status === 'fault').length,
  disabled: deviceList.value.filter((d: any) => d.status === 'disabled').length,
  needInspection: 0
}))
const loading = ref(false)

// 统计卡片点击
const filterType = ref<string>('')
const handleStatClick = (status: string) => {
  filterType.value = filterType.value === status ? '' : status
}

// 筛选后的设备列表
const filteredDevices = computed(() => {
  if (!filterType.value) return deviceList.value
  return deviceList.value.filter((d: any) => d.status === filterType.value)
})

// 对话框状态
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const maintenanceDialogVisible = ref(false)
const inspectionDialogVisible = ref(false)
const dialogTitle = ref('新增设备')
const editingId = ref<number | null>(null)
const viewingDevice = ref<any | null>(null)

// 表单数据
const form = ref({
  deviceCode: '',
  deviceName: '',
  deviceType: 'surveillance',
  model: '',
  manufacturer: '',
  location: '',
  installDate: '',
  status: 'normal',
  inspectionCycle: 'monthly',
  buildingId: null as number | null,
  remark: ''
})

// 维修表单
const maintenanceForm = ref({
  date: '',
  type: 'repair' as 'repair' | 'replace' | 'check',
  description: '',
  handler: '',
  cost: 0,
  remark: ''
})

// 巡检表单
const inspectionForm = ref({
  date: '',
  inspector: '',
  result: 'normal' as 'normal' | 'abnormal',
  issue: '',
  remark: ''
})

// 设备类型选项
const typeOptions = Object.entries(deviceTypeLabels).map(([value, label]) => ({ value, label }))

// 设备状态选项
const statusOptions = Object.entries(deviceStatusLabels).map(([value, label]) => ({ value, label }))

// 巡检周期选项
const cycleOptions = Object.entries(inspectionCycleLabels).map(([value, label]) => ({ value, label }))

// 获取状态颜色
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    normal: 'success',
    maintenance: 'warning',
    fault: 'danger',
    disabled: 'info'
  }
  return map[status] || 'info'
}

// 加载设备数据
const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getDevices({ page: 1, pageSize: 200 })
    deviceList.value = res.data || res || []
    // 同时加载统计数据
    try {
      const statsRes: any = await getDeviceStatistics()
      if (statsRes && typeof statsRes === 'object') {
        stats.value = {
          total: statsRes.total || deviceList.value.length,
          normal: statsRes.normal || 0,
          maintenance: statsRes.maintenance || 0,
          fault: statsRes.fault || 0,
          disabled: statsRes.disabled || 0,
          needInspection: statsRes.needInspection || 0
        }
      }
    } catch (e) {
      // stats 失败不影响主数据加载
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载设备数据失败')
  } finally {
    loading.value = false
  }
}

// 打开新增对话框
const handleAdd = () => {
  dialogTitle.value = '新增设备'
  editingId.value = null
  form.value = {
    deviceCode: '',
    deviceName: '',
    deviceType: 'surveillance',
    model: '',
    manufacturer: '',
    location: '',
    installDate: '',
    status: 'normal',
    inspectionCycle: 'monthly',
    buildingId: null,
    remark: ''
  }
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑设备'
  editingId.value = row.id
  form.value = {
    deviceCode: row.deviceCode || row.deviceNo || '',
    deviceName: row.deviceName || row.name || '',
    deviceType: row.deviceType || row.type || 'surveillance',
    model: row.model || '',
    manufacturer: row.manufacturer || '',
    location: row.location || '',
    installDate: row.installDate ? row.installDate.split('T')[0] : '',
    status: row.status || 'normal',
    inspectionCycle: row.inspectionCycle || 'monthly',
    buildingId: row.buildingId || null,
    remark: row.remark || ''
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!form.value.deviceCode.trim()) {
    ElMessage.warning('请输入设备编号')
    return
  }
  if (!form.value.deviceName.trim()) {
    ElMessage.warning('请输入设备名称')
    return
  }
  if (!form.value.location.trim()) {
    ElMessage.warning('请输入安装位置')
    return
  }

  try {
    if (editingId.value) {
      await updateDevice(editingId.value, form.value)
      ElMessage.success('更新成功')
    } else {
      await createDevice(form.value)
      ElMessage.success('添加成功')
    }
    dialogVisible.value = false
    await loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

// 删除设备
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除设备 "${row.deviceName || row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await deleteDevice(row.id)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

// 查看设备详情
const handleView = (row: any) => {
  viewingDevice.value = row
  detailDialogVisible.value = true
}

// 打开维修记录对话框
const openMaintenanceDialog = (row: any) => {
  viewingDevice.value = row
  maintenanceForm.value = {
    date: new Date().toISOString().split('T')[0],
    type: 'repair',
    description: '',
    handler: '',
    cost: 0,
    remark: ''
  }
  maintenanceDialogVisible.value = true
}

// 提交维修记录
const handleSubmitMaintenance = async () => {
  if (!maintenanceForm.value.description.trim()) {
    ElMessage.warning('请输入维修描述')
    return
  }
  try {
    await submitDeviceRepair(viewingDevice.value!.id, {
      maintenanceType: maintenanceForm.value.type,
      maintenanceDate: maintenanceForm.value.date,
      description: maintenanceForm.value.description,
      technician: maintenanceForm.value.handler,
      cost: maintenanceForm.value.cost,
      notes: maintenanceForm.value.remark
    })
    maintenanceDialogVisible.value = false
    ElMessage.success('维修记录已添加')
    await loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

// 打开巡检记录对话框
const openInspectionDialog = (row: any) => {
  viewingDevice.value = row
  inspectionForm.value = {
    date: new Date().toISOString().split('T')[0],
    inspector: '',
    result: 'normal',
    issue: '',
    remark: ''
  }
  inspectionDialogVisible.value = true
}

// 提交巡检记录
const handleSubmitInspection = async () => {
  if (!inspectionForm.value.inspector.trim()) {
    ElMessage.warning('请输入巡检人')
    return
  }
  try {
    await masterApi.post(`/devices/${viewingDevice.value!.id}/inspection`, inspectionForm.value)
    inspectionDialogVisible.value = false
    ElMessage.success('巡检记录已添加')
    await loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

// 刷新数据
const handleRefresh = () => {
  loadData()
  ElMessage.success('已刷新')
}

onMounted(async () => {
  loadData()
  // 加载字段配置（alias 优先的 label + isEditable 控制）
  const config = await fetchFieldConfig('device')
  if (config) deviceLabels.value = config
})
</script>

<template>
  <div class="device-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>设备管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增设备
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="设备管理说明"
        description="设备管理包含设备基本信息、巡检计划、维修记录。支持按类型筛选设备。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('')">
          <div class="stat-content">
            <div class="stat-value">{{ stats.total }}</div>
            <div class="stat-label">设备总数</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('normal')">
          <div class="stat-content">
            <div class="stat-value normal">{{ stats.normal }}</div>
            <div class="stat-label">正常</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('maintenance')">
          <div class="stat-content">
            <div class="stat-value warning">{{ stats.maintenance }}</div>
            <div class="stat-label">维修中</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('fault')">
          <div class="stat-content">
            <div class="stat-value danger">{{ stats.fault }}</div>
            <div class="stat-label">故障</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card" @click="handleStatClick('disabled')">
          <div class="stat-content">
            <div class="stat-value info">{{ stats.disabled }}</div>
            <div class="stat-label">停用</div>
          </div>
        </el-card>
      </div>

      <!-- 设备列表 -->
      <el-table :data="filteredDevices" stripe v-loading="loading" @row-click="handleView">
        <el-table-column prop="deviceCode" :label="deviceLabels.deviceCode?.label || '设备编号'" width="120" />
        <el-table-column prop="deviceName" :label="deviceLabels.deviceName?.label || '设备名称'" min-width="150" />
        <el-table-column prop="deviceTypeName" :label="deviceLabels.deviceType?.label || '类型'" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small">{{ row.deviceTypeName || deviceTypeLabels[row.deviceType] || '其他' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="location" :label="deviceLabels.location?.label || '位置'" width="120" />
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ deviceStatusLabels[row.status] || row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="inspectionCycle" :label="deviceLabels.inspectionCycle?.label || '巡检周期'" width="100" align="center">
          <template #default="{ row }">
            {{ inspectionCycleLabels[row.inspectionCycle] || row.inspectionCycle }}
          </template>
        </el-table-column>
        <el-table-column prop="nextMaintenanceDate" :label="deviceLabels.nextMaintenanceDate?.label || '下次保养'" width="110" align="center">
          <template #default="{ row }">
            {{ row.nextMaintenanceDate ? row.nextMaintenanceDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="warning" size="small" @click.stop="openMaintenanceDialog(row)">
              <el-icon><Tools /></el-icon> 维修
            </el-button>
            <el-button link type="success" size="small" @click.stop="openInspectionDialog(row)">
              <el-icon><Document /></el-icon> 巡检
            </el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="650px">
      <el-form label-width="100px">
        <el-form-item :label="deviceLabels.deviceCode?.label || '设备编号'" required>
          <el-input v-model="form.deviceCode" placeholder="如：CAM-001" :disabled="!isFieldEditable('deviceCode')" />
        </el-form-item>
        <el-form-item :label="deviceLabels.deviceName?.label || '设备名称'" required>
          <el-input v-model="form.deviceName" placeholder="请输入设备名称" :disabled="!isFieldEditable('deviceName')" />
        </el-form-item>
        <el-form-item :label="deviceLabels.deviceType?.label || '设备类型'">
          <el-select v-model="form.deviceType" style="width: 100%" :disabled="!isFieldEditable('deviceType')">
            <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="deviceLabels.model?.label || '型号'">
          <el-input v-model="form.model" placeholder="请输入型号" :disabled="!isFieldEditable('model')" />
        </el-form-item>
        <el-form-item :label="deviceLabels.manufacturer?.label || '制造商'">
          <el-input v-model="form.manufacturer" placeholder="请输入制造商" :disabled="!isFieldEditable('manufacturer')" />
        </el-form-item>
        <el-form-item :label="deviceLabels.location?.label || '安装位置'" required>
          <el-input v-model="form.location" placeholder="如：A栋大堂" :disabled="!isFieldEditable('location')" />
        </el-form-item>
        <el-form-item :label="deviceLabels.installDate?.label || '安装日期'">
          <el-date-picker v-model="form.installDate" type="date" style="width: 100%" :disabled="!isFieldEditable('installDate')" />
        </el-form-item>
        <el-form-item :label="deviceLabels.status?.label || '设备状态'">
          <el-select v-model="form.status" style="width: 100%" :disabled="!isFieldEditable('status')">
            <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="deviceLabels.inspectionCycle?.label || '巡检周期'">
          <el-select v-model="form.inspectionCycle" style="width: 100%" :disabled="!isFieldEditable('inspectionCycle')">
            <el-option v-for="opt in cycleOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item :label="deviceLabels.remark?.label || '备注'">
          <el-input v-model="form.remark" type="textarea" placeholder="请输入备注" :disabled="!isFieldEditable('remark')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 设备详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="设备详情" width="700px">
      <div v-if="viewingDevice" class="device-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="设备编号">{{ viewingDevice.deviceCode }}</el-descriptions-item>
          <el-descriptions-item label="设备名称">{{ viewingDevice.deviceName }}</el-descriptions-item>
          <el-descriptions-item label="设备类型">{{ viewingDevice.deviceTypeName || deviceTypeLabels[viewingDevice.deviceType] }}</el-descriptions-item>
          <el-descriptions-item label="型号">{{ viewingDevice.model || '-' }}</el-descriptions-item>
          <el-descriptions-item label="制造商">{{ viewingDevice.manufacturer || '-' }}</el-descriptions-item>
          <el-descriptions-item label="安装位置">{{ viewingDevice.location }}</el-descriptions-item>
          <el-descriptions-item label="安装日期">{{ viewingDevice.purchaseDate ? viewingDevice.purchaseDate.split('T')[0] : '-' }}</el-descriptions-item>
          <el-descriptions-item label="设备状态">
            <el-tag :type="getStatusType(viewingDevice.status)">
              {{ deviceStatusLabels[viewingDevice.status] || viewingDevice.status }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="巡检周期">{{ inspectionCycleLabels[viewingDevice.inspectionCycle] || viewingDevice.inspectionCycle }}</el-descriptions-item>
          <el-descriptions-item label="下次保养">{{ viewingDevice.nextMaintenanceDate ? viewingDevice.nextMaintenanceDate.split('T')[0] : '-' }}</el-descriptions-item>
          <el-descriptions-item label="最近维修日期">{{ viewingDevice.lastMaintenanceDate ? viewingDevice.lastMaintenanceDate.split('T')[0] : '-' }}</el-descriptions-item>
          <el-descriptions-item label="供应商">{{ viewingDevice.supplierName || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 维修记录对话框 -->
    <el-dialog v-model="maintenanceDialogVisible" title="添加维修记录" width="500px">
      <el-form label-width="80px">
        <el-form-item label="维修日期" required>
          <el-date-picker v-model="maintenanceForm.date" type="date" style="width: 100%" />
        </el-form-item>
        <el-form-item label="维修类型">
          <el-select v-model="maintenanceForm.type" style="width: 100%">
            <el-option label="维修" value="repair" />
            <el-option label="更换" value="replace" />
            <el-option label="检查" value="check" />
          </el-select>
        </el-form-item>
        <el-form-item label="维修描述" required>
          <el-input v-model="maintenanceForm.description" type="textarea" placeholder="请输入维修描述" />
        </el-form-item>
        <el-form-item label="处理人">
          <el-input v-model="maintenanceForm.handler" placeholder="请输入处理人" />
        </el-form-item>
        <el-form-item label="费用">
          <el-input-number v-model="maintenanceForm.cost" :min="0" style="width: 100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="maintenanceForm.remark" type="textarea" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="maintenanceDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitMaintenance">确定</el-button>
      </template>
    </el-dialog>

    <!-- 巡检记录对话框 -->
    <el-dialog v-model="inspectionDialogVisible" title="添加巡检记录" width="500px">
      <el-form label-width="80px">
        <el-form-item label="巡检日期" required>
          <el-date-picker v-model="inspectionForm.date" type="date" style="width: 100%" />
        </el-form-item>
        <el-form-item label="巡检人" required>
          <el-input v-model="inspectionForm.inspector" placeholder="请输入巡检人" />
        </el-form-item>
        <el-form-item label="巡检结果">
          <el-select v-model="inspectionForm.result" style="width: 100%">
            <el-option label="正常" value="normal" />
            <el-option label="异常" value="abnormal" />
          </el-select>
        </el-form-item>
        <el-form-item label="问题描述">
          <el-input v-model="inspectionForm.issue" type="textarea" placeholder="请输入问题描述" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="inspectionForm.remark" type="textarea" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="inspectionDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitInspection">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="device"
      module-name="设备管理"
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.device-page {
  width: 100%;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.header-actions {
  display: flex;
  gap: 10px;
}
.stats-grid {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}
.stat-card {
  flex: 1;
  min-width: 100px;
  cursor: pointer;
  transition: all 0.3s;
}
.stat-card:hover {
  transform: translateY(-2px);
}
.stat-content {
  text-align: center;
}
.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: #409EFF;
}
.stat-value.normal { color: #67C23A; }
.stat-value.warning { color: #E6A23C; }
.stat-value.danger { color: #F56C6C; }
.stat-value.info { color: #909399; }
.stat-label {
  font-size: 13px;
  color: #909399;
  margin-top: 5px;
}
.device-detail {
  padding: 10px;
}
</style>