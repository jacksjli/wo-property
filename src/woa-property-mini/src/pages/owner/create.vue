<template>
  <view class="create-page">
    <view class="form">
      <!-- 当前项目（只读） -->
      <view class="form-item">
        <text class="label">项目</text>
        <view class="picker-value plain">
          {{ currentProjectName || '未选择' }}
        </view>
      </view>

      <!-- 工单类型 + 工种 并排 -->
      <view class="form-row">
        <view class="form-item flex-1">
          <text class="label required">工单类型</text>
          <picker :value="ticketTypeIndex" :range="ticketTypes" range-key="name" @change="onTicketTypeChange">
            <view class="picker-value">
              {{ ticketTypeIndex >= 0 ? ticketTypes[ticketTypeIndex].name : '请选择工单类型' }}
            </view>
          </picker>
        </view>
        <view class="form-item flex-1">
          <text class="label required">工种</text>
          <picker :value="jobTypeIndex" :range="filteredJobTypes" range-key="name" @change="onJobTypeChange" :disabled="!form.ticketTypeId">
            <view class="picker-value">
              {{ jobTypeIndex >= 0 ? filteredJobTypes[jobTypeIndex].name : '请选择工种' }}
            </view>
          </picker>
        </view>
      </view>

      <!-- 区域 + 楼栋 并排 -->
      <view class="form-row">
        <view class="form-item flex-1">
          <text class="label required">区域</text>
          <picker :value="areaIndex" :range="areas" range-key="name" @change="onAreaChange">
            <view class="picker-value">
              {{ areaIndex >= 0 ? areas[areaIndex].name : '请选择区域' }}
            </view>
          </picker>
        </view>
        <view class="form-item flex-1">
          <text :class="['label', { required: buildings.length > 0 }]">楼栋</text>
          <picker :value="buildingIndex" :range="buildings" range-key="name" @change="onBuildingChange" :disabled="!buildings.length">
            <view class="picker-value">
              {{ buildingIndex >= 0 ? buildings[buildingIndex].name : '请选择楼栋' }}
            </view>
          </picker>
        </view>
      </view>

      <!-- 房号（加宽） -->
      <view class="form-item">
        <text class="label">房号</text>
        <input class="input" v-model="form.room" placeholder="请输入房号，如：101" />
      </view>

      <!-- 情况说明 -->
      <view class="form-item">
        <text class="label">情况说明</text>
        <textarea class="textarea" v-model="form.description" placeholder="请详细描述您的问题" maxlength="500" />
        <text class="char-count">{{ form.description.length }}/500</text>
      </view>

      <!-- 图片上传 -->
      <view class="form-item">
        <text class="label">图片（选填，最多3张）</text>
        <view class="images">
          <view v-for="(img, index) in form.images" :key="index" class="image-item">
            <image :src="img" mode="aspectFill" />
            <view class="delete-btn" @click="removeImage(index)">×</view>
          </view>
          <view v-if="form.images.length < 3" class="add-image" @click="chooseImage">
            <text class="icon">+</text>
            <text class="text">添加图片</text>
          </view>
        </view>
      </view>

      <!-- 姓名 + 电话 并排 -->
      <view class="form-row">
        <view class="form-item flex-1">
          <text class="label required">姓名</text>
          <input class="input" v-model="form.name" placeholder="请输入您的姓名" />
        </view>
        <view class="form-item flex-1">
          <text class="label required">电话</text>
          <input class="input" v-model="form.phone" type="number" placeholder="请输入手机号码" maxlength="11" />
        </view>
      </view>
    </view>

    <!-- 提交按钮 -->
    <view class="submit-bar">
      <button class="btn-submit" :disabled="submitting" @click="handleSubmit">
        {{ submitting ? '提交中...' : '提交报修' }}
      </button>
    </view>
  </view>
</template>
<script setup>
import { ref, reactive, onMounted } from 'vue'
import ticketApi from '@/api/ticket'
import useAutoRefresh from '@/mixins/autoRefresh'
const { start } = useAutoRefresh()

const submitting = ref(false)
const currentProjectName = ref('')
const ticketTypes = ref([])
const ticketTypeIndex = ref(-1)
const jobTypes = ref([])
const filteredJobTypes = ref([])
const jobTypeIndex = ref(-1)
const areas = ref([])
const areaIndex = ref(-1)
const buildings = ref([])
const buildingIndex = ref(-1)

const form = reactive({
  ticketTypeId: null,
  jobTypeId: null,
  areaId: null,
  buildingId: null,
  room: '',
  description: '',
  name: '',
  phone: '',
  images: []
})

