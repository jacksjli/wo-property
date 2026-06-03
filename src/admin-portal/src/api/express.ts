import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('express'))

const ENDPOINTS = {
  RECORDS: '/api/tenant/express',
  RECORD: (id: number) => `/api/tenant/express/${id}`,
  PICKUP: (id: number) => `/api/tenant/express/${id}/pickup`,
}

export const expressApi = {
  // 获取快递记录列表
  getRecords(params?: { page?: number; pageSize?: number; status?: string; keyword?: string }) {
    return httpClient.get(ENDPOINTS.RECORDS, { params })
  },

  // 获取单个记录
  getRecord(id: number) {
    return httpClient.get(ENDPOINTS.RECORD(id))
  },

  // 创建记录
  createRecord(data: {
    roomId: number
    recipientName: string
    recipientPhone?: string
    courierCompany?: string
    trackingNumber?: string
    pickupCode?: string
    remarks?: string
  }) {
    return httpClient.post(ENDPOINTS.RECORDS, data)
  },

  // 更新记录
  updateRecord(id: number, data: { status?: string; remarks?: string; pickupTime?: string }) {
    return httpClient.put(ENDPOINTS.RECORD(id), data)
  },

  // 删除记录
  deleteRecord(id: number) {
    return httpClient.delete(ENDPOINTS.RECORD(id))
  },

  // 确认取件（pickup）
  pickupRecord(id: number, pickupTime?: string) {
    return httpClient.post(ENDPOINTS.PICKUP(id), pickupTime ? { pickupTime } : {})
  },
}

export default expressApi