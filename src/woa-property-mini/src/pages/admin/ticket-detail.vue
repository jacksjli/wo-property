<template>
  <view class="detail-page">
    <ProjectTabBar />
    <view class="header">
      <text class="ticket-code">{{ ticketCode }}</text>
      <text :class="['status', status]">{{ statusText }}</text>
    </view>
    
    <view class="info-section">
      <view class="info-row">
        <text class="label">工单类型</text>
        <text class="value">{{ ticketTypeName }}</text>
      </view>
      <view class="info-row">
        <text class="label">区域</text>
        <text class="value">{{ areaName }}</text>
      </view>
      <view class="info-row">
        <text class="label">楼栋</text>
        <text class="value">{{ buildingName }}</text>
      </view>
      <view class="info-row">
        <text class="label">位置</text>
        <text class="value">{{ location }}</text>
      </view>
      <view class="info-row">
        <text class="label">描述</text>
        <text class="value">{{ description }}</text>
      </view>
      <view class="info-row">
        <text class="label">创建时间</text>
        <text class="value">{{ createdAt }}</text>
      </view>
    </view>
    
    <view class="action-section">
      <button v-if="status === 'New'" class="btn-primary" @click="goDispatch">去派单</button>
      <button class="btn-default" @click="goBack">返回</button>
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import ProjectTabBar from '@/components/ProjectTabBar.vue'
import ticketApi from '@/api/ticket'

const ticketCode = ref('')
const status = ref('')
const ticketTypeName = ref('')
const areaName = ref('')
const buildingName = ref('')
const location = ref('')
const description = ref('')
const createdAt = ref('')

const statusMap = { New: '新工单', Dispatched: '已派单', Accepted: '已接单', Processing: '处理中', Completed: '已完成', Closed: '已关闭' }
const statusText = statusMap[status.value] || status.value

function goDispatch() {
  uni.navigateTo({ url: `/pages/admin/dispatch?id=${getTicketId()}` })
}

function getTicketId() {
  const pages = getCurrentPages()
  const currentPage = pages[pages.length - 1]
  return currentPage?.options?.id || ''
}

function goBack() {
  uni.navigateBack()
}

onLoad((options) => {
  if (options.id) {
    loadDetail(options.id)
  }
})

async function loadDetail(id) {
  try {
    const res = await ticketApi.getTicketDetail(id)
    if (res.success && res.data) {
      const t = res.data
      ticketCode.value = t.ticketCode || ''
      status.value = t.status || ''
      ticketTypeName.value = t.ticketTypeName || t.categoryName || ''
      areaName.value = t.areaName || ''
      buildingName.value = t.buildingName || ''
      location.value = t.location || ''
      description.value = t.description || ''
      createdAt.value = t.createdAt ? formatTime(t.createdAt) : ''
    }
  } catch (e) {
    console.error('加载工单详情失败:', e)
  }
}

function formatTime(time) {
  if (!time) return ''
  const d = new Date(time)
  if (isNaN(d.getTime())) return time
  const date = new Date(d.getTime() + 8 * 60 * 60 * 1000)
  const pad = n => n.toString().padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth()+1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
</script>

<style scoped>
.detail-page { padding: 20rpx; }
.header { background: #ffffff; padding: 30rpx; border-radius: 8rpx; margin-bottom: 20rpx; display: flex; justify-content: space-between; align-items: center; }
.ticket-code { font-size: 32rpx; font-weight: bold; }
.status { font-size: 24rpx; padding: 8rpx 20rpx; border-radius: 20rpx; }
.status.New { background: #E6F7FF; color: #1890FF; }
.status.Dispatched { background: #FFF3E6; color: #FA8C16; }
.status.Accepted, .status.Processing { background: #F6FFED; color: #52C41A; }
.status.Completed { background: #f5f5f5; color: #909399; }
.info-section { background: #ffffff; padding: 20rpx; border-radius: 8rpx; margin-bottom: 20rpx; }
.info-row { display: flex; padding: 20rpx 0; border-bottom: 1px solid #ebeef5; }
.info-row:last-child { border-bottom: none; }
.label { width: 160rpx; color: #606266; font-size: 28rpx; }
.value { flex: 1; color: #303133; font-size: 28rpx; }
.action-section { display: flex; gap: 20rpx; }
.btn-primary { flex: 1; background: #409EFF; color: #ffffff; border-radius: 8rpx; }
.btn-default { flex: 1; background: #ffffff; color: #606266; border: 1px solid #dcdfe6; border-radius: 8rpx; }
</style>
