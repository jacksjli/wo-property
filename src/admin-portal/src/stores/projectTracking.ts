import { ref } from 'vue'

// 项目类型
export type ProjectType = '招标' | '投标' | '意向'
export type ProjectStatus = '意向' | '跟踪中' | '报名' | '已投标' | '开标' | '公告' | '中标' | '落标'

// 成功率评分维度
export interface SuccessScore {
  competitors: number      // 竞争对手数量 (1-5分，5分最好)
  budgetMatch: number       // 预算合理性 (1-5分)
  time充足: number          // 时间充足度 (1-5分)
  historyCoop: number       // 历史合作 (1-5分)
  techDifficulty: number    // 技术难度 (1-5分，5分最低难度)
  relationResource: number  // 关系资源 (1-5分)
}

// 项目跟踪记录
export interface TrackingRecord {
  id: number
  timestamp: string
  type: '状态变更' | '备注' | '提醒'
  content: string
  operator?: string
}

// 项目信息
export interface TrackingProject {
  id: number
  projectNo: string         // 项目编号
  name: string              // 项目名称
  type: ProjectType         // 项目类型
  client: string            // 客户/招标方
  budget: number            // 项目预算
  bidAmount?: number        // 投标金额
  registerDeadline?: string // 报名截止日期
  bidDeadline?: string      // 投标截止日期
  bidOpenDate?: string      // 开标日期
  location: string          // 项目所在地
  description: string       // 项目描述
  fileStatus: '未获取' | '已获取' | '已购买'
  status: ProjectStatus     // 项目状态
  successScore: SuccessScore // 成功率评分
  successRate: number       // 综合成功率百分比
  trackingRecords: TrackingRecord[] // 跟踪记录
  remark?: string           // 备注
  createdAt: string
  updatedAt: string
}

// 存储键名
const STORAGE_KEY = 'wo_project_tracking'

