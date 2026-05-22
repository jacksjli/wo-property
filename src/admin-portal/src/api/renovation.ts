import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5521'
const renovationApi = createHttpClient(BASE_URL)

export interface RenovationRecord {
  id: number
  roomNumber: string
  ownerName: string
  ownerPhone: string
  renovationType: string
  startDate: string
  endDate: string
  status: string
  createdAt: string
}

export const renovationApi = {
  getAll: (params?: any) => renovationApi.get('/api/tenant/renovation', { params }),
  getById: (id: number) => renovationApi.get(`/api/tenant/renovation/${id}`),
  create: (data: Partial<RenovationRecord>) => renovationApi.post('/api/tenant/renovation', data),
  update: (id: number, data: Partial<RenovationRecord>) => renovationApi.put(`/api/tenant/renovation/${id}`, data),
  delete: (id: number) => renovationApi.delete(`/api/tenant/renovation/${id}`),
}

export default renovationApi
