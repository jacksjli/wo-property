<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { statisticsApi } from '../api/http'
import { ElCard, ElRow, ElCol, ElTag, ElTable, ElTableColumn, ElDrawer, ElCheckboxGroup, ElCheckbox, ElButton, ElIcon, ElSelect, ElOption } from 'element-plus'
import { House, User, Ticket, Warning, Avatar, Key, Bell, Setting, TrendCharts, Money, Refresh } from '@element-plus/icons-vue'
import { use } from 'echarts/core'
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, LegendComponent, GridComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import VChart from 'vue-echarts'

use([BarChart, LineChart, PieChart, TitleComponent, TooltipComponent, LegendComponent, GridComponent, CanvasRenderer])

const loading = ref(false)
const drawerVisible = ref(false)

// 图表类型
const ticketChartType = ref<'bar' | 'line' | 'pie'>('bar')
const feeChartType = ref<'bar' | 'line'>('bar')

// 工单趋势图配置
const ticketChartOption = computed(() => {
  const months = ticketTrendData.value.map(d => d.month)
  const counts = ticketTrendData.value.map(d => d.count)

  if (ticketChartType.value === 'line') {
    return {
      tooltip: { trigger: 'axis' },
      grid: { left: '3%', right: '4%', bottom: '10%', containLabel: true },
      xAxis: { type: 'category', data: months },
      yAxis: { type: 'value', name: '工单数' },
      series: [{
        name: '工单数',
        type: 'line',
        data: counts,
        itemStyle: { color: '#409eff' },
        smooth: true,
        areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: 'rgba(64,158,255,0.3)' }, { offset: 1, color: 'rgba(64,158,255,0.05)' }] } }
      }]
    }
  } else if (ticketChartType.value === 'pie') {
    return {
      tooltip: { trigger: 'item' },
      legend: { bottom: 0 },
      series: [{
        name: '工单数',
        type: 'pie',
        radius: ['40%', '70%'],
        itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
        label: { show: true, formatter: '{b}: {c}' },
        data: ticketTrendData.value.map((d, i) => ({
          name: d.month,
          value: d.count,
          itemStyle: { color: ['#409eff', '#67c23a', '#e6a23c', '#f56c6c', '#909399', '#00bcd4'][i % 6] }
        }))
      }]
    }
  }
  // 默认 bar
  return {
    tooltip: { trigger: 'axis' },
    grid: { left: '3%', right: '4%', bottom: '10%', containLabel: true },
    xAxis: { type: 'category', data: months },
    yAxis: { type: 'value', name: '工单数' },
    series: [{
      name: '工单数',
      type: 'bar',
      data: counts,
      itemStyle: { color: '#409eff' },
      barWidth: '40%'
    }]
  }
})

// 收费趋势图配置
const feeChartOption = computed(() => {
  const months = feeTrendData.value.map(d => d.month)
  const amounts = feeTrendData.value.map(d => d.amount / 10000)

  if (feeChartType.value === 'line') {
    return {
      tooltip: { trigger: 'axis', formatter: (params: any) => `${params[0].name}<br/>实收: ${params[0].value}万` },
      grid: { left: '3%', right: '4%', bottom: '10%', containLabel: true },
      xAxis: { type: 'category', data: months },
      yAxis: { type: 'value', name: '金额(万)' },
      series: [{
        name: '实收金额',
        type: 'line',
        data: amounts,
        itemStyle: { color: '#67c23a' },
        smooth: true,
        areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: 'rgba(103,194,58,0.3)' }, { offset: 1, color: 'rgba(103,194,58,0.05)' }] } }
      }]
    }
  }
  // 默认 bar
  return {
    tooltip: { trigger: 'axis', formatter: (params: any) => `${params[0].name}<br/>实收: ${params[0].value}万` },
    grid: { left: '3%', right: '4%', bottom: '10%', containLabel: true },
    xAxis: { type: 'category', data: months },
    yAxis: { type: 'value', name: '金额(万)' },
    series: [{
      name: '实收金额',
      type: 'bar',
      data: amounts,
      itemStyle: { color: '#67c23a' },
      barWidth: '40%'
    }]
  }
})

