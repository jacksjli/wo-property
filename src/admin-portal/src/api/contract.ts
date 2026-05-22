import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5501'
const contractApi = createHttpClient(BASE_URL)

export interface Contract {
  id: number
  contractNumber: string
  title: string
  partyA: string
  partyB: string
  amount: number
  startDate: string
  endDate: string
  status: string
  createdAt: string
}

export const contractApi = {
  getAll: (params?: any) => contractApi.get('/api/tenant/contract', { params }),
  getById: (id: number) => contractApi.get(`/api/tenant/contract/${id}`),
  create: (data: Partial<Contract>) => contractApi.post('/api/tenant/contract', data),
  update: (id: number, data: Partial<Contract>) => contractApi.put(`/api/tenant/contract/${id}`, data),
  delete: (id: number) => contractApi.delete(`/api/tenant/contract/${id}`),
}

export default contractApi
