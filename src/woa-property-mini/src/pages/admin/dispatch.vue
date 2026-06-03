<template>
  <view class="dispatch-page">
    <ProjectTabBar />
    <view class="header">
      <text class="title">工单派单</text>
      <text class="ticket-code">{{ ticketCode }}</text>
    </view>

    <view class="form">
      <view class="form-item">
        <text class="label">工单信息</text>
        <text class="value">{{ ticketInfo || '加载中...' }}</text>
      </view>

      <view class="form-item">
        <text class="label">选择维修工</text>
        <picker mode="selector" :range="workerList" range-key="name" @change="onWorkerChange">
          <view class="picker">
            {{ selectedWorker ? selectedWorker.name : '请选择维修工' }}
          </view>
        </picker>
      </view>

      <view class="form-item">
        <text class="label">备注</text>
        <textarea v-model="remark" placeholder="可选填写备注" class="textarea" />
      </view>

      <button class="btn-primary" @click="doDispatch">确认派单</button>
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import ProjectTabBar from '@/components/ProjectTabBar.vue'
import { onLoad } from '@dcloudio/uni-app'
import dispatchApi from '@/api/dispatch'

const ticketId = ref('')
const ticketCode = ref('')
const ticketInfo = ref('')
const workerList = ref([])
const selectedWorker = ref(null)
const remark = ref('')

onLoad((options) => {
  if (options.id) {
    ticketId.value = options.id
    ticketCode.value = options.ticketCode || ''
    ticketInfo.value = options.info || ''
  }
  loadWorkers()
})

async function loadWorkers() {
  try {
    const res = await dispatchApi.getWorkers()
    if (res.success) {
      workerList.value = res.data || []
    }
  } catch (e) {
    console.error('加载工人列表失败:', e)
  }
}

function onWorkerChange(e) {
  const idx = e.detail.value
  selectedWorker.value = workerList.value[idx]
}

async function doDispatch() {
  if (!selectedWorker.value) {
    uni.showToast({ title: '请选择维修工', icon: 'none' })
    return
  }
  try {
    const res = await dispatchApi.dispatch({
      ticketId: ticketId.value,
      workerId: selectedWorker.value.id,
      remark: remark.value
    })
    if (res.success) {
      uni.showToast({ title: '派单成功', icon: 'success' })
      setTimeout(() => uni.navigateBack(), 1500)
    } else {
      uni.showToast({ title: res.message || '派单失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '派单失败', icon: 'none' })
  }
}
</script>

<style scoped>
.dispatch-page { padding: 20rpx; }
.header { padding: 20rpx; background: #ffffff; border-radius: 8rpx; margin-bottom: 20rpx; }
.title { font-size: 32rpx; font-weight: bold; }
.ticket-code { font-size: 24rpx; color: #606266; margin-left: 20rpx; }
.form-item { background: #ffffff; padding: 20rpx; border-radius: 8rpx; margin-bottom: 20rpx; }
.label { font-size: 28rpx; color: #303133; display: block; margin-bottom: 10rpx; }
.value { font-size: 28rpx; color: #606266; }
.picker { padding: 20rpx; background: #f5f5f5; border-radius: 8rpx; }
.textarea { width: 100%; padding: 20rpx; background: #f5f5f5; border-radius: 8rpx; box-sizing: border-box; min-height: 120rpx; }
.btn-primary { background: #409EFF; color: #ffffff; border-radius: 8rpx; margin-top: 20rpx; height: 88rpx; line-height: 88rpx; }
</style>