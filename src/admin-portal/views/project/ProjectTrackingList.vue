<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  Plus, Delete, Edit, Search, Refresh, View, Document, Clock,
  Money, User, Phone, Location, Tickets, Check, Warning, SuccessFilled
} from '@element-plus/icons-vue'
import { useProjectTrackingStore, type ProjectTracking, type ProjectType, type ProjectStatus, type TrackingRecord } from '@/stores/projectTracking'

const store = useProjectTrackingStore()

onMounted(() => {
  store.init()
})

// 统计数据
const stats = computed(() => store.statistics)

// 搜索筛选
const searchForm = ref({
  keyword: '',
  status: '' as ProjectStatus | '',
  projectType: '' as ProjectType | '',
  rateRange: '' as string | ''
})

// 筛选后的数据
const filteredProjects = computed(() => {
  let result = [...store.projects]

  if (searchForm.value.keyword) {
    const kw = searchForm.value.keyword.toLowerCase()
    result = result.filter(p =>
      p.projectName.toLowerCase().includes(kw) ||
      p.projectNo.toLowerCase().includes(kw) ||
      p.clientName.toLowerCase().includes(kw)
    )
  }

  if (searchForm.value.status) {
    result = result.filter(p => p.status === searchForm.value.status)
  }

  if (searchForm.value.projectType) {
    result = result.filter(p => p.projectType === searchForm.value.projectType)
  }

  if (searchForm.value.rateRange) {
    const ranges: Record<string, [number, number]> = {
      'high': [60, 100],
      'medium': [30, 60],
      'low': [0, 30]
    }
    const [min, max] = ranges[searchForm.value.rateRange] || [0, 100]
    result = result.filter(p => {
      const rate = store.calculateSuccessRate(p)
      return rate >= min && rate <= max
    })
  }

  return result
})

// 获取成功率
const getSuccessRate = (project: ProjectTracking) => store.calculateSuccessRate(project)
const getSuccessRateColor = (rate: number) => store.getSuccessRateColor(rate)
const getSuccessRateTagType = (rate: number) => store.getSuccessRateTagType(rate)

// 状态选项
const statusOptions: ProjectStatus[] = ['意向', '跟踪中', '报名', '已投标', '开标', '公告', '中标', '落标']
const typeOptions: ProjectType[] = ['招标', '投标', '意向']
const documentStatusOptions = ['未获取', '已获取', '已购买']

// 状态颜色映射
const statusTypeMap: Record<ProjectStatus, string> = {
  '意向': 'info',
  '跟踪中': 'primary',
  '报名': 'warning',
  '已投标': 'success',
  '开标': 'success',
  '公告': 'success',
  '中标': 'success',
  '落标': 'danger'
}

// 对话框状态
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const trackingDialogVisible = ref(false)
const dialogTitle = ref('新增项目')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)
const viewingProject = ref<ProjectTracking | null>(null)
const trackingFormRef = ref<FormInstance>()

// 表单数据
const form = ref({
  projectName: '',
  projectType: '投标' as ProjectType,
  clientName: '',
  clientContact: '',
  clientPhone: '',
  budget: 0,
  bidAmount: 0,
  registrationDeadline: '',
  bidDeadline: '',
  openingDate: '',
  location: '',
  description: '',
  requirements: '',
  documentStatus: '未获取' as '未获取' | '已获取' | '已购买',
  status: '意向' as ProjectStatus,
  competitorCount: 3,
  budgetReasonableness: 70,
  timeAdequacy: 70,
  historicalCooperation: 50,
  technicalDifficulty: 50,
  relationshipResources: 50
})

// 跟踪记录表单
const trackingForm = ref({
  status: '跟踪中' as ProjectStatus,
  content: '',
  operator: ''
})

// 表单验证规则
const rules: FormRules = {
  projectName: [{ required: true, message: '请输入项目名称', trigger: 'blur' }],
  projectType: [{ required: true, message: '请选择项目类型', trigger: 'change' }],
  clientName: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
  budget: [{ required: true, message: '请输入项目预算', trigger: 'blur' }]
}

// 重置表单
const resetForm = () => {
  form.value = {
    projectName: '',
    projectType: '投标',
    clientName: '',
    clientContact: '',
    clientPhone: '',
    budget: 0,
    bidAmount: 0,
    registrationDeadline: '',
    bidDeadline: '',
    openingDate: '',
    location: '',
    description: '',
    requirements: '',
    documentStatus: '未获取',
    status: '意向',
    competitorCount: 3,
    budgetReasonableness: 70,
    timeAdequacy: 70,
    historicalCooperation: 50,
    technicalDifficulty: 50,
    relationshipResources: 50
  }
}

