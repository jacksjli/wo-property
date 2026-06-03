import { ticketApi } from './http'

const ENDPOINTS = {
  CLEANING_LIST: '/api/tenant/cleaning',
  CLEANING: (id: number) => `/api/tenant/cleaning/${id}`,
  ASSIGN: (id: number) => `/api/tenant/cleaning/${id}/assign`,
  COMPLETE: (id: number) => `/api/tenant/cleaning/${id}/complete`,
}

export interface CleaningTask {
  id: number
  title?: string
  content?: string
  buildingId?: number
  buildingName?: string
  cleaningArea: string
  cleanerName?: string
  cleaningType?: string
  planDate?: string
  actualDate?: string
  remarks?: string
  status: string
  qualityLevel?: string
  createdAt?: string
}

export interface CleaningQuery {
  status?: string
  keyword?: string
  page?: number
  pageSize?: number
}

// 获取清洁列表
export const getCleanings = async (params?: CleaningQuery) => {
  const res = await ticketApi.get(ENDPOINTS.CLEANING_LIST, { params })
  return res
}

// 获取清洁详情
export const getCleaningById = async (id: number) => {
  const res = await ticketApi.get(ENDPOINTS.CLEANING(id))
  return res
}

// 创建清洁计划
export const createCleaning = async (data: {
  buildingId: number
  cleaningArea: string
  cleanerName?: string
  cleaningType?: string
  planDate?: string
  remarks?: string
}) => {
  const res = await ticketApi.post(ENDPOINTS.CLEANING_LIST, data)
  return res
}

// 更新清洁计划
export const updateCleaning = async (id: number, data: Partial<CleaningTask>) => {
  const res = await ticketApi.put(ENDPOINTS.CLEANING(id), data)
  return res
}

// 删除清洁计划
export const deleteCleaning = async (id: number) => {
  const res = await ticketApi.delete(ENDPOINTS.CLEANING(id))
  return res
}

// 指派清洁人员
export const assignCleaning = async (id: number, assigneeId: number, remark?: string) => {
  const res = await ticketApi.post(ENDPOINTS.ASSIGN(id), { assigneeId, remark })
  return res
}

// 完成清洁
export const completeCleaning = async (id: number, solution?: string) => {
  const res = await ticketApi.post(ENDPOINTS.COMPLETE(id), { solution })
  return res
}

export default {
  getCleanings,
  getCleaningById,
  createCleaning,
  updateCleaning,
  deleteCleaning,
  assignCleaning,
  completeCleaning,
}