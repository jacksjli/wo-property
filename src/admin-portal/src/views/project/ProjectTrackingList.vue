<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Search, Document, Clock, Check, Warning } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import {
  getAllProjects,
  addProject,
  updateProject,
  deleteProject,
  getStats,
  getStatusType,
  getTypeTag,
  getSuccessRateColor,
  type TrackingProject
} from '@/stores/projectTracking'

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getProjectTrackingFields = () => getActiveFields('projectTracking')

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

// 数据
const projects = ref<TrackingProject[]>(getAllProjects())
const stats = computed(() => getStats())

// 搜索表单
const searchForm = ref({
  name: '',
  client: '',
  type: '',
  status: ''
})

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增项目')
const editingProject = ref<TrackingProject | null>(null)

// 表单数据
const formData = ref<TrackingProject>({
  id: 0,
  projectNo: '',
  name: '',
  type: '投标',
  client: '',
  budget: 0,
  bidAmount: 0,
  registerDeadline: '',
  bidDeadline: '',
  bidOpenDate: '',
  location: '',
  description: '',
  fileStatus: '未获取',
  status: '意向',
  successScore: {
    competitors: 3,
    budgetMatch: 3,
    time充足: 3,
    historyCoop: 3,
    techDifficulty: 3,
    relationResource: 3
  },
  successRate: 50,
  trackingRecords: [],
  remark: '',
  createdAt: '',
  updatedAt: ''
})

// 选项
const typeOptions = [
  { value: '招标', label: '招标' },
  { value: '投标', label: '投标' },
  { value: '意向', label: '意向' }
]

const statusOptions = [
  { value: '意向', label: '意向' },
  { value: '跟踪中', label: '跟踪中' },
  { value: '报名', label: '报名' },
  { value: '已投标', label: '已投标' },
  { value: '开标', label: '开标' },
  { value: '公告', label: '公告' },
  { value: '中标', label: '中标' },
  { value: '落标', label: '落标' }
]

// 过滤数据
const filteredProjects = computed(() => {
  return projects.value.filter(project => {
    const matchName = !searchForm.value.name || project.name.includes(searchForm.value.name)
    const matchClient = !searchForm.value.client || project.client.includes(searchForm.value.client)
    const matchType = !searchForm.value.type || project.type === searchForm.value.type
    const matchStatus = !searchForm.value.status || project.status === searchForm.value.status
    return matchName && matchClient && matchType && matchStatus
  })
})

// 重置表单
const resetForm = () => {
  formData.value = {
    id: 0,
    projectNo: '',
    name: '',
    type: '投标',
    client: '',
    budget: 0,
    bidAmount: 0,
    registerDeadline: '',
    bidDeadline: '',
    bidOpenDate: '',
    location: '',
    description: '',
    fileStatus: '未获取',
    status: '意向',
    successScore: {
      competitors: 3,
      budgetMatch: 3,
      time充足: 3,
      historyCoop: 3,
      techDifficulty: 3,
      relationResource: 3
    },
    successRate: 50,
    trackingRecords: [],
    remark: '',
    createdAt: '',
    updatedAt: ''
  }
}

