<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <aside class="sidebar">
      <div class="sidebar-header">
        <h1 class="logo">WO物业管理</h1>
        <p class="subtitle">智能管理平台</p>
      </div>
      
      <nav class="sidebar-nav">
        <router-link
          to="/"
          class="nav-item"
          :class="{ active: $route.name === 'Dashboard' }"
        >
          <el-icon><House /></el-icon>
          <span>仪表板</span>
        </router-link>

        <!-- 业务服务 -->
        <div class="nav-divider"></div>
        <div class="section-title">业务服务</div>

        <router-link
          to="/tickets"
          class="nav-item"
          :class="{ active: $route.name === 'Ticket' || $route.name === 'DeviceDetail' }"
        >
          <el-icon><Ticket /></el-icon>
          <span>工单管理</span>
        </router-link>

        <router-link
          to="/device"
          class="nav-item"
          :class="{ active: $route.name === 'Device' || $route.name === 'DeviceDetail' }"
        >
          <el-icon><Monitor /></el-icon>
          <span>设备管理</span>
        </router-link>

        <router-link
          to="/material"
          class="nav-item"
          :class="{ active: $route.name === 'Material' }"
        >
          <el-icon><Box /></el-icon>
          <span>物料管理</span>
        </router-link>

        <router-link
          to="/resident"
          class="nav-item"
          :class="{ active: $route.name === 'Resident' }"
        >
          <el-icon><House /></el-icon>
          <span>住户管理</span>
        </router-link>

        <router-link
          to="/visitor"
          class="nav-item"
          :class="{ active: $route.name === 'Visitor' }"
        >
          <el-icon><User /></el-icon>
          <span>访客管理</span>
        </router-link>

        <router-link
          to="/contract"
          class="nav-item"
          :class="{ active: $route.name === 'Contract' }"
        >
          <el-icon><Document /></el-icon>
          <span>合同管理</span>
        </router-link>

        <router-link
          to="/finance"
          class="nav-item"
          :class="{ active: $route.name === 'Finance' }"
        >
          <el-icon><Money /></el-icon>
          <span>财务管理</span>
        </router-link>

        <router-link
          to="/inspection"
          class="nav-item"
          :class="{ active: $route.name === 'Inspection' }"
        >
          <el-icon><Location /></el-icon>
          <span>巡检管理</span>
        </router-link>

        <router-link
          to="/complaint"
          class="nav-item"
          :class="{ active: $route.name === 'Complaint' }"
        >
          <el-icon><Warning /></el-icon>
          <span>投诉管理</span>
        </router-link>

        <router-link
          to="/key"
          class="nav-item"
          :class="{ active: $route.name === 'Key' }"
        >
          <el-icon><Key /></el-icon>
          <span>钥匙管理</span>
        </router-link>

        <router-link
          to="/notification"
          class="nav-item"
          :class="{ active: $route.name === 'Notification' }"
        >
          <el-icon><Bell /></el-icon>
          <span>通知中心</span>
        </router-link>

        <router-link
          to="/express"
          class="nav-item"
          :class="{ active: $route.name === 'Express' }"
        >
          <el-icon><Box /></el-icon>
          <span>快递管理</span>
        </router-link>

        <router-link
          to="/delivery"
          class="nav-item"
          :class="{ active: $route.name === 'Delivery' }"
        >
          <el-icon><Food /></el-icon>
          <span>外卖管理</span>
        </router-link>

        <router-link
          to="/statistics"
          class="nav-item"
          :class="{ active: $route.name === 'Statistics' }"
        >
          <el-icon><DataAnalysis /></el-icon>
          <span>统计分析</span>
        </router-link>

        <router-link
          to="/payment"
          class="nav-item"
          :class="{ active: $route.name === 'Payment' }"
        >
          <el-icon><Coin /></el-icon>
          <span>收费大厅</span>
        </router-link>

        <router-link
          to="/parking"
          class="nav-item"
          :class="{ active: $route.name === 'Parking' }"
        >
          <el-icon><Van /></el-icon>
          <span>停车管理</span>
        </router-link>

        <router-link
          to="/announcements"
          class="nav-item"
          :class="{ active: $route.name === 'Announcements' }"
        >
          <el-icon><Bell /></el-icon>
          <span>公告管理</span>
        </router-link>

        <router-link
          to="/access-control"
          class="nav-item"
          :class="{ active: $route.name === 'AccessControl' }"
        >
          <el-icon><Key /></el-icon>
          <span>门禁管理</span>
        </router-link>

        <router-link
          to="/renovation"
          class="nav-item"
          :class="{ active: $route.name === 'Renovation' }"
        >
          <el-icon><HomeFilled /></el-icon>
          <span>装修管理</span>
        </router-link>

        <router-link
          to="/cleaning"
          class="nav-item"
          :class="{ active: $route.name === 'Cleaning' }"
        >
          <el-icon><Brush /></el-icon>
          <span>保洁绿化</span>
        </router-link>

        <router-link
          to="/community"
          class="nav-item"
          :class="{ active: $route.name === 'Community' }"
        >
          <el-icon><UserFilled /></el-icon>
          <span>社区活动</span>
        </router-link>

        <router-link
          to="/contractor"
          class="nav-item"
          :class="{ active: $route.name === 'Contractor' }"
        >
          <el-icon><Tools /></el-icon>
          <span>工程外包</span>
        </router-link>

        <!-- 基础配置 -->
        <div class="nav-divider"></div>
        <div class="section-title">基础配置</div>

        <router-link
          to="/master/regions"
          class="nav-item"
          :class="{ active: $route.name === 'MasterRegions' }"
        >
          <el-icon><OfficeBuilding /></el-icon>
          <span>大区省市区</span>
        </router-link>

        <router-link
          to="/master/departments"
          class="nav-item"
          :class="{ active: $route.name === 'MasterDepartments' }"
        >
          <el-icon><Folder /></el-icon>
          <span>部门管理</span>
        </router-link>

        <router-link
          to="/master/areas"
          class="nav-item"
          :class="{ active: $route.name === 'MasterAreas' }"
        >
          <el-icon><LocationInformation /></el-icon>
          <span>区域管理</span>
        </router-link>

        <router-link
          to="/master/buildings"
          class="nav-item"
          :class="{ active: $route.name === 'MasterBuildings' }"
        >
          <el-icon><OfficeBuilding /></el-icon>
          <span>楼栋管理</span>
        </router-link>

        <router-link
          to="/master/rooms"
          class="nav-item"
          :class="{ active: $route.name === 'MasterRooms' }"
        >
          <el-icon><Location /></el-icon>
          <span>房号管理</span>
        </router-link>

        <router-link
          to="/master/job-types"
          class="nav-item"
          :class="{ active: $route.name === 'MasterJobTypes' }"
        >
          <el-icon><Tools /></el-icon>
          <span>工种管理</span>
        </router-link>

        <router-link
          to="/master/suppliers"
          class="nav-item"
          :class="{ active: $route.name === 'MasterSuppliers' }"
        >
          <el-icon><Van /></el-icon>
          <span>供应商管理</span>
        </router-link>

        <router-link
          to="/master/device-types"
          class="nav-item"
          :class="{ active: $route.name === 'MasterDeviceTypes' }"
        >
          <el-icon><Monitor /></el-icon>
          <span>设备类型</span>
        </router-link>

        <router-link
          to="/person"
          class="nav-item"
          :class="{ active: $route.name === 'Person' }"
        >
          <el-icon><User /></el-icon>
          <span>人员中心</span>
        </router-link>

        <router-link
          to="/hr-settings"
          class="nav-item"
          :class="{ active: $route.name === 'HRSettings' }"
        >
          <el-icon><UserFilled /></el-icon>
          <span>人员管理</span>
        </router-link>

        <!-- 个人资料 -->
        <div class="nav-divider"></div>

        <router-link
          to="/user"
          class="nav-item"
          :class="{ active: $route.name === 'User' }"
        >
          <el-icon><User /></el-icon>
          <span>个人资料</span>
        </router-link>

        <!-- 管理员菜单 -->
        <div v-if="authStore.isAdmin" class="admin-section">
          <div class="nav-divider"></div>
          <div class="section-title">系统管理</div>

          <router-link
            to="/user"
            class="nav-item"
            :class="{ active: $route.name === 'User' }"
          >
            <el-icon><UserFilled /></el-icon>
            <span>用户管理</span>
          </router-link>

          <router-link
            to="/settings"
            class="nav-item"
            :class="{ active: $route.name === 'Settings' }"
          >
            <el-icon><Setting /></el-icon>
            <span>系统设置</span>
          </router-link>
        </div>
      </nav>
      
      <div class="sidebar-footer">
        <div class="user-info">
          <el-avatar :size="32" :src="userAvatar" />
          <div class="user-details">
            <span class="user-name">{{ authStore.userName }}</span>
            <span class="user-role">{{ authStore.userRole }}</span>
          </div>
        </div>
        <el-button 
          type="text" 
          class="logout-btn"
          @click="handleLogout"
        >
          <el-icon><SwitchButton /></el-icon>
          退出
        </el-button>
      </div>
    </aside>
    
    <!-- 主内容区 -->
    <main class="main-content">
      <!-- 顶部导航栏 -->
      <header class="topbar">
        <div class="topbar-left">
          <h2 class="page-title">{{ pageTitle }}</h2>
        </div>
        <div class="topbar-right">
          <el-badge :value="notificationCount" :max="99" class="notification-badge">
            <el-button circle>
              <el-icon><Bell /></el-icon>
            </el-button>
          </el-badge>
          <el-dropdown>
            <span class="user-dropdown">
              {{ authStore.userName }}
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="$router.push('/profile')">
                  <el-icon><User /></el-icon>
                  个人资料
                </el-dropdown-item>
                <el-dropdown-item divided @click="handleLogout">
                  <el-icon><SwitchButton /></el-icon>
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </header>
      
      <!-- 页面内容 -->
      <div class="content-wrapper">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { 
  House, 
  Ticket, 
  Monitor, 
  Box,
  Bell,
  User, 
  UserFilled,
  Setting,
  SwitchButton, 
  ArrowDown,
  Van,
  Coin,
  Brush,
  HomeFilled,
  Key,
  Tools
} from '@element-plus/icons-vue';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

