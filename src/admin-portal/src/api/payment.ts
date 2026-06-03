import { createHttpClient } from './http'
import { getServiceUrl } from './config'

// PaymentService 运行在端口 5109，路径前缀 /api/tenant/payments
const paymentServiceUrl = getServiceUrl('payment')
const paymentHttp = createHttpClient(paymentServiceUrl)

// 路径前缀
const BASE = '/api/tenant/payments'

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

// 获取缴费记录列表
export const getPayments = (params?: {
  page?: number
  pageSize?: number
  status?: string
  keyword?: string
}) => {
  return paymentHttp.get(BASE, { params })
}

// 获取单个缴费记录
export const getPayment = (id: number) => {
  return paymentHttp.get(`${BASE}/${id}`)
}

// 创建缴费记录
export const createPayment = (data: Partial<PaymentRecord>) => {
  return paymentHttp.post(BASE, data)
}

// 更新缴费记录
export const updatePayment = (id: number, data: Partial<PaymentRecord>) => {
  return paymentHttp.put(`${BASE}/${id}`, data)
}

// 删除缴费记录
export const deletePayment = (id: number) => {
  return paymentHttp.delete(`${BASE}/${id}`)
}

// 确认支付
export const payPayment = (id: number, paymentMethod?: string) => {
  return paymentHttp.post(`${BASE}/${id}/pay`, { paymentMethod })
}

// 退款
export const refundPayment = (id: number, reason?: string) => {
  return paymentHttp.post(`${BASE}/${id}/refund`, { reason })
}

// 获取待支付记录
export const getPendingPayments = (params?: { page?: number; pageSize?: number }) => {
  return paymentHttp.get(`${BASE}/pending`, { params })
}

// 获取统计数据
export const getPaymentStats = (params?: { startDate?: string; endDate?: string }) => {
  return paymentHttp.get(`${BASE}/stats`, { params })
}

export default {
  getPayments,
  getPayment,
  createPayment,
  updatePayment,
  deletePayment,
  payPayment,
  refundPayment,
  getPendingPayments,
  getPaymentStats,
}
