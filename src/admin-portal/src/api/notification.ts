import { createHttpClient } from './http'
import { getServiceUrl } from './config'

const httpClient = createHttpClient(getServiceUrl('notification'))

// API 端点（匹配后端 TenantNotificationController）
const ENDPOINTS = {
  LIST: '/api/tenant/notifications',
  NOTIFICATION: (id: number) => `/api/tenant/notifications/${id}`,
  MARK_READ: (id: number) => `/api/tenant/notifications/${id}/read`,
  STATS: '/api/tenant/notifications/stats',
}

// 获取通知列表
export const getNotifications = async (params?: {
  page?: number;
  pageSize?: number;
  type?: string;
  priority?: string;
  keyword?: string;
}) => {
  const response = await httpClient.get(ENDPOINTS.LIST, { params });
  return response;
};

// 获取单个通知
export const getNotification = async (id: number) => {
  const response = await httpClient.get(ENDPOINTS.NOTIFICATION(id));
  return response;
};

// 发送通知
export const createNotification = async (data: {
  userId?: number;
  title: string;
  content: string;
  type?: string;
  priority?: string;
}) => {
  const response = await httpClient.post(ENDPOINTS.LIST, data);
  return response;
};

// 更新通知
export const updateNotification = async (id: number, data: {
  title?: string;
  content?: string;
  type?: string;
  priority?: string;
}) => {
  const response = await httpClient.put(ENDPOINTS.NOTIFICATION(id), data);
  return response;
};

// 删除通知
export const deleteNotification = async (id: number) => {
  const response = await httpClient.delete(ENDPOINTS.NOTIFICATION(id));
  return response;
};

// 标记已读
export const markReadNotification = async (id: number) => {
  const response = await httpClient.put(ENDPOINTS.MARK_READ(id), {});
  return response;
};

// 获取统计
export const getNotificationStats = async () => {
  const response = await httpClient.get(ENDPOINTS.STATS);
  return response;
};

export default {
  getNotifications,
  getNotification,
  createNotification,
  updateNotification,
  deleteNotification,
  markReadNotification,
  getNotificationStats,
};