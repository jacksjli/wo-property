import { inspectionApi } from './http'

// API 端点（匹配后端 InspectionController）
const ENDPOINTS = {
  INSPECTIONS: '/api/tenant/inspections',
  INSPECTION: (id: number) => `/api/tenant/inspections/${id}`,
  RECORDS: '/api/tenant/inspections/records',
  EXECUTE: (id: number) => `/api/tenant/inspections/${id}/execute`,
}

// 获取巡检列表
export const getInspections = async (params?: {
  status?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
  keyword?: string;
  buildingId?: number;
  areaId?: number;
}) => {
  const response = await inspectionApi.get(ENDPOINTS.INSPECTIONS, { params })
  return response
}

// 获取单个巡检
export const getInspection = async (id: number) => {
  const response = await inspectionApi.get(ENDPOINTS.INSPECTION(id))
  return response
}

// 创建巡检
export const createInspection = async (data: {
  title: string
  buildingId?: number
  area?: string
  inspectorId?: number
  inspectorName?: string
  planDate?: string
  planTime?: string
  nextDate?: string
  remarks?: string
}) => {
  const response = await inspectionApi.post(ENDPOINTS.INSPECTIONS, data)
  return response
}

// 更新巡检
export const updateInspection = async (id: number, data: {
  title?: string
  buildingId?: number
  area?: string
  inspectorId?: number
  inspectorName?: string
  planDate?: string
  planTime?: string
  nextDate?: string
  remarks?: string
  status?: string
}) => {
  const response = await inspectionApi.put(ENDPOINTS.INSPECTION(id), data)
  return response
}

// 删除巡检
export const deleteInspection = async (id: number) => {
  const response = await inspectionApi.delete(ENDPOINTS.INSPECTION(id))
  return response
}

// 执行巡检
export const executeInspection = async (id: number, data: {
  result?: string
  findings?: string
  remark?: string
}) => {
  const response = await inspectionApi.post(ENDPOINTS.EXECUTE(id), data)
  return response
}

// 获取巡检记录
export const getInspectionRecords = async (params?: {
  inspectionId?: number
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
}) => {
  const response = await inspectionApi.get(ENDPOINTS.RECORDS, { params })
  return response
}

export default {
  getInspections,
  getInspection,
  createInspection,
  updateInspection,
  deleteInspection,
  executeInspection,
  getInspectionRecords,
}