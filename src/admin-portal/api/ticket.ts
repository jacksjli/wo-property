import { ticketHttp } from './http';
import { API_CONFIG } from './config';

// 获取工单列表
export const getTickets = async (params?: {
  status?: string;
  priority?: string;
  page?: number;
  pageSize?: number;
}) => {
  const response = await ticketHttp.get(API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKETS, { params });
  return response.data;
};

// 获取单个工单
export const getTicket = async (id: number) => {
  const response = await ticketHttp.get(API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKET(id));
  return response.data;
};

// 创建工单
export const createTicket = async (ticketData: {
  title: string;
  description: string;
  priority: string;
  category?: string;
}) => {
  const response = await ticketHttp.post(API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKETS, ticketData);
  return response.data;
};

// 更新工单
export const updateTicket = async (id: number, ticketData: any) => {
  const response = await ticketHttp.put(API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKET(id), ticketData);
  return response.data;
};

// 删除工单
export const deleteTicket = async (id: number) => {
  const response = await ticketHttp.delete(API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKET(id));
  return response.data;
};

// 分配工单
export const assignTicket = async (id: number, assigneeId: number) => {
  const response = await ticketHttp.post(`${API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKET(id)}/assign`, {
    assigneeId
  });
  return response.data;
};

// 获取工单统计
export const getTicketStatistics = async () => {
  const response = await ticketHttp.get(`${API_CONFIG.TICKET_SERVICE.ENDPOINTS.TICKETS}/statistics`);
  return response.data;
};

export default {
  getTickets,
  getTicket,
  createTicket,
  updateTicket,
  deleteTicket,
  assignTicket,
  getTicketStatistics
};
