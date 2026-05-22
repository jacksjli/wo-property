<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { statisticsApi } from '../../api/http'
import { ElMessage } from 'element-plus'
import { TrendCharts, DataAnalysis, List } from '@element-plus/icons-vue'

const loading = ref(false)
const statsData = ref<any>(null)

const loadStatistics = async () => {
  loading.value = true
  try {
    const response = await statisticsApi.get('/api/metrics/indicators')
    if (response && response.operation) {
      // 后端返回的嵌套结构
      statsData.value = {
        totalTickets: response.customer?.tickets || 0,
        resolvedTickets: response.customer?.completedTickets || 0,
        openTickets: (response.customer?.tickets || 0) - (response.customer?.completedTickets || 0),
        totalProperties: response.operation?.totalProperties || 0,
        totalUnits: response.operation?.totalUnits || 0,
        occupancyRate: response.operation?.occupancyRate || 0,
        totalRevenue: response.financial?.totalRevenue || 0,
        propertyFeeRevenue: response.financial?.propertyFeeRevenue || 0,
        collectionRate: response.financial?.collectionRate || 0,
        totalExpense: response.financial?.totalExpense || 0,
        totalComplaints: response.customer?.complaints || 0,
        resolvedComplaints: response.customer?.resolvedComplaints || 0,
        complaintResolveRate: response.customer?.complaintResolveRate || 0,
        totalInspections: response.inspection?.totalInspections || 0,
        passedInspections: response.inspection?.passedInspections || 0,
        issuesFound: response.inspection?.issuesFound || 0,
        issuesResolved: response.inspection?.resolvedIssues || 0,
        totalVisitors: response.visitor?.totalVisitors || 0,
        activeVisitors: response.visitor?.activeVisitors || 0,
      }
    } else {
      statsData.value = getMockData()
    }
  } catch (error) {
    console.error('加载统计数据失败:', error)
    statsData.value = getMockData()
  }
  loading.value = false
}

const getMockData = () => {
  return {
    totalTickets: 156,
    openTickets: 23,
    resolvedTickets: 133,
    totalProperties: 5,
    totalUnits: 1200,
    occupancyRate: 87.5,
    totalRevenue: 125800,
    propertyFeeRevenue: 98000,
    collectionRate: 92.3,
    totalExpense: 89200,
    totalComplaints: 12,
    resolvedComplaints: 10,
    complaintResolveRate: 83.3,
    totalInspections: 45,
    passedInspections: 42,
    issuesFound: 18,
    issuesResolved: 15,
    totalVisitors: 89,
    activeVisitors: 12
  }
}

const formatMoney = (amount: number) => {
  return new Intl.NumberFormat('zh-CN', {
    style: 'currency',
    currency: 'CNY',
    minimumFractionDigits: 0
  }).format(amount)
}

const formatPercent = (val: number) => {
  return (val || 0).toFixed(1) + '%'
}

const formatNumber = (val: number) => {
  return (val || 0).toLocaleString('zh-CN')
}

onMounted(() => {
  loadStatistics()
})
</script>

