import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5525'
const parkingApi = createHttpClient(BASE_URL)

export interface ParkingRecord {
  id: number
  plateNumber: string
  ownerName: string
  ownerPhone: string
  parkingSpace: string
  startDate: string
  endDate: string
  status: string
  createdAt: string
}

export const parkingApi = {
  getAll: (params?: any) => parkingApi.get('/api/tenant/parking', { params }),
  getById: (id: number) => parkingApi.get(`/api/tenant/parking/${id}`),
  create: (data: Partial<ParkingRecord>) => parkingApi.post('/api/tenant/parking', data),
  update: (id: number, data: Partial<ParkingRecord>) => parkingApi.put(`/api/tenant/parking/${id}`, data),
  delete: (id: number) => parkingApi.delete(`/api/tenant/parking/${id}`),
}

export default parkingApi
