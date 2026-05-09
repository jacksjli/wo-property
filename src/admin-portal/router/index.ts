import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import(/* webpackChunkName: "login" */ '../views/LoginView.vue'),
    meta: { title: '登录' }
  },
  {
    path: '/',
    name: 'Dashboard',
    component: () => import(/* webpackChunkName: "dashboard" */ '../views/DashboardView.vue'),
    meta: { title: '首页' }
  },
  {
    path: '/tickets',
    name: 'Ticket',
    component: () => import(/* webpackChunkName: "ticket" */ '../views/ticket/TicketList.vue'),
    meta: { title: '工单管理' }
  },
  {
    path: '/project',
    name: 'Project',
    component: () => import(/* webpackChunkName: "project" */ '../views/project/ProjectList.vue'),
    meta: { title: '项目管理' }
  },
  {
    path: '/device',
    name: 'Device',
    component: () => import(/* webpackChunkName: "device" */ '../views/device/DeviceList.vue'),
    meta: { title: '设备管理' }
  },
  {
    path: '/material',
    name: 'Material',
    component: () => import(/* webpackChunkName: "material" */ '../views/material/MaterialList.vue'),
    meta: { title: '物料管理' }
  },
  {
    path: '/notification',
    name: 'Notification',
    component: () => import(/* webpackChunkName: "notification" */ '../views/notification/NotificationList.vue'),
    meta: { title: '通知管理' }
  },
  {
    path: '/contract',
    name: 'Contract',
    component: () => import(/* webpackChunkName: "contract" */ '../views/contract/ContractList.vue'),
    meta: { title: '合同管理' }
  },
  {
    path: '/finance',
    name: 'Finance',
    component: () => import(/* webpackChunkName: "finance" */ '../views/finance/FinanceList.vue'),
    meta: { title: '财务管理' }
  },
  {
    path: '/inspection',
    name: 'Inspection',
    component: () => import(/* webpackChunkName: "inspection" */ '../views/inspection/InspectionList.vue'),
    meta: { title: '巡检管理' }
  },
  {
    path: '/complaint',
    name: 'Complaint',
    component: () => import(/* webpackChunkName: "complaint" */ '../views/complaint/ComplaintList.vue'),
    meta: { title: '投诉管理' }
  },
  {
    path: '/key',
    name: 'Key',
    component: () => import(/* webpackChunkName: "key" */ '../views/key/KeyList.vue'),
    meta: { title: '钥匙管理' }
  },
  {
    path: '/visitor',
    name: 'Visitor',
    component: () => import(/* webpackChunkName: "visitor" */ '../views/visitor/VisitorList.vue'),
    meta: { title: '访客管理' }
  },
  {
    path: '/parking',
    name: 'Parking',
    component: () => import(/* webpackChunkName: "parking" */ '../views/parking/ParkingList.vue'),
    meta: { title: '车位管理' }
  },
  {
    path: '/payment',
    name: 'Payment',
    component: () => import(/* webpackChunkName: "payment" */ '../views/payment/PaymentList.vue'),
    meta: { title: '缴费管理' }
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: () => import(/* webpackChunkName: "statistics" */ '../views/statistics/StatisticsView.vue'),
    meta: { title: '统计分析' }
  },
  {
    path: '/user',
    name: 'User',
    component: () => import(/* webpackChunkName: "user" */ '../views/user/UserList.vue'),
    meta: { title: '用户管理' }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: () => import(/* webpackChunkName: "settings" */ '../views/settings/SystemSettingsView.vue'),
    meta: { title: '系统设置' }
  },
  {
    path: '/personnel',
    name: 'Personnel',
    component: () => import(/* webpackChunkName: "personnel" */ '../views/personnel/PersonnelList.vue'),
    meta: { title: '人员管理' }
  },
  {
    path: '/dispatch-rules',
    name: 'DispatchRules',
    component: () => import(/* webpackChunkName: "dispatch" */ '../views/dispatch/DispatchRules.vue'),
    meta: { title: '派单规则' }
  },
  {
    path: '/timeout-settings',
    name: 'TimeoutSettings',
    component: () => import(/* webpackChunkName: "dispatch" */ '../views/dispatch/TimeoutSettings.vue'),
    meta: { title: '超时设置' }
  },
  {
    path: '/project-tracking',
    name: 'ProjectTracking',
    component: () => import(/* webpackChunkName: "project" */ '../views/project/ProjectTrackingList.vue'),
    meta: { title: '项目跟踪' }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import(/* webpackChunkName: "not-found" */ '../views/NotFoundView.vue'),
    meta: { title: '404' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')

  if (to.path === '/login') {
    next()
  } else if (!token) {
    next('/login')
  } else {
    next()
  }
})

export default router