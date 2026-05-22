import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5019'  // MasterDataService

const dispatchApi = createHttpClient(BASE_URL)

export interface DispatchRule {
  id: number
  name: string
  description: string
  ticketType: string
  priority: string
  autoAssign: boolean
  assignToDepartment?: string
  assignToPerson?: string
  conditionType?: string
  conditionValue?: string
  enabled: boolean
  createdAt: string
  updatedAt: string
}

export interface TimeoutRule {
  id: number
  name: string
  ticketType: string
  priority: string
  timeoutMinutes: number
  warningMinutes: number
  actionType: string
  actionConfig?: string
  enabled: boolean
  createdAt: string
  updatedAt: string
}

export const dispatchApi = {
  // 获取派单规则列表
  getRules: (params?: { ticketType?: string; enabled?: boolean }) =>
    dispatchApi.get<{ success: boolean; data: DispatchRule[] }>('/api/dispatch/rules', { params }),

  // 获取单个派单规则
  getRule: (id: number) =>
    dispatchApi.get<{ success: boolean; data: DispatchRule }>(`/api/dispatch/rules/${id}`),

  // 创建派单规则
  createRule: (data: Partial<DispatchRule>) =>
    dispatchApi.post<{ success: boolean; data: DispatchRule; message: string }>('/api/dispatch/rules', data),

  // 更新派单规则
  updateRule: (id: number, data: Partial<DispatchRule>) =>
    dispatchApi.put<{ success: boolean; data: DispatchRule; message: string }>(`/api/dispatch/rules/${id}`, data),

  // 删除派单规则
  deleteRule: (id: number) =>
    dispatchApi.delete<{ success: boolean; message: string }>(`/api/dispatch/rules/${id}`),

  // 获取超时规则列表
  getTimeoutRules: () =>
    dispatchApi.get<{ success: boolean; data: TimeoutRule[] }>('/api/dispatch/timeout-rules'),

  // 获取单个超时规则
  getTimeoutRule: (id: number) =>
    dispatchApi.get<{ success: boolean; data: TimeoutRule }>(`/api/dispatch/timeout-rules/${id}`),

  // 创建超时规则
  createTimeoutRule: (data: Partial<TimeoutRule>) =>
    dispatchApi.post<{ success: boolean; data: TimeoutRule; message: string }>('/api/dispatch/timeout-rules', data),

  // 更新超时规则
  updateTimeoutRule: (id: number, data: Partial<TimeoutRule>) =>
    dispatchApi.put<{ success: boolean; data: TimeoutRule; message: string }>(`/api/dispatch/timeout-rules/${id}`, data),

  // 删除超时规则
  deleteTimeoutRule: (id: number) =>
    dispatchApi.delete<{ success: boolean; message: string }>(`/api/dispatch/timeout-rules/${id}`),

  // 手动派单
  manualDispatch: (ticketId: number, personId: number, remarks?: string) =>
    dispatchApi.post<{ success: boolean; message: string }>('/api/dispatch/manual', {
      ticketId,
      personId,
      remarks
    }),
}

export default dispatchApi
