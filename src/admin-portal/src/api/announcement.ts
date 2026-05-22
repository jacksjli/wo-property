import http from './http'

// 公告分类
export const ANNOUNCEMENT_CATEGORIES = [
  { value: 'property', label: '物业通知' },
  { value: 'security', label: '安全公告' },
  { value: 'maintenance', label: '设施维护' },
  { value: 'activity', label: '社区活动' },
  { value: 'emergency', label: '紧急通知' },
  { value: 'other', label: '其他' }
]

// 转换前端表单数据为后端格式
const toBackendData = (form: any) => ({
  title: form.title,
  content: form.content,
  type: form.category,
  priority: form.level || 'Normal',
  isTop: form.isPinned || false,
  status: form.status || 'draft'
})

export const announcementApi = {
  getList: (params?: { page?: number; pageSize?: number; status?: string; type?: string }) => {
    return http.get('/api/tenant/announcements', { params })
  },
  
  getById: (id: number) => {
    return http.get(`/api/tenant/announcements/${id}`)
  },
  
  create: (data: any) => {
    return http.post('/api/tenant/announcements', toBackendData(data))
  },
  
  update: (id: number, data: any) => {
    return http.put(`/api/tenant/announcements/${id}`, toBackendData(data))
  },
  
  delete: (id: number) => {
    return http.delete(`/api/tenant/announcements/${id}`)
  },
  
  getCategories: () => {
    return Promise.resolve({ data: ANNOUNCEMENT_CATEGORIES })
  }
}
