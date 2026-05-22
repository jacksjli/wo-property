import http from './http'

export const materialCategoryApi = {
  getList: () => {
    return http.get('/api/tenant/material/categories')
  },
  
  getById: (id: number) => {
    return http.get(`/api/tenant/material/categories/${id}`)
  },
  
  create: (data: { name: string; code: string; description?: string }) => {
    return http.post('/api/tenant/material/categories', data)
  },
  
  update: (id: number, data: { name?: string; code?: string; description?: string }) => {
    return http.put(`/api/tenant/material/categories/${id}`, data)
  },
  
  delete: (id: number) => {
    return http.delete(`/api/tenant/material/categories/${id}`)
  }
}