// 新增项目
const handleAdd = () => {
  dialogTitle.value = '新增项目'
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

// 编辑项目
const handleEdit = (row: ProjectTracking) => {
  dialogTitle.value = '编辑项目'
  editingId.value = row.id
  form.value = {
    projectName: row.projectName,
    projectType: row.projectType,
    clientName: row.clientName,
    clientContact: row.clientContact,
    clientPhone: row.clientPhone,
    budget: row.budget,
    bidAmount: row.bidAmount,
    registrationDeadline: row.registrationDeadline,
    bidDeadline: row.bidDeadline,
    openingDate: row.openingDate,
    location: row.location,
    description: row.description,
    requirements: row.requirements,
    documentStatus: row.documentStatus,
    status: row.status,
    competitorCount: row.competitorCount,
    budgetReasonableness: row.budgetReasonableness,
    timeAdequacy: row.timeAdequacy,
    historicalCooperation: row.historicalCooperation,
    technicalDifficulty: row.technicalDifficulty,
    relationshipResources: row.relationshipResources
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    submitting.value = true

    if (editingId.value) {
      store.updateProject(editingId.value, form.value)
      ElMessage.success('项目更新成功')
    } else {
      store.addProject(form.value)
      ElMessage.success('项目创建成功')
    }

    dialogVisible.value = false
  } catch (error) {
    // 表单验证失败
  } finally {
    submitting.value = false
  }
}

// 删除项目
const handleDelete = async (row: ProjectTracking) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除项目「${row.projectName}」吗？此操作不可恢复！`,
      '删除确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' }
    )

    store.deleteProject(row.id)
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 查看详情
const handleView = (row: ProjectTracking) => {
  viewingProject.value = row
  detailDialogVisible.value = true
}

// 查看跟踪记录
const handleViewTracking = (row: ProjectTracking) => {
  viewingProject.value = row
  trackingDialogVisible.value = true
}

// 添加跟踪记录
const handleAddTracking = async () => {
  if (!trackingFormRef.value || !viewingProject.value) return

  try {
    await trackingFormRef.value.validate()

    const now = new Date().toLocaleString('zh-CN')
    store.addTrackingRecord(viewingProject.value.id, {
      timestamp: now,
      status: trackingForm.value.status,
      content: trackingForm.value.content,
      operator: trackingForm.value.operator || '系统'
    })

    // 更新项目状态
    store.updateProject(viewingProject.value.id, { status: trackingForm.value.status })

    ElMessage.success('跟踪记录已添加')
    trackingForm.value = { status: '跟踪中', content: '', operator: '' }

    // 刷新详情
    viewingProject.value = store.getProject(viewingProject.value.id) || null
  } catch (error) {
    // 表单验证失败
  }
}

// 格式化金额
const formatMoney = (amount: number): string => {
  if (amount >= 10000) {
    return (amount / 10000).toFixed(1) + '万'
  }
  return amount.toLocaleString()
}

// 搜索
const handleSearch = () => {
  ElMessage.success('搜索已触发')
}

// 重置
const handleReset = () => {
  searchForm.value = {
    keyword: '',
    status: '',
    projectType: '',
    rateRange: ''
  }
  ElMessage.info('已重置搜索条件')
}

// 获取状态标签类型
const getStatusType = (status: ProjectStatus) => statusTypeMap[status] || 'info'
</script>

<template>
  <div class="project-tracking-page">
    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #409eff;"><Tickets /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.total }}</span>
              <span class="stat-label">项目总数</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #67c23a;"><Clock /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.byStatus['跟踪中'] + stats.byStatus['报名'] }}</span>
              <span class="stat-label">进行中</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #e6a23c;"><Money /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ formatMoney(stats.totalBudget) }}</span>
              <span class="stat-label">预算总额</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <el-icon class="stat-icon" style="color: #f56c6c;"><SuccessFilled /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.byStatus['中标'] }}</span>
              <span class="stat-label">已中标</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏 -->
    <el-card shadow="never" class="search-card">
      <el-form :model="searchForm" inline>
        <el-form-item label="关键词">
          <el-input v-model="searchForm.keyword" placeholder="项目名称/编号/客户" clearable style="width: 180px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
            <el-option v-for="opt in statusOptions" :key="opt" :label="opt" :value="opt" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="searchForm.projectType" placeholder="全部" clearable style="width: 100px">
            <el-option v-for="opt in typeOptions" :key="opt" :label="opt" :value="opt" />
          </el-select>
        </el-form-item>
        <el-form-item label="成功率">
          <el-select v-model="searchForm.rateRange" placeholder="全部" clearable style="width: 120px">
            <el-option label="高(>60%)" value="high" />
            <el-option label="中(30-60%)" value="medium" />
            <el-option label="低(<30%)" value="low" />
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
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>新增项目
      </el-button>
    </div>

    <!-- 数据表格 -->
    <el-card shadow="never" class="table-card">
      <el-table :data="filteredProjects" stripe style="width: 100%" v-loading="false">
        <el-table-column prop="projectNo" label="项目编号" width="130" />
        <el-table-column prop="projectName" label="项目名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="projectType" label="类型" width="80" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="row.projectType === '投标' ? 'success' : row.projectType === '招标' ? 'warning' : 'info'">
              {{ row.projectType }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="clientName" label="客户" width="150" show-overflow-tooltip />
        <el-table-column prop="budget" label="预算" width="100" align="right">
          <template #default="{ row }">
            ¥{{ formatMoney(row.budget) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="getStatusType(row.status)">
              {{ row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="成功率" width="120" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="getSuccessRateTagType(getSuccessRate(row))" :style="{ backgroundColor: getSuccessRateColor(getSuccessRate(row)), borderColor: getSuccessRateColor(getSuccessRate(row)), color: '#fff' }">
              {{ getSuccessRate(row) }}%
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>详情
            </el-button>
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
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="800px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-divider content-position="left">基本信息</el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="项目名称" prop="projectName">
              <el-input v-model="form.projectName" placeholder="请输入项目名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目类型" prop="projectType">
              <el-select v-model="form.projectType" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt" :label="opt" :value="opt" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">客户信息</el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="客户名称" prop="clientName">
              <el-input v-model="form.clientName" placeholder="请输入客户名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系人">
              <el-input v-model="form.clientContact" placeholder="请输入联系人" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="联系电话">
              <el-input v-model="form.clientPhone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目地点">
              <el-input v-model="form.location" placeholder="请输入项目地点" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">财务信息</el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="项目预算" prop="budget">
              <el-input-number v-model="form.budget" :min="0" :precision="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="投标金额">
              <el-input-number v-model="form.bidAmount" :min="0" :precision="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">时间节点</el-divider>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="报名截止">
              <el-date-picker v-model="form.registrationDeadline" type="date" placeholder="选择日期" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="投标截止">
              <el-date-picker v-model="form.bidDeadline" type="date" placeholder="选择日期" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="开标日期">
              <el-date-picker v-model="form.openingDate" type="date" placeholder="选择日期" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">项目详情</el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="招标文件">
              <el-select v-model="form.documentStatus" style="width: 100%">
                <el-option v-for="opt in documentStatusOptions" :key="opt" :label="opt" :value="opt" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="项目状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="opt in statusOptions" :key="opt" :label="opt" :value="opt" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="项目描述">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="请输入项目描述" />
        </el-form-item>
        <el-form-item label="详细需求">
          <el-input v-model="form.requirements" type="textarea" :rows="2" placeholder="请输入详细需求" />
        </el-form-item>

        <el-divider content-position="left">成功率评分</el-divider>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="竞争对手数量">
              <el-input-number v-model="form.competitorCount" :min="0" :max="20" style="width: 100%" />
              <div class="score-hint">越少分数越高</div>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="历史合作关系">
              <el-slider v-model="form.historicalCooperation" :min="0" :max="100" show-input />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="关系资源">
              <el-slider v-model="form.relationshipResources" :min="0" :max="100" show-input />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="时间充足度">
              <el-slider v-model="form.timeAdequacy" :min="0" :max="100" show-input />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="技术难度">
              <el-slider v-model="form.technicalDifficulty" :min="0" :max="100" show-input />
              <div class="score-hint">越低分数越高</div>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="预算合理性">
              <el-slider v-model="form.budgetReasonableness" :min="0" :max="100" show-input />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="项目详情" width="900px">
      <template v-if="viewingProject">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="项目编号">{{ viewingProject.projectNo }}</el-descriptions-item>
          <el-descriptions-item label="项目名称">{{ viewingProject.projectName }}</el-descriptions-item>
          <el-descriptions-item label="项目类型">
            <el-tag size="small" :type="viewingProject.projectType === '投标' ? 'success' : viewingProject.projectType === '招标' ? 'warning' : 'info'">
              {{ viewingProject.projectType }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="项目状态">
            <el-tag size="small" :type="getStatusType(viewingProject.status)">{{ viewingProject.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="客户名称">{{ viewingProject.clientName }}</el-descriptions-item>
          <el-descriptions-item label="联系人">{{ viewingProject.clientContact || '-' }}</el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ viewingProject.clientPhone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="项目地点">{{ viewingProject.location || '-' }}</el-descriptions-item>
          <el-descriptions-item label="项目预算">¥{{ viewingProject.budget.toLocaleString() }}</el-descriptions-item>
          <el-descriptions-item label="投标金额">
            {{ viewingProject.bidAmount > 0 ? '¥' + viewingProject.bidAmount.toLocaleString() : '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="报名截止">{{ viewingProject.registrationDeadline || '-' }}</el-descriptions-item>
          <el-descriptions-item label="投标截止">{{ viewingProject.bidDeadline || '-' }}</el-descriptions-item>
          <el-descriptions-item label="开标日期">{{ viewingProject.openingDate || '-' }}</el-descriptions-item>
          <el-descriptions-item label="招标文件">
            <el-tag size="small">{{ viewingProject.documentStatus }}</el-tag>
          </el-descriptions-item>
        </el-descriptions>

        <el-divider content-position="left">成功率分析</el-divider>
        <el-row :gutter="20">
          <el-col :span="8">
            <div class="rate-item">
              <span class="rate-label">综合成功率</span>
              <span class="rate-value" :style="{ color: getSuccessRateColor(getSuccessRate(viewingProject)) }">
                {{ getSuccessRate(viewingProject) }}%
              </span>
            </div>
          </el-col>
          <el-col :span="16">
            <el-row :gutter="10">
              <el-col :span="8">
                <div class="score-item">
                  <span class="score-label">竞争对手</span>
                  <el-progress :percentage="Math.max(0, 100 - (viewingProject.competitorCount - 1) * 15)" :color="getSuccessRateColor" />
                </div>
              </el-col>
              <el-col :span="8">
                <div class="score-item">
                  <span class="score-label">历史合作</span>
                  <el-progress :percentage="viewingProject.historicalCooperation" :color="getSuccessRateColor" />
                </div>
              </el-col>
              <el-col :span="8">
                <div class="score-item">
                  <span class="score-label">关系资源</span>
                  <el-progress :percentage="viewingProject.relationshipResources" :color="getSuccessRateColor" />
                </div>
              </el-col>
            </el-row>
          </el-col>
        </el-row>

        <el-divider content-position="left">项目描述</el-divider>
        <div class="description-text">
          {{ viewingProject.description || '暂无描述' }}
        </div>

        <el-divider content-position="left">详细需求</el-divider>
        <div class="description-text">
          {{ viewingProject.requirements || '暂无需求说明' }}
        </div>

        <el-divider content-position="left">
          跟踪记录
          <el-button type="primary" size="small" style="margin-left: 12px" @click="handleViewTracking(viewingProject)">
            添加记录
          </el-button>
        </el-divider>
        <el-timeline>
          <el-timeline-item
            v-for="record in viewingProject.trackingRecords.slice().reverse()"
            :key="record.id"
            :timestamp="record.timestamp"
            placement="top"
          >
            <el-card shadow="hover">
              <el-tag size="small" :type="getStatusType(record.status)" style="margin-right: 8px">
                {{ record.status }}
              </el-tag>
              {{ record.content }}
              <div class="record-operator">操作人: {{ record.operator }}</div>
            </el-card>
          </el-timeline-item>
        </el-timeline>
      </template>

      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 添加跟踪记录对话框 -->
    <el-dialog v-model="trackingDialogVisible" title="添加跟踪记录" width="500px">
      <el-form ref="trackingFormRef" :model="trackingForm" label-width="80px">
        <el-form-item label="状态" prop="status">
          <el-select v-model="trackingForm.status" style="width: 100%">
            <el-option v-for="opt in statusOptions" :key="opt" :label="opt" :value="opt" />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人" prop="operator">
          <el-input v-model="trackingForm.operator" placeholder="请输入操作人姓名" />
        </el-form-item>
        <el-form-item label="记录内容" prop="content">
          <el-input v-model="trackingForm.content" type="textarea" :rows="4" placeholder="请输入跟踪记录内容" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="trackingDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleAddTracking">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.project-tracking-page {
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

.score-hint {
  font-size: 12px;
  color: #909399;
  line-height: 1.2;
  margin-top: 4px;
}

.rate-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 20px;
  background: #f5f7fa;
  border-radius: 8px;
}

.rate-label {
  font-size: 14px;
  color: #606266;
  margin-bottom: 8px;
}

.rate-value {
  font-size: 36px;
  font-weight: 700;
}

.score-item {
  padding: 8px;
}

.score-label {
  font-size: 12px;
  color: #606266;
  display: block;
  margin-bottom: 4px;
}

.description-text {
  color: #606266;
  line-height: 1.6;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 4px;
}

.record-operator {
  font-size: 12px;
  color: #909399;
  margin-top: 8px;
}
</style>
