<template>
  <view class="main-page">
    <!-- 动态内容区域 -->
    <view class="content">
      <!-- 报修 tab content -->
      <view v-if="activeTab === 'repair'" class="tab-content">
        <include src="/pages/owner/index.vue" />
      </view>
      
      <!-- 管理 tab content -->
      <view v-if="activeTab === 'admin'" class="tab-content">
        <include src="/pages/admin/dashboard.vue" />
      </view>
      
      <!-- 工程师 tab content -->
      <view v-if="activeTab === 'engineer'" class="tab-content">
        <include src="/pages/ticket/list.vue" />
      </view>
      
      <!-- 我的 tab content -->
      <view v-if="activeTab === 'mine'" class="tab-content">
        <include src="/pages/mine/index.vue" />
      </view>
    </view>
    
    <!-- 自定义 TabBar -->
    <custom-tabbar 
      :activeTab="activeTab" 
      @switch="onTabSwitch"
      :employeeType="employeeType"
    />
  </view>
</template>

<script>
import customTabbar from '@/components/custom-tabbar.vue'
import { getEmployeeType } from '@/api/personAuth.js'

export default {
  components: { customTabbar },
  data() {
    return {
      activeTab: 'repair',
      employeeType: 'Guest'
    }
  },
  onLaunch() {
    // 获取员工类型
    const type = wx.getStorageSync('employeeType') || 'Guest'
    this.employeeType = type
    
    // 决定默认 tab
    if (type === 'Matched') {
      this.activeTab = 'repair'  // 默认显示报修
    } else if (type === 'Guest') {
      this.activeTab = 'repair'  // Guest 也默认显示报修
    }
  },
  onShow() {
    // 更新 activeTab
    const pages = getCurrentPages()
    if (pages.length > 0) {
      const currentPage = pages[pages.length - 1]
      const route = currentPage.route || ''
      this.updateActiveTabFromRoute('/' + route)
    }
  },
  methods: {
    onTabSwitch(tab) {
      this.activeTab = tab
    },
    updateActiveTabFromRoute(route) {
      if (route.includes('/pages/owner') || route.includes('/pages/repair')) {
        this.activeTab = 'repair'
      } else if (route.includes('/pages/admin')) {
        this.activeTab = 'admin'
      } else if (route.includes('/pages/ticket')) {
        this.activeTab = 'engineer'
      } else if (route.includes('/pages/mine')) {
        this.activeTab = 'mine'
      }
    }
  }
}
</script>

<style scoped>
.main-page {
  min-height: 100vh;
  background: #f5f5f5;
  padding-bottom: 120rpx;
}

.content {
  min-height: calc(100vh - 120rpx);
}

.tab-content {
  min-height: calc(100vh - 120rpx);
}
</style>