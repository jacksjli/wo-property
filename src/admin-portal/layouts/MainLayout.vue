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
          to="/notification" 
          class="nav-item"
          :class="{ active: $route.name === 'Notification' }"
        >
          <el-icon><Bell /></el-icon>
          <span>通知中心</span>
        </router-link>
        
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
          <div class="section-title">管理员</div>
          
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
  ArrowDown 
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
