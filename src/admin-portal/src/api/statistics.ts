import { createHttpClient } from './http'

// StatisticsService 端口 5250，路径 /api/tenant/statistics
import { getServiceUrl } from './config'
const BASE_URL = getServiceUrl('statistics')
const statisticsApi = createHttpClient(BASE_URL)

// 支持的 API
export const statisticsApi = {
  // GET /api/tenant/statistics/overview - 运营概览
  getOverview: () => statisticsApi.get('/api/tenant/statistics/overview'),

  // GET /api/tenant/statistics/tickets - 工单统计
  getTickets: (params?: { startDate?: string; endDate?: string; projectId?: number }) =>
    statisticsApi.get('/api/tenant/statistics/tickets', { params }),

  // GET /api/tenant/statistics/engineers - 工程师统计
  getEngineers: (params?: { startDate?: string; endDate?: string }) =>
    statisticsApi.get('/api/tenant/statistics/engineers', { params }),

  // GET /api/tenant/statistics/projects - 项目统计
  getProjects: (params?: { startDate?: string; endDate?: string }) =>
    statisticsApi.get('/api/tenant/statistics/projects', { params }),

  // GET /api/tenant/statistics/trends?days=30 - 趋势数据
  getTrends: (days = 30) =>
    statisticsApi.get('/api/tenant/statistics/trends', { params: { days } }),
}

export default statisticsApi