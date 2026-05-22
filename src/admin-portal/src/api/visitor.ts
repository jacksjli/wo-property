import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('visitor'))

export const visitorApi = {
  getList(params?: { page?: number; pageSize?: number; status?: string }) {
    return httpClient.get('/api/tenant/visitor/visitors', { params })
  },

  get(id: number) {
    return httpClient.get(`/api/tenant/visitor/visitors/${id}`)
  },

  create(data: {
    visitorName: string
    visitorPhone?: string
    idCardNumber?: string
    visitPurpose?: string
    visitDate?: string
    visitTime?: string
    buildingId?: number
    roomId?: number
    hostName?: string
    hostPhone?: string
    remarks?: string
  }) {
    return httpClient.post('/api/tenant/visitor/visitors', data)
  },

  update(id: number, data: { status?: string; leaveTime?: string; remarks?: string }) {
    return httpClient.put(`/api/tenant/visitor/visitors/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`/api/tenant/visitor/visitors/${id}`)
  },

  checkIn(id: number) {
    return httpClient.post(`/api/tenant/visitor/visitors/${id}/check-in`)
  },

  checkOut(id: number) {
    return httpClient.post(`/api/tenant/visitor/visitors/${id}/check-out`)
  },
}

export default visitorApi