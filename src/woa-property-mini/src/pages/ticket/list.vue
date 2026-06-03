<template>
  <view class="engineer-index">
    <!-- 标签页 -->
    <view class="tabs">
      <view 
        v-for="tab in tabs" 
        :key="tab.key"
        :class="['tab', { active: activeTab === tab.key }]"
        @click="switchTab(tab.key)"
      >
        {{ tab.name }}
        <text v-if="tab.count > 0" class="badge">{{ tab.count }}</text>
      </view>
    </view>

    <!-- 今日统计 -->
    <view class="stats">
      <view class="stat-item">
        <text class="num">{{ stats.todayNew }}</text>
        <text class="label">今日新增</text>
      </view>
      <view class="stat-item">
        <text class="num">{{ stats.pending }}</text>
        <text class="label">待处理</text>
      </view>
      <view class="stat-item">
        <text class="num">{{ stats.completed }}</text>
        <text class="label">已完成</text>
      </view>
      <view class="stat-item">
        <text class="num">{{ stats.score }}</text>
        <text class="label">评价分</text>
      </view>
    </view>

    <!-- 工单列表 -->
    <scroll-view scroll-y class="list" @scrolltolower="loadMore">
      <view v-if="activeTab === 'transfers' && transferLoading && transferList.length === 0" class="empty">加载中...</view>
      <view v-else-if="activeTab === 'transfers' && transferList.length === 0" class="empty">暂无待接受的转单</view>
      <view v-else-if="loading && list.length === 0" class="empty">加载中...</view>
      <view v-else-if="list.length === 0" class="empty">暂无工单</view>
      <view 
        v-for="item in (activeTab === 'transfers' ? transferList : list)" 
        :key="item.id" 
        class="ticket-card"
        @click="goDetail(item)"
      >
        <view class="card-header">
          <text class="ticket-code">{{ item.ticketCode }}</text>
          <text :class="['status', 'status-' + item.status.toLowerCase()]">
            {{ getStatusText(item.status) }}
          </text>
        </view>
        <view class="card-body">
          <view class="info-row">
            <text class="label">位置：</text>
            <text class="value">{{ formatLocation(item) }}</text>
          </view>
          <view class="info-row">
            <text class="label">派单时间：</text>
            <text class="value">{{ formatTime(item.dispatchTime) }}</text>
          </view>
          <view v-if="item.source || item.escalationLevel" class="info-row">
            <text class="label">来源：</text>
            <view class="source-tags">
              <text v-if="item.source === 'Escalation'" class="esc-tag">升级</text>
              <text v-if="item.escalationLevel" :class="['esc-level', 'esc-' + item.escalationLevel.toLowerCase()]">{{ item.escalationLevel }}</text>
              <text v-else class="value">{{ item.source }}</text>
            </view>
          </view>
        </view>
        <view class="card-footer">
          <template v-if="activeTab === 'transfers'">
            <button class="btn btn-success" size="mini" @click.stop="handleAccept(item)">接受</button>
            <button class="btn btn-default" size="mini" @click.stop="handleReject(item)">拒绝</button>
          </template>
          <template v-else-if="activeTab === 'pending'">
            <button class="btn btn-success" size="mini" @click.stop="handleReceive(item)">接单</button>
            <button class="btn btn-default" size="mini" @click.stop="handleTransfer(item)">转单</button>
          </template>
          <template v-else-if="activeTab === 'received'">
            <button class="btn btn-primary" size="mini" @click.stop="goDetail(item)">完工</button>
          </template>
        </view>
      </view>
      <view v-if="loading && list.length > 0" class="loading">加载中...</view>
      <view v-if="noMore && list.length > 0" class="no-more">没有更多了</view>
    </scroll-view>
  </view>
  <ProjectTabBar />
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import dispatchApi from '@/api/dispatch'
import ProjectTabBar from '@/components/ProjectTabBar.vue'
let ws = null

const tabs = [
  { key: 'pending', name: '待接收', count: 0 },
  { key: 'received', name: '已接收', count: 0 },
  { key: 'completed', name: '已完成', count: 0 },
  { key: 'transfers', name: '待接受', count: 0 }
]

const activeTab = ref('pending')
const loading = ref(false)
const list = ref([])
const page = ref(1)
const pageSize = 20
const transferList = ref([])
const transferLoading = ref(false)
const { start, stop } = useAutoRefresh()
const noMore = ref(false)

const stats = ref({
  todayNew: 0,
  pending: 0,
  completed: 0,
  score: '5.0'
})

const statusMap = {
  Pending: '待接收',
  WReceived: '已接收', 
  Completed: '已完工',
  Confirmed: '已确认',
  Transferred: '已转单'
}

