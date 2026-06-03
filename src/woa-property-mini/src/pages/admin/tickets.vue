<template>
  <view class="tickets-page">
    <ProjectTabBar />
    <!-- 筛选栏 -->
    <view class="filter-bar">
      <picker mode="selector" :range="areaOptions" range-key="name" @change="onAreaChange">
        <view class="filter-value">
          {{ currentAreaName || '选择区域' }}
          <text class="arrow">▼</text>
        </view>
      </picker>
      
      <picker mode="selector" :range="buildingOptions" range-key="name" @change="onBuildingChange">
        <view class="filter-value">
          {{ currentBuildingName || '全部楼栋' }}
          <text class="arrow">▼</text>
        </view>
      </picker>
      
      <picker mode="selector" :range="ticketTypeOptions" range-key="name" @change="onTicketTypeChange">
        <view class="filter-value">
          {{ currentTicketTypeName || '全部类型' }}
          <text class="arrow">▼</text>
        </view>
      </picker>
    </view>

    <!-- 工单列表 -->
    <scroll-view scroll-y class="list" @scrolltolower="loadMore">
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
            <text class="value">{{ item.categoryName || item.ticketTypeName || '-' }}</text>
          </view>
          <view class="info-row">
            <text class="label">提交时间：</text>
            <text class="value">{{ formatTime(item.createdAt) }}</text>
          </view>
          <view class="info-row">
            <text class="label">执行人：</text>
            <text class="value">{{ item.toPersonName || '待派单' }}</text>
          </view>
        </view>
        <view class="card-footer">
          <button v-if="item.status === 'New'" class="btn btn-primary" size="mini" @click.stop="handleDispatch(item)">派单</button>
          <button class="btn btn-default" size="mini" @click.stop="goDetail(item)">详情</button>
        </view>
      </view>
      <view v-if="loading && list.length > 0" class="loading">加载中...</view>
      <view v-if="noMore && list.length > 0" class="no-more">没有更多了</view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref, onMounted, onUnmounted, onActivated } from 'vue'
import { getCurrentInstance } from 'vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import ticketApi from '@/api/ticket'
import dispatchApi from '@/api/dispatch'
import ProjectTabBar from '@/components/ProjectTabBar.vue'

const loading = ref(false)
const list = ref([])
const page = ref(1)
const { start, stop } = useAutoRefresh()
const noMore = ref(false)
const silent = ref(false)
const pageSize = 20
let refreshTimer = null
let failCount = 0
const BASE_INTERVAL = 30000

// 筛选数据
const areaOptions = ref([])
const buildingOptions = ref([{ id: null, name: '全部楼栋' }])
const ticketTypeOptions = ref([{ id: null, name: '全部类型' }])

const currentAreaId = ref(null)
const currentBuildingId = ref(null)
const currentTicketTypeId = ref(null)
const currentAreaName = ref('')
const currentBuildingName = ref('全部楼栋')
const currentTicketTypeName = ref('全部类型')

function startAutoRefresh() {
  if (refreshTimer) return
  refreshTimer = setInterval(async () => {
    try {
      silent.value = true
      await loadData()
      failCount = 0
      silent.value = false
    } catch (e) {
      silent.value = false
      failCount++
      if (failCount >= 3) {
        clearInterval(refreshTimer)
        refreshTimer = null
      }
    }
  }, BASE_INTERVAL)
}

function stopAutoRefresh() {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
}

