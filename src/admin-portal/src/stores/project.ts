import { ref } from 'vue'

// 存储键名
const STORAGE_KEY = 'wo_project_config'

// 共享项目状态
const currentProject = ref<any>(null)
const projects = ref<any[]>([
  {
    id: 8,
    name: '虹桥机场',
    code: '交通枢纽',
    status: 'Active',
    modules: ['工单管理', '设备管理', '物料管理', '合同管理', '财务管理', '巡检管理', '钥匙管理', '访客管理', '消息管理', '统计分析', '住户管理', '车位管理', '缴费管理', '人员管理', '派单规则', '超时设置', '工单类型', '项目跟踪', '权限控制', '清洁管理', '社区管理', '配送管理', '快递管理', '装修管理', '项目配置', '字段管理', '部门管理', '大区省市区', '区域管理', '楼栋管理', '房号管理', '工种管理', '供应商管理', '设备类型', '公告管理', '社区活动', '设备报表', '工单报表', '物料报表', '满意度调查', '采购订单', '库存事务', '枚举定义', '综合报表']
  },
  {
    id: 9,
    name: '测试项目',
    code: 'testproject',
    status: 'Active',
    modules: ['工单管理', '设备管理', '物料管理', '合同管理', '财务管理', '巡检管理', '钥匙管理', '访客管理', '消息管理', '统计分析', '住户管理', '车位管理', '缴费管理', '人员管理', '派单规则', '超时设置', '工单类型', '项目跟踪', '权限控制', '清洁管理', '社区管理', '配送管理', '快递管理', '装修管理', '项目配置', '字段管理', '部门管理', '大区省市区', '区域管理', '楼栋管理', '房号管理', '工种管理', '供应商管理', '设备类型', '公告管理', '社区活动', '设备报表', '工单报表', '物料报表', '满意度调查', '采购订单', '库存事务', '枚举定义', '综合报表']
  },
  {
    id: 10,
    name: '原始数据库',
    code: 'wo_property',
    status: 'Active',
    modules: ['工单管理', '设备管理', '物料管理', '合同管理', '财务管理', '巡检管理', '钥匙管理', '访客管理', '消息管理', '统计分析', '住户管理', '车位管理', '缴费管理', '人员管理', '派单规则', '超时设置', '工单类型', '项目跟踪', '权限控制', '清洁管理', '社区管理', '配送管理', '快递管理', '装修管理', '项目配置', '字段管理', '部门管理', '大区省市区', '区域管理', '楼栋管理', '房号管理', '工种管理', '供应商管理', '设备类型', '公告管理', '社区活动', '设备报表', '工单报表', '物料报表', '满意度调查', '采购订单', '库存事务', '枚举定义', '综合报表']
  },
  {
    id: 11,
    name: '流程测试项目',
    code: 'testflow',
    status: 'Active',
    modules: ['工单管理', '设备管理', '物料管理', '合同管理', '财务管理', '巡检管理', '钥匙管理', '访客管理', '消息管理', '统计分析', '住户管理', '车位管理', '缴费管理', '人员管理', '派单规则', '超时设置', '工单类型', '项目跟踪', '权限控制', '清洁管理', '社区管理', '配送管理', '快递管理', '装修管理', '项目配置', '字段管理', '部门管理', '大区省市区', '区域管理', '楼栋管理', '房号管理', '工种管理', '供应商管理', '设备类型', '公告管理', '社区活动', '设备报表', '工单报表', '物料报表', '满意度调查', '采购订单', '库存事务', '枚举定义', '综合报表']
  }
])

