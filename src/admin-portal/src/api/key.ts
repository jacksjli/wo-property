import { createHttpClient } from './http'

import { getServiceUrl } from './config'
const BASE_URL = getServiceUrl('key')
const keyApi = createHttpClient(BASE_URL)

export interface KeyRecord {
  id: number
  keyName: string
  keyCode: string
  location: string
  status: string
  borrower?: string
  borrowDate?: string
  returnDate?: string
  createdAt: string
}

export const keyApi = {
  getAll: (params?: any) => keyApi.get('/api/tenant/keys', { params }),
  getById: (id: number) => keyApi.get(`/api/tenant/keys/${id}`),
  create: (data: Partial<KeyRecord>) => keyApi.post('/api/tenant/keys', data),
  update: (id: number, data: Partial<KeyRecord>) => keyApi.put(`/api/tenant/keys/${id}`, data),
  delete: (id: number) => keyApi.delete(`/api/tenant/keys/${id}`),
}

export default keyApi
