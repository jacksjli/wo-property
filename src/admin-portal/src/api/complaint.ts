import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5011'  // ComplaintService (待实现或确认端口)

const complaintApi = createHttpClient(BASE_URL)

export interface Complaint {
  id: number
  ticketNumber: string
  title: string
  type: string  // complaint, suggestion, feedback
  category: string
  priority: string  // low, medium, high, urgent
  description: string
  reporterName: string
  reporterPhone: string
  reporterRoom?: string
  status: string  // pending, processing, resolved, closed
  assignedTo?: number
  assignedToName?: string
  result?: string
  responseAt?: string
  resolvedAt?: string
  source: string  // phone, wechat, walkin, app
  createdAt: string
  updatedAt: string
}

export interface CreateComplaintDto {
  title: string
  type: string
  category?: string
  priority?: string
  description: string
  reporterName: string
  reporterPhone: string
  reporterRoom?: string
  source?: string
}

export interface UpdateComplaintDto {
  title?: string
  type?: string
  category?: string
  priority?: string
  status?: string
  assignedTo?: number
  result?: string
}

export const complaintApi = {
  // 获取投诉列表
  list: (params?: { 
    keyword?: string
    type?: string
    status?: string
    priority?: string
    startDate?: string
    endDate?: string
    page?: number
    pageSize?: number 
  }) =>
    complaintApi.get<{ success: boolean; total: number; data: Complaint[] }>('/api/complaints', { params }),

  // 获取单个投诉
  get: (id: number) =>
    complaintApi.get<{ success: boolean; data: Complaint }>(`/api/complaints/${id}`),

  // 创建投诉
  create: (data: CreateComplaintDto) =>
    complaintApi.post<{ success: boolean; data: Complaint; message: string }>('/api/complaints', data),

  // 更新投诉
  update: (id: number, data: UpdateComplaintDto) =>
    complaintApi.put<{ success: boolean; data: Complaint; message: string }>(`/api/complaints/${id}`, data),

  // 删除投诉
  delete: (id: number) =>
    complaintApi.delete<{ success: boolean; message: string }>(`/api/complaints/${id}`),

  // 处理投诉（分配/回应）
  process: (id: number, data: { assignedTo?: number; response?: string }) =>
    complaintApi.post<{ success: boolean; data: Complaint; message: string }>(`/api/complaints/${id}/process`, data),

  // 关闭投诉
  close: (id: number, result: string) =>
    complaintApi.post<{ success: boolean; message: string }>(`/api/complaints/${id}/close`, { result }),

  // 获取统计数据
  getStats: () =>
    complaintApi.get<{ success: boolean; data: {
      total: number
      pending: number
      processing: number
      resolved: number
      closed: number
      todayNew: number
      todayResolved: number
    } }>('/api/complaints/stats'),
}

export default complaintApi
