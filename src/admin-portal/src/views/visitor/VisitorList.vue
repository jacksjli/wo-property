<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, UserFilled, MoreFilled } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { visitorApi } from '@/api/visitor'
import { useFieldConfig } from '@/composables/useFieldConfig'

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getVisitorFields = () => getActiveFields('visitor')

// 字段配置（alias 优先的 label + isEditable 控制）
const { fetchFieldConfig, getLabel } = useFieldConfig()
const visitorLabels = ref<Record<string, any>>({})

// 获取某字段是否可编辑
const isFieldEditable = (fieldKey: string): boolean => {
  return visitorLabels.value[fieldKey]?.isEditable ?? true
}

// 打开字段配置
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

// 访客类型/状态标签
const visitorTypeLabels: Record<string, string> = {
  family: '探亲',
  friend: '朋友',
  delivery: '快递',
  business: '商务',
  other: '其他'
}

const visitorStatusLabels: Record<string, string> = {
  registered: '已登记',
  checked_in: '已进入',
  checked_out: '已离开'
}

// 数据
const visitors = ref<any[]>([])
const loading = ref(false)

const stats = computed(() => ({
  total: visitors.value.length,
  today: visitors.value.filter((v: any) => {
    const today = new Date().toISOString().split('T')[0]
    return v.visitDate && v.visitDate.startsWith(today)
  }).length,
  checkedIn: visitors.value.filter((v: any) => v.status === 'checked_in').length,
  checkedOut: visitors.value.filter((v: any) => v.status === 'checked_out').length
}))

// 筛选
const filterStatus = ref<string>('')
const filterType = ref<string>('')

// 筛选后的访客
const filteredVisitors = computed(() => {
  let result = visitors.value
  
  if (filterStatus.value) {
    result = result.filter((v: any) => v.status === filterStatus.value)
  }
  
  if (filterType.value) {
    result = result.filter((v: any) => v.visitorType === filterType.value)
  }
  
  return result.sort((a: any, b: any) => (b.createdAt || '').localeCompare(a.createdAt || ''))
})

// 对话框状态
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const dialogTitle = ref('访客登记')
const editingId = ref<number | null>(null)
const viewingVisitor = ref<any | null>(null)

// 表单数据
const form = ref({
  visitorName: '',
  visitorPhone: '',
  idCardNumber: '',
  visitorType: 'family',
  plateNo: '',
  buildingId: null as number | null,
  roomId: null as number | null,
  hostName: '',
  hostPhone: '',
  visitPurpose: '',
  visitDate: '',
  visitTime: '',
  companion: '',
  handler: '前台登记',
  remark: ''
})

// 选项
const typeOptions = Object.entries(visitorTypeLabels).map(([value, label]) => ({ value, label }))

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res: any = await visitorApi.getList({ page: 1, pageSize: 200, status: filterStatus.value || undefined })
    if (res.success) {
      visitors.value = res.data || []
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载访客数据失败')
  } finally {
    loading.value = false
  }
}

// 打开登记对话框
const handleAdd = () => {
  dialogTitle.value = '访客登记'
  editingId.value = null
  const now = new Date()
  const dateStr = now.toISOString().split('T')[0]
  const timeStr = now.toTimeString().slice(0, 5)
  form.value = {
    visitorName: '',
    visitorPhone: '',
    idCardNumber: '',
    visitorType: 'family',
    plateNo: '',
    buildingId: null,
    roomId: null,
    hostName: '',
    hostPhone: '',
    visitPurpose: '',
    visitDate: dateStr,
    visitTime: timeStr,
    companion: '',
    handler: '前台登记',
    remark: ''
  }
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑访客'
  editingId.value = row.id
  form.value = {
    visitorName: row.visitorName || row.name || '',
    visitorPhone: row.visitorPhone || row.phone || '',
    idCardNumber: row.idCardNumber || '',
    visitorType: row.visitorType || row.type || 'family',
    plateNo: row.plateNo || '',
    buildingId: row.buildingId || null,
    roomId: row.roomId || null,
    hostName: row.hostName || row.residentName || '',
    hostPhone: row.hostPhone || '',
    visitPurpose: row.visitPurpose || '',
    visitDate: row.visitDate ? row.visitDate.split('T')[0] : '',
    visitTime: row.visitTime || '',
    companion: row.companion || '',
    handler: row.handler || '前台登记',
    remark: row.remark || ''
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!form.value.visitorName.trim()) {
    ElMessage.warning('请输入访客姓名')
    return
  }
  if (!form.value.visitorPhone.trim()) {
    ElMessage.warning('请输入联系电话')
    return
  }
  if (!form.value.hostName.trim()) {
    ElMessage.warning('请输入被访住户姓名')
    return
  }
  if (!form.value.visitPurpose.trim()) {
    ElMessage.warning('请输入访问事由')
    return
  }

  try {
    const payload = {
      visitorName: form.value.visitorName,
      visitorPhone: form.value.visitorPhone || undefined,
      idCardNumber: form.value.idCardNumber || undefined,
      visitPurpose: form.value.visitPurpose || undefined,
      visitDate: form.value.visitDate ? new Date(form.value.visitDate) : undefined,
      visitTime: form.value.visitTime ? new Date(`1970-01-01T${form.value.visitTime}`).getTime() / 1000 : undefined,
      buildingId: form.value.buildingId || undefined,
      roomId: form.value.roomId || undefined,
      hostName: form.value.hostName,
      hostPhone: form.value.hostPhone || undefined,
      remarks: form.value.remark || undefined,
    }
    if (editingId.value) {
      await visitorApi.update(editingId.value, { remarks: form.value.remark })
      ElMessage.success('更新成功')
    } else {
      await visitorApi.create(payload)
      ElMessage.success('访客登记成功')
    }
    dialogVisible.value = false
    await loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

// 删除访客
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除访客登记 "${row.visitorName || row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await visitorApi.delete(row.id)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

// 查看详情
const handleView = (row: any) => {
  viewingVisitor.value = row
  detailDialogVisible.value = true
}

// 进入
const handleCheckIn = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定访客 "${row.visitorName || row.name}" 已经进入吗？`, '确认进入', {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      type: 'info'
    })
    await visitorApi.checkIn(row.id)
    ElMessage.success('访客已进入')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '操作失败')
  }
}

// 离开
const handleCheckOut = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定访客 "${row.visitorName || row.name}" 已经离开吗？`, '确认离开', {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      type: 'info'
    })
    await visitorApi.checkOut(row.id)
    ElMessage.success('访客已离开')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '操作失败')
  }
}

