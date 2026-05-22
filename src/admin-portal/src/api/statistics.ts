import { createHttpClient } from './http'

const BASE_URL = 'http://localhost:5026'  // StatisticsService 端口
const statisticsApi = createHttpClient(BASE_URL)

export interface TicketStatistics {
  total: number
  byStatus: Record<string, number>
  byPriority: Record<string, number>
  byType: Record<string, number>
  byArea: Record<string, number>
}

export interface DashboardMetric {
  id: number
  metricName: string
  metricValue: number
  metricUnit?: string
  recordedAt: string
}

export const statisticsApi = {
  // 工单统计
  getTicketStats: (params?: any) => statisticsApi.get('/api/tenant/statistics/tickets', { params }),
  
  // 仪表盘指标
  getDashboardMetrics: () => statisticsApi.get('/api/tenant/statistics/dashboard'),
  
  // 趋势数据
  getTrendData: (type: string, startDate: string, endDate: string) =>
    statisticsApi.get('/api/tenant/statistics/trends', { params: { type, startDate, endDate } }),
  
  // 导出报表
  exportReport: (type: string, format: string) =>
    statisticsApi.get('/api/tenant/statistics/export', { params: { type, format } }),
}

export default statisticsApi
