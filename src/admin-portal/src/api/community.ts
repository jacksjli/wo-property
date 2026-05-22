import http from './http'

export const communityApi = {
  getActivities: (params?: { page?: number; pageSize?: number; type?: string }) => {
    return http.get('/api/tenant/community/activities', { params })
  },
  
  getById: (id: number) => {
    return http.get(`/api/tenant/community/activities/${id}`)
  },
  
  create: (data: any) => {
    return http.post('/api/tenant/community/activities', data)
  },
  
  update: (id: number, data: any) => {
    return http.put(`/api/tenant/community/activities/${id}`, data)
  },
  
  delete: (id: number) => {
    return http.delete(`/api/tenant/community/activities/${id}`)
  }
}
