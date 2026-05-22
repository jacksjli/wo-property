import { ticketTypeApi as baseTicketApi } from './http'

export interface TicketType {
  id: number
  name: string
  code: string
  icon?: string
  color?: string
  status: string
  sortOrder?: number
  jobTypes?: JobType[]
}

export interface JobType {
  id: number
  name: string
  code: string
  description?: string
  category?: string
  status: string
}

const api = baseTicketApi

export const ticketTypeApi = {
  getAll: () => api.get<{ success: boolean; data: TicketType[] }>('/api/ticket-types'),
  getById: (id: number) => api.get<{ success: boolean; data: TicketType }>(`/api/ticket-types/${id}`),
  create: (data: Partial<TicketType>) => api.post<{ success: boolean; data: TicketType }>('/api/ticket-types', data),
  update: (id: number, data: Partial<TicketType>) => api.put<{ success: boolean; data: TicketType }>(`/api/ticket-types/${id}`, data),
  delete: (id: number) => api.delete(`/api/ticket-types/${id}`),
  createJobType: (typeId: number, data: Partial<JobType>) => api.post<{ success: boolean; message?: string }>(`/api/ticket-types/${typeId}/job-types`, data),
}