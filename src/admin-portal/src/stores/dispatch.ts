import { ref } from 'vue'

// ============ 类型定义 ============

// 规则类型
export type RuleType = 'ticket_type' | 'device' | 'inspection' | 'complaint' | 'other'

// 工单颜色
export type TicketColor = 'green' | 'blue' | 'orange' | 'red' | 'all'

// 地点匹配方式
export type LocationType = 'exact' | 'contains'

// 规则状态
export type RuleStatus = 'draft' | 'active' | 'inactive'

// 任务状态
export type TaskStatus = 'pending' | 'accepted' | 'rejected' | 'completed' | 'cancelled'

// 通知方式
export type NotifyMethod = 'sms' | 'app' | 'phone' | 'email'

// ============ 接口定义 ============

// 派单规则
export interface DispatchRule {
  id: number
  ruleNo: string
  name: string
  description: string
  type: RuleType
  ticketColor: TicketColor
  location: string
  locationType: LocationType
  priority: number
  autoAssign: boolean
  notifyBackup: boolean
  allowTransfer: boolean
  timeoutEscalation: boolean
  operatorIds: string
  supervisorId: string
  managerId: string
  departmentHeadId: string
  companyHeadId: string
  backupIds: string
  notifyMethods: string
  status: RuleStatus
  matchCount: number
  successCount: number
  avgResponseTime: number
  createdAt: string
  updatedAt: string
  createdBy: string
  remark: string
}

// 派单任务
export interface DispatchTask {
  id: number
  taskNo: string
  ticketId: number | null
  ruleId: number | null
  assignedTo: string
  assignedBy: string | null
  assignedAt: string
  status: TaskStatus
  acceptedAt: string | null
  completedAt: string | null
  responseTime: number | null
  timeoutAt: string | null
  escalationLevel: number
  notes: string | null
}

// 统计数据
export interface DispatchStats {
  tasks: {
    total: number
    pending: number
    accepted: number
    completed: number
    rejected: number
    todayNew: number
    weekNew: number
    avgResponseTime: number
  }
  rules: {
    total: number
    active: number
    inactive: number
  }
  byStatus: { status: string; count: number }[]
}

// ============ 标签映射 ============

export const ruleTypeLabels: Record<RuleType, string> = {
  'ticket_type': '工单类型',
  'device': '设备',
  'inspection': '巡检',
  'complaint': '投诉',
  'other': '其他'
}

export const ticketColorLabels: Record<TicketColor, string> = {
  'green': '绿色',
  'blue': '蓝色',
  'orange': '橙色',
  'red': '红色',
  'all': '全部'
}

export const ticketColorMap: Record<string, string> = {
  'green': '#67C23A',
  'blue': '#409EFF',
  'orange': '#E6A23C',
  'red': '#F56C6C',
  'all': '#909399'
}

export const taskStatusLabels: Record<TaskStatus, string> = {
  'pending': '待接受',
  'accepted': '已接受',
  'rejected': '已拒绝',
  'completed': '已完成',
  'cancelled': '已取消'
}

export const taskStatusTypeMap: Record<TaskStatus, string> = {
  'pending': 'warning',
  'accepted': 'primary',
  'rejected': 'danger',
  'completed': 'success',
  'cancelled': 'info'
}

export const notifyMethodLabels: Record<NotifyMethod, string> = {
  'sms': '短信',
  'app': 'APP推送',
  'phone': '电话',
  'email': '邮件'
}

// ============ 存储键名 ============
const RULES_STORAGE_KEY = 'wo_dispatch_rules'
const TASKS_STORAGE_KEY = 'wo_dispatch_tasks'

// ============ 从 localStorage 加载数据 ============
const loadRulesFromStorage = (): DispatchRule[] => {
  try {
    const saved = localStorage.getItem(RULES_STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) return parsed
    }
  } catch (error) {
    console.error('加载派单规则数据失败:', error)
  }
  return []
}

const loadTasksFromStorage = (): DispatchTask[] => {
  try {
    const saved = localStorage.getItem(TASKS_STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) return parsed
    }
  } catch (error) {
    console.error('加载派单任务数据失败:', error)
  }
  return []
}

