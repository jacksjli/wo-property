import { masterApi } from './http'
import type { AxiosInstance } from 'axios'

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

// 等价组条目
export interface FieldEquivalentGroup {
  groupKey: string
  members: string[]
}

/**
 * 带等价组关联信息的字段定义
 */
export interface FieldDefinitionWithEquivalents extends FieldDefinition {
  equivalentGroup?: string     // 所在等价组 key，如 "person_name"
  equivalentCount: number      // 该组有多少个成员（含自己）
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
  /** 获取所有字段定义（不分页，一次返回全部） */
  getAllFieldsNoPagination: () =>
    masterApi.get<ApiResponse<FieldDefinition[]>>('/api/field-definitions/all'),

  /** 获取所有字段定义（分页） */
  getAllFields: (page: number = 1, pageSize: number = 100) =>
    masterApi.get<ApiResponse<FieldDefinition[]>>('/api/field-definitions', { params: { page, pageSize } }),

  /** 获取所有共享字段定义 */
  getSharedFields: () =>
    masterApi.get<ApiResponse<FieldDefinition[]>>('/api/field-definitions/shared'),

  /** 获取某模块可用的所有字段（私有 + 共享） */
  getFieldsByModule: (module: string) =>
    masterApi.get<ApiResponse<FieldDefinition[]>>(`/api/field-definitions/by-module/${module}`),

  /** 获取某模块已选的字段（ModuleField 关联） */
  getModuleFields: (module: string) =>
    masterApi.get<ApiResponse<ModuleField[]>>(`/api/module-fields/${module}`),

  /** 获取某模块的字段配置（含 alias 覆盖） */
  getModuleFieldConfig: (module: string) =>
    masterApi.get<ApiResponse<Record<string, any>>>(`/api/module-fields/${module}/field-config`),

  /** 创建字段定义 */
  createFieldDefinition: (data: Partial<FieldDefinition>) =>
    masterApi.post<FieldDefinition>('/api/field-definitions', data),

  /** 更新字段定义 */
  updateFieldDefinition: (id: number, data: Partial<FieldDefinition>) =>
    masterApi.put<FieldDefinition>(`/api/field-definitions/${id}`, data),

  /** 为模块添加字段 */
  addModuleField: (module: string, fieldDefinitionId: number) =>
    masterApi.post('/api/module-fields', { module, fieldDefinitionId }),

  /** 从模块移除字段 */
  removeModuleField: (id: number) =>
    masterApi.delete(`/api/module-fields/${id}`),

  /** 更新模块字段（如排序、宽度、状态） */
  updateModuleField: (id: number, updates: Partial<ModuleField>) =>
    masterApi.put(`/api/module-fields/${id}`, updates),

  /** 获取字段等价组关系 */
  getFieldEquivalentGroups: () =>
    masterApi.get<ApiResponse<Record<string, string[]>>>(`/api/field-definition-equivalents`),
}

export default masterDataApi