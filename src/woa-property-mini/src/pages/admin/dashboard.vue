<template>
  <view class="dashboard">
    <ProjectTabBar />
    
    <!-- 运营看板 -->
    <view class="header">
      <text class="title">运营看板</text>
      <text class="date">{{ today }}</text>
    </view>

    <!-- 核心指标 -->
    <view class="stats-grid">
      <view class="stat-card stat-primary">
        <text class="num">{{ stats.todayNew }}</text>
        <text class="label">今日新增</text>
      </view>
      <view class="stat-card stat-warning">
        <text class="num">{{ stats.pending }}</text>
        <text class="label">待处理</text>
      </view>
      <view class="stat-card stat-info">
        <text class="num">{{ stats.processing }}</text>
        <text class="label">处理中</text>
      </view>
      <view class="stat-card stat-success">
        <text class="num">{{ stats.completed }}</text>
        <text class="label">已完成</text>
      </view>
      <view class="stat-card stat-danger">
        <text class="num">{{ stats.timeout }}</text>
        <text class="label">超时工单</text>
      </view>
      <view class="stat-card stat-secondary">
        <text class="num">{{ stats.satisfaction }}</text>
        <text class="label">满意度</text>
      </view>
    </view>

    <!-- 快捷入口 -->
    <view class="section">
      <view class="section-title">快捷操作</view>
      <view class="quick-actions">
        <view class="action-item" @click="goTickets('New')">
          <view class="icon bg-blue">新</view>
          <text class="name">新工单</text>
        </view>
        <view class="action-item" @click="goTickets('Processing')">
          <view class="icon bg-orange">处</view>
          <text class="name">处理中</text>
        </view>
        <view class="action-item" @click="goAlerts">
          <view class="icon bg-red">超</view>
          <text class="name">超时告警</text>
        </view>
        <view class="action-item" @click="goTransfers">
          <view class="icon bg-purple">转</view>
          <text class="name">转单审批</text>
        </view>
      </view>
    </view>

    <!-- 工单类型分布 -->
    <view class="section">
      <view class="section-title">工单类型分布</view>
      <view class="type-list">
        <view v-for="item in typeStats" :key="item.name" class="type-item">
          <text class="name">{{ item.name }}</text>
          <view class="bar-bg">
            <view class="bar" :style="{ width: item.percent + '%', background: item.color }"></view>
          </view>
          <text class="count">{{ item.count }}</text>
        </view>
      </view>
    </view>

    <!-- 工程师排行 -->
    <view class="section">
      <view class="section-title">工程师排行（本月）</view>
      <view class="rank-list">
        <view v-for="(item, idx) in engineerRank" :key="item.id" class="rank-item">
          <text :class="['rank', { gold: idx === 0, silver: idx === 1, bronze: idx === 2 }]">{{ idx + 1 }}</text>
          <text class="name">{{ item.name }}</text>
          <text class="score">{{ item.completed }}单</text>
          <text class="rating">{{ item.rating }}分</text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref, onMounted  } from 'vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import dispatchApi from '@/api/dispatch'
import ticketApi from '@/api/ticket'
import ProjectTabBar from '@/components/ProjectTabBar.vue'

const today = new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' })

const stats = ref({
  todayNew: 0,
  pending: 0,
  processing: 0,
  completed: 0,
  timeout: 0,
  satisfaction: '0'
})

const typeStats = ref([])
const engineerRank = ref([])

function goTickets(status) {
  uni.navigateTo({ url: `/pages/admin/tickets?status=${status}` })
}

function goAlerts() {
  uni.navigateTo({ url: '/pages/admin/alerts' })
}

function goTransfers() {
  uni.navigateTo({ url: '/pages/admin/tickets?type=transfer' })
}

// 转换工单状态显示
function getStatusText(status) {
  const map = { 'New': '新工单', 'Created': '已创建', 'Open': '已派单', 'Processing': '处理中', 'Closed': '已完成' }
  return map[status] || status
}

// 格式化时间
function formatTime(time) {
  if (!time) return ''
  const d = new Date(time)
  return `${d.getMonth()+1}/${d.getDate()} ${d.getHours().toString().padStart(2,'0')}:${d.getMinutes().toString().padStart(2,'0')}`
}