// 从 localStorage 加载
const loadFromStorage = (): TrackingProject[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      return JSON.parse(saved)
    }
  } catch (e) {
    console.error('加载项目跟踪数据失败:', e)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: TrackingProject[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (e) {
    console.error('保存项目跟踪数据失败:', e)
  }
}

// 计算成功率
export const calculateSuccessRate = (score: SuccessScore): number => {
  const total = score.competitors + score.budgetMatch + score.time充足 + 
                score.historyCoop + score.techDifficulty + score.relationResource
  return Math.round((total / 30) * 100) // 30分满分，转换为百分比
}

// 状态标签颜色
export const getStatusType = (status: ProjectStatus): string => {
  const map: Record<ProjectStatus, string> = {
    '意向': 'info',
    '跟踪中': 'primary',
    '报名': 'warning',
    '已投标': 'warning',
    '开标': 'warning',
    '公告': 'success',
    '中标': 'success',
    '落标': 'danger'
  }
  return map[status] || 'info'
}

// 类型标签
export const getTypeTag = (type: ProjectType): string => {
  const map: Record<ProjectType, string> = {
    '招标': '',
    '投标': 'warning',
    '意向': 'info'
  }
  return map[type] || ''
}

// 成功率颜色
export const getSuccessRateColor = (rate: number): string => {
  if (rate >= 60) return 'success'
  if (rate >= 30) return 'warning'
  return 'danger'
}

// 项目数据
const projects = ref<TrackingProject[]>(loadFromStorage())

// 如果没有数据，使用示例数据
if (projects.value.length === 0) {
  projects.value = [
    {
      id: 1,
      projectNo: 'PT-2026-001',
      name: '某市智慧社区改造项目',
      type: '投标',
      client: '某市城管局',
      budget: 5000000,
      bidAmount: 4800000,
      registerDeadline: '2026-04-25',
      bidDeadline: '2026-05-01',
      bidOpenDate: '2026-05-10',
      location: '某市中心城区',
      description: '对20个老旧小区进行智慧化改造，包括门禁、监控、停车系统等',
      fileStatus: '已获取',
      status: '跟踪中',
      successScore: {
        competitors: 4,
        budgetMatch: 4,
        time充足: 3,
        historyCoop: 3,
        techDifficulty: 4,
        relationResource: 3
      },
      successRate: 70,
      trackingRecords: [
        { id: 1, timestamp: '2026-04-15 09:00', type: '状态变更', content: '项目入库，开始跟踪', operator: '管理员' },
        { id: 2, timestamp: '2026-04-18 14:30', type: '备注', content: '已获取招标文件，初步分析可行', operator: '管理员' }
      ],
      remark: '',
      createdAt: '2026-04-15',
      updatedAt: '2026-04-18'
    },
    {
      id: 2,
      projectNo: 'PT-2026-002',
      name: '某区政府办公楼智能化项目',
      type: '招标',
      client: '某区政府办公室',
      budget: 8000000,
      registerDeadline: '2026-04-28',
      bidDeadline: '2026-05-05',
      bidOpenDate: '2026-05-15',
      location: '某区政务中心',
      description: '区政府办公楼整体智能化改造，包括综合布线、网络、监控、会议系统等',
      fileStatus: '未获取',
      status: '意向',
      successScore: {
        competitors: 2,
        budgetMatch: 2,
        time充足: 2,
        historyCoop: 2,
        techDifficulty: 2,
        relationResource: 1
      },
      successRate: 18,
      trackingRecords: [
        { id: 1, timestamp: '2026-04-20 10:00', type: '状态变更', content: '获取项目信息，建立意向', operator: '管理员' }
      ],
      remark: '竞争对手实力强，需谨慎',
      createdAt: '2026-04-20',
      updatedAt: '2026-04-20'
    },
    {
      id: 3,
      projectNo: 'PT-2026-003',
      name: '高档住宅小区物业服务项目',
      type: '投标',
      client: '某房地产公司',
      budget: 3000000,
      bidAmount: 2900000,
      registerDeadline: '2026-04-30',
      bidDeadline: '2026-05-08',
      bidOpenDate: '2026-05-18',
      location: '某市高新区',
      description: '高档住宅小区前期物业管理服务，服务期限3年',
      fileStatus: '已购买',
      status: '报名',
      successScore: {
        competitors: 3,
        budgetMatch: 5,
        time充足: 4,
        historyCoop: 4,
        techDifficulty: 5,
        relationResource: 4
      },
      successRate: 75,
      trackingRecords: [
        { id: 1, timestamp: '2026-04-10 09:00', type: '状态变更', content: '项目入库', operator: '管理员' },
        { id: 2, timestamp: '2026-04-12 11:00', type: '状态变更', content: '购买招标文件', operator: '管理员' },
        { id: 3, timestamp: '2026-04-22 15:00', type: '状态变更', content: '完成报名', operator: '管理员' }
      ],
      remark: '与甲方有良好合作关系',
      createdAt: '2026-04-10',
      updatedAt: '2026-04-22'
    }
  ]
  saveToStorage(projects.value)
}

// 生成项目编号
const generateProjectNo = (): string => {
  const date = new Date()
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const count = projects.value.filter(p => p.projectNo.startsWith(`PT-${year}`)).length + 1
  return `PT-${year}-${String(count).padStart(3, '0')}`
}

// 获取所有项目
export const getAllProjects = () => projects.value

// 按ID获取项目
export const getProjectById = (id: number) => projects.value.find(p => p.id === id)

// 按状态获取项目
export const getProjectsByStatus = (status: ProjectStatus) => projects.value.filter(p => p.status === status)

// 按类型获取项目
export const getProjectsByType = (type: ProjectType) => projects.value.filter(p => p.type === type)

// 添加项目
export const addProject = (project: Omit<TrackingProject, 'id' | 'projectNo' | 'createdAt' | 'updatedAt'>): TrackingProject => {
  const now = new Date().toISOString().split('T')[0]
  const newProject: TrackingProject = {
    ...project,
    id: Math.max(...projects.value.map(p => p.id), 0) + 1,
    projectNo: generateProjectNo(),
    createdAt: now,
    updatedAt: now
  }
  projects.value.push(newProject)
  saveToStorage(projects.value)
  return newProject
}

// 更新项目
export const updateProject = (id: number, updates: Partial<TrackingProject>) => {
  const index = projects.value.findIndex(p => p.id === id)
  if (index !== -1) {
    const now = new Date().toISOString().split('T')[0]
    projects.value[index] = {
      ...projects.value[index],
      ...updates,
      updatedAt: now
    }
    // 重新计算成功率
    if (updates.successScore) {
      projects.value[index].successRate = calculateSuccessRate(updates.successScore)
    }
    saveToStorage(projects.value)
  }
}

// 删除项目
export const deleteProject = (id: number) => {
  const index = projects.value.findIndex(p => p.id === id)
  if (index !== -1) {
    projects.value.splice(index, 1)
    saveToStorage(projects.value)
  }
}

// 添加跟踪记录
export const addTrackingRecord = (projectId: number, record: Omit<TrackingRecord, 'id'>) => {
  const project = projects.value.find(p => p.id === projectId)
  if (project) {
    const newRecord: TrackingRecord = {
      ...record,
      id: Math.max(...project.trackingRecords.map(r => r.id), 0) + 1
    }
    project.trackingRecords.unshift(newRecord)
    project.updatedAt = new Date().toISOString().split('T')[0]
    saveToStorage(projects.value)
  }
}

// 获取统计
export const getStats = () => {
  const total = projects.value.length
  const active = projects.value.filter(p => !['中标', '落标'].includes(p.status)).length
  const won = projects.value.filter(p => p.status === '中标').length
  const lost = projects.value.filter(p => p.status === '落标').length
  const highRate = projects.value.filter(p => p.successRate >= 60).length
  const lowRate = projects.value.filter(p => p.successRate < 30).length
  return { total, active, won, lost, highRate, lowRate }
}

// 更新成功率评分并重新计算
export const updateSuccessScore = (projectId: number, score: Partial<SuccessScore>) => {
  const project = projects.value.find(p => p.id === projectId)
  if (project) {
    project.successScore = { ...project.successScore, ...score }
    project.successRate = calculateSuccessRate(project.successScore)
    project.updatedAt = new Date().toISOString().split('T')[0]
    saveToStorage(projects.value)
  }
}

// 导出 store
export const projectTrackingStore = {
  projects,
  getAllProjects,
  getProjectById,
  getProjectsByStatus,
  getProjectsByType,
  addProject,
  updateProject,
  deleteProject,
  addTrackingRecord,
  getStats,
  updateSuccessScore,
  calculateSuccessRate
}