// ============ 保存到 localStorage ============
const saveRulesToStorage = (data: DispatchRule[]) => {
  try {
    localStorage.setItem(RULES_STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存派单规则数据失败:', error)
  }
}

const saveTasksToStorage = (data: DispatchTask[]) => {
  try {
    localStorage.setItem(TASKS_STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存派单任务数据失败:', error)
  }
}

// ============ 数据存储 ============
const dispatchRules = ref<DispatchRule[]>(loadRulesFromStorage())
const dispatchTasks = ref<DispatchTask[]>(loadTasksFromStorage())

// ============ 默认数据 ============
if (dispatchRules.value.length === 0) {
  dispatchRules.value = [
    {
      id: 1,
      ruleNo: 'DR-2026-0001',
      name: '紧急工单派单规则',
      description: '处理紧急程度的报修工单',
      type: 'ticket_type',
      ticketColor: 'red',
      location: '',
      locationType: 'contains',
      priority: 1,
      autoAssign: true,
      notifyBackup: true,
      allowTransfer: true,
      timeoutEscalation: true,
      operatorIds: '1,2,3',
      supervisorId: '5',
      managerId: '6',
      departmentHeadId: '7',
      companyHeadId: '8',
      backupIds: '4',
      notifyMethods: 'app,phone',
      status: 'active',
      matchCount: 15,
      successCount: 14,
      avgResponseTime: 12.5,
      createdAt: '2026-01-15T08:00:00Z',
      updatedAt: '2026-04-01T10:00:00Z',
      createdBy: 'admin',
      remark: '优先派单给资深维修人员'
    },
    {
      id: 2,
      ruleNo: 'DR-2026-0002',
      name: '电梯故障派单规则',
      description: '电梯相关故障统一派单',
      type: 'device',
      ticketColor: 'orange',
      location: '电梯',
      locationType: 'contains',
      priority: 2,
      autoAssign: true,
      notifyBackup: true,
      allowTransfer: false,
      timeoutEscalation: true,
      operatorIds: '2,3',
      supervisorId: '5',
      managerId: '',
      departmentHeadId: '7',
      companyHeadId: '8',
      backupIds: '',
      notifyMethods: 'app,phone',
      status: 'active',
      matchCount: 8,
      successCount: 8,
      avgResponseTime: 8.3,
      createdAt: '2026-02-01T09:00:00Z',
      updatedAt: '2026-03-20T14:00:00Z',
      createdBy: 'admin',
      remark: '电梯故障必须由持证人员处理'
    },
    {
      id: 3,
      ruleNo: 'DR-2026-0003',
      name: '安保投诉处理规则',
      description: '安保相关投诉快速响应',
      type: 'complaint',
      ticketColor: 'blue',
      location: '',
      locationType: 'contains',
      priority: 3,
      autoAssign: true,
      notifyBackup: false,
      allowTransfer: true,
      timeoutEscalation: true,
      operatorIds: '9,10',
      supervisorId: '11',
      managerId: '6',
      departmentHeadId: '',
      companyHeadId: '',
      backupIds: '',
      notifyMethods: 'app',
      status: 'active',
      matchCount: 5,
      successCount: 5,
      avgResponseTime: 25.0,
      createdAt: '2026-03-01T10:00:00Z',
      updatedAt: '2026-04-10T16:00:00Z',
      createdBy: 'admin',
      remark: ''
    }
  ]
  saveRulesToStorage(dispatchRules.value)
}

if (dispatchTasks.value.length === 0) {
  const now = new Date()
  dispatchTasks.value = [
    {
      id: 1,
      taskNo: 'DT-2026-00001',
      ticketId: 101,
      ruleId: 1,
      assignedTo: '张师傅',
      assignedBy: '系统',
      assignedAt: new Date(now.getTime() - 2 * 60 * 60 * 1000).toISOString(),
      status: 'accepted',
      acceptedAt: new Date(now.getTime() - 1.5 * 60 * 60 * 1000).toISOString(),
      completedAt: null,
      responseTime: 30,
      timeoutAt: new Date(now.getTime() + 2 * 60 * 60 * 1000).toISOString(),
      escalationLevel: 0,
      notes: 'A栋电梯故障报修'
    },
    {
      id: 2,
      taskNo: 'DT-2026-00002',
      ticketId: 102,
      ruleId: 2,
      assignedTo: '李师傅',
      assignedBy: '管理员',
      assignedAt: new Date(now.getTime() - 30 * 60 * 1000).toISOString(),
      status: 'pending',
      acceptedAt: null,
      completedAt: null,
      responseTime: null,
      timeoutAt: new Date(now.getTime() + 3.5 * 60 * 60 * 1000).toISOString(),
      escalationLevel: 0,
      notes: 'B栋门禁系统异常'
    },
    {
      id: 3,
      taskNo: 'DT-2026-00003',
      ticketId: 103,
      ruleId: 3,
      assignedTo: '王师傅',
      assignedBy: '系统',
      assignedAt: new Date(now.getTime() - 5 * 60 * 60 * 1000).toISOString(),
      status: 'completed',
      acceptedAt: new Date(now.getTime() - 4.5 * 60 * 60 * 1000).toISOString(),
      completedAt: new Date(now.getTime() - 1 * 60 * 60 * 1000).toISOString(),
      responseTime: 30,
      timeoutAt: null,
      escalationLevel: 0,
      notes: 'C栋住户投诉噪音问题'
    },
    {
      id: 4,
      taskNo: 'DT-2026-00004',
      ticketId: 104,
      ruleId: 1,
      assignedTo: '赵师傅',
      assignedBy: '管理员',
      assignedAt: new Date(now.getTime() - 25 * 60 * 1000).toISOString(),
      status: 'rejected',
      acceptedAt: null,
      completedAt: null,
      responseTime: null,
      timeoutAt: new Date(now.getTime() + 3 * 60 * 60 * 1000).toISOString(),
      escalationLevel: 0,
      notes: '消防设备检查（该人员无证）'
    },
    {
      id: 5,
      taskNo: 'DT-2026-00005',
      ticketId: 105,
      ruleId: 2,
      assignedTo: '张师傅',
      assignedBy: '系统',
      assignedAt: new Date(now.getTime() - 1 * 60 * 60 * 1000).toISOString(),
      status: 'pending',
      acceptedAt: null,
      completedAt: null,
      responseTime: null,
      timeoutAt: new Date(now.getTime() + 4 * 60 * 60 * 1000).toISOString(),
      escalationLevel: 0,
      notes: '监控摄像头角度调整'
    }
  ]
  saveTasksToStorage(dispatchTasks.value)
}

let ruleIdCounter = Math.max(...dispatchRules.value.map(r => r.id), 0) + 1
let taskIdCounter = Math.max(...dispatchTasks.value.map(t => t.id), 0) + 1

// ============ 规则操作 ============
export const getAllRules = () => dispatchRules.value

export const getRuleById = (id: number) => dispatchRules.value.find(r => r.id === id)

export const getActiveRules = () => dispatchRules.value.filter(r => r.status === 'active')

export const getRulesByType = (type: RuleType) => dispatchRules.value.filter(r => r.type === type)

export const matchRule = (ticketType: string, ticketColor: string, location: string): DispatchRule | null => {
  const rules = dispatchRules.value.filter(r => r.status === 'active')
  
  for (const rule of rules.sort((a, b) => a.priority - b.priority)) {
    // 匹配类型
    if (rule.type !== ticketType && rule.type !== 'other') continue
    
    // 匹配颜色
    if (rule.ticketColor !== 'all' && rule.ticketColor !== ticketColor) continue
    
    // 匹配地点
    if (rule.location) {
      if (rule.locationType === 'exact' && rule.location !== location) continue
      if (rule.locationType === 'contains' && !location.includes(rule.location)) continue
    }
    
    return rule
  }
  
  return null
}

export const addRule = (rule: Omit<DispatchRule, 'id' | 'ruleNo' | 'createdAt' | 'updatedAt' | 'matchCount' | 'successCount' | 'avgResponseTime'>): DispatchRule => {
  const year = new Date().getFullYear()
  const count = dispatchRules.value.length + 1
  const newRule: DispatchRule = {
    ...rule,
    id: ruleIdCounter++,
    ruleNo: `DR-${year}-${count.toString().padStart(4, '0')}`,
    matchCount: 0,
    successCount: 0,
    avgResponseTime: 0,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
  dispatchRules.value.push(newRule)
  saveRulesToStorage(dispatchRules.value)
  return newRule
}

export const updateRule = (id: number, updates: Partial<DispatchRule>) => {
  const index = dispatchRules.value.findIndex(r => r.id === id)
  if (index !== -1) {
    dispatchRules.value[index] = {
      ...dispatchRules.value[index],
      ...updates,
      updatedAt: new Date().toISOString()
    }
    saveRulesToStorage(dispatchRules.value)
  }
}

export const deleteRule = (id: number) => {
  const index = dispatchRules.value.findIndex(r => r.id === id)
  if (index !== -1) {
    dispatchRules.value.splice(index, 1)
    saveRulesToStorage(dispatchRules.value)
  }
}

export const toggleRuleStatus = (id: number) => {
  const rule = dispatchRules.value.find(r => r.id === id)
  if (rule) {
    rule.status = rule.status === 'active' ? 'inactive' : 'active'
    rule.updatedAt = new Date().toISOString()
    saveRulesToStorage(dispatchRules.value)
  }
}

// ============ 任务操作 ============
export const getAllTasks = () => dispatchTasks.value

export const getTaskById = (id: number) => dispatchTasks.value.find(t => t.id === id)

export const getTasksByStatus = (status: TaskStatus) => dispatchTasks.value.filter(t => t.status === status)

export const getTasksByAssignee = (assignee: string) => dispatchTasks.value.filter(t => t.assignedTo === assignee)

export const addTask = (task: Omit<DispatchTask, 'id' | 'taskNo' | 'assignedAt' | 'status' | 'acceptedAt' | 'completedAt' | 'responseTime' | 'escalationLevel'>): DispatchTask => {
  const year = new Date().getFullYear()
  const count = dispatchTasks.value.length + 1
  const newTask: DispatchTask = {
    ...task,
    id: taskIdCounter++,
    taskNo: `DT-${year}-${count.toString().padStart(5, '0')}`,
    assignedAt: new Date().toISOString(),
    status: 'pending',
    acceptedAt: null,
    completedAt: null,
    responseTime: null,
    escalationLevel: 0
  }
  dispatchTasks.value.push(newTask)
  saveTasksToStorage(dispatchTasks.value)
  return newTask
}

export const updateTask = (id: number, updates: Partial<DispatchTask>) => {
  const index = dispatchTasks.value.findIndex(t => t.id === id)
  if (index !== -1) {
    dispatchTasks.value[index] = { ...dispatchTasks.value[index], ...updates }
    saveTasksToStorage(dispatchTasks.value)
  }
}

export const acceptTask = (id: number) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task && task.status === 'pending') {
    const now = new Date()
    task.status = 'accepted'
    task.acceptedAt = now.toISOString()
    task.responseTime = (now.getTime() - new Date(task.assignedAt).getTime()) / (1000 * 60)
    saveTasksToStorage(dispatchTasks.value)
  }
}

export const rejectTask = (id: number, reason: string) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'rejected'
    task.notes = task.notes ? `${task.notes}; 拒绝原因: ${reason}` : `拒绝原因: ${reason}`
    saveTasksToStorage(dispatchTasks.value)
  }
}