// 获取状态
const getVisitorStatus = (visitor: any): string => {
  return visitor.status || 'registered'
}

// 获取状态类型
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    registered: 'info',
    checked_in: 'success',
    checked_out: ''
  }
  return map[status] || 'info'
}

// 获取状态标签
const getStatusLabel = (visitor: any) => {
  return visitorStatusLabels[getVisitorStatus(visitor)] || '未知'
}

// 刷新数据
const handleRefresh = () => {
  loadData()
  ElMessage.success('已刷新')
}

onMounted(async () => {
  loadData()
  // 加载字段配置（alias 优先的 label + isEditable 控制）
  const config = await fetchFieldConfig('visitor')
  if (config) visitorLabels.value = config
})
</script>

<template>
  <div class="visitor-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>访客管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 访客登记
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="访客管理说明"
        description="管理所有访客登记，支持查看进入/离开状态，记录访客信息。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value">{{ stats.total }}</div>
            <div class="stat-label">访客总数</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card today">
          <div class="stat-content">
            <div class="stat-value">{{ stats.today }}</div>
            <div class="stat-label">今日访客</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card checked-in" @click="filterStatus = filterStatus === 'checked_in' ? '' : 'checked_in'">
          <div class="stat-content">
            <div class="stat-value">{{ stats.checkedIn }}</div>
            <div class="stat-label">已进入</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card checked-out" @click="filterStatus = filterStatus === 'checked_out' ? '' : 'checked_out'">
          <div class="stat-content">
            <div class="stat-value">{{ stats.checkedOut }}</div>
            <div class="stat-label">已离开</div>
          </div>
        </el-card>
      </div>

      <!-- 访客列表 -->
      <el-table :data="filteredVisitors" stripe v-loading="loading" @row-click="handleView">
        <el-table-column prop="visitorName" :label="visitorLabels.visitorName?.label || '访客姓名'" width="100" />
        <el-table-column prop="visitorPhone" :label="visitorLabels.visitorPhone?.label || '联系电话'" width="120" />
        <el-table-column prop="visitorType" :label="visitorLabels.visitorType?.label || '访客类型'" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small">{{ visitorTypeLabels[row.visitorType] || '其他' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="hostName" :label="visitorLabels.hostName?.label || '被访住户'" width="100" align="center" />
        <el-table-column prop="roomNumber" :label="visitorLabels.roomNumber?.label || '房号'" width="90" align="center">
          <template #default="{ row }">
            {{ row.roomNumber || row.residentRoom || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="visitPurpose" :label="visitorLabels.visitPurpose?.label || '访问事由'" min-width="120" />
        <el-table-column prop="plateNo" :label="visitorLabels.plateNo?.label || '车牌'" width="100" align="center">
          <template #default="{ row }">
            {{ row.plateNo || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="visitDate" :label="visitorLabels.visitDate?.label || '访问日期'" width="110" align="center">
          <template #default="{ row }">
            {{ row.visitDate ? row.visitDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="visitTime" :label="visitorLabels.visitTime?.label || '访问时间'" width="80" align="center" />
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusLabel(row) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="success" size="small" @click.stop="handleCheckIn(row)" v-if="row.status !== 'checked_in'">
              <el-icon><UserFilled /></el-icon> 进入
            </el-button>
            <el-button link type="warning" size="small" @click.stop="handleCheckOut(row)" v-if="row.status === 'checked_in'">
              <el-icon><MoreFilled /></el-icon> 离开
            </el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item :label="visitorLabels.visitorName?.label || '访客姓名'" required>
              <el-input v-model="form.visitorName" placeholder="请输入访客姓名" :disabled="!isFieldEditable('visitorName')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="visitorLabels.visitorPhone?.label || '联系电话'" required>
              <el-input v-model="form.visitorPhone" placeholder="请输入联系电话" :disabled="!isFieldEditable('visitorPhone')" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item :label="visitorLabels.idCardNumber?.label || '身份证号'">
              <el-input v-model="form.idCardNumber" placeholder="请输入身份证号" :disabled="!isFieldEditable('idCardNumber')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="visitorLabels.visitorType?.label || '访客类型'">
              <el-select v-model="form.visitorType" style="width: 100%" :disabled="!isFieldEditable('visitorType')">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item :label="visitorLabels.plateNo?.label || '车牌号'">
              <el-input v-model="form.plateNo" placeholder="请输入车牌号" :disabled="!isFieldEditable('plateNo')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="visitorLabels.hostName?.label || '被访住户'" required>
              <el-input v-model="form.hostName" placeholder="请输入被访住户姓名" :disabled="!isFieldEditable('hostName')" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item :label="visitorLabels.hostPhone?.label || '被访电话'">
              <el-input v-model="form.hostPhone" placeholder="请输入被访电话" :disabled="!isFieldEditable('hostPhone')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="visitorLabels.visitPurpose?.label || '访问事由'" required>
              <el-input v-model="form.visitPurpose" placeholder="请输入访问事由" :disabled="!isFieldEditable('visitPurpose')" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item :label="visitorLabels.visitDate?.label || '访问日期'">
              <el-date-picker v-model="form.visitDate" type="date" placeholder="选择日期" style="width: 100%" :disabled="!isFieldEditable('visitDate')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="visitorLabels.visitTime?.label || '访问时间'">
              <el-time-picker v-model="form.visitTime" placeholder="选择时间" style="width: 100%" format="HH:mm" value-format="HH:mm:ss" :disabled="!isFieldEditable('visitTime')" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item :label="visitorLabels.companion?.label || '同行人数'">
              <el-input v-model="form.companion" placeholder="如：2人" :disabled="!isFieldEditable('companion')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="visitorLabels.handler?.label || '登记人'">
              <el-input v-model="form.handler" placeholder="请输入登记人" :disabled="!isFieldEditable('handler')" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="visitorLabels.remark?.label || '备注'">
          <el-input v-model="form.remark" type="textarea" placeholder="请输入备注" :disabled="!isFieldEditable('remark')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 访客详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="访客详情" width="650px">
      <div v-if="viewingVisitor" class="visitor-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="访客姓名">{{ viewingVisitor.visitorName }}</el-descriptions-item>
          <el-descriptions-item label="访客类型">
            <el-tag size="small">{{ visitorTypeLabels[viewingVisitor.visitorType] || '其他' }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ viewingVisitor.visitorPhone }}</el-descriptions-item>
          <el-descriptions-item label="身份证号">{{ viewingVisitor.idCardNumber || '-' }}</el-descriptions-item>
          <el-descriptions-item label="车牌号">{{ viewingVisitor.plateNo || '-' }}</el-descriptions-item>
          <el-descriptions-item label="被访住户">{{ viewingVisitor.hostName }}</el-descriptions-item>
          <el-descriptions-item label="被访电话">{{ viewingVisitor.hostPhone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="房号">{{ viewingVisitor.roomNumber || '-' }}</el-descriptions-item>
          <el-descriptions-item label="访问事由" :span="2">{{ viewingVisitor.visitPurpose }}</el-descriptions-item>
          <el-descriptions-item label="访问日期">{{ viewingVisitor.visitDate ? viewingVisitor.visitDate.split('T')[0] : '-' }}</el-descriptions-item>
          <el-descriptions-item label="访问时间">{{ viewingVisitor.visitTime }}</el-descriptions-item>
          <el-descriptions-item label="同行人数">{{ viewingVisitor.companion || '-' }}</el-descriptions-item>
          <el-descriptions-item label="登记人">{{ viewingVisitor.handler }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(viewingVisitor.status)">
              {{ getStatusLabel(viewingVisitor) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ viewingVisitor.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="visitor"
      module-name="访客管理"
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.visitor-page {
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
}
.stat-card {
  flex: 1;
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
.stat-card.today .stat-value { color: #E6A23C; }
.stat-card.checked-in .stat-value { color: #67C23A; }
.stat-card.checked-out .stat-value { color: #909399; }
.stat-label {
  font-size: 13px;
  color: #909399;
  margin-top: 5px;
}
.visitor-detail {
  padding: 10px;
}
</style>