<template>
  <view class="mine-page">
    
    <!-- 用户信息 -->
    <view class="user-card">
      <view class="avatar">
        <text class="avatar-text">{{ userInfo.name?.charAt(0) || 'U' }}</text>
      </view>
      <view class="user-info">
        <text class="name">{{ userInfo.name || '用户' }}</text>
        <text class="role">{{ userInfo.role || '维修人员' }}</text>
      </view>
    </view>

    <!-- 今日统计 -->
    <view class="stats-section">
      <view class="stat-item">
        <text class="stat-num">{{ stats.pendingCount }}</text>
        <text class="stat-label">待处理</text>
      </view>
      <view class="stat-item">
        <text class="stat-num">{{ stats.completedCount }}</text>
        <text class="stat-label">已完成</text>
      </view>
      <view class="stat-item">
        <text class="stat-num">{{ stats.totalScore }}</text>
        <text class="stat-label">评价总分</text>
      </view>
    </view>

    <!-- 工单列表 -->
    <view class="section">
      <view class="section-title">我的工单</view>
      <view v-if="loading && list.length === 0" class="empty">加载中...</view>
      <view v-else-if="list.length === 0" class="empty">暂无工单</view>
      <view 
        v-for="item in list" 
        :key="item.id" 
        class="ticket-card"
        @click="goDetail(item)"
      >
        <view class="card-header">
          <text class="ticket-code">{{ item.ticketCode }}</text>
          <text :class="['status', 'status-' + getStatusKey(item.status)]">
            {{ getStatusText(item.status) }}
          </text>
        </view>
        <view class="card-body">
          <view class="info-row">
            <text class="label">位置：</text>
            <text class="value">{{ formatLocation(item) }}</text>
          </view>
          <view class="info-row">
            <text class="label">工单类型：</text>
            <text class="value">{{ item.category || '-' }}</text>
          </view>
          <view class="info-row" v-if="item.contactPersonName || item.contactPhone">
            <text class="label">联系人：</text>
            <text class="value">{{ item.contactPersonName || '' }}{{ item.contactPhone ? ' ' + item.contactPhone : '' }}</text>
          </view>
          <view class="info-row">
            <text class="label">提交时间：</text>
            <text class="value">{{ formatTime(item.createdAt) }}</text>
          </view>
        </view>
        <view class="card-footer">
          <button class="btn btn-transfer" size="mini" @click.stop="goTransfer(item)">转单</button>
          <button 
            v-if="item.status === 'InProgress'" 
            class="btn btn-finish" 
            size="mini" 
            :disabled="processingId === item.id"
            :loading="processingId === item.id"
            @click.stop="handleFinish(item)"
          >
            {{ processingId === item.id ? '完成中...' : '完成' }}
          </button>
          <button class="btn btn-detail" size="mini" @click.stop="goDetail(item)">详情</button>
        </view>
      </view>
      <view v-if="loading && list.length > 0" class="loading">加载中...</view>
      <view v-if="noMore && list.length > 0" class="no-more">没有更多了</view>
    </view>

    <!-- 评价统计 -->
    <view class="section" v-if="ratingStats">
      <view class="section-title">评价统计</view>
      <view class="rating-info">
        <view class="rating-item">
          <text class="rating-label">服务数量</text>
          <text class="rating-value">{{ ratingStats.totalCount }}</text>
        </view>
        <view class="rating-item">
          <text class="rating-label">平均总体评分</text>
          <text class="rating-value">{{ ratingStats.avgOverall?.toFixed(1) || '-' }}</text>
        </view>
        <view class="rating-item">
          <text class="rating-label">平均服务质量</text>
          <text class="rating-value">{{ ratingStats.avgQuality?.toFixed(1) || '-' }}</text>
        </view>
        <view class="rating-item">
          <text class="rating-label">平均服务态度</text>
          <text class="rating-value">{{ ratingStats.avgAttitude?.toFixed(1) || '-' }}</text>
        </view>
        <view class="rating-item">
          <text class="rating-label">平均及时性</text>
          <text class="rating-value">{{ ratingStats.avgTimeliness?.toFixed(1) || '-' }}</text>
        </view>
      </view>
    </view>

    <!-- 功能菜单 -->
    <view class="menu-section">
      <view class="menu-item" @click="goTransfers">
        <text class="menu-text">我的转单</text>
        <text class="menu-arrow">›</text>
      </view>
      <view class="menu-item" @click="goAlerts">
        <text class="menu-text">超时告警</text>
        <text class="menu-arrow">›</text>
      </view>
      <view class="menu-item">
        <text class="menu-text">设置</text>
        <text class="menu-arrow">›</text>
      </view>
    </view>
  </view>
  <ProjectTabBar />
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import ProjectTabBar from '@/components/ProjectTabBar.vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import ticketApi from '@/api/ticket'

