<template>
  <view class="owner-index">
    <!-- 自定义 TabBar -->


    <!-- 标签页 -->
    <view class="tabs">
      <view
        v-for="tab in tabs"
        :key="tab.key"
        :class="['tab', { active: activeTab === tab.key }]"
        @click="switchTab(tab.key)"
      >
        {{ tab.name }}
      </view>
    </view>

    <!-- 快捷操作 -->
    <view class="quick-action" @click="goCreate">
      <text class="icon">+</text>
      <text class="text">新建报修</text>
    </view>

    <view class="divider"></view>

    <!-- 工单列表 -->
    <scroll-view scroll-y class="list" @scrolltolower="loadMore">
      <view v-if="loading && list.length === 0" class="empty">加载中...</view>
      <view v-else-if="list.length === 0" class="empty">
        <text class="empty-text">暂无工单</text>
      </view>
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
            <text class="label">工单类型:</text>
            <text class="value">{{ item.categoryName || item.category || '维修' }}</text>
          </view>
          <view class="info-row">
            <text class="label">报修时间:</text>
            <text class="value">{{ formatTime(item.createdAt) }}</text>
          </view>
          <view v-if="item.contactPersonName" class="info-row">
            <text class="label">联系人:</text>
            <text class="value">{{ item.contactPersonName }} {{ item.contactPhone }}</text>
          </view>
          <view v-if="item.description" class="info-row desc">
            <text class="value">{{ item.description }}</text>
          </view>
          <view v-if="item.assignedAt" class="info-row">
            <text class="label">处理时间:</text>
            <text class="value">{{ formatTime(item.assignedAt) }}</text>
          </view>
          <view v-if="item.finishedAt" class="info-row">
            <text class="label">完成时间:</text>
            <text class="value">{{ formatTime(item.finishedAt) }}</text>
          </view>
        </view>
        <view class="card-footer">
          <button v-if="item.status === 'Closed' || item.status === 'Finished'" class="btn btn-primary" size="mini" @click.stop="goRate(item)">去评价</button>
          <button v-if="item.status !== 'Closed'" class="btn btn-default" size="mini" @click.stop="handleRemind(item)">催单</button>
        </view>
      </view>
      <view v-if="loading && list.length > 0" class="loading">加载中...</view>
      <view v-if="noMore && list.length > 0" class="no-more">没有更多了</view>
      <view style="height: 120rpx;"><!-- 底部占位 --></view>
    </scroll-view>
    <ProjectTabBar />
  </view>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { getCurrentInstance } from 'vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import ticketApi from '@/api/ticket'
import ProjectTabBar from '@/components/ProjectTabBar.vue'
let ws = null

const tabs = [
  { key: 'all', name: '全部' },
  { key: 'Created', name: '新工单' },
  { key: 'Dispatched', name: '已派单' },
  { key: 'Accepted', name: '已接单' },
  { key: 'Processing', name: '处理中' },
  { key: 'Closed', name: '已完成' }
]

const activeTab = ref('all')
const loading = ref(false)
const list = ref([])
const page = ref(1)
const pageSize = 20
const { start, stop } = useAutoRefresh()
const noMore = ref(false)

onMounted(() => {
  checkLogin()
  
  // 监听项目切换，刷新工单
  uni.$on('ticketCreated', () => {
    page.value = 1
    loadTickets()
  })
  
  uni.$on('projectChanged', (project) => {
    page.value = 1
    loadTickets()
  })
  
  start(async () => { page.value = 1; await loadTickets(); }, 10000)

  // 连接 WebSocket 监听工单更新
  const app = getApp()
  if (app && app.globalData && app.globalData.ws) {
    ws = app.globalData.ws
    ws.connect({
      onTicketCreated: (data) => {
        console.log('[Owner Index] New ticket created:', data.ticketCode)
        uni.showToast({ title: `新工单: ${data.ticketCode}`, icon: 'none', duration: 3000 })
        // 刷新列表
        page.value = 1
        list.value = []
        loadTickets()
      },
      onTicketStatusChanged: (data) => {
        console.log('[Owner Index] Ticket status changed:', data.ticketCode, '->', data.currentStatus)
        // 找到对应的工单并更新
        const idx = list.value.findIndex(item => item.id === data.ticketId || item.ticketCode === data.ticketCode)
        if (idx > -1) {
          list.value[idx] = { ...list.value[idx], status: data.currentStatus }
          uni.showToast({ title: `工单 ${data.ticketCode} 状态更新: ${data.currentStatus}`, icon: 'none', duration: 2000 })
        }
      }
    })
  }
})

onUnmounted(() => {
  // 不主动断开连接,保持 WebSocket
})

function handleTicketUpdated(data) {
  console.log('[Owner Index] Ticket updated:', data)
  // 找到对应的工单并更新
  const index = list.value.findIndex(item => item.id === data.ticketId)
  if (index > -1) {
    // 更新现有工单
    list.value[index] = { ...list.value[index], ...data }
    // 如果状态变化为 Closed,从列表移除(可选)
    if (data.status === 'Closed') {
      list.value.splice(index, 1)
    }
  }
  // 显示更新提示
  uni.showToast({
    title: `工单 ${data.ticketCode} 已更新`,
    icon: 'none',
    duration: 2000
  })
}

function checkLogin() {
  const token = uni.getStorageSync('token')
  if (!token) {
    uni.switchTab({ url: '/pages/login/index' })
    return
  }
  // 确保 WebSocket 已连接
  if (false) {

  }
  loadTickets()
}

function getStatusText(status) {
  const map = {
    'Created': '新工单',
    'Dispatched': '已派单',
    'Accepted': '已接单',
    'Processing': '处理中',
    'Finished': '已完成',
    'Closed': '已关闭',
    'Survey_Pending': '待评价'
  }
  return map[status] || status || '新工单'
}

