import http from './http'

export const reportsApi = {
  // ==================== 报表 API ====================
  getDeviceReports: (params?: { page?: number; pageSize?: number }) => {
    return http.get('/api/tenant/announcements/reports/device', { params })
  },

  getTicketReports: (params?: { page?: number; pageSize?: number }) => {
    return http.get('/api/tenant/announcements/reports/ticket', { params })
  },

  getMaterialReports: (params?: { page?: number; pageSize?: number }) => {
    return http.get('/api/tenant/announcements/reports/material', { params })
  },

  getSatisfactionSurveys: (params?: { page?: number; pageSize?: number }) => {
    return http.get('/api/tenant/announcements/reports/satisfaction', { params })
  },

  // ==================== 采购订单 ====================
  getPurchaseOrders: (params?: { page?: number; pageSize?: number }) => {
    return http.get('/api/tenant/announcements/purchase-orders', { params })
  },

  // ==================== 库存事务 ====================
  getStockTransactions: (params?: { page?: number; pageSize?: number; materialId?: number }) => {
    return http.get('/api/tenant/announcements/stock-transactions', { params })
  },

  // ==================== 枚举定义 ====================
  getEnumDefinitions: (params?: { category?: string }) => {
    return http.get('/api/tenant/announcements/enum-definitions', { params })
  },

  // ==================== 综合报表 ====================
  getGeneralReports: (params?: { page?: number; pageSize?: number; type?: string }) => {
    return http.get('/api/tenant/announcements/general-reports', { params })
  }
}