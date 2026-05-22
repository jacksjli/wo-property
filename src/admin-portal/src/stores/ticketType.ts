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
  return {
    id: apiType.id,
    name: apiType.name,
    icon: apiType.icon || 'Document',
    color: apiType.color || '#909399',
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

// 从 API 加载数据（导出供组件使用）
export const fetchFromApi = async () => {
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

// 添加类型（调用 API）
export const addTicketType = async (type: Omit<TicketType, 'id'>): Promise<TicketType> => {
  try {
    const response = await ticketTypeApi.create({
      name: type.name,
      icon: type.icon,
      color: type.color,
      status: type.status
    })
    if (response.success && response.data) {
      const newType: TicketType = convertFromApi(response.data)
      ticketTypes.value.push(newType)
      saveToStorage(ticketTypes.value)
      return newType
    } else {
      throw new Error(response.message || '创建失败')
    }
  } catch (error) {
    console.error('添加工单类型失败:', error)
    throw error
  }
}

// 更新类型（调用 API）
export const updateTicketType = async (id: number, updates: Partial<TicketType>): Promise<void> => {
  try {
    const response = await ticketTypeApi.update(id, {
      name: updates.name,
      icon: updates.icon,
      color: updates.color,
      status: updates.status
    })
    if (response.success) {
      const index = ticketTypes.value.findIndex(t => t.id === id)
      if (index !== -1) {
        ticketTypes.value[index] = { ...ticketTypes.value[index], ...updates }
        saveToStorage(ticketTypes.value)
      }
    } else {
      throw new Error(response.message || '更新失败')
    }
  } catch (error) {
    console.error('更新工单类型失败:', error)
    throw error
  }
}

// 删除类型（调用 API）
export const deleteTicketType = async (id: number): Promise<void> => {
  try {
    const response = await ticketTypeApi.delete(id)
    if (response.success) {
      const index = ticketTypes.value.findIndex(t => t.id === id)
      if (index !== -1) {
        ticketTypes.value.splice(index, 1)
        saveToStorage(ticketTypes.value)
      }
    } else {
      throw new Error(response.message || '删除失败')
    }
  } catch (error) {
    console.error('删除工单类型失败:', error)
    throw error
  }
}

// 切换状态（调用 API）
export const toggleTicketTypeStatus = async (id: number): Promise<void> => {
  const type = ticketTypes.value.find(t => t.id === id)
  if (!type) return
  
  const newStatus = type.status === 'Active' ? 'Inactive' : 'Active'
  try {
    const response = await ticketTypeApi.update(id, { status: newStatus })
    if (response.success) {
      type.status = newStatus
      saveToStorage(ticketTypes.value)
    } else {
      throw new Error(response.message || '状态更新失败')
    }
  } catch (error) {
    console.error('切换工单类型状态失败:', error)
    throw error
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