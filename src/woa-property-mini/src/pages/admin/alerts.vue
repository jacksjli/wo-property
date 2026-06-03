<template>
  <view class="alerts-page">
    <ProjectTabBar />
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

    <!-- 告警列表 -->
    <scroll-view scroll-y class="list" @scrolltolower="loadMore">
      <view v-if="loading && list.length === 0" class="empty">加载中...</view>
      <view v-else-if="list.length === 0" class="empty">暂无告警</view>
      <view 
        v-for="item in list" 
        :key="item.id" 
        :class="['alert-card', 'level-' + item.level]"
      >
        <view class="card-header">
          <text class="ticket-code">{{ item.ticketCode }}</text>
          <text :class="['level-tag', 'level-' + item.level]">
            {{ getLevelText(item.level) }}
          </text>
        </view>
        <view class="card-body">
          <view class="info-row">
            <text class="label">告警类型：</text>
            <text class="value">{{ getAlertTypeText(item.alertType) }}</text>
          </view>
          <view class="info-row">
            <text class="label">超时时间：</text>
            <text class="value">{{ item.timeoutMinutes }}分钟</text>
          </view>
          <view class="info-row">
            <text class="label">通知对象：</text>
            <text class="value">{{ item.notifyTargetName }}</text>
          </view>
          <view class="info-row">
            <text class="label">创建时间：</text>
            <text class="value">{{ formatTime(item.createdAt) }}</text>
          </view>
        </view>
        <view class="card-footer">
          <button v-if="item.status === 'Pending'" class="btn btn-primary" size="mini" @click="handleProcess(item)">处理</button>
          <text :class="['status-text', item.status === 'Processed' ? 'processed' : '']">
            {{ item.status === 'Processed' ? '已处理' : '待处理' }}
          </text>
        </view>
      </view>
      <view v-if="loading && list.length > 0" class="loading">加载中...</view>
      <view v-if="noMore && list.length > 0" class="no-more">没有更多了</view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import ProjectTabBar from '@/components/ProjectTabBar.vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import dispatchApi from '@/api/dispatch'

const tabs = [
  { key: 'all', name: '全部' },
  { key: 'Pending', name: '待处理' },
  { key: 'Processed', name: '已处理' }
]

const activeTab = ref('all')
const loading = ref(false)
const list = ref([])
const page = ref(1)
const pageSize = 20
const { start, stop } = useAutoRefresh()
const noMore = ref(false)

function getLevelText(level) {
  const map = { 1: '紧急', 2: '重要', 3: '一般' }
  return map[level] || '一般'
}

function getAlertTypeText(type) {
  const map = {
    'PendingTimeout': '待接收超时',
    'ProcessingTimeout': '处理中超时',
    'CompletedTimeout': '完工超时'
  }
  return map[type] || type
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

function switchTab(key) {
  activeTab.value = key
  refresh()
}

function onRefreshFail(count) {
  if (count >= 3) stop()
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
    const status = activeTab.value === 'all' ? '' : activeTab.value
    const res = await dispatchApi.getAlerts(page.value, pageSize, status)
    
    if (res.success && res.data) {
      if (page.value === 1) {
        list.value = res.data
      } else {
        list.value = [...list.value, ...res.data]
      }
      noMore.value = res.data.length < pageSize
    }
  } catch (e) {
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

function handleProcess(item) {
  uni.showModal({
    title: '处理告警',
    content: `确定处理告警 ${item.ticketCode}？`,
    success: async (res) => {
      if (res.confirm) {
        try {
          const result = await dispatchApi.processAlert(item.id)
          if (result.success) {
            uni.showToast({ title: '处理成功', icon: 'success' })
            refresh()
          }
        } catch (e) {
          uni.showToast({ title: '处理失败', icon: 'none' })
        }
      }
    }
  })
}

onMounted(() => {
  loadData()
  
  
  start(async () => { page.value = 1; await loadData(); }, 10000)
})
</script>

<style scoped>
.alerts-page {
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
  padding: 14px 0;
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
}
.list {
  padding: 12px 16px;
  height: calc(100vh - 60px);
}
.alert-card {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
  border-left: 4px solid;
}
.alert-card.level-1 { border-left-color: #F56C6C; }
.alert-card.level-2 { border-left-color: #faad14; }
.alert-card.level-3 { border-left-color: #909399; }
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.ticket-code { font-size: 15px; font-weight: 600; color: #303133; }
.level-tag {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 4px;
}
.level-tag.level-1 { background: #fef0f0; color: #F56C6C; }
.level-tag.level-2 { background: #fef9e6; color: #faad14; }
.level-tag.level-3 { background: #f5f5f5; color: #909399; }
.card-body { margin-bottom: 12px; }
.info-row { display: flex; font-size: 13px; color: #606266; margin-bottom: 6px; }
.label { width: 80px; flex-shrink: 0; }
.value { color: #303133; }
.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.btn { border: none; font-size: 12px; padding: 4px 12px; border-radius: 4px; }
.btn-primary { background: #409EFF; color: #ffffff; }
.status-text { font-size: 12px; color: #909399; }
.status-text.processed { color: #67C23A; }
.empty, .loading, .no-more { text-align: center; color: #909399; font-size: 14px; padding: 40px 0; }
</style>