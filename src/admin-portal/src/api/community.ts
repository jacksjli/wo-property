import http from './http'

const BASE = '/api/tenant/community'

export const communityApi = {
  // ============ Activities ============
  getActivities: (params?: { page?: number; pageSize?: number; type?: string }) => {
    return http.get(`${BASE}/activities`, { params })
  },
  getActivity: (id: number) => http.get(`${BASE}/activities/${id}`),
  createActivity: (data: any) => http.post(`${BASE}/activities`, data),
  updateActivity: (id: number, data: any) => http.put(`${BASE}/activities/${id}`, data),
  deleteActivity: (id: number) => http.delete(`${BASE}/activities/${id}`),

  // ============ Notices ============
  getNotices: (params?: { page?: number; pageSize?: number; type?: string }) => {
    return http.get(`${BASE}/notices`, { params })
  },
  getNotice: (id: number) => http.get(`${BASE}/notices/${id}`),
  createNotice: (data: any) => http.post(`${BASE}/notices`, data),
  updateNotice: (id: number, data: any) => http.put(`${BASE}/notices/${id}`, data),
  deleteNotice: (id: number) => http.delete(`${BASE}/notices/${id}`),

  // ============ Suggestions ============
  getSuggestions: (params?: { page?: number; pageSize?: number; type?: string }) => {
    return http.get(`${BASE}/suggestions`, { params })
  },
  getSuggestion: (id: number) => http.get(`${BASE}/suggestions/${id}`),
  createSuggestion: (data: any) => http.post(`${BASE}/suggestions`, data),
  updateSuggestion: (id: number, data: any) => http.put(`${BASE}/suggestions/${id}`, data),
  deleteSuggestion: (id: number) => http.delete(`${BASE}/suggestions/${id}`),
}