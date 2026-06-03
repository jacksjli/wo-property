<template>
  <view class="tabbar-wrapper">
    <!-- 报修 tab -->
    <view 
      class="tab-item" 
      :class="{ active: activeTab === 'repair' }"
      @click="switchTab('repair')"
    >
      <image 
        class="tab-icon" 
        :src="activeTab === 'repair' ? '/static/ticket-active.png' : '/static/ticket.png'"
        mode="aspectFit"
      />
      <text class="tab-text">报修</text>
    </view>

    <!-- 管理 tab (只有Matched/Guest可见) -->
    <view 
      v-if="showAdminTab"
      class="tab-item" 
      :class="{ active: activeTab === 'admin' }"
      @click="switchTab('admin')"
    >
      <image 
        class="tab-icon" 
        :src="activeTab === 'admin' ? '/static/admin-active.png' : '/static/admin.png'"
        mode="aspectFit"
      />
      <text class="tab-text">管理</text>
    </view>

    <!-- 工程师 tab (只有Matched可见) -->
    <view 
      v-if="showEngineerTab"
      class="tab-item" 
      :class="{ active: activeTab === 'engineer' }"
      @click="switchTab('engineer')"
    >
      <image 
        class="tab-icon" 
        :src="activeTab === 'engineer' ? '/static/engineer-active.png' : '/static/engineer.png'"
        mode="aspectFit"
      />
      <text class="tab-text">工程师</text>
    </view>

    <!-- 我的 tab -->
    <view 
      class="tab-item" 
      :class="{ active: activeTab === 'mine' }"
      @click="switchTab('mine')"
    >
      <image 
        class="tab-icon" 
        :src="activeTab === 'mine' ? '/static/mine-active.png' : '/static/mine.png'"
        mode="aspectFit"
      />
      <text class="tab-text">我的</text>
    </view>
  </view>
</template>

<script>
import { getEmployeeType } from '@/api/personAuth.js'

export default {
  data() {
    return {
      activeTab: 'repair'
    }
  },
  computed: {
    employeeType() {
      return wx.getStorageSync('employeeType') || 'Guest'
    },
    showAdminTab() {
      // Guest 和 Matched 都有管理 tab
      return this.employeeType === 'Guest' || this.employeeType === 'Matched'
    },
    showEngineerTab() {
      // 只有 Matched 有工程师 tab
      return this.employeeType === 'Matched'
    }
  },
  methods: {
    switchTab(tab) {
      this.activeTab = tab
      
      const pages = {
        repair: '/pages/owner/index',
        admin: '/pages/admin/dashboard',
        engineer: '/pages/ticket/list',
        mine: '/pages/mine/index'
      }
      
      const url = pages[tab]
      if (url) {
        wx.switchTab({ url })
      }
    },
    updateActiveTab() {
      const pages = getCurrentPages()
      if (pages.length > 0) {
        const currentPage = pages[pages.length - 1]
        const route = '/' + currentPage.route
        
        if (route.startsWith('/pages/owner')) {
          this.activeTab = 'repair'
        } else if (route.startsWith('/pages/admin')) {
          this.activeTab = 'admin'
        } else if (route.startsWith('/pages/ticket')) {
          this.activeTab = 'engineer'
        } else if (route.startsWith('/pages/mine')) {
          this.activeTab = 'mine'
        }
      }
    }
  },
  mounted() {
    this.updateActiveTab()
    
    // 监听 TabBar 点击事件
    wx.eventCenter.on('tabbar.switch', (tab) => {
      this.switchTab(tab)
    })
  },
  onShow() {
    this.updateActiveTab()
  }
}
</script>

<style scoped>
.tabbar-wrapper {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  display: flex;
  height: 100rpx;
  background: #fff;
  border-top: 1px solid #e5e5e5;
  padding-bottom: env(safe-area-inset-bottom);
  z-index: 999;
}

.tab-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding-top: 16rpx;
}

.tab-icon {
  width: 48rpx;
  height: 48rpx;
  margin-bottom: 8rpx;
}

.tab-text {
  font-size: 22rpx;
  color: #999;
}

.tab-item.active .tab-text {
  color: #409EFF;
}
</style>