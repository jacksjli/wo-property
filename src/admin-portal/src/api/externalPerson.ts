import http from './http'

export const externalPersonApi = {
  getList: (params?: { page?: number; pageSize?: number; type?: string }) => {
    return http.get('/api/tenant/visitor/visitors/external-persons', { params })
  },
  
  getById: (id: number) => {
    return http.get(`/api/tenant/visitor/visitors/external-persons/${id}`)
  },
  
  create: (data: { name: string; phone: string; type: string }) => {
    return http.post('/api/tenant/visitor/visitors/external-persons', data)
  },
  
  update: (id: number, data: { name?: string; phone?: string; type?: string }) => {
    return http.put(`/api/tenant/visitor/visitors/external-persons/${id}`, data)
  },
  
  delete: (id: number) => {
    return http.delete(`/api/tenant/visitor/visitors/external-persons/${id}`)
  }
}