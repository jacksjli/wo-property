import { ref } from 'vue'
import { roleLabels, type StaffRole } from '@/stores/staff'

// 颜色类型
export type TicketColor = 'green' | 'blue' | 'orange' | 'red'
export type TimeoutColor = TicketColor  // 别名

// 角色类型
export type StaffRoleType = StaffRole
export type TimeoutRole = StaffRoleType  // 别名

// 颜色标签
export const colorLabels: Record<TicketColor, string> = {
  green: '绿色',
  blue: '蓝色',
  orange: '橙色',
  red: '红色'
}

// 超时设置专用的颜色标签
export const timeoutColorLabels = colorLabels

// 颜色标签（带样式的tag对象）
export const timeoutColorTags: Record<TicketColor, string> = {
  green: '',
  blue: 'primary',
  orange: 'warning',
  red: 'danger'
}

// 角色标签
export const timeoutRoleLabels: Record<StaffRoleType, string> = {
  operator: '操作员',
  supervisor: '主管',
  manager: '经理',
  department_head: '部门负责人',
  company_head: '公司负责人'
}

// 颜色说明
export const colorDescriptions: Record<TicketColor, string> = {
  green: '一般',
  blue: '普通',
  orange: '较急',
  red: '紧急'
}

// 优先级 → 超时颜色 映射
export const priorityToTimeoutColor: Record<string, TicketColor> = {
  'Urgent': 'red',
  'High': 'orange',
  'Medium': 'blue',
  'Low': 'green'
}

// 根据优先级获取超时颜色
export const getTimeoutColorByPriority = (priority: string): TicketColor => {
  return priorityToTimeoutColor[priority] || 'blue'
}

// 超时配置（颜色 × 角色）
export interface TimeoutRule {
  hours: number       // 超时小时数
  enabled: boolean    // 是否启用
}

export type TimeoutConfig = Record<TicketColor, Record<StaffRoleType, TimeoutRule>>

// 存储键名
const STORAGE_KEY = 'wo_timeout_config'

// 从 localStorage 加载数据
const loadFromStorage = (): TimeoutConfig => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      // 确保数据格式正确
      if (parsed && typeof parsed === 'object') {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载超时配置数据失败:', error)
  }
  return {} as TimeoutConfig
}

// 保存到 localStorage
const saveToStorage = (data: TimeoutConfig) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存超时配置数据失败:', error)
  }
}

// 默认超时配置
const defaultTimeoutConfig: TimeoutConfig = {
  green: {
    operator: { hours: 24, enabled: true },
    supervisor: { hours: 24, enabled: true },
    manager: { hours: 24, enabled: true },
    department_head: { hours: 24, enabled: true },
    company_head: { hours: 24, enabled: true }
  },
  blue: {
    operator: { hours: 12, enabled: true },
    supervisor: { hours: 12, enabled: true },
    manager: { hours: 12, enabled: true },
    department_head: { hours: 12, enabled: true },
    company_head: { hours: 12, enabled: true }
  },
  orange: {
    operator: { hours: 6, enabled: true },
    supervisor: { hours: 6, enabled: true },
    manager: { hours: 6, enabled: true },
    department_head: { hours: 6, enabled: true },
    company_head: { hours: 6, enabled: true }
  },
  red: {
    operator: { hours: 2, enabled: true },
    supervisor: { hours: 2, enabled: true },
    manager: { hours: 2, enabled: true },
    department_head: { hours: 2, enabled: true },
    company_head: { hours: 2, enabled: true }
  }
}

// 超时配置数据
const timeoutConfig = ref<TimeoutConfig>(loadFromStorage())

// 如果没有数据，使用默认数据
if (Object.keys(timeoutConfig.value).length === 0) {
  timeoutConfig.value = defaultTimeoutConfig
  saveToStorage(timeoutConfig.value)
}

// 获取配置
export const getTimeoutConfig = () => timeoutConfig.value

// 获取特定颜色和角色的超时规则
export const getTimeoutRule = (color: TicketColor, role: StaffRoleType): TimeoutRule => {
  return timeoutConfig.value[color][role]
}

// 更新特定颜色和角色的超时规则
export const updateTimeoutRule = (color: TicketColor, role: StaffRoleType, rule: Partial<TimeoutRule>) => {
  timeoutConfig.value[color][role] = { ...timeoutConfig.value[color][role], ...rule }
  saveToStorage(timeoutConfig.value)  // 自动保存
}

// 更新整个配置
export const updateTimeoutConfig = (config: Partial<TimeoutConfig>) => {
  timeoutConfig.value = { ...timeoutConfig.value, ...config }
  saveToStorage(timeoutConfig.value)  // 自动保存
}

// 重置为默认
export const resetTimeoutConfig = () => {
  timeoutConfig.value = JSON.parse(JSON.stringify(defaultTimeoutConfig))
  saveToStorage(timeoutConfig.value)  // 自动保存
}

// 获取所有颜色
export const getAllColors = (): TicketColor[] => ['green', 'blue', 'orange', 'red']

// 获取所有角色
export const getAllRoles = (): StaffRoleType[] => ['operator', 'supervisor', 'manager', 'department_head', 'company_head']

// 超时规则接口（用于表格展示）
export interface TimeoutRuleDisplay {
  id: string
  color: TicketColor
  role: StaffRoleType
  hours: number
  enabled: boolean
}

// 获取所有超时规则（扁平化格式）
export const getAllTimeoutRules = (): TimeoutRuleDisplay[] => {
  const rules: TimeoutRuleDisplay[] = []
  const colors = getAllColors()
  const roles = getAllRoles()
  
  colors.forEach(color => {
    roles.forEach(role => {
      const rule = timeoutConfig.value[color]?.[role]
      if (rule) {
        rules.push({
          id: `${color}-${role}`,
          color,
          role,
          hours: rule.hours,
          enabled: rule.enabled
        })
      }
    })
  })
  
  return rules
}

// 更新超时规则
export const updateTimeoutRuleById = (id: string, updates: Partial<TimeoutRule>) => {
  const [color, role] = id.split('-') as [TicketColor, StaffRoleType]
  if (color && role) {
    updateTimeoutRule(color, role, updates)
  }
}

// 重置超时规则（兼容组件）
export const resetTimeoutRules = () => {
  resetTimeoutConfig()
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
  timeoutConfig,
  getTimeoutConfig,
  getTimeoutRule,
  updateTimeoutRule,
  updateTimeoutConfig,
  resetTimeoutConfig,
  getAllColors,
  getAllRoles
}