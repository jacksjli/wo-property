import { ref } from 'vue'
import { masterApi } from '@/api/http'

// ============ 类型定义 ============

export type RuleType = 'ticket_type' | 'device' | 'inspection' | 'complaint' | 'other'
export type TicketColor = 'green' | 'blue' | 'orange' | 'red' | 'all'
export type LocationType = 'exact' | 'contains'
export type RuleStatus = 'draft' | 'active' | 'inactive'
export type TaskStatus = 'pending' | 'accepted' | 'rejected' | 'completed' | 'cancelled'
export type NotifyMethod = 'sms' | 'app' | 'phone' | 'email'

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
  'ticket_type': '工单类型', 'device': '设备', 'inspection': '巡检', 'complaint': '投诉', 'other': '其他'
}

export const ticketColorLabels: Record<TicketColor, string> = {
  'green': '绿色', 'blue': '蓝色', 'orange': '橙色', 'red': '红色', 'all': '全部'
}

export const ticketColorMap: Record<string, string> = {
  'green': '#67C23A', 'blue': '#409EFF', 'orange': '#E6A23C', 'red': '#F56C6C', 'all': '#909399'
}

export const taskStatusLabels: Record<TaskStatus, string> = {
  'pending': '待接受', 'accepted': '已接受', 'rejected': '已拒绝', 'completed': '已完成', 'cancelled': '已取消'
}

export const taskStatusTypeMap: Record<TaskStatus, string> = {
  'pending': 'warning', 'accepted': 'primary', 'rejected': 'danger', 'completed': 'success', 'cancelled': 'info'
}

export const notifyMethodLabels: Record<NotifyMethod, string> = {
  'sms': '短信', 'app': 'APP推送', 'phone': '电话', 'email': '邮件'
}

// ============ 数据存储 ============
const dispatchRules = ref<DispatchRule[]>([])
const dispatchTasks = ref<DispatchTask[]>([])

// ============ API 加载 ============
export const loadRulesFromApi = async () => {
  try {
    const res: any = await masterApi.get('/dispatch-rules')
    if (res.success && Array.isArray(res.data)) {
      dispatchRules.value = res.data
    }
  } catch (error) {
    console.error('加载派单规则失败:', error)
  }
}

// ============ 规则操作 ============
export const getAllRules = () => dispatchRules.value
export const getRuleById = (id: number) => dispatchRules.value.find(r => r.id === id)
export const getActiveRules = () => dispatchRules.value.filter(r => r.status === 'active')
export const getRulesByType = (type: RuleType) => dispatchRules.value.filter(r => r.type === type)

export const matchRule = (ticketType: string, ticketColor: string, location: string): DispatchRule | null => {
  const rules = dispatchRules.value.filter(r => r.status === 'active')
  for (const rule of rules.sort((a, b) => a.priority - b.priority)) {
    if (rule.type !== ticketType && rule.type !== 'other') continue
    if (rule.ticketColor !== 'all' && rule.ticketColor !== ticketColor) continue
    if (rule.location) {
      if (rule.locationType === 'exact' && rule.location !== location) continue
      if (rule.locationType === 'contains' && !location.includes(rule.location)) continue
    }
    return rule
  }
  return null
}

// API 请求 payload 转换：DispatchRule → CreateDispatchRuleRequest (PascalCase)
const toCreatePayload = (rule: Omit<DispatchRule, 'id' | 'ruleNo' | 'createdAt' | 'updatedAt' | 'matchCount' | 'successCount' | 'avgResponseTime'>) => ({
  Name: rule.name,
  Description: rule.description,
  Type: rule.type,
  TicketColor: rule.ticketColor,
  Location: rule.location,
  LocationType: rule.locationType,
  Priority: rule.priority,
  AutoAssign: rule.autoAssign,
  NotifyBackup: rule.notifyBackup,
  AllowTransfer: rule.allowTransfer,
  TimeoutEscalation: rule.timeoutEscalation,
  OperatorIds: rule.operatorIds,
  SupervisorId: rule.supervisorId,
  ManagerId: rule.managerId,
  DepartmentHeadId: rule.departmentHeadId,
  CompanyHeadId: rule.companyHeadId,
  BackupIds: rule.backupIds,
  NotifyMethods: rule.notifyMethods,
  Status: rule.status,
  Category: 'property',
  Remark: rule.remark,
})