function getStatusText(status) {
  return statusMap[status] || status
}

function formatTime(time) {
  if (!time) return '-'
  // UTC 时间转本地时间（+8小时，中国时区）
  const date = new Date(time)
  if (isNaN(date.getTime())) return time.slice(0, 16)
  // 北京时间 = UTC + 8小时
  const localTime = new Date(date.getTime() + 8 * 60 * 60 * 1000)
  const pad = n => n.toString().padStart(2, '0')
  return `${localTime.getFullYear()}-${pad(localTime.getMonth()+1)}-${pad(localTime.getDate())} ${pad(localTime.getHours())}:${pad(localTime.getMinutes())}`
}

function formatLocation(item) {
  const parts = [item.areaName, item.buildingName, item.roomName].filter(Boolean)
  return parts.length > 0 ? parts.join(' / ') : '-'
}

function switchTab(key) {
  activeTab.value = key
  page.value = 1
  noMore.value = false
  list.value = []
  if (key === 'transfers') {
    loadTransferList()
  } else {
    loadData()
  }
}

async function loadTransferList() {
  if (transferLoading.value) return
  transferLoading.value = true
  try {
    const personId = uni.getStorageSync('personId') || 0
    const res = await dispatchApi.getMyPendingTransfers(personId)
    if (res.success) {
      transferList.value = (res.data || []).map(d => ({
        id: d.ticketId,
        ticketCode: d.ticketCode,
        status: d.status,
        dispatchStatus: d.status,
        toPersonName: d.toPersonName,
        sourceType: 'Transfer',
        dispatchRecordId: d.id,
        reason: d.reason || ''
      }))
    }
  } catch (e) {
    console.error('加载转单列表失败', e)
  } finally {
    transferLoading.value = false
  }
}

