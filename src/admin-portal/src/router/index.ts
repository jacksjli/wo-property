import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'

// 懒加载包装器：单个模块加载失败不影响其他模块
const safeImport = (componentPath: string) => {
  return () => import(/* @vite-ignore */ componentPath).catch(err => {
    console.error(`[Router] Failed to load module: ${componentPath}`, err)
    return {
      template: `<el-result icon="error" title="模块加载失败" sub-title="请刷新页面或联系管理员"></el-result>`
    }
  })
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: safeImport('../views/LoginView.vue'),
    meta: { title: '登录' }
  },
  {
    path: '/',
    name: 'Dashboard',
    component: safeImport('../views/DashboardView.vue'),
    meta: { title: '首页' }
  },
  {
    path: '/tickets',
    name: 'Ticket',
    component: safeImport('../views/ticket/TicketList.vue'),
    meta: { title: '工单管理' }
  },
  {
    path: '/announcements',
    name: 'Announcement',
    component: safeImport('../views/announcement/AnnouncementList.vue'),
    meta: { title: '公告管理' }
  },
  {
    path: '/project',
    name: 'Project',
    component: safeImport('../views/project/ProjectList.vue'),
    meta: { title: '项目管理' }
  },
  {
    path: '/device',
    name: 'Device',
    component: safeImport('../views/device/DeviceList.vue'),
    meta: { title: '设备管理' }
  },
  {
    path: '/material',
    name: 'Material',
    component: safeImport('../views/material/MaterialList.vue'),
    meta: { title: '物料管理' }
  },
  {
    path: '/material-category',
    name: 'MaterialCategory',
    component: safeImport('../views/materialCategory/MaterialCategoryList.vue'),
    meta: { title: '物料分类' }
  },
  {
    path: '/notification',
    name: 'Notification',
    component: safeImport('../views/notification/NotificationList.vue'),
    meta: { title: '通知管理' }
  },
  {
    path: '/message-template',
    name: 'MessageTemplate',
    component: safeImport('../views/messageTemplate/MessageTemplateList.vue'),
    meta: { title: '消息模板' }
  },
  {
    path: '/contract',
    name: 'Contract',
    component: safeImport('../views/contract/ContractList.vue'),
    meta: { title: '合同管理' }
  },
  {
    path: '/finance',
    name: 'Finance',
    component: safeImport('../views/finance/FinanceList.vue'),
    meta: { title: '财务管理' }
  },
  {
    path: '/inspection',
    name: 'Inspection',
    component: safeImport('../views/inspection/InspectionList.vue'),
    meta: { title: '巡检管理' }
  },
  {
    path: '/complaint',
    name: 'Complaint',
    component: safeImport('../views/complaint/ComplaintList.vue'),
    meta: { title: '投诉管理' }
  },
  {
    path: '/key',
    name: 'Key',
    component: safeImport('../views/key/KeyList.vue'),
    meta: { title: '钥匙管理' }
  },
  {
    path: '/visitor',
    name: 'Visitor',
    component: safeImport('../views/visitor/VisitorList.vue'),
    meta: { title: '访客管理' }
  },
  {
    path: '/external-person',
    name: 'ExternalPerson',
    component: safeImport('../views/externalPerson/ExternalPersonList.vue'),
    meta: { title: '外部人员' }
  },
  {
    path: '/parking',
    name: 'Parking',
    component: safeImport('../views/parking/ParkingList.vue'),
    meta: { title: '车位管理' }
  },
  {
    path: '/payment',
    name: 'Payment',
    component: safeImport('../views/payment/PaymentList.vue'),
    meta: { title: '缴费管理' }
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: safeImport('../views/statistics/StatisticsView.vue'),
    meta: { title: '统计分析' }
  },
  {
    path: '/reports',
    name: 'Reports',
    component: safeImport('../views/reports/ReportsView.vue'),
    meta: { title: '报表中心' }
  },
  {
    path: '/user',
    name: 'User',
    component: safeImport('../views/user/UserList.vue'),
    meta: { title: '用户管理' }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: safeImport('../views/settings/SystemSettingsView.vue'),
    meta: { title: '系统设置' }
  },
  {
    path: '/personnel',
    name: 'Personnel',
    component: safeImport('../views/personnel/PersonnelList.vue'),
    meta: { title: '人员管理' }
  },
  {
    path: '/takeout',
    name: 'Takeout',
    component: safeImport('../views/takeout/TakeoutList.vue'),
    meta: { title: '外卖管理' }
  },
  {
    path: '/resident',
    name: 'Resident',
    component: safeImport('../views/resident/ResidentList.vue'),
    meta: { title: '住户管理' }
  },
  {
    path: '/ticket-type',
    name: 'TicketType',
    component: safeImport('../views/ticketType/TicketTypeList.vue'),
    meta: { title: '工单类型' }
  },
  {
    path: '/dispatch-rules',
    name: 'DispatchRules',
    component: safeImport('../views/dispatch/DispatchRules.vue'),
    meta: { title: '派单规则' }
  },
  {
    path: '/timeout-settings',
    name: 'TimeoutSettings',
    component: safeImport('../views/dispatch/TimeoutSettings.vue'),
    meta: { title: '超时规则' }
  },
  {
    path: '/project-tracking',
    name: 'ProjectTracking',
    component: safeImport('../views/project/ProjectTrackingList.vue'),
    meta: { title: '项目跟踪' }
  },
  {
    path: '/field-management',
    name: 'FieldManagement',
    component: safeImport('../views/FieldManagementView.vue'),
    meta: { title: '字段管理', requiresAdmin: false }
  },
  {
    path: '/master/field-definitions',
    name: 'FieldDefinitions',
    component: safeImport('../views/FieldManagementView.vue'),
    meta: { title: '字段管理', requiresAdmin: false }
  },
  {
    path: '/module-fields',
    name: 'ModuleFields',
    component: safeImport('../views/ModuleFieldsView.vue'),
    meta: { title: '模块字段配置', requiresAdmin: false }
  },
  {
    path: '/master/regions',
    name: 'MasterRegions',
    component: safeImport('../views/master/RegionList.vue'),
    meta: { title: '大区省市区' }
  },
  {
    path: '/master/area-building',
    name: 'MasterAreaBuilding',
    component: safeImport('../views/master/AreaBuildingView.vue'),
    meta: { title: '区域·楼栋·房号' }
  },
  {
    path: '/master/departments',
    name: 'MasterDepartments',
    component: safeImport('../views/master/DepartmentList.vue'),
    meta: { title: '部门管理' }
  },
  {
    path: '/master/areas',
    name: 'MasterAreas',
    component: safeImport('../views/master/AreaList.vue'),
    meta: { title: '区域管理' }
  },
  {
    path: '/master/buildings',
    name: 'MasterBuildings',
    component: safeImport('../views/master/BuildingList.vue'),
    meta: { title: '楼栋管理' }
  },
  {
    path: '/master/rooms',
    name: 'MasterRooms',
    component: safeImport('../views/master/RoomList.vue'),
    meta: { title: '房号管理' }
  },
  {
    path: '/master/job-types',
    name: 'MasterJobTypes',
    component: safeImport('../views/master/JobTypeList.vue'),
    meta: { title: '工种管理' }
  },
  {
    path: '/master/suppliers',
    name: 'MasterSuppliers',
    component: safeImport('../views/master/SupplierList.vue'),
    meta: { title: '供应商管理' }
  },
  {
    path: '/access-control',
    name: 'AccessControl',
    component: safeImport('../views/access-control/RoleManagementView.vue'),
    meta: { title: '权限控制' }
  },
  {
    path: '/master/device-types',
    name: 'MasterDeviceTypes',
    component: safeImport('../views/master/DeviceTypeList.vue'),
    meta: { title: '设备类型' }
  },
  {
    path: '/renovation',
    name: 'Renovation',
    component: safeImport('../views/renovation/RenovationList.vue'),
    meta: { title: '装修管理' }
  },
  {
    path: '/express',
    name: 'Express',
    component: safeImport('../views/express/ExpressList.vue'),
    meta: { title: '快递管理' }
  },
  {
    path: '/cleaning',
    name: 'Cleaning',
    component: safeImport('../views/cleaning/CleaningList.vue'),
    meta: { title: '清洁管理' }
  },
  {
    path: '/community',
    name: 'Community',
    component: safeImport('../views/community/CommunityList.vue'),
    meta: { title: '社区活动' }
  },
  {
    path: '/community-activities',
    name: 'CommunityActivities',
    component: safeImport('../views/community/CommunityList.vue'),
    meta: { title: '社区活动' }
  },
  {
    path: '/delivery',
    name: 'Delivery',
    component: safeImport('../views/delivery/DeliveryList.vue'),
    meta: { title: '配送管理' }
  },

  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: safeImport('../views/NotFoundView.vue'),
    meta: { title: '404' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach((to, from) => {
  const authStore = useAuthStore()
  
  if (to.path === '/login') {
    return true
  } else if (!authStore.token) {
    return '/login'
  } else if (to.meta.requiresAdmin && !authStore.isAdmin) {
    ElMessage.warning('需要管理员权限')
    return '/'
  } else {
    return true
  }
})

// 路由错误处理
router.onError((error) => {
  console.error('[Router] Route error:', error)
  ElMessage.error('页面加载失败，正在返回首页...')
  router.push('/')
})

export default router
