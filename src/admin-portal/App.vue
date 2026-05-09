<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { currentProject, projects, allModules, getProjectModules, enterProject, exitProject } from './stores/project'

const router = useRouter()
const route = useRoute()

const isCollapse = ref(false)
const activeMenu = computed(() => route.path)

const userInfo = ref({
  username: 'admin',
  name: '管理员',
  role: 'Administrator'
})

// 侧边栏菜单
const menuItems = computed(() => {
  // 如果进入了项目，显示项目模块
  if (currentProject.value) {
    return [
      { key: 'project', title: '项目管理', icon: 'FolderOpened', path: '/project', isActive: true },
      { divider: true },
      { key: 'project-home', title: currentProject.value.name, icon: 'Folder', isHeader: true },
      { divider: true },
      ...getProjectModules(currentProject.value).map(m => ({
        key: m.key,
        title: m.name,
        icon: m.icon,
        path: m.path
      })),
      { divider: true },
      { key: 'exit', title: '退出项目', icon: 'ArrowLeft', path: '/project', isExit: true }
    ]
  }
  
  // 默认显示项目管理
  return [
    { key: 'dashboard', title: '首页', icon: 'Odometer', path: '/' },
    { key: 'project', title: '项目管理', icon: 'Folder', path: '/project' }
  ]
})

const handleMenuClick = (item: any) => {
  if (item.isExit) {
    exitProject()
    ElMessage.info('已退出项目')
  }
  router.push(item.path)
}

const handleCommand = (command: string) => {
  if (command === 'logout') {
    ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }).then(() => {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      localStorage.removeItem('currentProject')
      router.push('/login')
      ElMessage.success('已退出登录')
    }).catch(() => {})
  }
}

onMounted(() => {
  // 恢复项目状态或跳转到项目列表
  if (currentProject.value) {
    router.push('/project')
  } else {
    router.push('/project')
  }
})
</script>

<template>
  <el-container class="app-container">
    <!-- 侧边栏 -->
    <el-aside :width="isCollapse ? '64px' : '220px'" class="sidebar">
      <div class="logo">
        <el-icon size="24"><House /></el-icon>
        <span v-show="!isCollapse">WO物业</span>
      </div>
      
      <el-menu
        :default-active="activeMenu"
        :collapse="isCollapse"
        :collapse-transition="false"
        class="sidebar-menu"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
      >
        <template v-for="item in menuItems" :key="item.key">
          <el-menu-item 
            v-if="!item.divider && !item.isHeader && !item.isExit"
            :index="item.path"
            @click="handleMenuClick(item)"
          >
            <el-icon><component :is="item.icon" /></el-icon>
            <template #title>{{ item.title }}</template>
          </el-menu-item>
          
          <el-menu-item 
            v-else-if="item.isExit"
            :index="item.path"
            @click="handleMenuClick(item)"
            class="exit-item"
          >
            <el-icon><component :is="item.icon" /></el-icon>
            <template #title>{{ item.title }}</template>
          </el-menu-item>
          
          <el-divider v-else-if="item.divider" />
          
          <div v-else-if="item.isHeader" class="menu-header">
            <el-icon><Folder /></el-icon>
            <span>{{ item.title }}</span>
          </div>
        </template>
      </el-menu>
    </el-aside>
    
    <!-- 主内容区 -->
    <el-container>
      <!-- 顶部导航 -->
      <el-header class="header">
        <div class="header-left">
          <el-icon class="collapse-btn" @click="isCollapse = !isCollapse">
            <Fold v-if="!isCollapse" />
            <Expand v-else />
          </el-icon>
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item v-if="currentProject">
              {{ currentProject.name }}
            </el-breadcrumb-item>
            <el-breadcrumb-item v-else-if="route.path !== '/'">
              {{ route.meta.title || '管理' }}
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        
        <div class="header-right">
          <el-icon class="header-icon"><Bell /></el-icon>
          
          <el-dropdown @command="handleCommand" trigger="click">
            <span class="user-info">
              <el-avatar :size="32" style="background: #409eff;">
                {{ userInfo.name[0] }}
              </el-avatar>
              <span class="username">{{ userInfo.name }}</span>
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="logout">
                  <el-icon><SwitchButton /></el-icon>退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      
      <!-- 内容区 -->
      <el-main class="main-content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped>
.app-container {
  height: 100vh;
}

.sidebar {
  background: #304156;
  transition: width 0.3s;
  overflow-x: hidden;
}

.logo {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  color: #fff;
  font-size: 18px;
  font-weight: bold;
  border-bottom: 1px solid #3d4a5c;
}

.sidebar-menu {
  border-right: none;
  background: transparent;
}

.sidebar-menu:not(.el-menu--collapse) {
  width: 220px;
}

:deep(.el-menu-item) {
  height: 50px;
  line-height: 50px;
}

:deep(.el-menu-item:hover) {
  background: #263445 !important;
}

.exit-item {
  color: #e6a23c !important;
}

.exit-item:hover {
  background: #263445 !important;
}

:deep(.el-divider) {
  margin: 8px 20px !important;
  border-color: #3d4a5c !important;
}

.menu-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 20px;
  color: #409eff;
  font-weight: 600;
  font-size: 14px;
}

.header {
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 15px;
}

.collapse-btn {
  font-size: 20px;
  cursor: pointer;
  color: #666;
}

.collapse-btn:hover {
  color: #409eff;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.header-icon {
  font-size: 20px;
  color: #666;
  cursor: pointer;
}

.header-icon:hover {
  color: #409eff;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}

.username {
  color: #333;
  font-size: 14px;
}

.main-content {
  background: #f0f2f5;
  padding: 20px;
}
</style>