const userInfo = ref({
  name: uni.getStorageSync('userInfo')?.name || '维修人员',
  role: uni.getStorageSync('userInfo')?.role || '维修技师'
})

const loading = ref(false)
const list = ref([])
const page = ref(1)
const pageSize = 20
const { start, stop } = useAutoRefresh()
const noMore = ref(false)
const processingId = ref(null)

const stats = ref({
  pendingCount: 0,
  completedCount: 0,
  totalScore: 0
})

const ratingStats = ref(null)

function getStatusText(status) {
  const map = {
    'New': '新工单',
    'Dispatched': '已派单',
    'Accepted': '已接单',
    'InProgress': '处理中',
    'Completed': '已完成',
    'Closed': '已关闭'
  }
  return map[status] || status || '新工单'
}

function getStatusKey(status) {
  return (status || 'New').toLowerCase()
}

function formatTime(time) {
  if (!time) return '-'
  const date = new Date(time)
  if (isNaN(date.getTime())) return time.slice(0, 16)
  const localTime = new Date(date.getTime() + 8 * 60 * 60 * 1000)
  const pad = n => n.toString().padStart(2, '0')
  return `${localTime.getFullYear()}-${pad(localTime.getMonth()+1)}-${pad(localTime.getDate())} ${pad(localTime.getHours())}:${pad(localTime.getMinutes())}`
}

function formatLocation(item) {
  const parts = [item.areaName, item.buildingName, item.roomName].filter(Boolean)
  return parts.length > 0 ? parts.join(' / ') : item.location || '-'
}

async function loadData() {
  if (loading.value) return
  loading.value = true
  
  try {
    const res = await ticketApi.getAssignedTickets(page.value, pageSize)
    if (res.success && res.data) {
      const data = Array.isArray(res.data) ? res.data : res.data.list || []
      if (page.value === 1) {
        list.value = data
      } else {
        list.value = [...list.value, ...data]
      }
      noMore.value = data.length < pageSize
      
      // 更新统计数据
      stats.value.pendingCount = list.value.filter(t => 
        t.status === 'Dispatched' || t.status === 'Accepted' || t.status === 'InProgress'
      ).length
      stats.value.completedCount = list.value.filter(t => t.status === 'Completed' || t.status === 'Closed').length
    }
  } catch (e) {
    console.error('加载工单失败:', e)
  } finally {
    loading.value = false
  }
}

async function handleStart(item) {
  try {
    processingId.value = item.id
    item.status = 'InProgress'
    const res = await ticketApi.progressTicket(item.id)
    if (res.success) {
      uni.showToast({ title: '已开始处理', icon: 'success' })
      page.value = 1
      loadData()
    } else {
      item.status = 'Dispatched'
      uni.showToast({ title: res.message || '操作失败', icon: 'none' })
    }
  } catch (e) {
    item.status = 'Dispatched'
    uni.showToast({ title: '操作失败', icon: 'none' })
  } finally {
    processingId.value = null
  }
}

async function handleFinish(item) {
  try {
    processingId.value = item.id
    item.status = 'Completed'
    const res = await ticketApi.finishTicket(item.id)
    if (res.success) {
      uni.showToast({ title: '已完成', icon: 'success' })
      page.value = 1
      loadData()
    } else {
      item.status = 'InProgress'
      uni.showToast({ title: res.message || '操作失败', icon: 'none' })
    }
  } catch (e) {
    item.status = 'InProgress'
    uni.showToast({ title: '操作失败', icon: 'none' })
  } finally {
    processingId.value = null
  }
}

