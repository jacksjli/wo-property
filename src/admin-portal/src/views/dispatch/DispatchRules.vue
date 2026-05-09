<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Check, Close, Search, Document } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import {
  getAllRules,
  getStats,
  addRule,
  updateRule,
  deleteRule,
  toggleRuleStatus,
  ruleTypeLabels,
  ticketColorLabels,
  ticketColorMap,
  type DispatchRule,
  type RuleType,
  type TicketColor,
  type RuleStatus
} from '@/stores/dispatch'

// 权限验证
const { verifyAdminPassword } = usePermission()
const showFieldConfig = ref(false)

// 数据
const rules = ref<DispatchRule[]>(getAllRules())
const stats = computed(() => getStats())

// 筛选
const filterType = ref<RuleType | ''>('')
const filterStatus = ref<RuleStatus | ''>('')
const searchKeyword = ref('')

// 筛选后的规则
const filteredRules = computed(() => {
  let result = rules.value
  
  if (filterType.value) {
    result = result.filter(r => r.type === filterType.value)
  }
  
  if (filterStatus.value) {
    result = result.filter(r => r.status === filterStatus.value)
  }
  
  if (searchKeyword.value) {
    const keyword = searchKeyword.value.toLowerCase()
    result = result.filter(r => 
      r.ruleNo.toLowerCase().includes(keyword) ||
      r.name.toLowerCase().includes(keyword) ||
      r.description?.toLowerCase().includes(keyword)
    )
  }
  
  return result.sort((a, b) => b.priority - a.priority)
})

// 类型选项
const typeOptions = Object.entries(ruleTypeLabels).map(([value, label]) => ({ value, label }))
const statusOptions = [
  { value: 'active', label: '激活' },
  { value: 'inactive', label: '停用' },
  { value: 'draft', label: '草稿' }
]
const colorOptions = Object.entries(ticketColorLabels).map(([value, label]) => ({ value, label }))
const locationTypeOptions = [
  { value: 'exact', label: '精确匹配' },
  { value: 'contains', label: '包含' }
]

// 对话框状态
const dialogVisible = ref(false)
const dialogTitle = ref('新增规则')
const editingId = ref<number | null>(null)

// 表单数据
const form = ref({
  name: '',
  description: '',
  type: 'ticket_type' as RuleType,
  ticketColor: 'all' as TicketColor,
  location: '',
  locationType: 'contains' as const,
  priority: 5,
  autoAssign: true,
  notifyBackup: false,
  allowTransfer: true,
  timeoutEscalation: true,
  operatorIds: '',
  supervisorId: '',
  managerId: '',
  departmentHeadId: '',
  companyHeadId: '',
  backupIds: '',
  notifyMethods: 'app',
  status: 'active' as RuleStatus,
  remark: ''
})

// 重置表单
const resetForm = () => {
  form.value = {
    name: '',
    description: '',
    type: 'ticket_type',
    ticketColor: 'all',
    location: '',
    locationType: 'contains',
    priority: 5,
    autoAssign: true,
    notifyBackup: false,
    allowTransfer: true,
    timeoutEscalation: true,
    operatorIds: '',
    supervisorId: '',
    managerId: '',
    departmentHeadId: '',
    companyHeadId: '',
    backupIds: '',
    notifyMethods: 'app',
    status: 'active',
    remark: ''
  }
}

