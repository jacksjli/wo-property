import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('notification'))

export const notificationApi = {
  getList(params?: { page?: number; pageSize?: number; type?: string }) {
    return httpClient.get('/api/tenant/notification/notifications', { params })
  },

  get(id: number) {
    return httpClient.get(`/api/tenant/notification/notifications/${id}`)
  },

  create(data: { userId?: number; title: string; content: string; type?: string; priority?: string }) {
    return httpClient.post('/api/tenant/notification/notifications', data)
  },

  update(id: number, data: { title?: string; content?: string; type?: string; priority?: string }) {
    return httpClient.put(`/api/tenant/notification/notifications/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`/api/tenant/notification/notifications/${id}`)
  },

  markRead(id: number) {
    return httpClient.put(`/api/tenant/notification/notifications/${id}/read`, {})
  },
}

export default notificationApi