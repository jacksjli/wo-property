import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

// 项目类型
export type ProjectType = '招标' | '投标' | '意向'

// 项目状态
export type ProjectStatus = '意向' | '跟踪中' | '报名' | '已投标' | '开标' | '公告' | '中标' | '落标'

// 跟踪记录
export interface TrackingRecord {
  id: number
  timestamp: string
  status: ProjectStatus
  content: string
  operator: string
}

// 项目数据
export interface ProjectTracking {
  id: number
  projectNo: string
  projectName: string
  projectType: ProjectType
  clientName: string
  clientContact: string
  clientPhone: string
  budget: number
  bidAmount: number
  registrationDeadline: string
  bidDeadline: string
  openingDate: string
  location: string
  description: string
  requirements: string
  documentStatus: '未获取' | '已获取' | '已购买'
  status: ProjectStatus
  // 成功率评分维度
  competitorCount: number
  budgetReasonableness: number
  timeAdequacy: number
  historicalCooperation: number
  technicalDifficulty: number
  relationshipResources: number
  // 跟踪记录
  trackingRecords: TrackingRecord[]
  // 创建时间
  createTime: string
  updateTime: string
}

const STORAGE_KEY = 'wo_project_tracking'

export const useProjectTrackingStore = defineStore('projectTracking', () => {
  // 项目列表
  const projects = ref<ProjectTracking[]>([])

  // 加载数据
  const loadFromStorage = () => {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      try {
        projects.value = JSON.parse(saved)
      } catch (e) {
        console.error('加载项目跟踪数据失败:', e)
        projects.value = []
      }
    }
  }

  // 保存数据
  const saveToStorage = () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(projects.value))
  }

  // 初始化
  const init = () => {
    loadFromStorage()
    if (projects.value.length === 0) {
      // 添加示例数据
      projects.value = getDefaultProjects()
      saveToStorage()
    }
  }

  // 获取默认示例数据
  const getDefaultProjects = (): ProjectTracking[] => {
    const now = new Date().toLocaleDateString('zh-CN')
    return [
      {
        id: 1,
        projectNo: 'ZB-2026-001',
        projectName: '智慧社区安防系统升级项目',
        projectType: '投标',
        clientName: '绿城物业管理有限公司',
        clientContact: '王经理',
        clientPhone: '13800138001',
        budget: 5000000,
        bidAmount: 4800000,
        registrationDeadline: '2026-05-01',
        bidDeadline: '2026-05-15',
        openingDate: '2026-05-20',
        location: '杭州市西湖区',
        description: '对现有安防系统进行全面升级，包括视频监控、门禁系统、周界报警等',
        requirements: '系统需支持云平台接入，支持移动端查看，具备AI智能分析功能',
        documentStatus: '已获取',
        status: '跟踪中',
        competitorCount: 5,
        budgetReasonableness: 85,
        timeAdequacy: 70,
        historicalCooperation: 90,
        technicalDifficulty: 60,
        relationshipResources: 80,
        trackingRecords: [
          { id: 1, timestamp: '2026-04-01 10:00', status: '意向', content: '初步接触，了解项目需求', operator: '张三' },
          { id: 2, timestamp: '2026-04-05 14:30', status: '跟踪中', content: '获取项目招标文件初稿', operator: '张三' },
          { id: 3, timestamp: '2026-04-10 09:00', status: '跟踪中', content: '完成现场踏勘', operator: '李四' }
        ],
        createTime: now,
        updateTime: now
      },
      {
        id: 2,
        projectNo: 'ZB-2026-002',
        projectName: '物业费收缴系统采购项目',
        projectType: '招标',
        clientName: '万科物业股份有限公司',
        clientContact: '李总',
        clientPhone: '13900139002',
        budget: 2000000,
        bidAmount: 0,
        registrationDeadline: '2026-04-20',
        bidDeadline: '2026-05-10',
        openingDate: '2026-05-15',
        location: '深圳市南山区',
        description: '采购物业费收缴管理系统，支持多种支付方式',
        requirements: '需对接主流支付渠道，支持账单自动生成，具备报表分析功能',
        documentStatus: '已购买',
        status: '报名',
        competitorCount: 8,
        budgetReasonableness: 70,
        timeAdequacy: 50,
        historicalCooperation: 60,
        technicalDifficulty: 40,
        relationshipResources: 40,
        trackingRecords: [
          { id: 1, timestamp: '2026-03-15 11:00', status: '意向', content: '招标公告发布，关注并报名', operator: '王五' },
          { id: 2, timestamp: '2026-03-20 15:00', status: '报名', content: '完成投标报名，缴纳保证金', operator: '王五' }
        ],
        createTime: now,
        updateTime: now
      },
      {
        id: 3,
        projectNo: 'ZB-2026-003',
        projectName: '社区绿化养护服务项目',
        projectType: '投标',
        clientName: '中海物业集团有限公司',
        clientContact: '赵主任',
        clientPhone: '13700137003',
        budget: 800000,
        bidAmount: 750000,
        registrationDeadline: '2026-04-25',
        bidDeadline: '2026-05-05',
        openingDate: '2026-05-08',
        location: '上海市浦东新区',
        description: '提供社区绿化日常养护服务',
        requirements: '服务人员需具备相关专业资质，提供定期修剪、施肥、病虫害防治等',
        documentStatus: '已获取',
        status: '已投标',
        competitorCount: 3,
        budgetReasonableness: 90,
        timeAdequacy: 85,
        historicalCooperation: 95,
        technicalDifficulty: 20,
        relationshipResources: 90,
        trackingRecords: [
          { id: 1, timestamp: '2026-03-20 09:00', status: '意向', content: '客户主动联系，意向合作', operator: '张三' },
          { id: 2, timestamp: '2026-03-25 10:00', status: '跟踪中', content: '现场勘查完成', operator: '张三' },
          { id: 3, timestamp: '2026-04-01 14:00', status: '报名', content: '完成报名手续', operator: '李四' },
          { id: 4, timestamp: '2026-04-15 16:00', status: '已投标', content: '投标文件已提交', operator: '李四' }
        ],
        createTime: now,
        updateTime: now
      }
    ]
  }

  // 计算成功率
  const calculateSuccessRate = (project: ProjectTracking): number => {
    const weights = {
      competitorCount: 0.20,
      budgetReasonableness: 0.20,
      timeAdequacy: 0.15,
      historicalCooperation: 0.20,
      technicalDifficulty: 0.10,
      relationshipResources: 0.15
    }

    // 竞争对手数量评分：越少越高（8个以上0分，1个以下100分）
    const competitorScore = Math.max(0, 100 - (project.competitorCount - 1) * 15)

    // 预算合理性：投标金额/预算越接近100分
    const budgetScore = project.budget > 0 && project.bidAmount > 0
      ? Math.min(100, (project.bidAmount / project.budget) * 100)
      : 50

    // 技术难度：越低越高
    const techScore = 100 - project.technicalDifficulty

    return Math.round(
      competitorScore * weights.competitorCount +
      budgetScore * weights.budgetReasonableness +
      project.timeAdequacy * weights.timeAdequacy +
      project.historicalCooperation * weights.historicalCooperation +
      techScore * weights.technicalDifficulty +
      project.relationshipResources * weights.relationshipResources
    )
  }

  // 获取成功率颜色
  const getSuccessRateColor = (rate: number): string => {
    if (rate > 60) return '#67c23a'
    if (rate >= 30) return '#e6a23c'
    return '#f56c6c'
  }

  // 获取成功率标签类型
  const getSuccessRateTagType = (rate: number): 'success' | 'warning' | 'danger' => {
    if (rate > 60) return 'success'
    if (rate >= 30) return 'warning'
    return 'danger'
  }

  // 生成项目编号
  const generateProjectNo = (): string => {
    const date = new Date()
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const count = projects.value.length + 1
    return `ZB-${year}-${month}-${String(count).padStart(3, '0')}`
  }

  // 添加项目
  const addProject = (project: Omit<ProjectTracking, 'id' | 'projectNo' | 'createTime' | 'updateTime' | 'trackingRecords'>): ProjectTracking => {
    const now = new Date().toLocaleDateString('zh-CN')
    const newProject: ProjectTracking = {
      ...project,
      id: Math.max(...projects.value.map(p => p.id), 0) + 1,
      projectNo: generateProjectNo(),
      trackingRecords: [
        { id: 1, timestamp: `${now} 00:00`, status: project.status, content: '项目创建', operator: '系统' }
      ],
      createTime: now,
      updateTime: now
    }
    projects.value.unshift(newProject)
    saveToStorage()
    return newProject
  }

  // 更新项目
  const updateProject = (id: number, updates: Partial<ProjectTracking>): boolean => {
    const index = projects.value.findIndex(p => p.id === id)
    if (index === -1) return false

    const now = new Date().toLocaleDateString('zh-CN')
    projects.value[index] = {
      ...projects.value[index],
      ...updates,
      updateTime: now
    }
    saveToStorage()
    return true
  }

  // 删除项目
  const deleteProject = (id: number): boolean => {
    const index = projects.value.findIndex(p => p.id === id)
    if (index === -1) return false

    projects.value.splice(index, 1)
    saveToStorage()
    return true
  }

  // 添加跟踪记录
  const addTrackingRecord = (projectId: number, record: Omit<TrackingRecord, 'id'>): boolean => {
    const project = projects.value.find(p => p.id === projectId)
    if (!project) return false

    const newRecord: TrackingRecord = {
      ...record,
      id: Math.max(...project.trackingRecords.map(r => r.id), 0) + 1
    }
    project.trackingRecords.push(newRecord)
    project.updateTime = new Date().toLocaleDateString('zh-CN')
    saveToStorage()
    return true
  }

  // 获取单个项目
  const getProject = (id: number): ProjectTracking | undefined => {
    return projects.value.find(p => p.id === id)
  }

  // 筛选项目
  const filterProjects = (filters: {
    status?: ProjectStatus
    projectType?: ProjectType
    minRate?: number
    maxRate?: number
    keyword?: string
  }): ProjectTracking[] => {
    return projects.value.filter(p => {
      if (filters.status && p.status !== filters.status) return false
      if (filters.projectType && p.projectType !== filters.projectType) return false
      if (filters.keyword) {
        const kw = filters.keyword.toLowerCase()
        if (!p.projectName.toLowerCase().includes(kw) &&
            !p.projectNo.toLowerCase().includes(kw) &&
            !p.clientName.toLowerCase().includes(kw)) {
          return false
        }
      }
      if (filters.minRate !== undefined || filters.maxRate !== undefined) {
        const rate = calculateSuccessRate(p)
        if (filters.minRate !== undefined && rate < filters.minRate) return false
        if (filters.maxRate !== undefined && rate > filters.maxRate) return false
      }
      return true
    })
  }

  // 统计数据
  const statistics = computed(() => {
    const total = projects.value.length
    const byStatus: Record<ProjectStatus, number> = {
      '意向': 0, '跟踪中': 0, '报名': 0, '已投标': 0,
      '开标': 0, '公告': 0, '中标': 0, '落标': 0
    }
    const byType: Record<ProjectType, number> = {
      '招标': 0, '投标': 0, '意向': 0
    }
    let totalBudget = 0
    let totalBidAmount = 0

    projects.value.forEach(p => {
      byStatus[p.status]++
      byType[p.projectType]++
      totalBudget += p.budget
      totalBidAmount += p.bidAmount
    })

    return { total, byStatus, byType, totalBudget, totalBidAmount }
  })

  return {
    projects,
    statistics,
    init,
    loadFromStorage,
    saveToStorage,
    calculateSuccessRate,
    getSuccessRateColor,
    getSuccessRateTagType,
    addProject,
    updateProject,
    deleteProject,
    addTrackingRecord,
    getProject,
    filterProjects
  }
})
