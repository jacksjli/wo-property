<template>
  <view class="project-tab-bar-wrapper" :class="{ 'safe-area': isIphoneX }">
    <!-- 项目切换栏 -->
    <view class="project-switcher">
      <view
        v-for="project in projects"
        :key="project.projectCode"
        :class="['project-item', { active: currentProjectCode === project.projectCode }]"
        @click="switchProject(project)"
      >
        <text class="project-icon">{{ project.projectCode === 'YGHY001' ? '🌿' : '🏠' }}</text>
        <text class="project-name">{{ project.projectName }}</text>
      </view>
    </view>

    <!-- Tab 导航 -->
    <view class="tab-bar">
      <view
        v-for="tab in visibleTabs"
        :key="tab.path"
        :class="['tab-item', { active: currentPath === tab.path }]"
        @click="switchTab(tab)"
      >
        <view class="icon-wrapper">
          <text class="tab-emoji" :style="currentPath === tab.path ? activeEmojiStyle : inactiveEmojiStyle">{{ tab.emoji }}</text>
          <view v-if="currentPath === tab.path" class="active-pulse"></view>
        </view>
        <text class="tab-label" :style="currentPath === tab.path ? { color: activeColor } : { color: inactiveColor }">
          {{ tab.text }}
        </text>
        <view class="tab-line" :style="currentPath === tab.path ? { background: activeColor } : {}"></view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

const inactiveColor = '#999'
const activeColor = '#409EFF'

const allTabs = [
  {
    path: '/pages/owner/index',
    text: '报修',
    emoji: '🔧',
    roles: ['owner', 'operator', 'admin']
  },
  {
    path: '/pages/admin/dashboard',
    text: '管理',
    emoji: '📊',
    roles: ['operator', 'admin']
  },
  {
    path: '/pages/mine/index',
    text: '我的',
    emoji: '👤',
    roles: ['owner', 'operator', 'admin']
  }
]

const currentPath = ref('')
const isIphoneX = ref(false)
const projects = ref([])
const currentProjectCode = ref('')

const activeEmojiStyle = computed(() => ({
  transform: 'scale(1.2)',
  filter: 'drop-shadow(0 2rpx 8rpx rgba(64,158,255,0.5))'
}))

const inactiveEmojiStyle = computed(() => ({
  transform: 'scale(1)',
  filter: 'grayscale(0.3)'
}))

onMounted(() => {
  const pages = getCurrentPages()
  if (pages.length > 0) {
    currentPath.value = '/' + pages[pages.length - 1].route
  }

  uni.getSystemInfo({
    success: (res) => {
      isIphoneX.value = res.safeAreaInsets?.bottom > 0
    }
  })

  projects.value = uni.getStorageSync('projects') || []
  const current = uni.getStorageSync('currentProject') || {}
  currentProjectCode.value = current.projectCode || (projects.value[0]?.projectCode || '')
})

const visibleTabs = computed(() => {
  const role = uni.getStorageSync('userInfo')?.role || 'owner'
  return allTabs.filter(tab => tab.roles.includes(role))
})

function switchProject(project) {
  uni.setStorageSync('currentProject', project)
  currentProjectCode.value = project.projectCode
  uni.$emit('projectChanged', project)
}

function switchTab(tab) {
  if (currentPath.value === tab.path) return
  currentPath.value = tab.path
  uni.switchTab({
    url: tab.path,
    fail: () => uni.reLaunch({ url: tab.path })
  })
}
</script>

<style scoped>
.project-tab-bar-wrapper {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 999;
}

/* ========== 项目切换栏 ========== */
.project-switcher {
  display: flex;
  justify-content: center;
  gap: 24rpx;
  padding: 16rpx 40rpx;
  background: #ffffff;
  border-top: 1rpx solid #ebeef5;
  padding-bottom: 16rpx;
}

.project-item {
  display: flex;
  align-items: center;
  gap: 8rpx;
  padding: 8rpx 20rpx;
  border-radius: 24rpx;
  background: #f5f5f5;
  transition: all 0.3s ease;
}

.project-item.active {
  background: #409EFF;
}

.project-icon {
  font-size: 28rpx;
}

.project-name {
  font-size: 24rpx;
  color: #606266;
}

.project-item.active .project-name {
  color: #ffffff;
}

/* ========== Tab 导航 ========== */
.tab-bar {
  display: flex;
  align-items: center;
  justify-content: space-around;
  height: 100rpx;
  background: rgba(255, 255, 255, 0.98);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-top: 1rpx solid rgba(0, 0, 0, 0.05);
  padding-bottom: env(safe-area-inset-bottom);
  box-shadow: 0 -4rpx 20rpx rgba(0, 0, 0, 0.03);
}

.tab-item {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  height: 100%;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.icon-wrapper {
  position: relative;
  width: 56rpx;
  height: 56rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 2rpx;
}

.tab-emoji {
  font-size: 44rpx;
  display: block;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  line-height: 1;
}

.tab-item.active .tab-emoji {
  animation: bounce-in 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes bounce-in {
  0% { transform: scale(1); }
  40% { transform: scale(1.3); }
  70% { transform: scale(1.1); }
  100% { transform: scale(1.2); }
}

.active-pulse {
  position: absolute;
  top: -2rpx;
  right: -4rpx;
  width: 12rpx;
  height: 12rpx;
  background: linear-gradient(135deg, #ff6b6b, #ffa502);
  border-radius: 50%;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(1.4); }
}

.tab-label {
  font-size: 20rpx;
  font-weight: 500;
  transition: color 0.3s ease;
}

.tab-item.active .tab-label {
  font-weight: 600;
  color: #409EFF;
}

.tab-line {
  position: absolute;
  bottom: 8rpx;
  left: 50%;
  transform: translateX(-50%);
  width: 30rpx;
  height: 3rpx;
  border-radius: 2rpx;
  background: transparent;
}

.tab-item.active .tab-line {
  background: #409EFF;
  animation: line-slide 0.3s cubic-bezier(0.4, 0, 0.2, 1) both;
}

@keyframes line-slide {
  from { width: 0; opacity: 0; }
  to { width: 30rpx; opacity: 1; }
}

.safe-area {
  padding-bottom: env(safe-area-inset-bottom);
}
</style>