export const addRule = async (rule: Omit<DispatchRule, 'id' | 'ruleNo' | 'createdAt' | 'updatedAt' | 'matchCount' | 'successCount' | 'avgResponseTime'>): Promise<DispatchRule | null> => {
  try {
    const payload = toCreatePayload(rule)
    const res: any = await masterApi.post('/dispatch-rules', payload)
    if (res.success) {
      await loadRulesFromApi()
      return dispatchRules.value.find(r => r.id === res.data.id) || null
    }
  } catch (error) {
    console.error('创建派单规则失败:', error)
  }
  return null
}

export const updateRule = async (id: number, updates: Partial<DispatchRule>): Promise<boolean> => {
  try {
    const payload: Record<string, any> = {}
    if (updates.name !== undefined) payload.Name = updates.name
    if (updates.description !== undefined) payload.Description = updates.description
    if (updates.type !== undefined) payload.Type = updates.type
    if (updates.ticketColor !== undefined) payload.TicketColor = updates.ticketColor
    if (updates.location !== undefined) payload.Location = updates.location
    if (updates.locationType !== undefined) payload.LocationType = updates.locationType
    if (updates.priority !== undefined) payload.Priority = updates.priority
    if (updates.autoAssign !== undefined) payload.AutoAssign = updates.autoAssign
    if (updates.notifyBackup !== undefined) payload.NotifyBackup = updates.notifyBackup
    if (updates.allowTransfer !== undefined) payload.AllowTransfer = updates.allowTransfer
    if (updates.timeoutEscalation !== undefined) payload.TimeoutEscalation = updates.timeoutEscalation
    if (updates.operatorIds !== undefined) payload.OperatorIds = updates.operatorIds
    if (updates.supervisorId !== undefined) payload.SupervisorId = updates.supervisorId
    if (updates.managerId !== undefined) payload.ManagerId = updates.managerId
    if (updates.departmentHeadId !== undefined) payload.DepartmentHeadId = updates.departmentHeadId
    if (updates.companyHeadId !== undefined) payload.CompanyHeadId = updates.companyHeadId
    if (updates.backupIds !== undefined) payload.BackupIds = updates.backupIds
    if (updates.notifyMethods !== undefined) payload.NotifyMethods = updates.notifyMethods
    if (updates.status !== undefined) payload.Status = updates.status
    if (updates.remark !== undefined) payload.Remark = updates.remark

    const res: any = await masterApi.put(`/dispatch-rules/${id}`, payload)
    if (res.success) {
      await loadRulesFromApi()
      return true
    }
  } catch (error) {
    console.error('更新派单规则失败:', error)
  }
  return false
}

export const deleteRule = async (id: number): Promise<boolean> => {
  try {
    const res: any = await masterApi.delete(`/dispatch-rules/${id}`)
    if (res.success) {
      dispatchRules.value = dispatchRules.value.filter(r => r.id !== id)
      return true
    }
  } catch (error) {
    console.error('删除派单规则失败:', error)
  }
  return false
}

export const toggleRuleStatus = async (id: number): Promise<boolean> => {
  try {
    const res: any = await masterApi.post(`/dispatch-rules/${id}/toggle-status`)
    if (res.success) {
      await loadRulesFromApi()
      return true
    }
  } catch (error) {
    console.error('切换规则状态失败:', error)
  }
  return false
}

