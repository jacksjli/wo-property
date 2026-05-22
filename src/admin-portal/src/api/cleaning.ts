import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5516'
const cleaningApi = createHttpClient(BASE_URL)

export interface CleaningTask {
  id: number
  title: string
  content: string
  type: string
  status: string
  scheduledDate: string
  assignedTo?: number
  createdAt: string
}

export const cleaningApi = {
  getAll: (params?: any) => cleaningApi.get('/api/tenant/cleaning', { params }),
  getById: (id: number) => cleaningApi.get(`/api/tenant/cleaning/${id}`),
  create: (data: Partial<CleaningTask>) => cleaningApi.post('/api/tenant/cleaning', data),
  update: (id: number, data: Partial<CleaningTask>) => cleaningApi.put(`/api/tenant/cleaning/${id}`, data),
  delete: (id: number) => cleaningApi.delete(`/api/tenant/cleaning/${id}`),
}

export default cleaningApi