function getStatusKey(status) {
  return (status || 'Created').toLowerCase()
}

function formatTime(time) {
  if (!time) return '-'
  // UTC 时间转本地时间(+8小时,中国时区)
  const date = new Date(time)
  if (isNaN(date.getTime())) return time.slice(0, 16)
  // 北京时间 = UTC + 8小时
  const localTime = new Date(date.getTime() + 8 * 60 * 60 * 1000)
  const pad = n => n.toString().padStart(2, '0')
  return `${localTime.getFullYear()}-${pad(localTime.getMonth()+1)}-${pad(localTime.getDate())} ${pad(localTime.getHours())}:${pad(localTime.getMinutes())}`
}

function switchTab(key) {
  activeTab.value = key
  list.value = []
  page.value = 1
  noMore.value = false
  loadTickets()
}

async function loadTickets() {
  if (loading.value) return
  loading.value = true
  console.log('[Owner] loadTickets called, currentProject:', JSON.stringify(uni.getStorageSync('currentProject')))
  console.log('[Owner] token:', uni.getStorageSync('token')?.substring(0,20), 'personId:', uni.getStorageSync('personId'))

  try {
    const status = activeTab.value === 'all' ? null : activeTab.value
    const res = await ticketApi.getMyTickets(page.value, pageSize, status)
    console.log('[Owner] getMyTickets response:', JSON.stringify(res).substring(0,200))

    if (res.success) {
      if (page.value === 1) {
        list.value = res.data || []
      } else {
        list.value = [...list.value, ...(res.data || [])]
      }
      noMore.value = (res.data || []).length < pageSize
    }
  } catch (error) {
    console.error('加载工单失败:', error)
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

function loadMore() {
  if (noMore.value) return
  page.value++
  loadTickets()
}

function goCreate() {
  uni.navigateTo({ url: '/pages/owner/create' })
}

function goDetail(item) {
  uni.navigateTo({ url: `/pages/owner/detail?id=${item.id}` })
}

function goRate(item) {
  uni.navigateTo({ url: `/pages/owner/rate?id=${item.id}` })
}

async function handleRemind(item) {
  try {
    await ticketApi.remindTicket(item.id)
    uni.showToast({ title: '催单成功', icon: 'success' })
  } catch (error) {
    uni.showToast({ title: '催单失败', icon: 'none' })
  }
}
</script>

<style scoped>
.owner-index {
  min-height: 100vh;
  background: #f5f5f5;
}

.tabs {
  display: flex;
  background: #ffffff;
  padding: 0 24rpx;
  flex-wrap: wrap;
}

.tab {
  padding: 24rpx 32rpx;
  font-size: 28rpx;
  color: #909399;
  position: relative;
}

.tab.active {
  color: #409EFF;
  font-weight: 500;
}

.tab.active::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 48rpx;
  height: 4rpx;
  background: #409EFF;
  border-radius: 2rpx;
}

.quick-action {
  margin: 24rpx;
  padding: 32rpx;
  background: linear-gradient(135deg, #409EFF 0%, #337ecc 100%);
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4rpx 12rpx rgba(64, 158, 255, 0.3);
}

.divider {
  height: 1px;
  background: dashed #ebeef5;
  margin: 0 24rpx;
}

.quick-action .icon {
  width: 64rpx;
  height: 64rpx;
  background: #ffffff;
  border-radius: 32rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32rpx;
  color: #409EFF;
  margin-right: 16rpx;
}

.quick-action .text {
  color: #ffffff;
  font-size: 32rpx;
  font-weight: 500;
}

.list {
  padding: 0 24rpx;
  height: calc(100vh - 300rpx);
}

.ticket-card {
  background: #ffffff;
  border-radius: 16rpx;
  padding: 24rpx;
  margin-bottom: 24rpx;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16rpx;
}

.ticket-code {
  font-size: 28rpx;
  color: #303133;
  font-weight: 500;
}

.status {
  font-size: 24rpx;
  padding: 4rpx 16rpx;
  border-radius: 8rpx;
}

.status-created { background: #E6A23C; color: #ffffff; }
.status-dispatched { background: #909399; color: #ffffff; }
.status-accepted { background: #409EFF; color: #ffffff; }
.status-processing { background: #67C23A; color: #ffffff; }
.status-finished { background: #67C23A; color: #ffffff; }
.status-closed { background: #909399; color: #ffffff; }

.card-body .info-row {
  display: flex;
  font-size: 26rpx;
  color: #606266;
  margin-bottom: 8rpx;
}

.card-body .info-row .label {
  width: 140rpx;
}

.card-body .info-row.desc {
  margin-top: 8rpx;
  color: #303133;
}

.card-footer {
  display: flex;
  justify-content: flex-end;
  gap: 16rpx;
  margin-top: 16rpx;
}

.card-footer .btn {
  padding: 8rpx 24rpx;
  font-size: 24rpx;
  border-radius: 8rpx;
}

.card-footer .btn-primary {
  background: #409EFF;
  color: #ffffff;
}

.card-footer .btn-default {
  background: #ffffff;
  color: #909399;
  border: 1rpx solid #dcdfe6;
}

.empty {
  text-align: center;
  padding: 120rpx 0;
  color: #909399;
}

.empty-text {
  font-size: 28rpx;
  margin-bottom: 32rpx;
}

.btn-create {
  background: #409EFF;
  color: #ffffff;
  font-size: 28rpx;
  padding: 20rpx 60rpx;
  border-radius: 40rpx;
}

.loading, .no-more {
  text-align: center;
  padding: 24rpx;
  color: #909399;
  font-size: 24rpx;
}
</style>