// 打开新增对话框
const handleAdd = () => {
  resetForm()
  dialogTitle.value = '新增项目'
  editingProject.value = null
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (project: TrackingProject) => {
  formData.value = { ...project }
  dialogTitle.value = '编辑项目'
  editingProject.value = project
  dialogVisible.value = true
}

// 保存项目
const handleSave = () => {
  if (editingProject.value) {
    updateProject(editingProject.value.id, formData.value)
    ElMessage.success('项目信息更新成功')
  } else {
    addProject(formData.value)
    ElMessage.success('项目新增成功')
  }
  projects.value = getAllProjects()
  dialogVisible.value = false
}

// 删除项目
const handleDelete = (project: TrackingProject) => {
  ElMessageBox.confirm(
    `确定要删除项目「${project.name}」吗？此操作不可恢复！`,
    '删除确认',
    {
      confirmButtonText: '确定删除',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(() => {
    deleteProject(project.id)
    projects.value = getAllProjects()
    ElMessage.success('项目已删除')
  }).catch(() => {})
}

// 搜索
const handleSearch = () => {
  ElMessage.success('搜索完成')
}

// 重置搜索
const handleReset = () => {
  searchForm.value = {
    name: '',
    client: '',
    type: '',
    status: ''
  }
  ElMessage.info('已重置搜索条件')
}

// 刷新
const handleRefresh = () => {
  projects.value = getAllProjects()
  ElMessage.success('数据已刷新')
}

// 格式化金额
const formatMoney = (amount: number) => {
  if (amount >= 1000000) {
    return `¥${(amount / 1000000).toFixed(1)}百万`
  } else if (amount >= 10000) {
    return `¥${(amount / 10000).toFixed(1)}万`
  }
  return `¥${amount}`
}

// 格式化日期
const formatDate = (date: string) => {
  if (!date) return '-'
  return date
}
</script>

<template>
  <div class="project-tracking-page">
    <!-- 统计卡片 -->
    <div class="stats-container">
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon total-icon"><Document /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.total }}</span>
            <span class="stat-label">总项目数</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon active-icon"><Clock /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.active }}</span>
            <span class="stat-label">进行中</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon won-icon"><Check /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.won }}</span>
            <span class="stat-label">已中标</span>
          </div>
        </div>
      </el-card>
      <el-card shadow="never" class="stat-card">
        <div class="stat-content">
          <el-icon size="32" class="stat-icon high-rate-icon"><Warning /></el-icon>
          <div class="stat-info">
            <span class="stat-value">{{ stats.highRate }}</span>
            <span class="stat-label">高成功率</span>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 操作栏 -->
    <el-card shadow="never" class="操作栏">
      <el-form :model="searchForm" inline>
        <el-form-item label="项目名称">
          <el-input v-model="searchForm.name" placeholder="请输入项目名称" clearable style="width: 150px" />
        </el-form-item>
        <el-form-item label="客户">
          <el-input v-model="searchForm.client" placeholder="请输入客户名称" clearable style="width: 150px" />
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
        <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon>新增项目</el-button>
        <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Edit /></el-icon>配置字段</el-button>
      </div>

      <el-table :data="filteredProjects" stripe style="width: 100%" :max-height="600">
        <el-table-column prop="projectNo" label="项目编号" width="120" fixed />
        <el-table-column prop="name" label="项目名称" width="200" show-overflow-tooltip />
        <el-table-column prop="type" label="类型" width="80">
          <template #default="{ row }">
            <el-tag :type="getTypeTag(row.type)" size="small">{{ row.type }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="client" label="客户" width="120" />
        <el-table-column prop="budget" label="预算" width="100">
          <template #default="{ row }">{{ formatMoney(row.budget) }}</template>
        </el-table-column>
        <el-table-column prop="location" label="所在地" width="120" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="successRate" label="成功率" width="100">
          <template #default="{ row }">
            <el-tag :type="getSuccessRateColor(row.successRate)" size="small">{{ row.successRate }}%</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="registerDeadline" label="报名截止" width="110">
          <template #default="{ row }">
            <div>{{ formatDate(row.registerDeadline) }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="最后更新" width="110" />
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEdit(row)"><el-icon><Edit /></el-icon>编辑</el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)"><el-icon><Delete /></el-icon>删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="800px" destroy-on-close>
      <el-form :model="formData" label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="项目名称">
              <el-input v-model="formData.name" placeholder="请输入项目名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目类型">
              <el-select v-model="formData.type" style="width: 100%">
                <el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="客户名称">
              <el-input v-model="formData.client" placeholder="请输入客户名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目预算">
              <el-input v-model="formData.budget" type="number" placeholder="请输入项目预算" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="投标金额">
              <el-input v-model="formData.bidAmount" type="number" placeholder="请输入投标金额" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目所在地">
              <el-input v-model="formData.location" placeholder="请输入项目所在地" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="报名截止">
              <el-date-picker v-model="formData.registerDeadline" type="date" placeholder="选择日期" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="投标截止">
              <el-date-picker v-model="formData.bidDeadline" type="date" placeholder="选择日期" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="开标日期">
              <el-date-picker v-model="formData.bidOpenDate" type="date" placeholder="选择日期" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目状态">
              <el-select v-model="formData.status" style="width: 100%">
                <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="项目描述">
              <el-input v-model="formData.description" type="textarea" placeholder="请输入项目描述" :rows="3" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="formData.remark" type="textarea" placeholder="请输入备注" :rows="2" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleSave">保存</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="projectTracking"
      module-name="项目跟踪"
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.project-tracking-page {
  padding: 20px;
}

.stats-container {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  border-radius: 8px;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  padding: 8px;
  border-radius: 8px;
}

.total-icon {
  background-color: #e6f4ff;
  color: #1677ff;
}

.active-icon {
  background-color: #f6ffed;
  color: #52c41a;
}

.won-icon {
  background-color: #fff7e6;
  color: #fa8c16;
}

.high-rate-icon {
  background-color: #fff2f0;
  color: #ff4d4f;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 24px;
  font-weight: bold;
  line-height: 1.2;
}

.stat-label {
  font-size: 14px;
  color: #666;
}

.操作栏 {
  margin-bottom: 20px;
}

.操作按钮 {
  margin-bottom: 16px;
}
</style>
