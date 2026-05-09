import { ref } from 'vue'

// 存储键名
const STORAGE_KEY = 'wo_project_config'

// 共享项目状态
const currentProject = ref<any>(null)
const projects = ref<any[]>([
  { 
    id: 1, 
    name: 'WO物业管理系统', 
    code: 'WO-PMS',
    status: 'Active',
    modules: ['工单管理', '设备管理', '物料管理', '合同管理', '财务管理', '巡检管理', '投诉管理', '钥匙管理', '访客管理', '通知管理', '人员管理', '派单规则', '超时设置', '项目跟踪']
  },
  { 
    id: 2, 
    name: '小区住户管理', 
    code: 'RESIDENT',
    status: 'Active',
    modules: ['住户管理', '车位管理', '缴费管理', '人员管理', '派单规则', '超时设置']
  }
])

// 所有可用模块
const allModules = [
  { name: '工单管理', icon: 'Tickets', key: 'ticket', path: '/tickets' },
  { name: '设备管理', icon: 'Monitor', key: 'device', path: '/device' },
  { name: '物料管理', icon: 'Box', key: 'material', path: '/material' },
  { name: '合同管理', icon: 'Document', key: 'contract', path: '/contract' },
  { name: '财务管理', icon: 'Money', key: 'finance', path: '/finance' },
  { name: '巡检管理', icon: 'Location', key: 'inspection', path: '/inspection' },
  { name: '投诉管理', icon: 'Warning', key: 'complaint', path: '/complaint' },
  { name: '钥匙管理', icon: 'Key', key: 'key', path: '/key' },
  { name: '访客管理', icon: 'User', key: 'visitor', path: '/visitor' },
  { name: '通知管理', icon: 'Bell', key: 'notification', path: '/notification' },
  { name: '统计分析', icon: 'DataAnalysis', key: 'statistics', path: '/statistics' },
  { name: '住户管理', icon: 'House', key: 'resident', path: '/resident' },
  { name: '车位管理', icon: 'Van', key: 'parking', path: '/parking' },
  { name: '缴费管理', icon: 'CreditCard', key: 'payment', path: '/payment' },
  { name: '人员管理', icon: 'User', key: 'personnel', path: '/personnel' },
  { name: '派单规则', icon: 'Link', key: 'dispatch', path: '/dispatch-rules' },
  { name: '超时设置', icon: 'Clock', key: 'timeout', path: '/timeout-settings' },
  { name: '项目跟踪', icon: 'Tickets', key: 'projectTracking', path: '/project-tracking' }
]

// 保存项目配置到 localStorage
const saveProjectsConfig = () => {
  const config = {
    projects: projects.value,
    savedAt: new Date().toISOString()
  }
  localStorage.setItem(STORAGE_KEY, JSON.stringify(config))
}

// 从 localStorage 加载项目配置
const loadProjectsConfig = () => {
  const saved = localStorage.getItem(STORAGE_KEY)
  if (saved) {
    try {
      const config = JSON.parse(saved)
      if (config.projects && Array.isArray(config.projects)) {
        projects.value = config.projects
      }
    } catch (e) {
      console.error('加载项目配置失败:', e)
    }
  }
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

// 切换项目状态（需要确认）
const toggleProjectStatus = (projectId: number) => {
  const project = projects.value.find(p => p.id === projectId)
  if (project) {
    project.status = project.status === 'Active' ? 'Inactive' : 'Active'
  }
}

// 初始化（应用启动时调用）
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