function goDetail(item) {
  uni.navigateTo({ url: `/pages/ticket/detail?id=${item.id}` })
}

function goTransfer(item) {
  uni.navigateTo({ url: `/pages/ticket/transfer?id=${item.id}&ticketCode=${item.ticketCode}` })
}

function goAlerts() {
  uni.navigateTo({ url: '/pages/admin/alerts' })
}

onMounted(() => {
  loadData()
  
  
  start(async () => { page.value = 1; await loadData(); }, 10000)
})

onUnmounted(() => {
  stop()
})
</script>

<style scoped>
.mine-page {
  min-height: 100vh;
  background: #f5f5f5;
}
.user-card {
  background: linear-gradient(135deg, #409EFF 0%, #66b1ff 100%);
  padding: 30px 20px;
  display: flex;
  align-items: center;
  color: #ffffff;
}
.avatar {
  width: 60px;
  height: 60px;
  border-radius: 30px;
  background: rgba(255,255,255,0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 16px;
}
.avatar-text {
  font-size: 24px;
  font-weight: 600;
}
.user-info {
  display: flex;
  flex-direction: column;
}
.name {
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 4px;
}
.role {
  font-size: 13px;
  opacity: 0.9;
}
.stats-section {
  display: flex;
  background: #ffffff;
  padding: 20px 0;
  margin-bottom: 12px;
}
.stat-item {
  flex: 1;
  text-align: center;
  display: flex;
  flex-direction: column;
}
.stat-num {
  font-size: 24px;
  font-weight: 600;
  color: #409EFF;
  margin-bottom: 4px;
}
.stat-label {
  font-size: 12px;
  color: #909399;
}
.section {
  background: #ffffff;
  padding: 16px;
  margin-bottom: 12px;
}
.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 12px;
}
.ticket-card {
  background: #f8f8f8;
  border-radius: 8px;
  padding: 12px;
  margin-bottom: 10px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
}
.ticket-code { font-size: 14px; font-weight: 600; color: #303133; }
.status {
  font-size: 11px;
  padding: 3px 8px;
  border-radius: 4px;
}
.status-new { background: #fff7e6; color: #faad14; }
.status-dispatched { background: #e6f7ff; color: #1890ff; }
.status-accepted { background: #e6f7ff; color: #1890ff; }
.status-inprogress { background: #fff3e0; color: #ff9800; }
.status-completed { background: #f0f9eb; color: #52c41a; }
.status-closed { background: #f5f5f5; color: #909399; }
.card-body { margin-bottom: 8px; }
.info-row { display: flex; font-size: 12px; color: #606266; margin-bottom: 4px; }
.label { width: 70px; flex-shrink: 0; }
.value { color: #303133; }
.card-footer { display: flex; gap: 8px; justify-content: flex-end; }
.btn { border: none; font-size: 12px; padding: 4px 12px; border-radius: 4px; }
.btn-transfer { background: #E6A23C; color: #ffffff; }
.btn-start { background: #409EFF; color: #ffffff; }
.btn-finish { background: #67C23A; color: #ffffff; }
.btn-detail { background: #f5f5f5; color: #606266; }
.empty, .loading, .no-more { text-align: center; color: #909399; font-size: 14px; padding: 20px 0; }
.rating-info {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.rating-item {
  display: flex;
  justify-content: space-between;
  font-size: 14px;
}
.rating-label { color: #606266; }
.rating-value { color: #303133; font-weight: 500; }
.menu-section {
  background: #ffffff;
}
.menu-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  border-bottom: 1px solid #ebeef5;
}
.menu-item:last-child {
  border-bottom: none;
}
.menu-text {
  font-size: 15px;
  color: #303133;
}
.menu-arrow {
  font-size: 18px;
  color: #dcdfe6;
}
</style>