export const completeTask = (id: number, result: string) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'completed'
    task.completedAt = new Date().toISOString()
    task.notes = task.notes ? `${task.notes}; 完成: ${result}` : `完成: ${result}`
    
    // 更新规则统计
    if (task.ruleId) {
      const rule = dispatchRules.value.find(r => r.id === task.ruleId)
      if (rule) {
        rule.matchCount++
        rule.successCount++
        if (task.responseTime) {
          rule.avgResponseTime = rule.successCount === 1
            ? task.responseTime
            : (rule.avgResponseTime * (rule.successCount - 1) + task.responseTime) / rule.successCount
        }
      }
    }
    saveTasksToStorage(dispatchTasks.value)
  }
}

export const cancelTask = (id: number, reason: string) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'cancelled'
    task.notes = task.notes ? `${task.notes}; 取消原因: ${reason}` : `取消原因: ${reason}`
    saveTasksToStorage(dispatchTasks.value)
  }
}

export const escalateTask = (id: number) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.escalationLevel++
    saveTasksToStorage(dispatchTasks.value)
  }
}

// ============ 统计 ============
export const getDispatchStats = (): DispatchStats => {
  const now = new Date()
  const today = now.toISOString().split('T')[0]
  const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString()
  
  const pendingTasks = dispatchTasks.value.filter(t => t.status === 'pending').length
  const acceptedTasks = dispatchTasks.value.filter(t => t.status === 'accepted').length
  const completedTasks = dispatchTasks.value.filter(t => t.status === 'completed').length
  const rejectedTasks = dispatchTasks.value.filter(t => t.status === 'rejected').length
  
  const avgResponseTime = dispatchTasks.value
    .filter(t => t.responseTime !== null)
    .reduce((sum, t) => sum + (t.responseTime || 0), 0) / 
    (dispatchTasks.value.filter(t => t.responseTime !== null).length || 1)
  
  const activeRules = dispatchRules.value.filter(r => r.status === 'active').length
  
  return {
    tasks: {
      total: dispatchTasks.value.length,
      pending: pendingTasks,
      accepted: acceptedTasks,
      completed: completedTasks,
      rejected: rejectedTasks,
      todayNew: dispatchTasks.value.filter(t => t.assignedAt.split('T')[0] === today).length,
      weekNew: dispatchTasks.value.filter(t => t.assignedAt >= weekAgo).length,
      avgResponseTime: Math.round(avgResponseTime * 10) / 10
    },
    rules: {
      total: dispatchRules.value.length,
      active: activeRules,
      inactive: dispatchRules.value.length - activeRules
    },
    byStatus: [
      { status: 'pending', count: pendingTasks },
      { status: 'accepted', count: acceptedTasks },
      { status: 'completed', count: completedTasks },
      { status: 'rejected', count: rejectedTasks }
    ]
  }
}

