<template>
  <view class="ticket-detail">
    <view v-if="loading" class="loading">加载中...</view>
    <template v-else-if="ticket.id">
      <!-- 基本信息 -->
      <view class="section">
        <view class="section-title">工单信息</view>
        <view class="info-list">
          <view class="info-item">
            <text class="label">工单编号</text>
            <text class="value">{{ ticket.ticketCode }}</text>
          </view>
          <view class="info-item">
            <text class="label">工单类型</text>
            <text class="value">{{ ticket.categoryName || ticket.ticketTypeName || '-' }}</text>
          </view>
          <view class="info-item">
            <text class="label">当前状态</text>
            <text :class="['status', 'status-' + getStatusKey(ticket.status)]">
              {{ getStatusText(ticket.status) }}
            </text>
          </view>
          <view class="info-item">
            <text class="label">优先级</text>
            <text class="value">{{ ticket.priority || '-' }}</text>
          </view>
          <view class="info-item">
            <text class="label">创建时间</text>
            <text class="value">{{ formatTime(ticket.createdAt) }}</text>
          </view>
        </view>
      </view>

      <!-- 位置信息 -->
      <view class="section">
        <view class="section-title">位置信息</view>
        <view class="info-list">
          <view class="info-item">
            <text class="label">位置</text>
            <text class="value location">{{ formatLocation(ticket) }}</text>
          </view>
        </view>
      </view>

      <!-- 业主信息 -->
      <view class="section">
        <view class="section-title">业主信息</view>
        <view class="info-list">
          <view class="info-item">
            <text class="label">姓名</text>
            <text class="value">{{ ticket.contactPersonName || '-' }}</text>
          </view>
          <view class="info-item" @click="callPhone">
            <text class="label">电话</text>
            <text class="value phone">{{ ticket.contactPhone || '-' }}</text>
          </view>
        </view>
      </view>

      <!-- 接单人信息（已派单后显示） -->
      <view class="section" v-if="assigneeInfo && ticket.status !== 'Dispatched'">
        <view class="section-title">接单人信息</view>
        <view class="info-list">
          <view class="info-item">
            <text class="label">姓名</text>
            <text class="value">{{ assigneeInfo.name }}</text>
          </view>
          <view class="info-item" @click="callAssigneePhone">
            <text class="label">电话</text>
            <text class="value phone">{{ assigneeInfo.phone }}</text>
          </view>
        </view>
      </view>

      <!-- 描述信息 -->
      <view class="section" v-if="ticket.description">
        <view class="section-title">工单描述</view>
        <view class="description">{{ ticket.description }}</view>
      </view>

      <!-- 评价信息（已关闭时显示） -->
      <view class="section" v-if="ticket.status === 'Closed' && ratingData">
        <view class="section-title">住户评价</view>
        <view class="rating-box">
          <view class="rating-header">
            <text class="rating-score">{{ ratingData.overallScore || ticket.rating || 0 }}</text>
            <text class="rating-unit">分</text>
            <view class="stars">
              <text v-for="i in 5" :key="i" :class="['star', { active: i <= (ratingData.overallScore || ticket.rating || 0) }]">★</text>
            </view>
          </view>
          <view class="rating-dimensions">
            <view class="dimension-item">
              <text class="dim-label">服务质量</text>
              <view class="stars">
                <text v-for="i in 5" :key="i" :class="['star', { active: i <= (ratingData.qualityScore || 0) }]">★</text>
              </view>
            </view>
            <view class="dimension-item">
              <text class="dim-label">服务态度</text>
              <view class="stars">
                <text v-for="i in 5" :key="i" :class="['star', { active: i <= (ratingData.attitudeScore || 0) }]">★</text>
              </view>
            </view>
            <view class="dimension-item">
              <text class="dim-label">及时性</text>
              <view class="stars">
                <text v-for="i in 5" :key="i" :class="['star', { active: i <= (ratingData.timelinessScore || 0) }]">★</text>
              </view>
            </view>
          </view>
          <view class="rating-comment" v-if="ratingData.comment">
            <text class="comment-label">评语：</text>
            <text class="comment-text">{{ ratingData.comment }}</text>
          </view>
          <view class="rating-meta" v-if="ratingData.raterName">
            <text class="meta-text">评价人：{{ ratingData.raterName }}</text>
            <text class="meta-text" v-if="ratingData.isAutoRated" style="color:#E6A23C">(系统自动评价)</text>
          </view>
        </view>
      </view>

      <!-- 操作按钮 -->
      <view class="actions">
        <template v-if="ticket.dispatchStatus === 'Dispatched'">
          <button class="btn btn-success" :disabled="processingId === ticket.id" :loading="processingId === ticket.id" @click="handleAccept">接单</button>
        </template>
        <template v-if="ticket.status === 'InProgress'">
          <button class="btn btn-primary" :disabled="processingId === ticket.id" :loading="processingId === ticket.id" @click="handleStart">开始处理</button>
        </template>
        <template v-if="ticket.status === 'Processing'">
          <button class="btn btn-finish" :disabled="processingId === ticket.id" :loading="processingId === ticket.id" @click="handleFinish">完成</button>
        </template>
      </view>
    </template>
    <view v-else class="empty">工单不存在</view>
  </view>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import useAutoRefresh from '@/mixins/autoRefresh'
const { start, stop } = useAutoRefresh()
import ticketApi from '@/api/ticket'
import dispatchApi from '@/api/dispatch'

const loading = ref(true)
const ticket = ref({})
const processingId = ref(null)
const assigneeInfo = ref(null)
const ratingData = ref(null)

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

function callPhone() {
  if (!ticket.value.contactPhone) return
  uni.makePhoneCall({ phoneNumber: ticket.value.contactPhone })
}

