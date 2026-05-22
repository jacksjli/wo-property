<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { currentProject, projects, allModules, getProjectModules, enterProject, exitProject, initProject } from './stores/project'
import ErrorBoundary from './components/ErrorBoundary.vue'

const router = useRouter()
const route = useRoute()

const isCollapse = ref(false)
const activeMenu = computed(() => route.path)

// 分类折叠状态
const collapsedCategories = ref<Set<string>>(new Set())

// 切换分类折叠状态
const toggleCategory = (catKey: string) => {
  if (collapsedCategories.value.has(catKey)) {
    collapsedCategories.value.delete(catKey)
  } else {
    collapsedCategories.value.add(catKey)
  }
  // 触发响应式更新
  collapsedCategories.value = new Set(collapsedCategories.value)
}

const userInfo = ref({
  username: 'admin',
  name: '管理员',
  role: 'Administrator'
})

// 侧边栏菜单 - 按八大类分组
const menuItems = computed(() => {
  // 如果进入了项目，显示项目模块（按八大类分组）
  if (currentProject.value) {
    const projectMods = new Set(getProjectModules(currentProject.value).map((m: any) => m.name))

    // 八大类定义
    const categories = [
      {
        title: '🏗️ 基础数据',
        items: [
          { name: '大区省市区', key: 'region', path: '/master/regions', icon: 'Location' },
          { name: '区域管理', key: 'area', path: '/master/areas', icon: 'LocationInformation' },
          { name: '楼栋管理', key: 'building', path: '/master/buildings', icon: 'OfficeBuilding' },
          { name: '房号管理', key: 'room', path: '/master/rooms', icon: 'House' },
          { name: '部门管理', key: 'department', path: '/master/departments', icon: 'OfficeBuilding' },
          { name: '工种管理', key: 'jobType', path: '/master/job-types', icon: 'Tools' },
          { name: '设备类型', key: 'deviceType', path: '/master/device-types', icon: 'Monitor' },
          { name: '供应商管理', key: 'supplier', path: '/master/suppliers', icon: 'Van' },
          { name: '字段管理', key: 'fieldDefinition', path: '/master/field-definitions', icon: 'Document' },
        ]
      },
      {
        title: '🚪 钥匙/门禁',
        items: [
          { name: '钥匙管理', key: 'key', path: '/key', icon: 'Key' },
          { name: '权限控制', key: 'accessControl', path: '/access-control', icon: 'Lock' },
        ]
      },
      {
        title: '🏠 住户/房产',
        items: [
          { name: '住户管理', key: 'resident', path: '/resident', icon: 'House' },
          { name: '车位管理', key: 'parking', path: '/parking', icon: 'Van' },
          { name: '装修管理', key: 'renovation', path: '/renovation', icon: 'Tools' },
        ]
      },
      {
        title: '💰 财务类',
        items: [
          { name: '财务管理', key: 'finance', path: '/finance', icon: 'Money' },
          { name: '缴费管理', key: 'payment', path: '/payment', icon: 'CreditCard' },
          { name: '合同管理', key: 'contract', path: '/contract', icon: 'Document' },
        ]
      },
      {
        title: '📋 工单/任务',
        items: [
          { name: '工单管理', key: 'ticket', path: '/tickets', icon: 'Tickets' },
          { name: '巡检管理', key: 'inspection', path: '/inspection', icon: 'Location' },
          { name: '派单规则', key: 'dispatch', path: '/dispatch-rules', icon: 'Link' },
          { name: '超时设置', key: 'timeout', path: '/timeout-settings', icon: 'Clock' },
          { name: '工单类型', key: 'ticketType', path: '/ticket-type', icon: 'Tickets' },
        ]
      },
      {
        title: '🔧 设备/保洁',
        items: [
          { name: '设备管理', key: 'device', path: '/device', icon: 'Monitor' },
          { name: '物料管理', key: 'material', path: '/material', icon: 'Box' },
          { name: '物料分类', key: 'materialCategory', path: '/material-category', icon: 'Box' },
          { name: '清洁管理', key: 'cleaning', path: '/cleaning', icon: 'Brush' },
          { name: '项目跟踪', key: 'projectTracking', path: '/project-tracking', icon: 'Document' },
        ]
      },
      {
        title: '📦 物流/生活',
        items: [
          { name: '快递管理', key: 'express', path: '/express', icon: 'Box' },
          { name: '访客管理', key: 'visitor', path: '/visitor', icon: 'User' },
          { name: '社区管理', key: 'community', path: '/community', icon: 'User' },
          { name: '社区活动', key: 'communityActivity', path: '/community-activities', icon: 'User' },
          { name: '配送管理', key: 'delivery', path: '/delivery', icon: 'Van' },
        ]
      },
      {
        title: '📢 消息中心',
        items: [
          { name: '公告管理', key: 'announcement', path: '/announcements', icon: 'Bell' },
          { name: '消息管理', key: 'notification', path: '/notification', icon: 'Bell' },
          { name: '消息模板', key: 'messageTemplate', path: '/message-template', icon: 'Document' },
          { name: '外部人员', key: 'externalPerson', path: '/external-person', icon: 'User' },
        ]
      },
      {
        title: '📊 数据报表',
        items: [
          { name: '统计分析', key: 'statistics', path: '/statistics', icon: 'DataAnalysis' },
          { name: '设备报表', key: 'deviceReport', path: '/reports?tab=device', icon: 'DataAnalysis' },
          { name: '工单报表', key: 'ticketReport', path: '/reports?tab=ticket', icon: 'DataAnalysis' },
          { name: '物料报表', key: 'materialReport', path: '/reports?tab=material', icon: 'DataAnalysis' },
          { name: '满意度调查', key: 'satisfaction', path: '/reports?tab=satisfaction', icon: 'Star' },
          { name: '综合报表', key: 'generalReport', path: '/reports?tab=general', icon: 'Document' },
        ]
      },
      {
        title: '📦 采购库存',
        items: [
          { name: '采购订单', key: 'purchaseOrder', path: '/reports?tab=purchase', icon: 'ShoppingCart' },
          { name: '库存事务', key: 'stockTransaction', path: '/reports?tab=stock', icon: 'Box' },
          { name: '枚举定义', key: 'enumDefinition', path: '/reports?tab=enum', icon: 'List' },
        ]
      },
      {
        title: '👥 人员/系统',
        items: [
          { name: '人员管理', key: 'personnel', path: '/personnel', icon: 'User' },
        ]
      },
    ]

    // 构建菜单：只显示项目中启用的模块
    const menu: any[] = [
      { key: 'project', title: '项目管理', icon: 'FolderOpened', path: '/project', isActive: true },
      { divider: true },
      { key: 'project-home', title: currentProject.value.name, icon: 'Folder', isHeader: true },
      { divider: true },
    ]

    for (const cat of categories) {
      const enabledItems = cat.items.filter(item => projectMods.has(item.name))
      if (enabledItems.length > 0) {
        const catKey = cat.title
        const isCollapsed = collapsedCategories.value.has(catKey)
        menu.push({ key: 'cat-' + cat.title, title: cat.title, isHeader: true, isCategory: true, isCollapsed, catKey })
        if (!isCollapsed) {
          for (const item of enabledItems) {
            menu.push({ key: item.key, title: item.name, icon: item.icon, path: item.path })
          }
        }
        menu.push({ divider: true })
      }
    }

    menu.push({ key: 'exit', title: '退出项目', icon: 'ArrowLeft', path: '/project', isExit: true })
    return menu
  }

  // 默认显示所有菜单（含基础配置）
  return [
    { key: 'dashboard', title: '首页', icon: 'Odometer', path: '/' },
    { key: 'project', title: '项目管理', icon: 'Folder', path: '/project' },
    { divider: true },
    { key: 'master-header', title: '🏗️ 基础数据', isHeader: true },
    { key: 'master-regions', title: '大区省市区', icon: 'Location', path: '/master/regions' },
    { key: 'master-departments', title: '部门管理', icon: 'OfficeBuilding', path: '/master/departments' },
    { key: 'master-areas', title: '区域管理', icon: 'LocationInformation', path: '/master/areas' },
    { key: 'master-buildings', title: '楼栋管理', icon: 'OfficeBuilding', path: '/master/buildings' },
    { key: 'master-rooms', title: '房号管理', icon: 'House', path: '/master/rooms' },
    { key: 'master-job-types', title: '工种管理', icon: 'Tools', path: '/master/job-types' },
    { key: 'master-suppliers', title: '供应商管理', icon: 'Van', path: '/master/suppliers' },
    { key: 'master-device-types', title: '设备类型', icon: 'Monitor', path: '/master/device-types' },
    { key: 'master-field', title: '字段管理', icon: 'Document', path: '/master/field-definitions' },
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
  // 初始化项目状态
  initProject()
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
        :router="true"
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

          <div
            v-else-if="item.isHeader && item.isCategory"
            class="menu-header category-header"
            @click="toggleCategory(item.catKey)"
          >
            <el-icon><Folder /></el-icon>
            <span class="category-title">{{ item.title }}</span>
            <el-icon class="collapse-icon"><ArrowDown v-if="!item.isCollapsed" /><ArrowUp v-else /></el-icon>
          </div>

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
        <ErrorBoundary>
          <router-view />
        </ErrorBoundary>
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

.category-header {
  cursor: pointer;
  user-select: none;
  transition: background 0.2s;
}

.category-header:hover {
  background: #263445;
}

.category-title {
  flex: 1;
}

.collapse-icon {
  font-size: 12px;
  transition: transform 0.3s;
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
