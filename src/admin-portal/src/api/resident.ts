import { createHttpClient } from './http'

// Resident 数据由 PersonService 管理 (5018)
// 住户是人员的一种类型，通过 role='resident' 区分
const BASE_URL = 'http://localhost:5018'

const residentApi = createHttpClient(BASE_URL)

export interface Resident {
  id: number
  name: string
  phone: string
  idCard?: string
  buildingId?: number
  buildingName?: string
  roomId?: number
  roomNumber?: string
  relationship: string  // owner, tenant, family, other
  moveInDate?: string
  moveOutDate?: string
  status: string  // active, inactive
  emergencyContact?: string
  emergencyPhone?: string
  createdAt: string
  updatedAt: string
}

export const residentApi = {
  // 获取住户列表 (通过 role=resident 筛选)
  list: (params?: { 
    keyword?: string
    buildingId?: number
    roomId?: number
    status?: string
    page?: number
    pageSize?: number 
  }) =>
    residentApi.get<{ success: boolean; total: number; data: Resident[] }>('/api/persons', { 
      params: { ...params, role: 'resident', ...params } 
    }),

  // 获取单个住户
  get: (id: number) =>
    residentApi.get<{ success: boolean; data: Resident }>(`/api/persons/${id}`),

  // 创建住户
  create: (data: Partial<Resident>) =>
    residentApi.post<{ success: boolean; data: Resident; message: string }>('/api/persons', {
      ...data,
      role: 'resident'
    }),

  // 更新住户
  update: (id: number, data: Partial<Resident>) =>
    residentApi.put<{ success: boolean; data: Resident; message: string }>(`/api/persons/${id}`, data),

  // 删除住户
  delete: (id: number) =>
    residentApi.delete<{ success: boolean; message: string }>(`/api/persons/${id}`),

  // 获取楼栋列表
  getBuildings: () =>
    residentApi.get<{ success: boolean; data: Array<{ id: number; name: string }> }>('/api/buildings'),

  // 获取房号列表
  getRooms: (buildingId?: number) =>
    residentApi.get<{ success: boolean; data: Array<{ id: number; roomNumber: string }> }>('/api/rooms', {
      params: { buildingId }
    }),
}

export default residentApi