function callAssigneePhone() {
  if (!assigneeInfo.value?.phone) return
  uni.makePhoneCall({ phoneNumber: assigneeInfo.value.phone })
}

function getRecordId() {
  const pages = getCurrentPages()
  const current = pages[pages.length - 1]
  return current.options?.id || 0
}

async function loadData() {
  loading.value = true
  const id = getRecordId()
  
  if (!id) {
    uni.showToast({ title: '参数错误', icon: 'none' })
    uni.navigateBack()
    return
  }
  
  try {
    const res = await ticketApi.getTicketDetail(id)
    if (res.success && res.data) {
      ticket.value = res.data
      // 加载接单人信息
      if (ticket.value.assigneePersonId) {
        loadAssigneeInfo(ticket.value.assigneePersonId)
      }
      if (ticket.value.status === 'Closed') {
        loadRating(ticket.value.id)
      }
    } else {
      uni.showToast({ title: '工单不存在', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

async function loadAssigneeInfo(personId) {
  try {
    const res = await uni.request({
      url: `${getApp().globalData.PERSON_API}/persons/${personId}`,
      method: 'GET',
      timeout: 5000,
      header: {
        'Authorization': 'Bearer ' + (uni.getStorageSync('token') || ''),
        'Tenant-Code': uni.getStorageSync('tenantCode') || 'wo_property'
      }
    })
    if (res.data?.success && res.data?.data) {
      assigneeInfo.value = {
        name: res.data.data.name,
        phone: res.data.data.phone
      }
    }
  } catch (e) {
    console.warn('加载接单人信息失败', e)
  }
async function loadRating(ticketId) {
  try {
    const res = await dispatchApi.getRating(ticketId)
    if (res.success && res.data) {
      ratingData.value = res.data
    }
  } catch (e) {
    console.warn('加载评价信息失败', e)
  }
}

}

async function handleAccept() {
  try {
    processingId.value = ticket.value.id
    const res = await ticketApi.acceptTicket(ticket.value.id)
    if (res.success) {
      uni.showToast({ title: '已接单', icon: 'success' })
      ticket.value.status = 'Accepted'
    } else {
      uni.showToast({ title: res.message || '操作失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '操作失败', icon: 'none' })
  } finally {
    processingId.value = null
  }
}

async function handleStart() {
  try {
    processingId.value = ticket.value.id
    const res = await ticketApi.progressTicket(ticket.value.id)
    if (res.success) {
      uni.showToast({ title: '已开始处理', icon: 'success' })
      ticket.value.status = 'InProgress'
    } else {
      uni.showToast({ title: res.message || '操作失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '操作失败', icon: 'none' })
  } finally {
    processingId.value = null
  }
}

async function handleFinish() {
  try {
    processingId.value = ticket.value.id
    const res = await ticketApi.finishTicket(ticket.value.id)
    if (res.success) {
      uni.showToast({ title: '已完成', icon: 'success' })
      ticket.value.status = 'Completed'
    } else {
      uni.showToast({ title: res.message || '操作失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '操作失败', icon: 'none' })
  } finally {
    processingId.value = null
  }
}

onMounted(() => {
  loadData()
  start(async () => { await loadData(); }, 10000)
})

onUnmounted(() => {
  stop()
})
</script>

<style scoped>
.ticket-detail {
  min-height: 100vh;
  background: #f5f5f5;
  padding: 12px 16px 100px;
}
.loading, .empty {
  text-align: center;
  padding: 60px;
  color: #909399;
}
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
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid #ebeef5;
}
.info-list { display: flex; flex-direction: column; gap: 10px; }
.info-item { display: flex; justify-content: space-between; font-size: 14px; }
.label { color: #606266; }
.value { color: #303133; }
.location { color: #409EFF; font-weight: 500; }
.phone { color: #409EFF; }
.description {
  font-size: 14px;
  color: #303133;
  line-height: 1.6;
  white-space: pre-wrap;
}
.status {
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 4px;
}
.status-new, .status-pending { background: #fff7e6; color: #faad14; }
.status-dispatched { background: #e6f7ff; color: #1890ff; }
.status-accepted { background: #e6f7ff; color: #1890ff; }
.status-inprogress { background: #fff3e0; color: #ff9800; }
.status-completed { background: #f6ffed; color: #52c41a; }
.status-closed { background: #f5f5f5; color: #909399; }
.actions {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 12px 16px;
  background: #ffffff;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
  display: flex;
  gap: 12px;
}
.btn {
  flex: 1;
  height: 44px;
  line-height: 44px;
  border: none;
  border-radius: 22px;
  font-size: 15px;
}
.btn-primary { background: #409EFF; color: #ffffff; }
.btn-success { background: #67C23A; color: #ffffff; }
.btn-finish { background: #67C23A; color: #ffffff; }
.btn:disabled { opacity: 0.6; }
.rating-box {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.rating-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-bottom: 8px;
  border-bottom: 1px solid #ebeef5;
}
.rating-score {
  font-size: 28px;
  font-weight: 700;
  color: #E6A23C;
}
.rating-unit {
  font-size: 14px;
  color: #909399;
}
.stars { display: flex; gap: 2px; }
.star { font-size: 16px; color: #dcdfe6; }
.star.active { color: #E6A23C; }
.rating-dimensions { display: flex; flex-direction: column; gap: 8px; }
.dimension-item { display: flex; justify-content: space-between; align-items: center; }
.dim-label { font-size: 13px; color: #606266; }
.rating-comment { display: flex; gap: 6px; font-size: 13px; }
.comment-label { color: #909399; }
.comment-text { color: #303133; flex: 1; }
.rating-meta { display: flex; gap: 12px; font-size: 12px; }
.meta-text { color: #909399; }
</style>