// 所有可用模块
const allModules = [
  { name: '工单管理', icon: 'Tickets', key: 'ticket', path: '/tickets' },
  { name: '设备管理', icon: 'Monitor', key: 'device', path: '/device' },
  { name: '物料管理', icon: 'Box', key: 'material', path: '/material' },
  { name: '物料分类', icon: 'Box', key: 'materialCategory', path: '/material-category' },
  { name: '合同管理', icon: 'Document', key: 'contract', path: '/contract' },
  { name: '财务管理', icon: 'Money', key: 'finance', path: '/finance' },
  { name: '巡检管理', icon: 'Location', key: 'inspection', path: '/inspection' },
  { name: '钥匙管理', icon: 'Key', key: 'key', path: '/key' },
  { name: '访客管理', icon: 'User', key: 'visitor', path: '/visitor' },
  { name: '消息管理', icon: 'Bell', key: 'notification', path: '/notification' },
  { name: '统计分析', icon: 'DataAnalysis', key: 'statistics', path: '/statistics' },
  { name: '住户管理', icon: 'House', key: 'resident', path: '/resident' },
  { name: '车位管理', icon: 'Van', key: 'parking', path: '/parking' },
  { name: '缴费管理', icon: 'CreditCard', key: 'payment', path: '/payment' },
  { name: '人员管理', icon: 'User', key: 'personnel', path: '/personnel' },
  { name: '派单规则', icon: 'Link', key: 'dispatch', path: '/dispatch-rules' },
  { name: '超时设置', icon: 'Clock', key: 'timeout', path: '/timeout-settings' },
  { name: '工单类型', icon: 'Tickets', key: 'ticketType', path: '/ticket-type' },
  { name: '项目跟踪', icon: 'Document', key: 'projectTracking', path: '/project-tracking' },
  { name: '权限控制', icon: 'Lock', key: 'accessControl', path: '/access-control' },
  { name: '清洁管理', icon: 'Brush', key: 'cleaning', path: '/cleaning' },
  { name: '社区管理', icon: 'User', key: 'community', path: '/community' },
  { name: '配送管理', icon: 'Van', key: 'delivery', path: '/delivery' },
  { name: '快递管理', icon: 'Box', key: 'express', path: '/express' },
  { name: '装修管理', icon: 'Tools', key: 'renovation', path: '/renovation' },
  { name: '项目配置', icon: 'Setting', key: 'projectConfig', path: '/project-config' },
  { name: '字段管理', icon: 'Document', key: 'fieldDefinition', path: '/master/field-definitions' },
  { name: '部门管理', icon: 'OfficeBuilding', key: 'department', path: '/master/departments' },
  { name: '大区省市区', icon: 'Location', key: 'region', path: '/master/regions' },
  { name: '区域管理', icon: 'LocationInformation', key: 'area', path: '/master/areas' },
  { name: '楼栋管理', icon: 'OfficeBuilding', key: 'building', path: '/master/buildings' },
  { name: '房号管理', icon: 'House', key: 'room', path: '/master/rooms' },
  { name: '工种管理', icon: 'Tools', key: 'jobType', path: '/master/job-types' },
  { name: '供应商管理', icon: 'Van', key: 'supplier', path: '/master/suppliers' },
  { name: '设备类型', icon: 'Monitor', key: 'deviceType', path: '/master/device-types' },
  { name: '公告管理', icon: 'Bell', key: 'announcement', path: '/announcements' },
  { name: '社区活动', icon: 'User', key: 'communityActivity', path: '/community-activities' },
  { name: '设备报表', icon: 'DataAnalysis', key: 'deviceReport', path: '/reports?tab=device' },
  { name: '工单报表', icon: 'DataAnalysis', key: 'ticketReport', path: '/reports?tab=ticket' },
  { name: '物料报表', icon: 'DataAnalysis', key: 'materialReport', path: '/reports?tab=material' },
  { name: '满意度调查', icon: 'Star', key: 'satisfaction', path: '/reports?tab=satisfaction' },
  { name: '采购订单', icon: 'ShoppingCart', key: 'purchaseOrder', path: '/reports?tab=purchase' },
  { name: '库存事务', icon: 'Box', key: 'stockTransaction', path: '/reports?tab=stock' },
  { name: '枚举定义', icon: 'List', key: 'enumDefinition', path: '/reports?tab=enum' },
  { name: '综合报表', icon: 'Document', key: 'generalReport', path: '/reports?tab=general' }
]

// 保存项目配置到 localStorage
const saveProjectsConfig = () => {
  const config = {
    projects: projects.value,
    savedAt: new Date().toISOString()
  }
  localStorage.setItem(STORAGE_KEY, JSON.stringify(config))
}

// 从 localStorage 加载项目配置（暂时禁用，保留硬编码列表）
const loadProjectsConfig = () => {
  // 已禁用：不再从 localStorage 加载，避免缓存导致项目列表不更新
  // 未来可以通过后端 API 获取真实项目列表
}

// 获取项目的模块
const getProjectModules = (project: any) => {
  if (!project) return []
  return allModules.filter(m => project.modules.includes(m.name))
}

// 进入项目视图
const enterProject = (project: any) => {
  currentProject.value = project
  localStorage.setItem('currentProject', JSON.stringify(project))
}

// 退出项目视图
const exitProject = () => {
  currentProject.value = null
  localStorage.removeItem('currentProject')
}

// 切换项目状态(需要确认)
const toggleProjectStatus = (projectId: number) => {
  const project = projects.value.find(p => p.id === projectId)
  if (project) {
    project.status = project.status === 'Active' ? 'Inactive' : 'Active'
  }
}

// 初始化(应用启动时调用)
const initProject = () => {
  // 先加载保存的配置
  loadProjectsConfig()

  // 恢复当前项目状态
  const saved = localStorage.getItem('currentProject')
  if (saved) {
    try {
      const parsed = JSON.parse(saved)
      const current = projects.value.find(p => p.id === parsed.id)
      if (current) {
        currentProject.value = current
      }
    } catch (e) {
      currentProject.value = null
    }
  }
}

export {
  currentProject,
  projects,
  allModules,
  getProjectModules,
  enterProject,
  exitProject,
  toggleProjectStatus,
  initProject,
  saveProjectsConfig,
  loadProjectsConfig
}
