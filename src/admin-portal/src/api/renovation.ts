import { createHttpClient } from './http'

// RenovationService on port 5521
import { getServiceUrl } from './config'
const BASE_URL = getServiceUrl('renovation')
const renovationClient = createHttpClient(BASE_URL)

const ENDPOINTS = {
  LIST: '/api/tenant/renovations',
  DETAIL: (id: number) => `/api/tenant/renovations/${id}`,
  APPROVE: (id: number) => `/api/tenant/renovations/${id}/approve`,
  REJECT: (id: number) => `/api/tenant/renovations/${id}/reject`,
  INSPECT: (id: number) => `/api/tenant/renovations/${id}/inspect`,
  COMPLETE: (id: number) => `/api/tenant/renovations/${id}/complete`,
}

// GET list
export const getRenovations = async (params?: {
  status?: string;
  keyword?: string;
  page?: number;
  pageSize?: number;
}) => {
  return renovationClient.get(ENDPOINTS.LIST, { params })
}

// GET detail
export const getRenovationById = async (id: number) => {
  return renovationClient.get(ENDPOINTS.DETAIL(id))
}

// POST create
export const createRenovation = async (data: {
  roomId: number;
  applicantName: string;
  applicantPhone?: string;
  description: string;
  startDate?: string;
  endDate?: string;
  remarks?: string;
  projectCode?: string;
}) => {
  return renovationClient.post(ENDPOINTS.LIST, data)
}

// PUT update
export const updateRenovation = async (id: number, data: Partial<{
  applicantName: string;
  applicantPhone: string;
  description: string;
  startDate: string;
  endDate: string;
  remarks: string;
}>) => {
  return renovationClient.put(ENDPOINTS.DETAIL(id), data)
}

// DELETE
export const deleteRenovation = async (id: number) => {
  return renovationClient.delete(ENDPOINTS.DETAIL(id))
}

// POST approve
export const approveRenovation = async (id: number) => {
  return renovationClient.post(ENDPOINTS.APPROVE(id))
}

// POST reject
export const rejectRenovation = async (id: number) => {
  return renovationClient.post(ENDPOINTS.REJECT(id))
}

// POST inspect
export const inspectRenovation = async (id: number) => {
  return renovationClient.post(ENDPOINTS.INSPECT(id))
}

// POST complete
export const completeRenovation = async (id: number) => {
  return renovationClient.post(ENDPOINTS.COMPLETE(id))
}

export default {
  getRenovations,
  getRenovationById,
  createRenovation,
  updateRenovation,
  deleteRenovation,
  approveRenovation,
  rejectRenovation,
  inspectRenovation,
  completeRenovation,
}