import { ticketApi } from './http'

// TicketTypeService - via Gateway /api/ticket-types

export interface TicketType {
  id: number
  name: string
  code: string
  status: string
  sortOrder: number
}

export const ticketTypeApi = {
  getAll: () => ticketApi.get<{ success: boolean; data: TicketType[] }>('/api/ticket-types'),
  getById: (id: number) => ticketApi.get<{ success: boolean; data: TicketType }>(`/api/ticket-types/${id}`),
  create: (data: Partial<TicketType>) => ticketApi.post<{ success: boolean; data: TicketType }>('/api/ticket-types', data),
  update: (id: number, data: Partial<TicketType>) => ticketApi.put<{ success: boolean; data: TicketType }>(`/api/ticket-types/${id}`, data),
  delete: (id: number) => ticketApi.delete(`/api/ticket-types/${id}`)
}
