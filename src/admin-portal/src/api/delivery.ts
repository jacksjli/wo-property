import http from './http'

export const deliveryApi = {
  // 使用直接查询的 endpoint（兼容实际表结构）
  getList: (params?: { page?: number; pageSize?: number }) => {
    return http.get('/api/delivery-requests', { params })
  },
  
  getById: (id: number) => {
    return http.get(`/api/delivery-requests/${id}`)
  }
}
