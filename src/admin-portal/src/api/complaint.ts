import axios from 'axios'

const BASE_URL = 'http://localhost:5201'

// 创建 complaint 专用 client
const complaintClient = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' }
})

// 请求拦截器：附 token 和 tenant/project header
complaintClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  const tenantCode = localStorage.getItem('tenantCode')
  if (tenantCode) config.headers['X-Tenant'] = tenantCode
  const projectJson = localStorage.getItem('currentProject')
  if (projectJson) {
    try {
      const project = JSON.parse(projectJson)
      if (project.code) config.headers['X-Project'] = project.code
    } catch (e) { /* ignore */ }
  }
  return config
})

// 响应拦截器：直接返回 data（与 ticketApi 行为一致）
complaintClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    console.error('Complaint API error:', error)
    return Promise.reject(error)
  }
)

// ============ 接口定义 ============

export interface ComplaintDto {
  id: number
  complaintNo: string
  title: string
  type: string
  source?: string
  priority?: string
  description?: string
  complainantName: string
  complainantPhone?: string
  complainantRoom?: string
  location?: string
  handlerName?: string
  deadline?: string
  remark?: string
  handleStatus: string
  handleProgress?: string
  feedback?: string
  rating?: number
  createdAt: string
  updatedAt: string
}

export interface CreateComplaintDto {
  title: string
  type?: string
  source?: string
  priority?: string
  description?: string
  complainantName?: string
  complainantPhone?: string
  complainantRoom?: string
  location?: string
}

export interface UpdateComplaintDto {
  title?: string
  type?: string
  source?: string
  priority?: string
  description?: string
  handleStatus?: string
  handlerName?: string
  feedback?: string
  handleProgress?: string
  rating?: number
}

export interface ComplaintStats {
  total: number
  pending: number
  processing: number
  resolved: number
  closed: number
  todayNew: number
  todayResolved: number
}

// ============ API 函数 ============

const ENDPOINTS = {
  LIST: '/api/tenant/complaints',
  COMPLAINT: (id: number) => `/api/tenant/complaints/${id}`,
  STATS: '/api/tenant/complaints/stats',
  ACCEPT: (id: number) => `/api/tenant/complaints/${id}/accept`,
  RESOLVE: (id: number) => `/api/tenant/complaints/${id}/resolve`,
  CLOSE: (id: number) => `/api/tenant/complaints/${id}/close`,
  RATE: (id: number) => `/api/tenant/complaints/${id}/rate`,
}

// 获取投诉列表
export const getComplaints = async (params?: {
  keyword?: string
  type?: string
  status?: string
  priority?: string
  page?: number
  pageSize?: number
}) => {
  return complaintClient.get<any>(ENDPOINTS.LIST, { params })
}

// 获取单个投诉
export const getComplaint = async (id: number) => {
  return complaintClient.get<any>(ENDPOINTS.COMPLAINT(id))
}

// 创建投诉
export const createComplaint = async (data: CreateComplaintDto) => {
  return complaintClient.post<any>(ENDPOINTS.LIST, data)
}

// 更新投诉
export const updateComplaint = async (id: number, data: UpdateComplaintDto) => {
  return complaintClient.put<any>(ENDPOINTS.COMPLAINT(id), data)
}

// 删除投诉
export const deleteComplaint = async (id: number) => {
  return complaintClient.delete<any>(ENDPOINTS.COMPLAINT(id))
}

// 获取统计数据
export const getComplaintStats = async () => {
  return complaintClient.get<any>(ENDPOINTS.STATS)
}

// 受理投诉
export const acceptComplaint = async (id: number, handlerName?: string) => {
  return complaintClient.post<any>(ENDPOINTS.ACCEPT(id), { handlerName })
}

// 解决投诉
export const resolveComplaint = async (id: number, result?: string, handleProgress?: string) => {
  return complaintClient.post<any>(ENDPOINTS.RESOLVE(id), { result, handleProgress })
}

// 关闭投诉
export const closeComplaint = async (id: number, remark?: string) => {
  return complaintClient.post<any>(ENDPOINTS.CLOSE(id), { remark })
}

// 评价投诉
export const rateComplaint = async (id: number, rating: number, feedback?: string) => {
  return complaintClient.post<any>(ENDPOINTS.RATE(id), { rating, feedback })
}

export default {
  getComplaints,
  getComplaint,
  createComplaint,
  updateComplaint,
  deleteComplaint,
  getComplaintStats,
  acceptComplaint,
  resolveComplaint,
  closeComplaint,
  rateComplaint,
}