onMounted(async () => {
  try {
    // 获取工单列表用于统计
    const res = await ticketApi.getMyTickets(1, 100)
    if (res.success && res.data) {
      const list = res.data.items || res.data || []
      const todayStart = new Date()
      todayStart.setHours(0, 0, 0, 0)
      
      // 今日新增
      stats.value.todayNew = list.filter(t => new Date(t.createdAt) >= todayStart).length
      
      // 状态分布
      const statusCounts = {}
      for (const t of list) {
        statusCounts[t.status] = (statusCounts[t.status] || 0) + 1
      }
      stats.value.pending = (statusCounts['New'] || 0) + (statusCounts['Created'] || 0)
      stats.value.processing = (statusCounts['Open'] || 0) + (statusCounts['Processing'] || 0)
      stats.value.completed = (statusCounts['Closed'] || 0)
      
      // 工单类型分布
      const typeCounts = {}
      for (const t of list) {
        const typeName = t.ticketTypeName || t.category || '其他'
        typeCounts[typeName] = (typeCounts[typeName] || 0) + 1
      }
      const total = list.length || 1
      const colors = ['#409EFF', '#67C23A', '#E6A23C', '#909399', '#F56C6C', '#909399']
      typeStats.value = Object.entries(typeCounts)
        .map(([name, count], idx) => ({
          name,
          count,
          percent: Math.round(count / total * 100),
          color: colors[idx % colors.length]
        }))
        .sort((a, b) => b.count - a.count)
    }
  } catch (e) {
    console.log('load stats error', e)
  }
  
  // 获取评价统计
  try {
    const ratingRes = await dispatchApi.getRatingStats(1)
    if (ratingRes.success && ratingRes.data) {
      stats.value.satisfaction = ratingRes.data.avgOverall?.toFixed(1) || '0'
    }
  } catch (e) {
    console.log('load rating error', e)
  }
})
</script>

<style scoped>
.dashboard {
  min-height: 100vh;
  background: #f5f5f5;
  padding: 12px;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.title {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}
.date {
  font-size: 12px;
  color: #909399;
}
.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
  margin-bottom: 16px;
}
.stat-card {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px 12px;
  text-align: center;
}
.stat-card .num {
  display: block;
  font-size: 24px;
  font-weight: 600;
  margin-bottom: 4px;
}
.stat-card .label {
  font-size: 12px;
  color: #606266;
}
.stat-primary .num { color: #409EFF; }
.stat-warning .num { color: #faad14; }
.stat-info .num { color: #1890ff; }
.stat-success .num { color: #52c41a; }
.stat-danger .num { color: #F56C6C; }
.stat-secondary .num { color: #909399; }
.section {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
}
.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 16px;
}
.quick-actions {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
}
.action-item {
  text-align: center;
}
.action-item .icon {
  width: 40px;
  height: 40px;
  border-radius: 8px;
  color: #ffffff;
  font-size: 14px;
  line-height: 40px;
  margin: 0 auto 8px;
}
.bg-blue { background: #409EFF; }
.bg-orange { background: #faad14; }
.bg-red { background: #F56C6C; }
.bg-purple { background: #9c27b0; }
.action-item .name {
  font-size: 12px;
  color: #606266;
}
.type-list { }
.type-item {
  display: flex;
  align-items: center;
  margin-bottom: 12px;
}
.type-item .name {
  width: 50px;
  font-size: 13px;
  color: #606266;
}
.bar-bg {
  flex: 1;
  height: 8px;
  background: #ebeef5;
  border-radius: 4px;
  margin: 0 10px;
}
.bar {
  height: 100%;
  border-radius: 4px;
}
.type-item .count {
  width: 40px;
  font-size: 13px;
  color: #303133;
  text-align: right;
}
.rank-list { }
.rank-item {
  display: flex;
  align-items: center;
  padding: 10px 0;
  border-bottom: 1px solid #ebeef5;
}
.rank-item:last-child { border-bottom: none; }
.rank {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #dcdfe6;
  color: #ffffff;
  font-size: 12px;
  text-align: center;
  line-height: 20px;
  margin-right: 10px;
}
.rank.gold { background: #ffd700; }
.rank.silver { background: #c0c0c0; }
.rank.bronze { background: #cd7f32; }
.rank-item .name {
  flex: 1;
  font-size: 14px;
  color: #303133;
}
.rank-item .score {
  font-size: 13px;
  color: #606266;
  margin-right: 16px;
}
.rank-item .rating {
  font-size: 13px;
  color: #409EFF;
  font-weight: 500;
}
</style>