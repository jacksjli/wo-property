import { ref } from 'vue'
import { ticketTypeApi, type TicketType as TicketTypeApiModel } from '@/api/ticketType'

// 工单类型 - 兼容原有字段
export interface TicketType {
  id: number
  name: string       // 类型名称，如：报修、投诉、咨询
  icon: string       // 图标
  color: string      // 颜色
  status: 'Active' | 'Inactive'
}

// API 返回的 TicketType 转换为本地格式
const convertFromApi = (apiType: TicketTypeApiModel): TicketType => {
  const iconMap: Record<string, string> = {
    '报修': 'Tools',
    '投诉': 'Warning',
    '咨询': 'QuestionFilled',
    '建议': 'Edit',
    '设施报修': 'Tools',
    '网络问题': 'Connection',
    '电气问题': 'Lightning',
    '给排水': 'Water',
    '保洁服务': 'Brush',
    '投诉建议': 'ChatDotRound'
  }
  const colorMap: Record<string, string> = {
    '报修': '#409EFF',
    '投诉': '#F56C6C',
    '咨询': '#67C23A',
    '建议': '#E6A23C',
    '设施报修': '#409EFF',
    '网络问题': '#9C27B0',
    '电气问题': '#FF9800',
    '给排水': '#2196F3',
    '保洁服务': '#4CAF50',
    '投诉建议': '#F44336'
  }
  return {
    id: apiType.id,
    name: apiType.name,
    icon: iconMap[apiType.name] || 'Document',
    color: colorMap[apiType.name] || '#909399',
    status: apiType.status === 'Active' ? 'Active' : 'Inactive'
  }
}

// 从 localStorage 加载数据
const loadFromStorage = (): TicketType[] => {
  try {
    const saved = localStorage.getItem('wo_ticket_types')
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed.map(t => ({
          ...t,
          status: t.status || 'Active'
        }))
      }
    }
  } catch (error) {
    console.error('加载工单类型数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: TicketType[]) => {
  try {
    localStorage.setItem('wo_ticket_types', JSON.stringify(data))
  } catch (error) {
    console.error('保存工单类型数据失败:', error)
  }
}

// 工单类型数据 - 默认空数组，强制从 API 加载
const ticketTypes = ref<TicketType[]>([])
const loading = ref(false)

let typeIdCounter = 1

// 从 API 加载数据
const fetchFromApi = async () => {
  loading.value = true
  try {
    const response = await ticketTypeApi.getAll()
    if (response.success && Array.isArray(response.data) && response.data.length > 0) {
      ticketTypes.value = response.data.map(convertFromApi)
      typeIdCounter = Math.max(...ticketTypes.value.map(t => t.id), 0) + 1
      console.log('工单类型已从 API 加载:', ticketTypes.value)
    } else if (ticketTypes.value.length === 0) {
      const saved = loadFromStorage()
      if (saved.length > 0) {
        ticketTypes.value = saved
      }
    }
  } catch (error) {
    console.warn('从 API 加载工单类型失败:', error)
    const saved = loadFromStorage()
    if (saved.length > 0) {
      ticketTypes.value = saved
    }
  } finally {
    loading.value = false
  }
}

// 获取所有类型
export const getAllTicketTypes = () => ticketTypes.value

// 获取启用的类型
export const getActiveTicketTypes = () => ticketTypes.value.filter(t => t.status === 'Active')

// 添加类型
export const addTicketType = async (type: Omit<TicketType, 'id'>): Promise<TicketType> => {
  const newType: TicketType = { ...type, id: typeIdCounter++ }
  ticketTypes.value.push(newType)
  saveToStorage(ticketTypes.value)
  return newType
}

// 更新类型
export const updateTicketType = (id: number, updates: Partial<TicketType>) => {
  const index = ticketTypes.value.findIndex(t => t.id === id)
  if (index !== -1) {
    ticketTypes.value[index] = { ...ticketTypes.value[index], ...updates }
    saveToStorage(ticketTypes.value)
  }
}

// 删除类型
export const deleteTicketType = (id: number) => {
  const index = ticketTypes.value.findIndex(t => t.id === id)
  if (index !== -1) {
    ticketTypes.value.splice(index, 1)
    saveToStorage(ticketTypes.value)
  }
}

// 切换状态
export const toggleTicketTypeStatus = (id: number) => {
  const type = ticketTypes.value.find(t => t.id === id)
  if (type) {
    type.status = type.status === 'Active' ? 'Inactive' : 'Active'
    saveToStorage(ticketTypes.value)
  }
}

// 检查类型是否存在
export const ticketTypeExists = (name: string) => {
  return ticketTypes.value.some(t => t.name === name)
}

// 重置为默认
export const resetTicketTypes = () => {
  ticketTypes.value = [
    { id: 1, name: '报修', icon: 'Tools', color: '#409EFF', status: 'Active' },
    { id: 2, name: '投诉', icon: 'Warning', color: '#F56C6C', status: 'Active' }
  ]
  typeIdCounter = 3
  saveToStorage(ticketTypes.value)
}

export const ticketTypeStore = {
  ticketTypes,
  loading,
  fetchFromApi,
  getAllTicketTypes,
  getActiveTicketTypes,
  addTicketType,
  updateTicketType,
  deleteTicketType,
  toggleTicketTypeStatus,
  ticketTypeExists,
  resetTicketTypes
}