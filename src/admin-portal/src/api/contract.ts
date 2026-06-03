import { createHttpClient } from './http'

// ContractService 端口 5501，路径 /api/tenant/contract/contracts
import { getServiceUrl } from './config'
const BASE_URL = getServiceUrl('contract')
const contractApi = createHttpClient(BASE_URL)

// API 端点（匹配后端 TenantContractController）
const ENDPOINTS = {
  CONTRACTS: '/api/tenant/contract/contracts',
  CONTRACT: (id: number) => `/api/tenant/contract/contracts/${id}`,
  EXPIRING: '/api/tenant/contract/contracts/expiring',
  ACTIVATE: (id: number) => `/api/tenant/contract/contracts/${id}/activate`,
  TERMINATE: (id: number) => `/api/tenant/contract/contracts/${id}/terminate`,
  RENEW: (id: number) => `/api/tenant/contract/contracts/${id}/renew`,
}

// Contract 类型
export interface Contract {
  id: number
  contractNumber: string
  contractName?: string
  name?: string
  contractType?: string
  type?: string
  partyA: string
  partyB: string
  amount: number
  signedDate?: string
  signDate?: string
  startDate: string
  endDate: string
  status: string
  remarks?: string
  remark?: string
  attachmentUrl?: string
  createdAt?: string
}

// 获取合同列表
export const getContracts = async (params?: {
  page?: number
  pageSize?: number
  status?: string
  keyword?: string
}) => {
  const response = await contractApi.get(ENDPOINTS.CONTRACTS, { params })
  return response
}

// 获取单个合同
export const getContract = async (id: number) => {
  const response = await contractApi.get(ENDPOINTS.CONTRACT(id))
  return response
}

// 创建合同
export const createContract = async (data: Partial<Contract>) => {
  const response = await contractApi.post(ENDPOINTS.CONTRACTS, data)
  return response
}

// 更新合同
export const updateContract = async (id: number, data: Partial<Contract>) => {
  const response = await contractApi.put(ENDPOINTS.CONTRACT(id), data)
  return response
}

// 删除合同
export const deleteContract = async (id: number) => {
  const response = await contractApi.delete(ENDPOINTS.CONTRACT(id))
  return response
}

// 激活合同
export const activateContract = async (id: number) => {
  const response = await contractApi.post(ENDPOINTS.ACTIVATE(id))
  return response
}

// 终止合同
export const terminateContract = async (id: number, reason?: string) => {
  const response = await contractApi.post(ENDPOINTS.TERMINATE(id), { reason })
  return response
}

// 续约合同
export const renewContract = async (id: number, data: { newEndDate: string; newAmount?: number }) => {
  const response = await contractApi.post(ENDPOINTS.RENEW(id), data)
  return response
}

// 获取即将到期合同
export const getExpiringContracts = async () => {
  const response = await contractApi.get(ENDPOINTS.EXPIRING)
  return response
}

// 覆盖 http.ts 中同名的 contractApi（补充完整方法）
export const contractService = {
  getContracts,
  getContract,
  createContract,
  updateContract,
  deleteContract,
  activateContract,
  terminateContract,
  renewContract,
  getExpiringContracts,
}

export default contractService