// 打开新增对话框
const handleAdd = () => {
  dialogTitle.value = '新增规则'
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: DispatchRule) => {
  dialogTitle.value = '编辑规则'
  editingId.value = row.id
  form.value = {
    name: row.name,
    description: row.description || '',
    type: row.type,
    ticketColor: row.ticketColor,
    location: row.location || '',
    locationType: row.locationType,
    priority: row.priority,
    autoAssign: row.autoAssign,
    notifyBackup: row.notifyBackup,
    allowTransfer: row.allowTransfer,
    timeoutEscalation: row.timeoutEscalation,
    operatorIds: row.operatorIds || '',
    supervisorId: row.supervisorId || '',
    managerId: row.managerId || '',
    departmentHeadId: row.departmentHeadId || '',
    companyHeadId: row.companyHeadId || '',
    backupIds: row.backupIds || '',
    notifyMethods: row.notifyMethods || 'app',
    status: row.status,
    remark: row.remark || ''
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = () => {
  if (!form.value.name.trim()) {
    ElMessage.warning('请输入规则名称')
    return
  }

  if (editingId.value) {
    updateRule(editingId.value, form.value)
    ElMessage.success('规则已更新')
  } else {
    addRule({
      ...form.value,
      createdBy: 'admin'
    })
    ElMessage.success('规则已创建')
  }

  rules.value = getAllRules()
  dialogVisible.value = false
}

// 删除规则
const handleDelete = async (row: DispatchRule) => {
  try {
    await ElMessageBox.confirm(`确定删除规则 "${row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    deleteRule(row.id)
    rules.value = getAllRules()
    ElMessage.success('规则已删除')
  } catch {
    // 取消
  }
}

// 切换状态
const handleToggleStatus = (row: DispatchRule) => {
  toggleRuleStatus(row.id)
  rules.value = getAllRules()
  ElMessage.success(`规则已${row.status === 'active' ? '停用' : '激活'}`)
}

// 刷新
const handleRefresh = () => {
  rules.value = getAllRules()
  ElMessage.success('已刷新')
}

// 打开字段配置
const handleFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    showFieldConfig.value = true
  }
}

// 计算成功率
const getSuccessRate = (rule: DispatchRule) => {
  if (rule.matchCount === 0) return '0%'
  return `${Math.round(rule.successCount / rule.matchCount * 100)}%`
}

// 格式化时间
const formatTime = (time: string | null) => {
  if (!time) return '-'
  return new Date(time).toLocaleString('zh-CN', { hour12: false }).replace(/\//g, '-').slice(0, 16)
}
</script>

<template>
  <div class="rules-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>派单规则管理</span>
          <div class="header-actions">
            <el-button @click="handleFieldConfig">
              <el-icon><Document /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增规则
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="派单规则管理说明"
        description="配置自动派单规则，支持按工单类型、颜色、地点等条件匹配处理人员。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value">{{ stats.rules.total }}</div>
            <div class="stat-label">规则总数</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card active">
          <div class="stat-content">
            <div class="stat-value">{{ stats.rules.active }}</div>
            <div class="stat-label">已激活</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card inactive">
          <div class="stat-content">
            <div class="stat-value">{{ stats.rules.inactive }}</div>
            <div class="stat-label">已停用</div>
          </div>
        </el-card>
      </div>

      <!-- 筛选 -->
      <el-row :gutter="20" style="margin-bottom: 15px;">
        <el-col :span="6">
          <el-input v-model="searchKeyword" placeholder="搜索规则编号、名称、描述" clearable>
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>
        </el-col>
        <el-col :span="4">
          <el-select v-model="filterType" placeholder="规则类型" clearable>
            <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-select v-model="filterStatus" placeholder="状态筛选" clearable>
            <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-col>
      </el-row>

      <!-- 规则列表 -->
      <el-table :data="filteredRules" stripe>
        <el-table-column prop="ruleNo" label="规则编号" width="130" />
        <el-table-column prop="name" label="规则名称" min-width="150" />
        <el-table-column prop="type" label="类型" width="100" align="center">
          <template #default="{ row }">
            {{ ruleTypeLabels[row.type] }}
          </template>
        </el-table-column>
        <el-table-column label="工单颜色" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.ticketColor !== 'all'" :style="{ backgroundColor: ticketColorMap[row.ticketColor], borderColor: ticketColorMap[row.ticketColor], color: '#fff' }" size="small">
              {{ ticketColorLabels[row.ticketColor] }}
            </el-tag>
            <span v-else>全部</span>
          </template>
        </el-table-column>
        <el-table-column prop="priority" label="优先级" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.priority <= 3 ? 'danger' : row.priority <= 6 ? 'warning' : 'info'" size="small">
              {{ row.priority }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="location" label="地点" width="120">
          <template #default="{ row }">
            {{ row.location || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="自动派单" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.autoAssign ? 'success' : 'info'" size="small">
              {{ row.autoAssign ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="匹配/成功" width="100" align="center">
          <template #default="{ row }">
            <span>{{ row.matchCount }}/{{ row.successCount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="成功率" width="80" align="center">
          <template #default="{ row }">
            <span :class="{ 'success-rate': (row.successCount / (row.matchCount || 1)) >= 0.8 }">
              {{ getSuccessRate(row) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="avgResponseTime" label="平均响应" width="90" align="center">
          <template #default="{ row }">
            {{ row.avgResponseTime > 0 ? `${row.avgResponseTime}分钟` : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'active' ? 'success' : 'info'" size="small">
              {{ row.status === 'active' ? '激活' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon> 编辑
            </el-button>
            <el-button link :type="row.status === 'active' ? 'warning' : 'success'" size="small" @click="handleToggleStatus(row)">
              <el-icon><Check v-if="row.status === 'active'" /><Close v-else /></el-icon>
              {{ row.status === 'active' ? '停用' : '激活' }}
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">
              <el-icon><Delete /></el-icon>
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="750px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="规则名称" required>
              <el-input v-model="form.name" placeholder="请输入规则名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="规则类型">
              <el-select v-model="form.type" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-form-item label="规则描述">
          <el-input v-model="form.description" type="textarea" rows="2" placeholder="请输入规则描述" />
        </el-form-item>
        
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="工单颜色">
              <el-select v-model="form.ticketColor" style="width: 100%">
                <el-option v-for="opt in colorOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="优先级">
              <el-input-number v-model="form.priority" :min="1" :max="10" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="规则状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="匹配地点">
              <el-input v-model="form.location" placeholder="请输入匹配地点" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="匹配方式">
              <el-select v-model="form.locationType" style="width: 100%">
                <el-option v-for="opt in locationTypeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-divider content-position="left">派单设置</el-divider>
        
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="自动派单">
              <el-switch v-model="form.autoAssign" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="允许转单">
              <el-switch v-model="form.allowTransfer" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="超时升级">
              <el-switch v-model="form.timeoutEscalation" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="操作人员ID">
              <el-input v-model="form.operatorIds" placeholder="多个用逗号分隔，如: 1,2,3" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="通知方式">
              <el-input v-model="form.notifyMethods" placeholder="app,phone,sms,email" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="主管ID">
              <el-input v-model="form.supervisorId" placeholder="主管ID" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="经理ID">
              <el-input v-model="form.managerId" placeholder="经理ID" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="备用人员">
              <el-input v-model="form.backupIds" placeholder="备用人员ID" />
            </el-form-item>
          </el-col>
        </el-row>
        
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" rows="2" placeholder="备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      v-model="showFieldConfig"
      module="dispatch"
      title="派单规则字段配置"
    />
  </div>
</template>

<style scoped>
.rules-page {
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
.stat-card.active .stat-value { color: #67C23A; }
.stat-card.inactive .stat-value { color: #909399; }
.stat-label {
  font-size: 13px;
  color: #909399;
  margin-top: 5px;
}
.success-rate {
  color: #67C23A;
  font-weight: bold;
}
</style>
