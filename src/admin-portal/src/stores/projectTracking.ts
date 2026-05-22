import { ref } from 'vue'
import { projectTrackingApi } from '@/api/projectTracking'

// 项目类型
export type ProjectType = '招标' | '投标' | '意向'
export type ProjectStatus = '意向' | '跟踪中' | '报名' | '已投标' | '开标' | '公告' | '中标' | '落标'

// 成功率评分维度
export interface SuccessScore {
  competitors: number
  budgetMatch: number
  time充足: number
  historyCoop: number
  techDifficulty: number
  relationResource: number
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
  projectNo: string
  name: string
  type: ProjectType
  client: string
  budget: number
  bidAmount?: number
  registerDeadline?: string
  bidDeadline?: string
  bidOpenDate?: string
  location: string
  description: string
  fileStatus: '未获取' | '已获取' | '已购买'
  status: ProjectStatus
  successScore: SuccessScore
  successRate: number
  trackingRecords: TrackingRecord[]
  remark?: string
  createdAt: string
  updatedAt: string
}

// 计算成功率
export const calculateSuccessRate = (score: SuccessScore): number => {
  const total = score.competitors + score.budgetMatch + score.time充足 +
                score.historyCoop + score.techDifficulty + score.relationResource
  return Math.round((total / 30) * 100)
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
const projects = ref<TrackingProject[]>([])

// 获取所有项目（从API）
export const getAllProjects = () => projects.value

// 按ID获取项目
export const getProjectById = (id: number) => projects.value.find(p => p.id === id)

// 按状态获取项目
export const getProjectsByStatus = (status: ProjectStatus) => projects.value.filter(p => p.status === status)

// 按类型获取项目
export const getProjectsByType = (type: ProjectType) => projects.value.filter(p => p.type === type)

// 从API加载数据
export const loadProjectsFromApi = async (params?: { page?: number; pageSize?: number; status?: string; keyword?: string }) => {
  try {
    const res: any = await projectTrackingApi.getProjects(params)
    if (res.success) {
      projects.value = res.data || []
      return { total: res.total, page: res.page, pageSize: res.pageSize }
    }
    return null
  } catch (e) {
    console.error('加载项目跟踪数据失败:', e)
    return null
  }
}

// 添加项目
export const addProject = async (project: Omit<TrackingProject, 'id' | 'projectNo' | 'createdAt' | 'updatedAt' | 'records'>) => {
  try {
    const res: any = await projectTrackingApi.createProject({
      name: project.name,
      type: project.type,
      client: project.client,
      budget: project.budget,
      bidAmount: project.bidAmount,
      registerDeadline: project.registerDeadline,
      bidDeadline: project.bidDeadline,
      bidOpenDate: project.bidOpenDate,
      location: project.location,
      description: project.description,
      fileStatus: project.fileStatus,
      status: project.status,
      successRate: project.successRate,
      remark: project.remark
    })
    if (res.success) {
      await loadProjectsFromApi()
      return res.data
    }
    return null
  } catch (e) {
    console.error('创建项目失败:', e)
    return null
  }
}

// 更新项目
export const updateProject = async (id: number, updates: Partial<TrackingProject>) => {
  try {
    const res: any = await projectTrackingApi.updateProject(id, {
      name: updates.name,
      type: updates.type,
      client: updates.client,
      budget: updates.budget,
      bidAmount: updates.bidAmount,
      registerDeadline: updates.registerDeadline,
      bidDeadline: updates.bidDeadline,
      bidOpenDate: updates.bidOpenDate,
      location: updates.location,
      description: updates.description,
      fileStatus: updates.fileStatus,
      status: updates.status,
      successRate: updates.successRate,
      remark: updates.remark
    })
    if (res.success) {
      await loadProjectsFromApi()
      return res.data
    }
    return null
  } catch (e) {
    console.error('更新项目失败:', e)
    return null
  }
}

// 删除项目
export const deleteProject = async (id: number) => {
  try {
    const res: any = await projectTrackingApi.deleteProject(id)
    if (res.success) {
      await loadProjectsFromApi()
      return true
    }
    return false
  } catch (e) {
    console.error('删除项目失败:', e)
    return false
  }
}

// 添加跟踪记录
export const addTrackingRecord = async (projectId: number, record: { Type?: string; Content: string }) => {
  try {
    const res: any = await projectTrackingApi.addRecord(projectId, record)
    if (res.success) {
      // 重新加载项目详情
      return res.data
    }
    return null
  } catch (e) {
    console.error('添加跟踪记录失败:', e)
    return null
  }
}

// 删除跟踪记录
export const deleteTrackingRecord = async (projectId: number, recordId: number) => {
  try {
    const res: any = await projectTrackingApi.deleteRecord(projectId, recordId)
    return res.success
  } catch (e) {
    console.error('删除跟踪记录失败:', e)
    return false
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
export const updateSuccessScore = async (projectId: number, score: Partial<SuccessScore>) => {
  const project = projects.value.find(p => p.id === projectId)
  if (project) {
    project.successScore = { ...project.successScore, ...score }
    project.successRate = calculateSuccessRate(project.successScore)
    await updateProject(projectId, { successScore: project.successScore, successRate: project.successRate })
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
  deleteTrackingRecord,
  getStats,
  updateSuccessScore,
  loadProjectsFromApi,
  calculateSuccessRate
}
