import { announcementApi } from './http'

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
  category: form.category,
  level: form.level || 'Normal',
  isTop: form.isPinned || false,
  status: form.status === 'published' ? 'Published' : 'Draft'
})

// 获取公告列表
export const getAnnouncementList = async (params?: {
  page?: number
  pageSize?: number
  status?: string
  type?: string
  keyword?: string
}) => {
  return announcementApi.get('/api/tenant/announcements', { params })
}

// 获取单个公告
export const getAnnouncementById = async (id: number) => {
  return announcementApi.get(`/api/tenant/announcements/${id}`)
}

// 创建公告
export const createAnnouncement = async (data: any) => {
  return announcementApi.post('/api/tenant/announcements', toBackendData(data))
}

// 更新公告
export const updateAnnouncement = async (id: number, data: any) => {
  return announcementApi.put(`/api/tenant/announcements/${id}`, toBackendData(data))
}

// 删除公告
export const deleteAnnouncement = async (id: number) => {
  return announcementApi.delete(`/api/tenant/announcements/${id}`)
}

// 发布公告
export const publishAnnouncement = async (id: number) => {
  return announcementApi.post(`/api/tenant/announcements/${id}/publish`)
}

// 获取分类选项
export const getAnnouncementCategories = () => {
  return Promise.resolve({ data: ANNOUNCEMENT_CATEGORIES })
}

export const announcementSvc = {
  getList: getAnnouncementList,
  getById: getAnnouncementById,
  create: createAnnouncement,
  update: updateAnnouncement,
  delete: deleteAnnouncement,
  publish: publishAnnouncement,
  getCategories: getAnnouncementCategories
}

export default announcementSvc
