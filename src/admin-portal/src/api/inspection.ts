import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5510'
const inspectionApi = createHttpClient(BASE_URL)

export interface InspectionRecord {
  id: number
  title: string
  location: string
  inspector: string
  inspectionDate: string
  result: string
  status: string
  createdAt: string
}

export const inspectionApi = {
  getAll: (params?: any) => inspectionApi.get('/api/tenant/inspection', { params }),
  getById: (id: number) => inspectionApi.get(`/api/tenant/inspection/${id}`),
  create: (data: Partial<InspectionRecord>) => inspectionApi.post('/api/tenant/inspection', data),
  update: (id: number, data: Partial<InspectionRecord>) => inspectionApi.put(`/api/tenant/inspection/${id}`, data),
  delete: (id: number) => inspectionApi.delete(`/api/tenant/inspection/${id}`),
}

export default inspectionApi
