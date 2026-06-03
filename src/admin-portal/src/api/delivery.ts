import { createHttpClient } from './http'
import { getServiceUrl } from './config'

// DeliveryService 端口 5017，路径 /api/tenant/delivery
const deliveryHttp = createHttpClient(getServiceUrl('delivery') + '/api/tenant')

const ENDPOINTS = {
  DELIVERY_LIST: '/delivery',
  DELIVERY: (id: number) => `/delivery/${id}`,
}

// 获取配送列表
export const getDeliveryList = async (params?: {
  page?: number
  pageSize?: number
  status?: string
  keyword?: string
}) => {
  return deliveryHttp.get(ENDPOINTS.DELIVERY_LIST, { params })
}

// 获取配送详情
export const getDeliveryById = async (id: number) => {
  return deliveryHttp.get(ENDPOINTS.DELIVERY(id))
}

// 创建配送
export const createDelivery = async (data: {
  orderNumber?: string
  customerName: string
  customerPhone: string
  roomId?: number
  merchantName?: string
  totalAmount?: number
  remark?: string
}) => {
  return deliveryHttp.post(ENDPOINTS.DELIVERY_LIST, data)
}

// 更新配送
export const updateDelivery = async (id: number, data: {
  status?: string
  customerName?: string
  customerPhone?: string
  remark?: string
}) => {
  return deliveryHttp.put(ENDPOINTS.DELIVERY(id), data)
}

// 删除配送
export const deleteDelivery = async (id: number) => {
  return deliveryHttp.delete(ENDPOINTS.DELIVERY(id))
}

export default {
  getDeliveryList,
  getDeliveryById,
  createDelivery,
  updateDelivery,
  deleteDelivery,
}