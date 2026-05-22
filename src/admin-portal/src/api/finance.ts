import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5509'
const financeApi = createHttpClient(BASE_URL)

export interface FinanceRecord {
  id: number
  type: string
  amount: number
  description: string
  category: string
  recordDate: string
  status: string
  createdAt: string
}

export const financeApi = {
  getAll: (params?: any) => financeApi.get('/api/tenant/finance', { params }),
  getById: (id: number) => financeApi.get(`/api/tenant/finance/${id}`),
  create: (data: Partial<FinanceRecord>) => financeApi.post('/api/tenant/finance', data),
  update: (id: number, data: Partial<FinanceRecord>) => financeApi.put(`/api/tenant/finance/${id}`, data),
  delete: (id: number) => financeApi.delete(`/api/tenant/finance/${id}`),
}

export default financeApi
