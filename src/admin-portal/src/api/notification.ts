import { notificationApi } from './http'

// API 端点
const ENDPOINTS = {
  NOTIFICATIONS: '/api/notifications',
  ANNOUNCEMENTS: '/api/announcements',
  MESSAGE_TEMPLATES: '/api/message-templates',
}

// 通知接口
export interface Notification {
  id: number;
  userId: number;
  title: string;
  content: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

// 公告接口
export interface Announcement {
  id: number;
  title: string;
  content: string;
  type: string;
  priority: string;
  startDate: string;
  endDate: string;
  createdAt: string;
}

// 消息模板接口
export interface MessageTemplate {
  id: number;
  name: string;
  type: string;
  content: string;
  variables: string[];
}

// 报告接口
export interface TicketReport {
  total: number;
  pending: number;
  inProgress: number;
  resolved: number;
  byPriority: any;
  byCategory: any;
}

export interface DeviceReport {
  total: number;
  active: number;
  maintenance: number;
  fault: number;
  recentMaintenance: any[];
}

export interface MaterialReport {
  total: number;
  lowStock: number;
  totalValue: number;
  categories: any[];
}

// 获取通知列表
export const getNotifications = async (params?: any) => {
  const response = await notificationApi.get(ENDPOINTS.NOTIFICATIONS, { params });
  return response.data;
};

// 获取公告列表
export const getAnnouncements = async (params?: any) => {
  const response = await notificationApi.get(ENDPOINTS.ANNOUNCEMENTS, { params });
  return response.data;
};

// 获取消息模板
export const getMessageTemplates = async (params?: any) => {
  const response = await notificationApi.get(ENDPOINTS.MESSAGE_TEMPLATES, { params });
  return response.data;
};

// 获取统计
export const getStatistics = async () => {
  const response = await notificationApi.get(`${ENDPOINTS.NOTIFICATIONS}/statistics`);
  return response.data;
};

// 标记通知为已读
export const markAsRead = async (id: number) => {
  const response = await notificationApi.put(`${ENDPOINTS.NOTIFICATIONS}/${id}/read`);
  return response.data;
};

// 发送通知
export const sendNotification = async (data: {
  userId: number;
  title: string;
  content: string;
  type: string;
}) => {
  const response = await notificationApi.post(ENDPOINTS.NOTIFICATIONS, data);
  return response.data;
};

// 获取未读通知数量
export const getUnreadCount = async () => {
  const response = await notificationApi.get(`${ENDPOINTS.NOTIFICATIONS}/unread-count`);
  return response.data;
};

// 导出API对象（兼容旧的调用方式）
export const notificationApi = {
  getNotifications,
  getAnnouncements,
  getMessageTemplates,
  getStatistics,
  markAsRead,
  sendNotification,
  getUnreadCount
};

export default {
  getNotifications,
  getAnnouncements,
  getMessageTemplates,
  getStatistics,
  markAsRead,
  sendNotification,
  getUnreadCount
};