// ============ 任务操作（保留本地，暂不涉及后端） ============
export const getAllTasks = () => dispatchTasks.value
export const getTaskById = (id: number) => dispatchTasks.value.find(t => t.id === id)
export const getTasksByStatus = (status: TaskStatus) => dispatchTasks.value.filter(t => t.status === status)
export const getTasksByAssignee = (assignee: string) => dispatchTasks.value.filter(t => t.assignedTo === assignee)

export const addTask = (task: Omit<DispatchTask, 'id' | 'taskNo' | 'assignedAt' | 'status' | 'acceptedAt' | 'completedAt' | 'responseTime' | 'escalationLevel'>): DispatchTask => {
  const year = new Date().getFullYear()
  const count = dispatchTasks.value.length + 1
  const newTask: DispatchTask = {
    ...task,
    id: Date.now(),
    taskNo: `DT-${year}-${count.toString().padStart(5, '0')}`,
    assignedAt: new Date().toISOString(),
    status: 'pending',
    acceptedAt: null,
    completedAt: null,
    responseTime: null,
    escalationLevel: 0
  }
  dispatchTasks.value.push(newTask)
  return newTask
}

export const updateTask = (id: number, updates: Partial<DispatchTask>) => {
  const index = dispatchTasks.value.findIndex(t => t.id === id)
  if (index !== -1) {
    dispatchTasks.value[index] = { ...dispatchTasks.value[index], ...updates }
  }
}

export const acceptTask = (id: number) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task && task.status === 'pending') {
    const now = new Date()
    task.status = 'accepted'
    task.acceptedAt = now.toISOString()
    task.responseTime = (now.getTime() - new Date(task.assignedAt).getTime()) / (1000 * 60)
  }
}

export const rejectTask = (id: number, reason: string) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'rejected'
    task.notes = task.notes ? `${task.notes}; 拒绝原因: ${reason}` : `拒绝原因: ${reason}`
  }
}

export const completeTask = (id: number, result: string) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'completed'
    task.completedAt = new Date().toISOString()
    task.notes = task.notes ? `${task.notes}; 完成: ${result}` : `完成: ${result}`
  }
}

export const cancelTask = (id: number, reason: string) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'cancelled'
    task.notes = task.notes ? `${task.notes}; 取消原因: ${reason}` : `取消原因: ${reason}`
  }
}

export const escalateTask = (id: number) => {
  const task = dispatchTasks.value.find(t => t.id === id)
  if (task) task.escalationLevel++
}

// ============ 统计 ============
export const getDispatchStats = (): DispatchStats => {
  const now = new Date()
  const today = now.toISOString().split('T')[0]
  const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString()
  return {
    tasks: {
      total: dispatchTasks.value.length,
      pending: dispatchTasks.value.filter(t => t.status === 'pending').length,
      accepted: dispatchTasks.value.filter(t => t.status === 'accepted').length,
      completed: dispatchTasks.value.filter(t => t.status === 'completed').length,
      rejected: dispatchTasks.value.filter(t => t.status === 'rejected').length,
      todayNew: dispatchTasks.value.filter(t => t.assignedAt.split('T')[0] === today).length,
      weekNew: dispatchTasks.value.filter(t => t.assignedAt >= weekAgo).length,
      avgResponseTime: 0
    },
    rules: {
      total: dispatchRules.value.length,
      active: dispatchRules.value.filter(r => r.status === 'active').length,
      inactive: dispatchRules.value.filter(r => r.status === 'inactive').length
    },
    byStatus: []
  }
}

export const getStats = getDispatchStats

export const recordMatch = (id: number, success: boolean) => {
  const rule = dispatchRules.value.find(r => r.id === id)
  if (rule) {
    rule.matchCount++
    if (success) rule.successCount++
  }
}

export const getStatusColor = (status: RuleStatus) => ({ draft: '#909399', active: '#67C23A', inactive: '#F56C6C' }[status])
export const getStatusType = (status: RuleStatus) => ({ draft: 'info', active: 'success', inactive: 'danger' }[status])

// ============ 导出 store ============
export const dispatchStore = {
  dispatchRules,
  dispatchTasks,
  loadRulesFromApi,
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