// 模块可见性配置
const defaultModules = {
  statCards: true,
  ticketTrend: true,
  feeTrend: true,
  serviceStatus: true,
  recentActivity: true,
}

// 从 localStorage 恢复用户配置
const getStoredConfig = () => {
  try {
    const stored = localStorage.getItem('dashboard-modules')
    if (stored) {
      return { ...defaultModules, ...JSON.parse(stored) }
    }
  } catch (e) {
    console.error('恢复仪表盘配置失败:', e)
  }
  return { ...defaultModules }
}

const moduleConfig = reactive(getStoredConfig())

const moduleOptions = [
  { label: '统计卡片区（8个卡片）', value: 'statCards' },
  { label: '工单趋势图', value: 'ticketTrend' },
  { label: '收费月度趋势图', value: 'feeTrend' },
  { label: '服务状态', value: 'serviceStatus' },
  { label: '最近活动', value: 'recentActivity' },
]

const saveConfig = () => {
  localStorage.setItem('dashboard-modules', JSON.stringify({ ...moduleConfig }))
  drawerVisible.value = false
}

const resetConfig = () => {
  Object.assign(moduleConfig, defaultModules)
  localStorage.removeItem('dashboard-modules')
}

const stats = ref({
  totalProperties: 500,
  occupancyRate: 92.5,
  activeTickets: 15,
  pendingComplaints: 8,
  todayVisitors: 23,
  unreadNotifications: 5,
  availableKeys: 45,
  totalKeys: 50
})

const recentActivities = ref([
  { time: '10:30', action: '新增工单 #TK-2026-0045', user: '王先生', type: 'ticket' },
  { time: '10:15', action: '访客 张先生 已签到', user: '门禁', type: 'visitor' },
  { time: '09:45', action: '投诉已解决: 电梯故障', user: '张师傅', type: 'complaint' },
  { time: '09:30', action: '物料出库: 灯泡 x50', user: '李仓管', type: 'material' },
  { time: '09:00', action: '巡检任务完成: A栋', user: '巡检员', type: 'inspection' },
])

const serviceStatus = ref([
  { name: '认证服务', port: 5006, status: 'running' },
  { name: '物料服务', port: 5004, status: 'running' },
  { name: '通知服务', port: 5005, status: 'running' },
  { name: '合同服务', port: 5008, status: 'running' },
  { name: '财务服务', port: 5009, status: 'running' },
  { name: '巡检服务', port: 5010, status: 'running' },
  { name: '投诉服务', port: 5011, status: 'running' },
  { name: '钥匙服务', port: 5012, status: 'running' },
  { name: '访客服务', port: 5013, status: 'running' },
  { name: '统计服务', port: 5014, status: 'running' },
  { name: '移动服务', port: 5015, status: 'running' },
])

// 工单趋势图数据（模拟）
const ticketTrendData = ref([
  { month: '1月', count: 42 },
  { month: '2月', count: 38 },
  { month: '3月', count: 55 },
  { month: '4月', count: 48 },
  { month: '5月', count: 63 },
  { month: '6月', count: 71 },
])

// 收费月度趋势数据（模拟）
const feeTrendData = ref([
  { month: '1月', amount: 128000 },
  { month: '2月', amount: 135000 },
  { month: '3月', amount: 142000 },
  { month: '4月', amount: 138000 },
  { month: '5月', amount: 156000 },
  { month: '6月', amount: 168000 },
])

// 计算工单趋势图最大高度
const maxTicketCount = computed(() => Math.max(...ticketTrendData.value.map(d => d.count)))
const maxFeeAmount = computed(() => Math.max(...feeTrendData.value.map(d => d.amount)))

const getBarHeight = (count: number, max: number) => {
  return `${(count / max) * 100}%`
}

const getFeeBarHeight = (amount: number, max: number) => {
  return `${(amount / max) * 100}%`
}

const formatFee = (amount: number) => {
  return (amount / 10000).toFixed(1)
}

const getStatusType = (status: string) => {
  return status === 'running' ? 'success' : 'danger'
}