<template>
  <div class="statistics-view">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>数据统计</span>
          <el-button @click="loadStatistics" :loading="loading">
            刷新数据
          </el-button>
        </div>
      </template>

      <!-- 工单统计 -->
      <el-row :gutter="16" class="stat-row">
        <el-col :span="6">
          <div class="stat-card primary">
            <div class="stat-icon"><el-icon><TrendCharts /></el-icon></div>
            <div class="stat-info">
              <div class="stat-value">{{ formatNumber(statsData?.totalTickets) }}</div>
              <div class="stat-label">工单总数</div>
            </div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-card success">
            <div class="stat-icon"><el-icon><DataAnalysis /></el-icon></div>
            <div class="stat-info">
              <div class="stat-value">{{ formatNumber(statsData?.resolvedTickets) }}</div>
              <div class="stat-label">已完成工单</div>
            </div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-card warning">
            <div class="stat-icon"><el-icon><List /></el-icon></div>
            <div class="stat-info">
              <div class="stat-value">{{ formatNumber(statsData?.openTickets) }}</div>
              <div class="stat-label">进行中工单</div>
            </div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-card danger">
            <div class="stat-info">
              <div class="stat-value">{{ formatPercent(statsData?.complaintResolveRate) }}</div>
              <div class="stat-label">投诉解决率</div>
            </div>
          </div>
        </el-col>
      </el-row>

      <!-- 财务统计 -->
      <el-divider content-position="left">财务概况</el-divider>
      <el-row :gutter="16" class="stat-row">
        <el-col :span="6">
          <div class="stat-card info">
            <div class="stat-info">
              <div class="stat-value">{{ formatMoney(statsData?.totalRevenue) }}</div>
              <div class="stat-label">总收入</div>
            </div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-card info">
            <div class="stat-info">
              <div class="stat-value">{{ formatMoney(statsData?.propertyFeeRevenue) }}</div>
              <div class="stat-label">物业费收入</div>
            </div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-card success">
            <div class="stat-info">
              <div class="stat-value">{{ formatPercent(statsData?.collectionRate) }}</div>
              <div class="stat-label">收缴率</div>
            </div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-card danger">
            <div class="stat-info">
              <div class="stat-value">{{ formatMoney(statsData?.totalExpense) }}</div>
              <div class="stat-label">总支出</div>
            </div>
          </div>
        </el-col>
      </el-row>

      <!-- 物业概况 -->
      <el-divider content-position="left">物业概况</el-divider>
      <el-row :gutter="16" class="stat-row">
        <el-col :span="4">
          <div class="stat-mini">
            <div class="stat-value">{{ formatNumber(statsData?.totalProperties) }}</div>
            <div class="stat-label">物业总数</div>
          </div>
        </el-col>
        <el-col :span="4">
          <div class="stat-mini">
            <div class="stat-value">{{ formatNumber(statsData?.totalUnits) }}</div>
            <div class="stat-label">单元总数</div>
          </div>
        </el-col>
        <el-col :span="4">
          <div class="stat-mini">
            <div class="stat-value">{{ formatPercent(statsData?.occupancyRate) }}</div>
            <div class="stat-label">入住率</div>
          </div>
        </el-col>
        <el-col :span="4">
          <div class="stat-mini">
            <div class="stat-value">{{ formatNumber(statsData?.totalVisitors) }}</div>
            <div class="stat-label">访客总数</div>
          </div>
        </el-col>
        <el-col :span="4">
          <div class="stat-mini">
            <div class="stat-value">{{ formatNumber(statsData?.activeVisitors) }}</div>
            <div class="stat-label">在场访客</div>
          </div>
        </el-col>
        <el-col :span="4">
          <div class="stat-mini">
            <div class="stat-value">{{ formatNumber(statsData?.totalInspections) }}</div>
            <div class="stat-label">巡检总数</div>
          </div>
        </el-col>
      </el-row>

      <!-- 投诉统计 -->
      <el-divider content-position="left">客户投诉</el-divider>
      <el-row :gutter="16" class="stat-row">
        <el-col :span="8">
          <el-card shadow="hover">
            <div class="complaint-stat">
              <div class="stat-value text-primary">{{ formatNumber(statsData?.totalComplaints) }}</div>
              <div class="stat-label">本月投诉</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="8">
          <el-card shadow="hover">
            <div class="complaint-stat">
              <div class="stat-value text-success">{{ formatNumber(statsData?.resolvedComplaints) }}</div>
              <div class="stat-label">已解决</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="8">
          <el-card shadow="hover">
            <div class="complaint-stat">
              <div class="stat-value text-danger">{{ formatNumber(statsData?.totalComplaints - statsData?.resolvedComplaints) }}</div>
              <div class="stat-label">待处理</div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<style scoped>
.statistics-view { padding: 20px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.stat-row { margin-bottom: 20px; }
.stat-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  border-radius: 8px;
  background: #f5f7fa;
}
.stat-card.primary { background: linear-gradient(135deg, #409EFF 0%, #66b1ff 100%); color: #fff; }
.stat-card.success { background: linear-gradient(135deg, #67C23A 0%, #85ce61 100%); color: #fff; }
.stat-card.warning { background: linear-gradient(135deg, #E6A23C 0%, #ebb563 100%); color: #fff; }
.stat-card.danger { background: linear-gradient(135deg, #F56C6C 0%, #f78989 100%); color: #fff; }
.stat-card.info { background: linear-gradient(135deg, #909399 0%, #a6a9ad 100%); color: #fff; }
.stat-icon { font-size: 36px; opacity: 0.9; }
.stat-info { flex: 1; }
.stat-value { font-size: 28px; font-weight: bold; }
.stat-label { font-size: 14px; opacity: 0.85; margin-top: 4px; }
.stat-mini { text-align: center; padding: 12px; background: #f5f7fa; border-radius: 8px; }
.stat-mini .stat-value { font-size: 20px; font-weight: bold; color: #303133; }
.stat-mini .stat-label { font-size: 12px; color: #909399; margin-top: 4px; }
.complaint-stat { text-align: center; padding: 16px; }
.complaint-stat .stat-value { font-size: 32px; font-weight: bold; }
.text-primary { color: #409EFF; }
.text-success { color: #67C23A; }
.text-danger { color: #F56C6C; }
</style>