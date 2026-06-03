<template>
  <view class="rate-page">
    <view class="ticket-info">
      <text class="ticket-code">{{ ticketCode }}</text>
    </view>

    <!-- 完工提交 -->
    <view v-if="mode === 'complete'" class="section">
      <view class="section-title">完工备注</view>
      <textarea 
        v-model="form.completionRemark"
        placeholder="请描述完成情况（选填）"
        class="remark-input"
      />
    </view>

    <!-- 评价 -->
    <view v-if="mode === 'rate'" class="section">
      <view class="section-title">服务评价</view>
      
      <view class="rate-item">
        <text class="rate-label">服务质量</text>
        <view class="stars">
          <text v-for="i in 5" :key="i" :class="['star', { active: form.qualityScore >= i }]" @click="form.qualityScore = i">★</text>
        </view>
      </view>

      <view class="rate-item">
        <text class="rate-label">服务态度</text>
        <view class="stars">
          <text v-for="i in 5" :key="i" :class="['star', { active: form.attitudeScore >= i }]" @click="form.attitudeScore = i">★</text>
        </view>
      </view>

      <view class="rate-item">
        <text class="rate-label">及时性</text>
        <view class="stars">
          <text v-for="i in 5" :key="i" :class="['star', { active: form.timelinessScore >= i }]" @click="form.timelinessScore = i">★</text>
        </view>
      </view>

      <view class="rate-item">
        <text class="rate-label">总体评价</text>
        <view class="stars">
          <text v-for="i in 5" :key="i" :class="['star', { active: form.overallScore >= i }]" @click="form.overallScore = i">★</text>
        </view>
      </view>

      <view class="comment">
        <textarea v-model="form.comment" placeholder="请输入评价备注（选填）" class="comment-input" />
      </view>
    </view>

    <view class="actions">
      <button v-if="mode === 'complete'" class="btn btn-primary" @click="handleComplete">确认完工</button>
      <button v-if="mode === 'rate'" class="btn btn-primary" @click="handleConfirm">确认评价</button>
    </view>
  </view>
</template>

<script setup>
import { reactive } from 'vue'
import dispatchApi from '@/api/dispatch'

let ticketCode = ''
let recordId = 0
let mode = 'complete'

const pages = getCurrentPages()
const current = pages[pages.length - 1]
if (current.options) {
  ticketCode = current.options.ticketCode || ''
  recordId = current.options.id || 0
  mode = current.options.mode || 'complete'
}

const form = reactive({
  completionRemark: '',
  qualityScore: 5,
  attitudeScore: 5,
  timelinessScore: 5,
  overallScore: 5,
  comment: ''
})

async function handleComplete() {
  try {
    const res = await dispatchApi.complete(recordId, form.completionRemark)
    if (res.success) {
      uni.showToast({ title: '完工提交成功', icon: 'success' })
      setTimeout(() => {
        uni.navigateBack()
        uni.navigateBack()
      }, 1500)
    } else {
      uni.showToast({ title: res.message || '提交失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '提交失败', icon: 'none' })
  }
}

async function handleConfirm() {
  try {
    const res = await dispatchApi.confirm(recordId, {
      raterId: 1,
      raterName: '业主',
      ...form
    })
    if (res.success) {
      uni.showToast({ title: '评价成功', icon: 'success' })
      setTimeout(() => uni.navigateBack(), 1500)
    } else {
      uni.showToast({ title: res.message || '提交失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '提交失败', icon: 'none' })
  }
}
</script>

<style scoped>
.rate-page {
  min-height: 100vh;
  background: #f5f5f5;
  padding: 12px 16px;
}
.ticket-info {
  background: #ffffff;
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 12px;
  text-align: center;
}
.ticket-code {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}
.section {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px;
}
.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 16px;
}
.rate-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #ebeef5;
}
.rate-label { font-size: 14px; color: #606266; }
.stars { display: flex; gap: 4px; }
.star { font-size: 24px; color: #dcdfe6; }
.star.active { color: #E6A23C; }
.comment { margin-top: 16px; }
.remark-input, .comment-input {
  width: 100%;
  height: 100px;
  padding: 12px;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  font-size: 14px;
  box-sizing: border-box;
}
.actions {
  padding: 16px 0;
}
.btn {
  width: 100%;
  height: 44px;
  line-height: 44px;
  border: none;
  border-radius: 22px;
  font-size: 15px;
  background: #409EFF;
  color: #ffffff;
}
</style>