function loadCurrentProject() {
  const current = uni.getStorageSync('currentProject') || {}
  currentProjectName.value = current.projectName || '未选择'
}

async function loadTicketTypes() {
  try {
    const res = await ticketApi.getTicketTypes()
    console.log('loadTicketTypes res:', res)
    if (res.success && res.data) {
      ticketTypes.value = res.data
      console.log('ticketTypes loaded:', ticketTypes.value.length, 'items')
    } else {
      console.log('loadTicketTypes failed:', res)
    }
  } catch (e) {
    console.error('load ticket types error', e.message || e)
  }
}

async function loadJobTypes() {
  try {
    const res = await ticketApi.getJobTypes()
    if (res.success && res.data) {
      jobTypes.value = res.data
    }
  } catch (e) {
    console.log('load job types error', e)
  }
}

async function loadAreas() {
  try {
    const res = await ticketApi.getAreas()
    if (res.success && res.data) {
      areas.value = res.data
    } else {
      areas.value = []
    }
  } catch (e) {
    areas.value = []
  }
}

async function loadBuildings(areaName) {
  try {
    const res = await ticketApi.getBuildings()
    if (res.success && res.data && res.data.length > 0) {
      if (areaName) {
        buildings.value = res.data.filter(b => b.area === areaName)
      } else {
        buildings.value = res.data
      }
    } else {
      buildings.value = []
    }
  } catch (e) {
    buildings.value = []
  }
}

// 加载保存的姓名电话作为默认值（实时从数据库更新）
async function loadSavedContact() {
  const savedName = uni.getStorageSync('contactName')
  const savedPhone = uni.getStorageSync('contactPhone')
  if (savedName) form.name = savedName
  if (savedPhone) form.phone = savedPhone
  
  const personId = uni.getStorageSync('personId')
  if (personId) {
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
        const p = res.data.data
        if (p.name) {
          form.name = p.name
          uni.setStorageSync('contactName', p.name)
        }
        if (p.phone) {
          form.phone = p.phone
          uni.setStorageSync('contactPhone', p.phone)
        }
      }
    } catch (e) {
      // 使用本地缓存的旧值
    }
  }
}

function onTicketTypeChange(e) {
  ticketTypeIndex.value = e.detail.value
  const selectedTicketType = ticketTypes.value[e.detail.value]
  form.ticketTypeId = selectedTicketType?.id
  console.log('onTicketTypeChange:', selectedTicketType?.id, selectedTicketType?.name)
  // 重置工种选择
  jobTypeIndex.value = -1
  form.jobTypeId = null
  // 根据工单类型过滤工种
  if (form.ticketTypeId) {
    filteredJobTypes.value = jobTypes.value.filter(j => j.ticket_type_id === form.ticketTypeId)
    console.log('filteredJobTypes:', filteredJobTypes.value.length, '条工种')
  } else {
    filteredJobTypes.value = []
  }
}

function onJobTypeChange(e) {
  jobTypeIndex.value = e.detail.value
  form.jobTypeId = filteredJobTypes.value[e.detail.value]?.id
}

function onAreaChange(e) {
  areaIndex.value = e.detail.value
  form.areaId = areas.value[e.detail.value]?.id
  buildingIndex.value = -1
  form.buildingId = null
  const areaName = areas.value[e.detail.value]?.name
  loadBuildings(areaName)
}

function onBuildingChange(e) {
  buildingIndex.value = e.detail.value
  form.buildingId = buildings.value[e.detail.value]?.id
}

function chooseImage() {
  uni.chooseImage({
    count: 3 - form.images.length,
    sizeType: ['compressed'],
    sourceType: ['album', 'camera'],
    success: (res) => {
      form.images = [...form.images, ...res.tempFilePaths].slice(0, 3)
    }
  })
}

function removeImage(index) {
  form.images.splice(index, 1)
}