onMounted(async () => {
  loading.value = true
  try {
    const res = await statisticsApi.get('/api/metrics/indicators')
    if (res.success && res.data) {
      stats.value = {
        totalProperties: res.data.operation?.totalUnits || 500,
        occupancyRate: res.data.operation?.occupancyRate || 92.5,
        activeTickets: res.data.customer?.totalTickets || 15,
        pendingComplaints: res.data.customer?.totalComplaints || 8,
        todayVisitors: res.data.visitor?.totalVisitors || 23,
        unreadNotifications: 5,
        availableKeys: 48,
        totalKeys: 50
      }
    }
  } catch (error) {
    console.error('获取数据失败:', error)
  }
  loading.value = false
})
</script>

<template>
  <div class="dashboard">
    <div class="page-header">
      <h2 class="page-title">物业管理后台</h2>
      <el-button type="primary" plain @click="drawerVisible = true">
        <el-icon class="el-icon--left"><Setting /></el-icon>
        仪表盘设置
      </el-button>
    </div>

    <!-- 统计卡片 -->
    <el-row v-if="moduleConfig.statCards" :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #409eff;">
            <el-icon size="30"><House /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.totalProperties }}</div>
            <div class="stat-label">物业单元总数</div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #67c23a;">
            <el-icon size="30"><User /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.occupancyRate }}%</div>
            <div class="stat-label">入住率</div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #e6a23c;">
            <el-icon size="30"><Tickets /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.activeTickets }}</div>
            <div class="stat-label">活跃工单</div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #f56c6c;">
            <el-icon size="30"><Warning /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.pendingComplaints }}</div>
            <div class="stat-label">待处理投诉</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row v-if="moduleConfig.statCards" :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #909399;">
            <el-icon size="30"><Avatar /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.todayVisitors }}</div>
            <div class="stat-label">今日访客</div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #00bcd4;">
            <el-icon size="30"><Key /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.availableKeys }}/{{ stats.totalKeys }}</div>
            <div class="stat-label">可用钥匙</div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #8bc34a;">
            <el-icon size="30"><Bell /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.unreadNotifications }}</div>
            <div class="stat-label">未读通知</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 工单趋势图 & 收费月度趋势图 -->
    <el-row v-if="moduleConfig.ticketTrend || moduleConfig.feeTrend" :gutter="20" class="stats-row chart-row">
      <el-col v-if="moduleConfig.ticketTrend" :span="12">
        <el-card shadow="hover">
          <template #header>
            <div class="card-header">
              <span><el-icon class="el-icon--left"><TrendCharts /></el-icon>工单趋势（近6月）</span>
              <el-select v-model="ticketChartType" size="small" style="width: 100px">
                <el-option value="bar" label="柱状图" />
                <el-option value="line" label="折线图" />
                <el-option value="pie" label="饼图" />
              </el-select>
            </div>
          </template>
          <v-chart :option="ticketChartOption" style="height: 280px" autoresize />
        </el-card>
      </el-col>

      <el-col v-if="moduleConfig.feeTrend" :span="12">
        <el-card shadow="hover">
          <template #header>
            <div class="card-header">
              <span><el-icon class="el-icon--left"><Money /></el-icon>收费月度趋势（万元）</span>
              <el-select v-model="feeChartType" size="small" style="width: 100px">
                <el-option value="bar" label="柱状图" />
                <el-option value="line" label="折线图" />
              </el-select>
            </div>
          </template>
          <v-chart :option="feeChartOption" style="height: 280px" autoresize />
        </el-card>
      </el-col>
    </el-row>

    <!-- 服务状态和活动 -->
    <el-row v-if="moduleConfig.serviceStatus || moduleConfig.recentActivity" :gutter="20">
      <el-col v-if="moduleConfig.serviceStatus" :span="14">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>服务状态</span>
              <el-tag type="success" size="small">11个服务运行中</el-tag>
            </div>
          </template>
          <el-table :data="serviceStatus" size="small">
            <el-table-column prop="name" label="服务" width="150" />
            <el-table-column prop="port" label="端口" width="80" />
            <el-table-column label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="getStatusType(row.status)" size="small">
                  {{ row.status === 'running' ? '运行中' : '异常' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="健康检查">
              <template #default="{ row }">
                <el-icon color="#67c23a" v-if="row.status === 'running'"><CircleCheck /></el-icon>
                <el-icon color="#f56c6c" v-else><CircleClose /></el-icon>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <el-col v-if="moduleConfig.recentActivity" :span="moduleConfig.serviceStatus ? 10 : 24">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>最近活动</span>
            </div>
          </template>
          <div class="activity-list">
            <div v-for="(activity, index) in recentActivities" :key="index" class="activity-item">
              <div class="activity-time">{{ activity.time }}</div>
              <div class="activity-content">
                <span class="activity-text">{{ activity.action }}</span>
                <span class="activity-user">by {{ activity.user }}</span>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 仪表盘设置抽屉 -->
    <el-drawer v-model="drawerVisible" title="仪表盘设置" direction="rtl" size="320px">
      <div class="drawer-content">
        <p class="drawer-hint">选择要在首页显示的模块</p>
        <div class="module-list">
          <div
            v-for="opt in moduleOptions"
            :key="opt.value"
            class="module-item"
            @click="moduleConfig[opt.value as keyof typeof moduleConfig] = !moduleConfig[opt.value as keyof typeof moduleConfig]"
          >
            <el-checkbox
              :model-value="moduleConfig[opt.value as keyof typeof moduleConfig]"
              @click.stop
              @change="moduleConfig[opt.value as keyof typeof moduleConfig] = !moduleConfig[opt.value as keyof typeof moduleConfig]"
            />
            <span class="module-label">{{ opt.label }}</span>
          </div>
        </div>

        <div class="drawer-actions">
          <el-button type="primary" @click="saveConfig">保存</el-button>
          <el-button @click="resetConfig">恢复默认</el-button>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<style scoped>
.dashboard {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-title {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.stats-row {
  margin-bottom: 20px;
}

.chart-row {
  margin-bottom: 20px;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 10px;
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  margin-right: 15px;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-top: 5px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

/* 柱状图样式 */
.bar-chart {
  display: flex;
  align-items: flex-end;
  justify-content: space-around;
  height: 180px;
  padding: 10px 0;
}

.bar-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
}

.bar-wrapper {
  height: 150px;
  width: 100%;
  display: flex;
  align-items: flex-end;
  justify-content: center;
}

.bar {
  width: 60%;
  max-width: 50px;
  background: linear-gradient(180deg, #409eff 0%, #66b1ff 100%);
  border-radius: 4px 4px 0 0;
  position: relative;
  transition: height 0.3s ease;
  min-height: 20px;
}

.bar.fee-bar {
  background: linear-gradient(180deg, #67c23a 0%, #85ce61 100%);
}

.bar-value {
  position: absolute;
  top: -24px;
  left: 50%;
  transform: translateX(-50%);
  font-size: 12px;
  font-weight: bold;
  color: #303133;
}

.bar-label {
  margin-top: 8px;
  font-size: 12px;
  color: #909399;
}

.activity-list {
  max-height: 300px;
  overflow-y: auto;
}

.activity-item {
  display: flex;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}

.activity-item:last-child {
  border-bottom: none;
}

.activity-time {
  width: 60px;
  color: #909399;
  font-size: 13px;
}

.activity-content {
  flex: 1;
}

.activity-text {
  display: block;
  color: #303133;
  font-size: 14px;
}

.activity-user {
  display: block;
  color: #c0c4cc;
  font-size: 12px;
  margin-top: 4px;
}

/* 抽屉样式 */
.drawer-content {
  padding: 0 10px;
}

.drawer-hint {
  color: #909399;
  font-size: 14px;
  margin-bottom: 20px;
}

.module-checkbox {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
  width: 100%;
}

.module-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.module-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.2s;
}

.module-item:hover {
  background: #f5f7fa;
}

.module-label {
  font-size: 14px;
  color: #303133;
}

.drawer-actions {
  margin-top: 30px;
  display: flex;
  gap: 10px;
}
</style>