function getStatusText(status) {
  const map = {
    'New': '新工单',
    'Dispatched': '已派单',
    'Accepted': '已接单',
    'Processing': '处理中',
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
  return parts.length > 0 ? parts.join(' / ') : item.location || '-'
}

async function loadAreas() {
  try {
    const res = await ticketApi.getAreas()
    if (res.success && res.data) {
      // 添加"全部区域"选项
      areaOptions.value = [{ id: null, name: '全部区域' }, ...(res.data.map(a => ({ id: a.id, name: a.name })) || [])]
    }
  } catch (e) {
    console.error('加载区域失败:', e)
  }
}

async function loadBuildings() {
  try {
    const res = await ticketApi.getBuildings()
    if (res.success && res.data) {
      buildingOptions.value = [
        { id: null, name: '全部楼栋' },
        ...(res.data.map(b => ({ id: b.id, name: b.name })) || [])
      ]
    }
  } catch (e) {
    console.error('加载楼栋失败:', e)
  }
}

async function loadTicketTypes() {
  try {
    const res = await ticketApi.getTicketTypes()
    if (res.success && res.data) {
      ticketTypeOptions.value = [
        { id: null, name: '全部类型' },
        ...(res.data.map(t => ({ id: t.id, name: t.name })) || [])
      ]
    }
  } catch (e) {
    console.error('加载工单类型失败:', e)
  }
}

function onAreaChange(e) {
  const idx = e.detail.value
  const area = areaOptions.value[idx]
  currentAreaId.value = area?.id
  currentAreaName.value = area?.name === '全部区域' ? '' : area?.name
  currentBuildingId.value = null  // 切换区域时重置楼栋
  currentBuildingName.value = '全部楼栋'
  refresh()
}

function onBuildingChange(e) {
  const idx = e.detail.value
  const building = buildingOptions.value[idx]
  currentBuildingId.value = building?.id
  currentBuildingName.value = building?.name === '全部楼栋' ? '全部楼栋' : building?.name
  refresh()
}

function onTicketTypeChange(e) {
  const idx = e.detail.value
  const type = ticketTypeOptions.value[idx]
  currentTicketTypeId.value = type?.id
  currentTicketTypeName.value = type?.name === '全部类型' ? '全部类型' : type?.name
  refresh()
}

function onRefreshFail(count) {
  if (count >= 3) {
    stop()
  }
}

function refresh() {
  page.value = 1
  noMore.value = false
  list.value = []
  loadData()
}

async function loadData() {
  if (loading.value) return
  loading.value = true
  
  try {
    const res = await ticketApi.getAssignedByMeTickets(page.value, pageSize, null, currentAreaId.value, currentBuildingId.value, currentTicketTypeId.value)
    
    if (res.success && res.data) {
      if (page.value === 1) {
        list.value = res.data
      } else {
        list.value = [...list.value, ...res.data]
      }
      noMore.value = res.data.length < pageSize
    }
  } catch (e) {
    console.error('加载工单失败:', e)
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

function loadMore() {
  if (noMore.value || loading.value) return
  page.value++
  loadData()
}

function goDetail(item) {
  uni.navigateTo({ url: `/pages/admin/ticket-detail?id=${item.id}` })
}

function handleDispatch(item) {
  uni.navigateTo({ url: `/pages/admin/dispatch?id=${item.id}` })
}

onUnmounted(() => {
  stopAutoRefresh()
})

onMounted(async () => {
  // 连接 WebSocket 监听工单更新
  const app = getCurrentInstance().appContext.config.globalProperties
  if (app.globalData && app.globalData.ws) {
    app.globalData.ws.connect({
      onTicketCreated: (data) => {
        console.log('[Admin] New ticket:', data.ticketCode)
        uni.showToast({ title: `新工单: ${data.ticketCode}`, icon: 'none', duration: 3000 })
        refresh()
      },
      onTicketStatusChanged: (data) => {
        console.log('[Admin] Ticket status changed:', data.ticketCode, '->', data.currentStatus)
        refresh()
      }
    })
  }
  
  await Promise.all([
    loadAreas(),
    loadBuildings(),
    loadTicketTypes()
  ])
  loadData()
  startAutoRefresh()
})

onActivated(() => {
  if (!refreshTimer && failCount >= 3) {
    failCount = 0
    startAutoRefresh()
  }
})
</script>

<style scoped>
.tickets-page {
  min-height: 100vh;
  background: #f5f5f5;
}
.filter-bar {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  background: #ffffff;
  gap: 8px;
  overflow-x: auto;
}
.filter-value {
  padding: 6px 10px;
  background: #f5f5f5;
  border-radius: 4px;
  font-size: 12px;
  color: #606266;
  white-space: nowrap;
  min-width: 60px;
}
.arrow { font-size: 10px; margin-left: 4px; }
.list {
  padding: 12px 16px;
  height: calc(100vh - 60px);
}
.ticket-card {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 12px;
}
.ticket-code { font-size: 15px; font-weight: 600; color: #303133; }
.status {
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 4px;
}
.status-new { background: #fff7e6; color: #faad14; }
.status-dispatched { background: #e6f7ff; color: #1890ff; }
.status-accepted { background: #e6f7ff; color: #1890ff; }
.status-processing { background: #f0f9eb; color: #67c23a; }
.status-completed { background: #f6ffed; color: #52c41a; }
.status-closed { background: #f5f5f5; color: #909399; }
.card-body { margin-bottom: 12px; }
.info-row { display: flex; font-size: 13px; color: #606266; margin-bottom: 6px; }
.label { width: 70px; flex-shrink: 0; }
.value { color: #303133; }
.card-footer { display: flex; gap: 8px; justify-content: flex-end; }
.btn { border: none; font-size: 12px; padding: 4px 12px; border-radius: 4px; }
.btn-primary { background: #409EFF; color: #ffffff; }
.btn-default { background: #f5f5f5; color: #606266; }
.empty, .loading, .no-more { text-align: center; color: #909399; font-size: 14px; padding: 40px 0; }
</style>