function validate() {
  if (!form.ticketTypeId) {
    uni.showToast({ title: '请选择工单类型', icon: 'none' })
    return false
  }
  if (!form.jobTypeId) {
    uni.showToast({ title: '请选择工种', icon: 'none' })
    return false
  }
  if (!form.areaId) {
    uni.showToast({ title: '请选择区域', icon: 'none' })
    return false
  }
  // 楼栋：当有楼栋数据时必填
  if (buildings.value.length > 0 && !form.buildingId) {
    uni.showToast({ title: '请选择楼栋', icon: 'none' })
    return false
  }
  // if (!form.description.trim()) {
  //   uni.showToast({ title: '请输入情况说明', icon: 'none' })
  //   return false
  // }
  if (!form.name.trim()) {
    uni.showToast({ title: '请输入姓名', icon: 'none' })
    return false
  }
  if (!form.phone.trim() || !/^1\d{10}$/.test(form.phone)) {
    uni.showToast({ title: '请输入正确的手机号', icon: 'none' })
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validate()) return
  if (submitting.value) return
  
  submitting.value = true
  try {
    const location = `${areas.value.find(a => a.id === form.areaId)?.name || ''}${buildings.value.find(b => b.id === form.buildingId)?.name || ''}${form.room}`
    
    const res = await ticketApi.createTicket({
      category: ticketTypes.value.find(t => t.id === form.ticketTypeId)?.name || '',
      title: `${ticketTypes.value.find(t => t.id === form.ticketTypeId)?.name || ''}报修`,
      description: form.description,
      ticketTypeId: form.ticketTypeId,
      jobTypeId: form.jobTypeId || null,
      areaId: form.areaId,
      buildingId: form.buildingId,
      room: form.room,
      location: location,
      contactPersonName: form.name,
      contactPhone: form.phone,
      images: form.images,
      projectCode: (uni.getStorageSync('currentProject') || {}).projectCode || '',  // 使用当前项目的 projectCode
      priority: 'Medium'
    })
    
    if (res.success) {
      // 保存姓名电话，下次默认填充
      uni.setStorageSync('contactName', form.name)
      uni.setStorageSync('contactPhone', form.phone)
      uni.showToast({ title: '提交成功', icon: 'success' })
      setTimeout(() => {
        uni.$emit('ticketCreated')
        uni.navigateBack()
      }, 1500)
    } else {
      uni.showToast({ title: res.message || '提交失败', icon: 'none' })
    }
  } catch (e) {
    console.error('submit error:', e)
    uni.showToast({ title: '提交失败', icon: 'none' })
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  await loadCurrentProject()
  await loadTicketTypes()
  await loadJobTypes()
  await loadAreas()
  loadSavedContact()
  
  
  start(async () => { await loadSavedContact(); }, 10000)
})
</script>
<style scoped>
.create-page {
  min-height: 100vh;
  background: #f5f5f5;
  padding-bottom: 80px;
}
.form {
  background: #ffffff;
  padding: 16px;
}
.form-item {
  margin-bottom: 20px;
}
.form-row {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
}
.form-row .form-item {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 0;
  margin-bottom: 0;
}
.form-row .input {
  width: 100%;
  min-width: 0;
}
.label {
  display: block;
  font-size: 14px;
  color: #303133;
  margin-bottom: 8px;
}
.label.required::before {
  content: '*';
  color: #F56C6C;
  margin-right: 4px;
}
.picker-value, .input, .textarea {
  width: 100%;
  height: 36px;
  padding: 8px 12px;
  line-height: 20px;
  background: #f5f5f5;
  border-radius: 4px;
  font-size: 14px;
  box-sizing: border-box;
}
.picker-value {
  color: #606266;
  line-height: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.picker-value::after {
  content: '▼';
  font-size: 10px;
  color: #909399;
  margin-left: 8px;
}
.textarea {
  height: 100px;
  padding: 12px;
  resize: none;
}
.char-count {
  display: block;
  text-align: right;
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
.images {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
.image-item {
  width: 80px;
  height: 80px;
  position: relative;
  border-radius: 4px;
  overflow: hidden;
}
.image-item image {
  width: 100%;
  height: 100%;
}
.delete-btn {
  position: absolute;
  top: 0;
  right: 0;
  width: 20px;
  height: 20px;
  background: rgba(0,0,0,0.5);
  color: #ffffff;
  text-align: center;
  line-height: 20px;
  font-size: 14px;
}
.add-image {
  width: 80px;
  height: 80px;
  background: #f5f5f5;
  border: 1px dashed #dcdfe6;
  border-radius: 4px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #909399;
}
.add-image .icon {
  font-size: 24px;
  line-height: 1;
}
.add-image .text {
  font-size: 11px;
  margin-top: 4px;
}
.submit-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 12px 16px;
  background: #ffffff;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}
.btn-submit {
  width: 100%;
  height: 44px;
  line-height: 44px;
  background: #409EFF;
  color: #ffffff;
  font-size: 15px;
  border: none;
  border-radius: 22px;
}
.btn-submit[disabled] {
  background: #dcdfe6;
}
</style>