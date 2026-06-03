import { ticketApi } from './http'

// API 端点（匹配后端 ParkingController）
const ENDPOINTS = {
  PARKINGS: '/api/tenant/parkings',
  PARKING: (id: number) => `/api/tenant/parkings/${id}`,
  CHECK_IN: (id: number) => `/api/tenant/parkings/${id}/check-in`,
  CHECK_OUT: (id: number) => `/api/tenant/parkings/${id}/check-out`,
  RECORDS: '/api/tenant/parkings/records',
}

// 获取车位列表
export const getParkings = async (params?: {
  status?: string
  spaceType?: string
  page?: number
  pageSize?: number
  keyword?: string
  buildingId?: number
}) => {
  const response = await ticketApi.get(ENDPOINTS.PARKINGS, { params })
  return response
}

// 获取单个车位
export const getParking = async (id: number) => {
  const response = await ticketApi.get(ENDPOINTS.PARKING(id))
  return response
}

// 创建车位
export const createParking = async (data: {
  parkingSpaceNumber: string
  buildingId?: number
  floor?: number
  spaceType: string
  licensePlate?: string
  residentId?: number
  startDate?: string
  endDate?: string
  monthlyFee?: number
  status: string
}) => {
  const response = await ticketApi.post(ENDPOINTS.PARKINGS, data)
  return response
}

// 更新车位
export const updateParking = async (id: number, data: {
  parkingSpaceNumber?: string
  buildingId?: number
  floor?: number
  spaceType?: string
  licensePlate?: string
  residentId?: number
  startDate?: string
  endDate?: string
  monthlyFee?: number
  status?: string
}) => {
  const response = await ticketApi.put(ENDPOINTS.PARKING(id), data)
  return response
}

// 删除车位
export const deleteParking = async (id: number) => {
  const response = await ticketApi.delete(ENDPOINTS.PARKING(id))
  return response
}

// 车辆入场
export const checkInParking = async (id: number, data: { licensePlate: string }) => {
  const response = await ticketApi.post(ENDPOINTS.CHECK_IN(id), data)
  return response
}

// 车辆出场
export const checkOutParking = async (id: number, data: { licensePlate: string }) => {
  const response = await ticketApi.post(ENDPOINTS.CHECK_OUT(id), data)
  return response
}

// 获取停车记录
export const getParkingRecords = async (params?: {
  page?: number
  pageSize?: number
  parkingId?: number
  startDate?: string
  endDate?: string
}) => {
  const response = await ticketApi.get(ENDPOINTS.RECORDS, { params })
  return response
}

export default {
  getParkings,
  getParking,
  createParking,
  updateParking,
  deleteParking,
  checkInParking,
  checkOutParking,
  getParkingRecords,
}