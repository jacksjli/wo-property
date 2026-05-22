import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('express'))

export const expressApi = {
  // 获取快递记录列表
  getRecords(params?: { page?: number; pageSize?: number; status?: string; keyword?: string }) {
    return httpClient.get('/api/tenant/express/express-records', { params })
  },

  // 获取单个记录
  getRecord(id: number) {
    return httpClient.get(`/api/tenant/express/express-records/${id}`)
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
    return httpClient.post('/api/tenant/express/express-records', data)
  },

  // 更新记录
  updateRecord(id: number, data: { status?: string; remarks?: string; pickupTime?: string }) {
    return httpClient.put(`/api/tenant/express/express-records/${id}`, data)
  },

  // 删除记录
  deleteRecord(id: number) {
    return httpClient.delete(`/api/tenant/express/express-records/${id}`)
  },

  // 获取房间待取快递
  getRoomPendingRecords(roomId: number) {
    return httpClient.get(`/api/tenant/express/express-records/rooms/${roomId}`)
  },
}

export default expressApi
