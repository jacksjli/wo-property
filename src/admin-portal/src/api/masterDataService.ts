import { createHttpClient } from './http'

const client = createHttpClient('http://localhost:5019')

// FieldDefinition: response from MasterDataService
export interface FieldDefinition {
  id: number
  fieldKey: string
  displayName: string
  fieldType: 'text' | 'number' | 'date' | 'select' | 'textarea'
  source: string
  isShared: boolean
  module: string | null
  options: string[]
  defaultValue: any
  isRequired: boolean
  width: number
  sortOrder: number
  status: 'Active' | 'Inactive'
}

// ModuleField: association between module and field
export interface ModuleField {
  id: number
  module: string
  fieldDefinitionId: number
  sortOrder: number
  width?: number
  status: 'Active' | 'Inactive'
}

// API response wrapper
export interface ApiResponse<T> {
  data: T
  total: number
  page?: number
  pageSize?: number
}

export const masterDataApi = {
  /** 获取所有字段定义（分页） */
  getAllFields: (page: number = 1, pageSize: number = 100) =>
    client.get<ApiResponse<FieldDefinition[]>>('/api/field-definitions', { params: { page, pageSize } }),

  /** 获取所有共享字段定义 */
  getSharedFields: () =>
    client.get<ApiResponse<FieldDefinition[]>>('/api/field-definitions/shared'),

  /** 获取某模块可用的所有字段（私有 + 共享） */
  getFieldsByModule: (module: string) =>
    client.get<ApiResponse<FieldDefinition[]>>(`/api/field-definitions/by-module/${module}`),

  /** 获取某模块已选的字段（ModuleField 关联） */
  getModuleFields: (module: string) =>
    client.get<ApiResponse<ModuleField[]>>(`/api/module-fields/${module}`),

  /** 获取某模块的字段配置（含 alias 覆盖） */
  getModuleFieldConfig: (module: string) =>
    client.get<ApiResponse<Record<string, any>>>(`/api/module-fields/${module}/field-config`),

  /** 创建字段定义 */
  createFieldDefinition: (data: Partial<FieldDefinition>) =>
    client.post<FieldDefinition>('/api/field-definitions', data),

  /** 更新字段定义 */
  updateFieldDefinition: (id: number, data: Partial<FieldDefinition>) =>
    client.put<FieldDefinition>(`/api/field-definitions/${id}`, data),

  /** 为模块添加字段 */
  addModuleField: (module: string, fieldDefinitionId: number) =>
    client.post('/api/module-fields', { module, fieldDefinitionId }),

  /** 从模块移除字段 */
  removeModuleField: (id: number) =>
    client.delete(`/api/module-fields/${id}`),

  /** 更新模块字段（如排序、宽度、状态） */
  updateModuleField: (id: number, updates: Partial<ModuleField>) =>
    client.put(`/api/module-fields/${id}`, updates),
}

export default masterDataApi