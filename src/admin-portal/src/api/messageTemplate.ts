import http from './http'

export const messageTemplateApi = {
  getList: () => {
    return http.get('/api/tenant/notification/message-templates')
  },
  
  getById: (id: number) => {
    return http.get(`/api/tenant/notification/message-templates/${id}`)
  },
  
  create: (data: { name: string; type: string; subject: string; content: string; variables?: string }) => {
    return http.post('/api/tenant/notification/message-templates', data)
  },
  
  update: (id: number, data: { name?: string; type?: string; subject?: string; content?: string; variables?: string }) => {
    return http.put(`/api/tenant/notification/message-templates/${id}`, data)
  },
  
  delete: (id: number) => {
    return http.delete(`/api/tenant/notification/message-templates/${id}`)
  }
}