// 计算页面标题
const pageTitle = computed(() => {
  return route.meta.title || 'WO物业管理';
});

// 用户头像
const userAvatar = computed(() => {
  // 这里可以根据用户信息生成头像
  return `https://api.dicebear.com/7.x/avataaars/svg?seed=${authStore.userName}`;
});

// 通知数量（示例）
const notificationCount = computed(() => {
  // 这里可以连接后端获取实际通知数量
  return 3;
});

// 处理退出登录
const handleLogout = () => {
  authStore.userLogout();
  router.push('/login');
};
</script>

<style scoped>
.main-layout {
  display: flex;
  height: 100vh;
  background-color: #f5f7fa;
}

/* 侧边栏样式 */
.sidebar {
  width: 240px;
  background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);
  color: white;
  display: flex;
  flex-direction: column;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
  z-index: 10;
}

.sidebar-header {
  padding: 24px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.logo {
  font-size: 20px;
  font-weight: 700;
  margin: 0 0 4px 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.subtitle {
  font-size: 12px;
  color: #94a3b8;
  margin: 0;
}

.sidebar-nav {
  flex: 1;
  padding: 20px 0;
}

.nav-item {
  display: flex;
  align-items: center;
  padding: 12px 20px;
  color: #cbd5e1;
  text-decoration: none;
  transition: all 0.3s ease;
}

.nav-item:hover {
  background-color: rgba(255, 255, 255, 0.05);
  color: white;
}

.nav-item.active {
  background-color: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
  border-right: 3px solid #3b82f6;
}

.nav-item .el-icon {
  margin-right: 12px;
  font-size: 18px;
}

.nav-divider {
  height: 1px;
  background-color: rgba(255, 255, 255, 0.1);
  margin: 16px 20px;
}

.section-title {
  padding: 0 20px 4px 20px;
  font-size: 11px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.45);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.sidebar-footer {
  padding: 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.user-info {
  display: flex;
  align-items: center;
  margin-bottom: 12px;
}

.user-details {
  margin-left: 12px;
  display: flex;
  flex-direction: column;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
}

.user-role {
  font-size: 12px;
  color: #94a3b8;
}

.logout-btn {
  width: 100%;
  color: #94a3b8 !important;
}

.logout-btn:hover {
  color: white !important;
}

/* 主内容区样式 */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.topbar {
  height: 64px;
  background-color: white;
  border-bottom: 1px solid #e5e7eb;
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0;
  color: #1f2937;
}

.topbar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.notification-badge {
  cursor: pointer;
}

.user-dropdown {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 8px 12px;
  border-radius: 6px;
  transition: background-color 0.3s;
}

.user-dropdown:hover {
  background-color: #f3f4f6;
}

/* 内容区域 */
.content-wrapper {
  flex: 1;
  padding: 24px;
  overflow-y: auto;
}

/* 过渡动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