// 获取统计数据的别名
export const getStats = getDispatchStats

// ============ 记录规则匹配 ============
export const recordMatch = (id: number, success: boolean) => {
  const rule = dispatchRules.value.find(r => r.id === id)
  if (rule) {
    rule.matchCount++
    if (success) {
      rule.successCount++
    }
    saveRulesToStorage(dispatchRules.value)
  }
}

// ============ 获取状态颜色 ============
export const getStatusColor = (status: RuleStatus) => {
  const colors: Record<RuleStatus, string> = {
    draft: '#909399',
    active: '#67C23A',
    inactive: '#F56C6C'
  }
  return colors[status]
}

// ============ 获取状态类型 ============
export const getStatusType = (status: RuleStatus) => {
  const types: Record<RuleStatus, string> = {
    draft: 'info',
    active: 'success',
    inactive: 'danger'
  }
  return types[status]
}

// ============ 导出 store ============
export const dispatchStore = {
  dispatchRules,
  dispatchTasks,
  getAllRules,
  getRuleById,
  getActiveRules,
  getRulesByType,
  matchRule,
  addRule,
  updateRule,
  deleteRule,
  toggleRuleStatus,
  getAllTasks,
  getTaskById,
  getTasksByStatus,
  getTasksByAssignee,
  addTask,
  updateTask,
  acceptTask,
  rejectTask,
  completeTask,
  cancelTask,
  escalateTask,
  getDispatchStats,
  getStats,
  recordMatch,
  getStatusColor,
  getStatusType
}
