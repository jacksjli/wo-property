import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5006'  // AccessControlService

const accessApi = createHttpClient(BASE_URL)

export interface Role {
  id: number
  code: string
  name: string
  description?: string
  permissions: string[]
  status: string  // active, inactive
  createdAt: string
  updatedAt: string
}

export interface Permission {
  id: string
  name: string
  description?: string
  module: string
  action: string
}

export interface CreateRoleDto {
  code: string
  name: string
  description?: string
  permissions: string[]
}

export const accessApi = {
  // 获取角色列表
  list: (params?: { keyword?: string; status?: string; page?: number; pageSize?: number }) =>
    accessApi.get<{ success: boolean; total: number; data: Role[] }>('/api/roles', { params }),

  // 获取单个角色
  get: (id: number) =>
    accessApi.get<{ success: boolean; data: Role }>(`/api/roles/${id}`),

  // 创建角色
  create: (data: CreateRoleDto) =>
    accessApi.post<{ success: boolean; data: Role; message: string }>('/api/roles', data),

  // 更新角色
  update: (id: number, data: Partial<Role>) =>
    accessApi.put<{ success: boolean; data: Role; message: string }>(`/api/roles/${id}`, data),

  // 删除角色
  delete: (id: number) =>
    accessApi.delete<{ success: boolean; message: string }>(`/api/roles/${id}`),

  // 获取权限列表
  getPermissions: () =>
    accessApi.get<{ success: boolean; data: Permission[] }>('/api/permissions'),

  // 获取所有角色（树形结构）
  getTree: () =>
    accessApi.get<{ success: boolean; data: Role[] }>('/api/roles/tree'),
}

export default accessApi
