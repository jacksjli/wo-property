import { createHttpClient } from './http'

// TicketTypeService - 端口 5029
const ticketTypeService = createHttpClient('http://localhost:5029')

export interface TicketType {
  id: number
  name: string
  code: string
  status: string
  sortOrder: number
}

export const ticketTypeApi = {
  getAll: () => ticketTypeService.get<{ success: boolean; data: TicketType[] }>('/api/ticket-types'),
  getById: (id: number) => ticketTypeService.get<{ success: boolean; data: TicketType }>(`/api/ticket-types/${id}`),
  create: (data: Partial<TicketType>) => ticketTypeService.post<{ success: boolean; data: TicketType }>('/api/ticket-types', data),
  update: (id: number, data: Partial<TicketType>) => ticketTypeService.put<{ success: boolean; data: TicketType }>(`/api/ticket-types/${id}`, data),
  delete: (id: number) => ticketTypeService.delete(`/api/ticket-types/${id}`)
}
