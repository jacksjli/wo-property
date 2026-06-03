<template>
  <view class="detail-page">
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
            <text class="value">{{ ticket.category || '维修' }}</text>
          </view>
          <view class="info-item">
            <text class="label">工单状态</text>
            <text :class="['status', 'status-' + ticket.status?.toLowerCase()]">
              {{ getStatusText(ticket.status) }}
            </text>
          </view>
          <view class="info-item">
            <text class="label">提交时间</text>
            <text class="value">{{ formatTime(ticket.createdAt) }}</text>
          </view>
        </view>
      </view>

      <!-- 位置信息 -->
      <view class="section">
        <view class="section-title">位置信息</view>
        <view class="info-list">
          <view class="info-item">
            <text class="value">{{ ticket.location || '-' }}</text>
          </view>
        </view>
      </view>

      <!-- 情况说明 -->
      <view class="section">
        <view class="section-title">情况说明</view>
        <view class="desc">{{ ticket.description || '-' }}</view>
      </view>

      <!-- 图片 -->
      <view v-if="ticket.images && ticket.images.length > 0" class="section">
        <view class="section-title">现场图片</view>
        <view class="images">
          <image v-for="(img, idx) in ticket.images" :key="idx" :src="img" mode="aspectFill" @click="previewImage(idx)" />
        </view>
      </view>

      <!-- 联系人 -->
      <view class="section">
        <view class="section-title">联系人</view>
        <view class="info-list">
          <view class="info-item">
            <text class="label">姓名</text>
            <text class="value">{{ ticket.contactPersonName || '-' }}</text>
          </view>
          <view class="info-item">
            <text class="label">电话</text>
            <text class="value phone" @click="callPhone">{{ ticket.contactPhone || '-' }}</text>
          </view>
        </view>
      </view>

      <!-- 处理进度 -->
      <view v-if="progress.length > 0" class="section">
        <view class="section-title">处理进度</view>
        <view class="timeline">
          <view v-for="(item, idx) in progress" :key="idx" class="timeline-item">
            <view class="dot" :class="{ active: idx === 0 }"></view>
            <view class="content">
              <view class="title">{{ item.title }}</view>
              <view class="time">{{ formatTime(item.time) }}</view>
              <view v-if="item.desc" class="desc">{{ item.desc }}</view>
            </view>
          </view>
        </view>
      </view>

      <!-- 操作按钮 -->
      <view class="actions">
        <button v-if="ticket.status === 'Completed' && !ticket.hasRated" class="btn btn-primary" @click="goRate">立即评价</button>
        <button v-if="ticket.status !== 'Completed' && ticket.status !== 'Closed'" class="btn btn-default" @click="handleRemind">催单</button>
      </view>
    </template>
  </view>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import useAutoRefresh from '@/mixins/autoRefresh'
import ticketApi from '@/api/ticket'

const loading = ref(true)
const ticket = ref({})
const progress = ref([])

function getStatusText(status) {
  const map = {
    New: '新工单',
    Assigned: '已派单',
    Processing: '处理中',
    Completed: '已完成',
    Closed: '已关闭'
  }
  return map[status] || status
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

function previewImage(index) {
  uni.previewImage({
    urls: ticket.value.images,
    current: index
  })
}

function callPhone() {
  if (!ticket.value.contactPhone) return
  uni.makePhoneCall({
    phoneNumber: ticket.value.contactPhone
  })
}

function handleRemind() {
  uni.showModal({
    title: '催单',
    content: '确定催单吗？',
    success: async (res) => {
      if (res.confirm) {
        try {
          const result = await ticketApi.remindTicket(ticket.value.id)
          if (result.success) {
            uni.showToast({ title: '催单成功', icon: 'success' })
          } else {
            uni.showToast({ title: result.message || '催单失败', icon: 'none' })
          }
        } catch (e) {
          uni.showToast({ title: '催单失败', icon: 'none' })
        }
      }
    }
  })
}

function goRate() {
  uni.navigateTo({
    url: `/pages/owner/rate?id=${ticket.value.id}&ticketCode=${ticket.value.ticketCode}`
  })
}

function buildProgress() {
  const p = []
  if (ticket.value.status === 'Completed' || ticket.value.status === 'Closed') {
    p.push({ title: '工单已完成', time: ticket.value.updatedAt, desc: '服务已完成' })
  }
  if (ticket.value.status === 'Processing' || ticket.value.status === 'Completed') {
    p.push({ title: '工程师处理中', time: ticket.value.assignedAt || ticket.value.processingAt, desc: ticket.value.handlerName ? `工程师：${ticket.value.handlerName}` : '' })
  }
  if (ticket.value.status !== 'New') {
    p.push({ title: '已派单', time: ticket.value.assignedAt })
  }
  p.push({ title: '工单已提交', time: ticket.value.createdAt })
  return p
}

async function loadData() {
  loading.value = true
  const pages = getCurrentPages()
  const current = pages[pages.length - 1]
  const id = current.options?.id
  
  if (!id) {
    uni.showToast({ title: '参数错误', icon: 'none' })
    uni.navigateBack()
    return
  }
  
  try {
    const res = await ticketApi.getTicketDetail(id)
    if (res.success && res.data) {
      ticket.value = res.data
      progress.value = buildProgress()
    } else {
      uni.showToast({ title: '工单不存在', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadData()
  
  
  start(async () => { page.value = 1; await loadData(); }, 10000)
})
</script>

<style scoped>
.detail-page {
  min-height: 100vh;
  background: #f5f5f5;
  padding: 12px 16px 80px;
}
.loading {
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
.phone { color: #409EFF; }
.status {
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 4px;
}
.status-new { background: #fff7e6; color: #faad14; }
.status-assigned { background: #e6f7ff; color: #1890ff; }
.status-processing { background: #f6ffed; color: #52c41a; }
.status-completed { background: #f5f5f5; color: #909399; }
.desc {
  font-size: 14px;
  color: #303133;
  line-height: 1.6;
}
.images {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.images image {
  width: 80px;
  height: 80px;
  border-radius: 4px;
}
.timeline { padding-left: 10px; }
.timeline-item {
  position: relative;
  padding-left: 20px;
  padding-bottom: 20px;
  border-left: 1px solid #eee;
}
.timeline-item:last-child { border-left: none; }
.timeline-item .dot {
  position: absolute;
  left: -5px;
  top: 0;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #dcdfe6;
}
.timeline-item .dot.active { background: #409EFF; }
.timeline-item .content { }
.timeline-item .title { font-size: 14px; color: #303133; }
.timeline-item .time { font-size: 12px; color: #909399; margin-top: 4px; }
.timeline-item .desc { font-size: 12px; color: #606266; margin-top: 4px; }
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
.btn-default { background: #f5f5f5; color: #606266; }
</style>