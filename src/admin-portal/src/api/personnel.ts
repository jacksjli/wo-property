import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5018'  // PersonService

const personnelApi = createHttpClient(BASE_URL)

export interface Personnel {
  id: number
  employeeNo: string
  name: string
  gender: string
  birthday?: string
  idCard?: string
  phone: string
  email?: string
  address?: string
  department: string
  position: string
  role: string
  status: string
  avatar?: string
  lastLoginAt?: string
  createdAt: string
  updatedAt: string
}

export const personnelApi = {
  // 获取人员列表
  list: (params?: { keyword?: string; department?: string; status?: string; page?: number; pageSize?: number }) =>
    personnelApi.get<{ success: boolean; total: number; data: Personnel[] }>('/api/persons', { params }),

  // 获取单个人员
  get: (id: number) =>
    personnelApi.get<{ success: boolean; data: Personnel }>(`/api/persons/${id}`),

  // 创建人员
  create: (data: Partial<Personnel>) =>
    personnelApi.post<{ success: boolean; data: Personnel; message: string }>('/api/persons', data),

  // 更新人员
  update: (id: number, data: Partial<Personnel>) =>
    personnelApi.put<{ success: boolean; data: Personnel; message: string }>(`/api/persons/${id}`, data),

  // 删除人员
  delete: (id: number) =>
    personnelApi.delete<{ success: boolean; message: string }>(`/api/persons/${id}`),

  // 获取所有枚举值
  getEnums: () =>
    personnelApi.get<{ success: boolean; data: Record<string, string[]> }>('/api/enums'),

  // 获取关联记录
  getLinkedRecords: (personId: number) =>
    personnelApi.get<{ success: boolean; data: any[] }>(`/api/linked-records/${personId}`),
}

export default personnelApi
