<template>
  <view class="transfer-page">
    <view class="info-card">
      <view class="info-row">
        <text class="label">工单编号</text>
        <text class="value">{{ ticketCode }}</text>
      </view>
    </view>

    <view class="section">
      <view class="section-title">转单信息</view>
      
      <view class="form-item">
        <text class="label">目标执行人</text>
        <picker mode="selector" :range="personList" range-key="name" @change="onPersonChange">
          <view class="picker-value">
            {{ selectedPerson?.name || '请选择' }}
          </view>
        </picker>
      </view>

      <view class="form-item">
        <text class="label">转单原因</text>
        <textarea 
          v-model="reason"
          placeholder="请输入转单原因"
          class="reason-input"
        />
      </view>
    </view>

    <view class="actions">
      <button class="btn btn-primary" :disabled="!canSubmit || submitting" :loading="submitting" @click="submit">提交申请</button>
    </view>
  </view>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import ticketApi from '@/api/ticket'
import dispatchApi from '@/api/dispatch'

const ticketCode = ref('')
const ticketId = ref(0)
const dispatchRecordId = ref(0)
const reason = ref('')
const personList = ref([])
const selectedPersonIndex = ref(-1)
const submitting = ref(false)

const selectedPerson = computed(() => {
  if (selectedPersonIndex.value >= 0 && selectedPersonIndex.value < personList.value.length) {
    return personList.value[selectedPersonIndex.value]
  }
  return null
})

const canSubmit = computed(() => {
  return dispatchRecordId.value > 0 && selectedPerson.value && reason.value.trim().length > 0
})

function onPersonChange(e) {
  selectedPersonIndex.value = e.detail.value
}

async function getParams() {
  const pages = getCurrentPages()
  const current = pages[pages.length - 1]
  const options = current.options || {}
  ticketCode.value = options.ticketCode || ''
  ticketId.value = Number(options.id) || 0

  // 如果 URL 没有 dispatchRecordId，通过 ticketCode 查找活跃派单
  if (!options.dispatchRecordId && ticketCode.value) {
    try {
      const res = await dispatchApi.getActiveDispatch(ticketCode.value)
      if (res.success && res.data) {
        dispatchRecordId.value = res.data.id
      } else {
        uni.showToast({ title: '无进行中的派单，无法转单', icon: 'none' })
        setTimeout(() => uni.navigateBack(), 1500)
      }
    } catch (e) {
      uni.showToast({ title: '获取派单信息失败', icon: 'none' })
    }
  } else {
    dispatchRecordId.value = Number(options.dispatchRecordId) || 0
  }
}

async function loadPersons() {
  try {
    const res = await uni.request({
      url: `${getApp().globalData.PERSON_API}/persons`,
      method: 'GET',
      timeout: 10000,
      header: {
        'Authorization': 'Bearer ' + (uni.getStorageSync('token') || ''),
        'Tenant-Code': uni.getStorageSync('tenantCode') || 'wo_property'
      }
    })
    if (res.data?.success && res.data?.data) {
      const data = res.data.data
      const items = Array.isArray(data) ? data : (data.items || [])
      // 只显示 operator 角色（工种人员）
      personList.value = items
        .filter(p => p.role === 'operator')
        .map(p => ({ id: p.id, name: p.name }))
    }
  } catch (e) {
    console.warn('加载人员列表失败', e)
    personList.value = []
  }
}

async function submit() {
  if (!canSubmit.value || submitting.value) return
  
  try {
    submitting.value = true
    const person = selectedPerson.value
    const res = await dispatchApi.transferDispatch({
      DispatchRecordId: dispatchRecordId.value,
      ToPersonId: person.id,
      ToPersonName: person.name,
      Reason: reason.value.trim()
    })
    if (res.success) {
      uni.showToast({ title: '已提交转单请求', icon: 'success' })
      setTimeout(() => uni.navigateBack(), 1500)
    } else {
      uni.showToast({ title: res.message || '提交失败', icon: 'none' })
    }
  } catch (e) {
    uni.showToast({ title: '提交失败', icon: 'none' })
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  await getParams()
  loadPersons()
})
</script>

<style scoped>
.transfer-page {
  min-height: 100vh;
  background: #f5f5f5;
  padding: 12px 16px;
}
.info-card {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
}
.info-row {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  font-size: 14px;
}
.label { color: #666; }
.value { color: #333; font-weight: 500; }
.section {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
}
.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #333;
  margin-bottom: 16px;
}
.form-item {
  margin-bottom: 16px;
}
.form-item .label {
  display: block;
  font-size: 14px;
  color: #666;
  margin-bottom: 8px;
}
.picker-value {
  padding: 10px 12px;
  background: #f5f5f5;
  border-radius: 4px;
  font-size: 14px;
  color: #333;
}
.reason-input {
  width: 100%;
  height: 100px;
  padding: 12px;
  border: 1px solid #eee;
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
  color: #fff;
}
.btn[disabled] {
  background: #ccc;
}
</style>