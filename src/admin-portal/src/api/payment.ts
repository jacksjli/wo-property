import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5507'
const paymentApi = createHttpClient(BASE_URL)

export interface PaymentRecord {
  id: number
  paymentNumber: string
  amount: number
  paymentMethod: string
  payerName: string
  payerPhone: string
  status: string
  paidAt?: string
  createdAt: string
}

export const paymentApi = {
  getAll: (params?: any) => paymentApi.get('/api/tenant/payment', { params }),
  getById: (id: number) => paymentApi.get(`/api/tenant/payment/${id}`),
  create: (data: Partial<PaymentRecord>) => paymentApi.post('/api/tenant/payment', data),
  update: (id: number, data: Partial<PaymentRecord>) => paymentApi.put(`/api/tenant/payment/${id}`, data),
  delete: (id: number) => paymentApi.delete(`/api/tenant/payment/${id}`),
}

export default paymentApi
