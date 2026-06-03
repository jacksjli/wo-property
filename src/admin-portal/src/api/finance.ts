import { createHttpClient } from './http'

import { getServiceUrl } from './config'
const financeServiceUrl = getServiceUrl('finance')

// FinanceService — 端口 5509，路径 /api/tenant/finance
const financeApi = createHttpClient(financeServiceUrl)

// API 端点（匹配后端 TenantFinanceController）
const ENDPOINTS = {
  TRANSACTIONS: '/api/tenant/finance/transactions',
  TRANSACTION: (id: number) => `/api/tenant/finance/transactions/${id}`,
  ACCOUNTS: '/api/tenant/finance/accounts',
  BALANCE: '/api/tenant/finance/balance',
  REPORTS_MONTHLY: '/api/tenant/finance/reports/monthly',
  REPORTS_QUARTERLY: '/api/tenant/finance/reports/quarterly',
  REPORTS_YEARLY: '/api/tenant/finance/reports/yearly',
  TRANSFERS: '/api/tenant/finance/transfers',
  TRANSFER: (id: number) => `/api/tenant/finance/transfers/${id}`,
}

// ============ 交易记录（Transactions）============

export interface FinanceTransaction {
  id?: number
  recordNumber?: string
  type: 'income' | 'expense'
  category: string
  amount: number
  paymentMethod?: string
  recordDate?: string
  handler?: string
  relatedParty?: string
  contractNo?: string
  billNo?: string
  description?: string
  receiptNo?: string
  status?: string
  remarks?: string
  projectCode?: string
}

export const getTransactions = async (params?: {
  type?: string
  category?: string
  startDate?: string
  endDate?: string
  status?: string
  page?: number
  pageSize?: number
  keyword?: string
}) => {
  return financeApi.get(ENDPOINTS.TRANSACTIONS, { params })
}

export const getTransaction = async (id: number) => {
  return financeApi.get(ENDPOINTS.TRANSACTION(id))
}

export const createTransaction = async (data: FinanceTransaction) => {
  return financeApi.post(ENDPOINTS.TRANSACTIONS, data)
}

export const updateTransaction = async (id: number, data: Partial<FinanceTransaction>) => {
  return financeApi.put(ENDPOINTS.TRANSACTION(id), data)
}

export const deleteTransaction = async (id: number) => {
  return financeApi.delete(ENDPOINTS.TRANSACTION(id))
}

// ============ 账户（Accounts）============

export const getAccounts = async (params?: any) => {
  return financeApi.get(ENDPOINTS.ACCOUNTS, { params })
}

// ============ 余额（Balance）============

export const getBalance = async (params?: { accountId?: number }) => {
  return financeApi.get(ENDPOINTS.BALANCE, { params })
}

// ============ 报表（Reports）============

export const getReportMonthly = async (params?: { year?: number; month?: number }) => {
  return financeApi.get(ENDPOINTS.REPORTS_MONTHLY, { params })
}

export const getReportQuarterly = async (params?: { year?: number; quarter?: number }) => {
  return financeApi.get(ENDPOINTS.REPORTS_QUARTERLY, { params })
}

export const getReportYearly = async (params?: { year?: number }) => {
  return financeApi.get(ENDPOINTS.REPORTS_YEARLY, { params })
}

// ============ 转账（Transfers）============

export const getTransfers = async (params?: any) => {
  return financeApi.get(ENDPOINTS.TRANSFERS, { params })
}

export const createTransfer = async (data: {
  fromAccountId: number
  toAccountId: number
  amount: number
  remark?: string
  transferDate?: string
}) => {
  return financeApi.post(ENDPOINTS.TRANSFERS, data)
}

export default {
  getTransactions,
  getTransaction,
  createTransaction,
  updateTransaction,
  deleteTransaction,
  getAccounts,
  getBalance,
  getReportMonthly,
  getReportQuarterly,
  getReportYearly,
  getTransfers,
  createTransfer,
}