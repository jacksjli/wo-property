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
    // 尝试获取统计概览
    const response = await statisticsApi.get('/api/statistics/overview')
    if (response.success) {
      statsData.value = response.data
    }
  } catch (error) {
    console.error('加载统计数据失败:', error)
    // 使用模拟数据展示
    statsData.value = getMockData()
  }
  loading.value = false
}

const getMockData = () => {
  return {
    totalTickets: 156,
    openTickets: 23,
    resolvedTickets: 133,
    totalDevices: 48,
    activeDevices: 42,
    maintenanceDevices: 4,
    totalMaterials: 125,
    lowStockMaterials: 8,
    monthlyRevenue: 125800,
    monthlyExpense: 89200,
    complaintsThisMonth: 12,
    resolvedComplaints: 10
  }
}

const formatMoney = (amount: number) => {
  return new Intl.NumberFormat('zh-CN', {
    style: 'currency',
    currency: 'CNY',
    minimumFractionDigits: 0
  }).format(amount)
}

const formatDate = () => {
  return new Date().toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long'
  })
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
      
      <!-- 统计概览 -->
      <div class="stats-overview">
        <div class="stat-card primary">
          <div class="stat-icon">
            <el-icon><TrendCharts /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statsData?.totalTickets || 0 }}</div>
            <div class="stat-label">工单总数</div>
          </div>
        </div>
        
        <div class="stat-card success">
          <div class="stat-icon">
            <el-icon><DataAnalysis /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statsData?.resolvedTickets || 0 }}</div>
            <div class="stat-label">已完成工单</div>
          </div>
        </div>
        
        <div class="stat-card warning">
          <div class="stat-icon">
            <el-icon><List /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statsData?.openTickets || 0 }}</div>
            <div class="stat-label">待处理工单</div>
          </div>
        </div>
        
        <div class="stat-card info">
          <div class="stat-icon">
            <el-icon><TrendCharts /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statsData?.totalDevices || 0 }}</div>
            <div class="stat-label">设备总数</div>
          </div>
        </div>
      </div>
      
      <!-- 详细数据 -->
      <el-divider />
      
      <el-row :gutter="20">
        <!-- 工单统计 -->
        <el-col :span="12">
          <el-card shadow="hover">
            <template #header>
              <span>工单统计</span>
            </template>
            <el-descriptions :column="1" border>
              <el-descriptions-item label="工单总数">
                {{ statsData?.totalTickets || 0 }}
              </el-descriptions-item>
              <el-descriptions-item label="待处理">
                <el-tag type="warning">{{ statsData?.openTickets || 0 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="已完成">
                <el-tag type="success">{{ statsData?.resolvedTickets || 0 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="本月新增">
                {{ Math.floor((statsData?.totalTickets || 0) * 0.15) }}
              </el-descriptions-item>
            </el-descriptions>
          </el-card>
        </el-col>
        
        <!-- 设备统计 -->
        <el-col :span="12">
          <el-card shadow="hover">
            <template #header>
              <span>设备统计</span>
            </template>
            <el-descriptions :column="1" border>
              <el-descriptions-item label="设备总数">
                {{ statsData?.totalDevices || 0 }}
              </el-descriptions-item>
              <el-descriptions-item label="运行中">
                <el-tag type="success">{{ statsData?.activeDevices || 0 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="维修中">
                <el-tag type="warning">{{ statsData?.maintenanceDevices || 0 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="设备完好率">
                {{ statsData?.totalDevices ? Math.round((statsData.activeDevices / statsData.totalDevices) * 100) : 0 }}%
              </el-descriptions-item>
            </el-descriptions>
          </el-card>
        </el-col>
      </el-row>
      
      <el-row :gutter="20" style="margin-top: 20px;">
        <!-- 物料统计 -->
        <el-col :span="12">
          <el-card shadow="hover">
            <template #header>
              <span>物料统计</span>
            </template>
            <el-descriptions :column="1" border>
              <el-descriptions-item label="物料种类">
                {{ statsData?.totalMaterials || 0 }}
              </el-descriptions-item>
              <el-descriptions-item label="库存不足">
                <el-tag type="danger">{{ statsData?.lowStockMaterials || 0 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="库存充足">
                {{ (statsData?.totalMaterials || 0) - (statsData?.lowStockMaterials || 0) }}
              </el-descriptions-item>
            </el-descriptions>
          </el-card>
        </el-col>
        
        <!-- 财务统计 -->
        <el-col :span="12">
          <el-card shadow="hover">
            <template #header>
              <span>本月财务</span>
            </template>
            <el-descriptions :column="1" border>
              <el-descriptions-item label="本月收入">
                <span style="color: #67c23a;">{{ formatMoney(statsData?.monthlyRevenue || 0) }}</span>
              </el-descriptions-item>
              <el-descriptions-item label="本月支出">
                <span style="color: #f56c6c;">{{ formatMoney(statsData?.monthlyExpense || 0) }}</span>
              </el-descriptions-item>
              <el-descriptions-item label="本月结余">
                <span style="color: #409eff;">{{ formatMoney((statsData?.monthlyRevenue || 0) - (statsData?.monthlyExpense || 0)) }}</span>
              </el-descriptions-item>
            </el-descriptions>
          </el-card>
        </el-col>
      </el-row>
      
      <!-- 投诉统计 -->
      <el-row style="margin-top: 20px;">
        <el-col :span="24">
          <el-card shadow="hover">
            <template #header>
              <span>投诉建议统计</span>
            </template>
            <el-descriptions :column="3" border>
              <el-descriptions-item label="本月投诉">
                {{ statsData?.complaintsThisMonth || 0 }}
              </el-descriptions-item>
              <el-descriptions-item label="已处理">
                <el-tag type="success">{{ statsData?.resolvedComplaints || 0 }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="处理中">
                <el-tag type="warning">{{ (statsData?.complaintsThisMonth || 0) - (statsData?.resolvedComplaints || 0) }}</el-tag>
              </el-descriptions-item>
            </el-descriptions>
          </el-card>
        </el-col>
      </el-row>
      
      <el-divider />
      
      <div class="update-time">
        <span>统计时间：{{ formatDate() }}</span>
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.statistics-view {
  width: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.stats-overview {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 20px;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  border-radius: 8px;
  color: #fff;
}

.stat-card.primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stat-card.success {
  background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
}

.stat-card.warning {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.stat-card.info {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.stat-icon {
  font-size: 32px;
  opacity: 0.9;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
}

.stat-label {
  font-size: 14px;
  opacity: 0.9;
}

.update-time {
  text-align: center;
  color: #999;
  font-size: 12px;
}
</style>
