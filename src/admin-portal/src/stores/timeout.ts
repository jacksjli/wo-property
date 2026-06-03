import { ref } from 'vue'
import { roleLabels, type StaffRole } from '@/stores/staff'
import http from '@/api/http'

// 颜色类型
export type TicketColor = 'green' | 'blue' | 'orange' | 'red'
export type TimeoutColor = TicketColor

// 角色类型
export type StaffRoleType = StaffRole
export type TimeoutRole = StaffRoleType

// 颜色标签
export const colorLabels: Record<TicketColor, string> = {
  green: '绿色',
  blue: '蓝色',
  orange: '橙色',
  red: '红色'
}

export const timeoutColorLabels = colorLabels

export const timeoutColorTags: Record<TicketColor, string> = {
  green: '',
  blue: 'primary',
  orange: 'warning',
  red: 'danger'
}

export const timeoutRoleLabels: Record<StaffRoleType, string> = {
  operator: '操作员',
  supervisor: '主管',
  manager: '经理',
  department_head: '部门负责人',
  company_head: '公司负责人'
}

export const colorDescriptions: Record<TicketColor, string> = {
  green: '一般',
  blue: '普通',
  orange: '较急',
  red: '紧急'
}

export const priorityToTimeoutColor: Record<string, TicketColor> = {
  'Urgent': 'red',
  'High': 'orange',
  'Medium': 'blue',
  'Low': 'green'
}

export const getTimeoutColorByPriority = (priority: string): TicketColor => {
  return priorityToTimeoutColor[priority] || 'blue'
}

// 后端超时规则接口
export interface BackendTimeoutRule {
  id: number
  color: string
  role: string
  hours: number
  enabled: boolean
  createdAt: string
  updatedAt?: string
}

// 前端展示用接口
export interface TimeoutRuleDisplay {
  id: string
  color: TicketColor
  role: TimeoutRole
  hours: number
  enabled: boolean
}

// 存储键名
const STORAGE_KEY = 'wo_timeout_config_cache'

// 从 localStorage 加载缓存
const loadFromStorage = (): TimeoutRuleDisplay[] | null => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      return JSON.parse(saved)
    }
  } catch {}
  return null
}

// 保存到 localStorage
const saveToStorage = (rules: TimeoutRuleDisplay[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(rules))
  } catch {}
}

// 超时规则数据
const timeoutRules = ref<TimeoutRuleDisplay[]>(loadFromStorage() || [])

// 标记是否已加载
let isLoaded = false

// 获取所有超时规则
export const getAllTimeoutRules = (): TimeoutRuleDisplay[] => {
  return timeoutRules.value
}

// 从后端加载数据
export const loadTimeoutRulesFromApi = async () => {
  try {
    const res = await http.get('/api/timeout/rules')
    if (res.success && res.data) {
      timeoutRules.value = res.data.map((rule: BackendTimeoutRule) => ({
        id: `${rule.color}-${rule.role}`,
        color: rule.color as TicketColor,
        role: rule.role as TimeoutRole,
        hours: rule.hours,
        enabled: rule.enabled
      }))
      saveToStorage(timeoutRules.value)
      isLoaded = true
    }
  } catch (error) {
    console.error('加载超时规则失败:', error)
    // 使用缓存
    if (timeoutRules.value.length === 0) {
      timeoutRules.value = getDefaultRules()
    }
  }
}

// 获取默认规则
const getDefaultRules = (): TimeoutRuleDisplay[] => {
  const colors: TicketColor[] = ['green', 'blue', 'orange', 'red']
  const roles: TimeoutRole[] = ['operator', 'supervisor', 'manager', 'department_head', 'company_head']
  const rules: TimeoutRuleDisplay[] = []
  
  const defaultHours: Record<TicketColor, number> = {
    green: 24,
    blue: 12,
    orange: 6,
    red: 2
  }
  
  colors.forEach(color => {
    roles.forEach(role => {
      rules.push({
        id: `${color}-${role}`,
        color,
        role,
        hours: defaultHours[color],
        enabled: true
      })
    })
  })
  return rules
}

// 更新超时规则
export const updateTimeoutRuleById = async (id: string, updates: { hours?: number; enabled?: boolean }) => {
  try {
    // 从 id 解析 color 和 role
    const [color, role] = id.split('-') as [TicketColor, TimeoutRole]
    
    // 找到对应的规则
    const rule = timeoutRules.value.find(r => r.id === id)
    if (!rule) return false
    
    // 调用后端 API
    // 需要找到后端的 rule id (格式是 color-role，但后端是数字 id)
    // 先查找后端规则
    const res = await http.get('/api/timeout/rules')
    if (res.success && res.data) {
      const backendRule = res.data.find((r: BackendTimeoutRule) => 
        r.color === color && r.role === role
      )
      if (backendRule) {
        await http.put(`/api/timeout/rules/${backendRule.id}`, updates)
      }
    }
    
    // 更新本地数据
    if (updates.hours !== undefined) rule.hours = updates.hours
    if (updates.enabled !== undefined) rule.enabled = updates.enabled
    
    saveToStorage(timeoutRules.value)
    return true
  } catch (error) {
    console.error('更新超时规则失败:', error)
    return false
  }
}

// 重置为默认
export const resetTimeoutRules = async () => {
  try {
    await http.post('/api/timeout/rules/reset', {})
    await loadTimeoutRulesFromApi()
    return true
  } catch (error) {
    console.error('重置超时规则失败:', error)
    return false
  }
}

// 格式化超时小时数
export const formatTimeoutHours = (hours: number): string => {
  if (hours < 24) {
    return `${hours}小时`
  } else {
    const days = Math.floor(hours / 24)
    const remainingHours = hours % 24
    if (remainingHours === 0) {
      return `${days}天`
    }
    return `${days}天${remainingHours}小时`
  }
}

export const timeoutStore = {
  timeoutRules,
  getAllTimeoutRules,
  loadTimeoutRulesFromApi,
  updateTimeoutRuleById,
  resetTimeoutRules
}

// 初始化时自动加载
loadTimeoutRulesFromApi()
