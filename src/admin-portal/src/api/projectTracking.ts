import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('projectTracking'))

export const projectTrackingApi = {
  // 获取项目列表
  getProjects(params?: { page?: number; pageSize?: number; status?: string; keyword?: string }) {
    return httpClient.get('/api/tenant/project-tracking', { params })
  },

  // 获取单个项目详情
  getProject(id: number) {
    return httpClient.get(`/api/tenant/project-tracking/${id}`)
  },

  // 创建项目
  createProject(data: any) {
    return httpClient.post('/api/tenant/project-tracking', data)
  },

  // 更新项目
  updateProject(id: number, data: any) {
    return httpClient.put(`/api/tenant/project-tracking/${id}`, data)
  },

  // 删除项目
  deleteProject(id: number) {
    return httpClient.delete(`/api/tenant/project-tracking/${id}`)
  },

  // 添加跟踪记录
  addRecord(projectId: number, data: { Type?: string; Content: string }) {
    return httpClient.post(`/api/tenant/project-tracking/${projectId}/records`, data)
  },

  // 删除跟踪记录
  deleteRecord(projectId: number, recordId: number) {
    return httpClient.delete(`/api/tenant/project-tracking/${projectId}/records/${recordId}`)
  },
}

export default projectTrackingApi