async function loadData() {
  if (loading.value) return
  loading.value = true
  
  try {
    const res = await ticketApi.getAssignedTickets(page.value, pageSize)
    console.log('接单页面 loadData res:', JSON.stringify(res))
    if (res.success) {
      let data = res.data || []
      
      console.log('接单页面 activeTab=', activeTab.value, '原始data=', data.map(d => ({id:d.id, status:d.status, dispatchStatus:d.dispatchStatus})))
      
      if (activeTab.value === 'received') {
        data = data.filter(d => d.dispatchStatus === 'Accepted')
      } else if (activeTab.value === 'completed') {
        data = data.filter(d => d.dispatchStatus === 'InProgress' || d.dispatchStatus === 'Finished')
      } else {
        data = data.filter(d => d.dispatchStatus === 'Dispatched')
      }
      
      if (page.value === 1) {
        list.value = data
      } else {
        list.value = [...list.value, ...data]
      }
      
      // 更新统计
      stats.value.pending = res.data.filter(d => d.dispatchStatus === 'Dispatched').length
      stats.value.todayNew = stats.value.pending
      
      noMore.value = data.length < pageSize
    }
  } catch (e) {
    console.log('load error', e)
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

function loadMore() {
  if (!noMore.value && !loading.value) {
    page.value++
    loadData()
  }
}

function goDetail(item) {
  uni.navigateTo({ url: `/pages/ticket/detail?id=${item.id}&ticketCode=${item.ticketCode}&ticketId=${item.ticketId}` })
}

function handleReceive(item) {
  uni.showModal({
    title: '确认接单',
    content: `确定接收工单 ${item.ticketCode}？`,
    success: async (res) => {
      if (res.confirm) {
        try {
          const result = await ticketApi.acceptTicket(item.id)
          if (result.success) {
            uni.showToast({ title: '接单成功', icon: 'success' })
            loadData()
          } else {
            uni.showToast({ title: result.message || '接单失败', icon: 'none' })
          }
        } catch (e) {
          uni.showToast({ title: '接单失败', icon: 'none' })
        }
      }
    }
  })
}

function handleTransfer(item) {
  // 传递 dispatchRecordId（工单详情中的当前派单ID）
  uni.navigateTo({ url: `/pages/ticket/transfer?dispatchRecordId=${item.dispatchRecordId || item.id}&ticketCode=${item.ticketCode}&fromPerson=${item.toPersonName}` })
}

async function handleAccept(item) {
  try {
    const res = await dispatchApi.acceptDispatch(item.dispatchRecordId)
    if (res.success) {
      uni.showToast({ title: '已接受工单', icon: 'success' })
      loadTransferList()
    } else {
      uni.showToast({ title: res.message || '接受失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '接受失败', icon: 'none' })
  }
}

async function handleReject(item) {
  try {
    const res = await dispatchApi.rejectDispatch(item.dispatchRecordId, '暂时无法接单')
    if (res.success) {
      uni.showToast({ title: '已拒绝', icon: 'success' })
      loadTransferList()
    } else {
      uni.showToast({ title: res.message || '拒绝失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '拒绝失败', icon: 'none' })
  }
}

onMounted(() => {
  // 连接 WebSocket 监听工单更新
  const app = getApp()
  if (app && app.globalData && app.globalData.ws) {
    ws = app.globalData.ws
    ws.connect({
      onTicketStatusChanged: (data) => {
        console.log('[Engineer] Ticket status changed:', data.ticketCode, '->', data.currentStatus)
        const idx = list.value.findIndex(item => item.id === data.ticketId)
        if (idx > -1) {
          list.value[idx] = { ...list.value[idx], status: data.currentStatus }
          uni.showToast({ title: `工单 ${data.ticketCode} 已更新`, icon: 'none' })
        }
      }
    })
  }
  loadData()
  
  // 监听项目切换，刷新工单列表
  uni.$on('projectChanged', (project) => {
    page.value = 1
    loadData()
  })
  
  start(async () => { page.value = 1; await loadData(); }, 10000)
})

onUnmounted(() => {
  // 保持连接不断
})

function handleTicketUpdated(data) {
  console.log('[Ticket List] Ticket updated:', data)
  // 更新对应的工单
  const index = list.value.findIndex(item => item.ticketId === data.ticketId)
  if (index > -1) {
    list.value[index] = { ...list.value[index], ...data }
    // 如果状态变化导致不在当前 tab，从列表移除
    if (activeTab.value === 'pending' && data.status !== 'Pending') {
      list.value.splice(index, 1)
    } else if (activeTab.value === 'received' && data.status !== 'WReceived') {
      list.value.splice(index, 1)
    } else if (activeTab.value === 'completed' && data.status !== 'Completed' && data.status !== 'Confirmed') {
      list.value.splice(index, 1)
    }
  }
  uni.showToast({ 
    title: `工单 ${data.ticketCode} 已更新`, 
    icon: 'none',
    duration: 2000
  })
}
</script>

<style scoped>
.engineer-index {
  min-height: 100vh;
  background: #f5f5f5;
}
.tabs {
  display: flex;
  background: #ffffff;
  padding: 0 16px;
  position: sticky;
  top: 0;
  z-index: 10;
}
.tab {
  flex: 1;
  text-align: center;
  padding: 16px 0;
  font-size: 14px;
  color: #606266;
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
  width: 40px;
  height: 3px;
  background: #409EFF;
  border-radius: 2px;
}
.badge {
  display: inline-block;
  background: #F56C6C;
  color: #ffffff;
  font-size: 10px;
  padding: 2px 6px;
  border-radius: 10px;
  margin-left: 4px;
}
.stats {
  display: flex;
  background: #ffffff;
  margin: 12px;
  border-radius: 8px;
  padding: 16px;
}
.stat-item {
  flex: 1;
  text-align: center;
}
.stat-item .num {
  display: block;
  font-size: 20px;
  font-weight: 600;
  color: #409EFF;
}
.stat-item .label {
  font-size: 12px;
  color: #909399;
}
.list {
  padding: 0 12px 12px;
  height: calc(100vh - 200px);
}
.ticket-card {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.ticket-code {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}
.status {
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 4px;
}
.status-pending { background: #fff7e6; color: #faad14; }
.status-wreceived { background: #e6f7ff; color: #1890ff; }
.status-completed { background: #f6ffed; color: #52c41a; }
.status-confirmed { background: #f5f5f5; color: #909399; }
.card-body { margin-bottom: 12px; }
.info-row {
  display: flex;
  font-size: 13px;
  color: #606266;
  margin-bottom: 6px;
}
.label { width: 70px; flex-shrink: 0; }
.value { color: #303133; }
.card-footer {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}
.btn {
  border: none;
  font-size: 13px;
  padding: 6px 14px;
  border-radius: 4px;
}
.btn-success { background: #67C23A; color: #ffffff; }
.btn-primary { background: #409EFF; color: #ffffff; }
.btn-default { background: #f5f5f5; color: #606266; }
.empty, .loading, .no-more {
  text-align: center;
  color: #909399;
  font-size: 14px;
  padding: 40px 0;
}
.source-tags {
  display: flex;
  align-items: center;
  gap: 6px;
}
.esc-tag {
  background: #E6A23C;
  color: #ffffff;
  font-size: 10px;
  padding: 2px 6px;
  border-radius: 4px;
}
.esc-level {
  font-size: 10px;
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: bold;
}
.esc-l1, .esc-l2 { background: #E6A23C; color: #ffffff; }
.esc-l3, .esc-l4 { background: #F56C6C; color: #ffffff; }
</style>