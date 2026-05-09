import { ticketApi } from './http'

// API 端点
const ENDPOINTS = {
  TICKETS: '/api/tickets',
  TICKET: (id: number) => `/api/tickets/${id}`,
  OPTIONS: '/api/tickets/options',
  DISPATCH: (id: number) => `/api/tickets/${id}/dispatch`,
  ACCEPT: (id: number) => `/api/tickets/${id}/accept`,
  REJECT: (id: number) => `/api/tickets/${id}/reject`,
  PROGRESS: (id: number) => `/api/tickets/${id}/progress`,
  FINISH: (id: number) => `/api/tickets/${id}/finish`,
  CONFIRM: (id: number) => `/api/tickets/${id}/confirm`,
  RATE: (id: number) => `/api/tickets/${id}/rate`,
}

// 获取工单列表
export const getTickets = async (params?: {
  status?: string;
  priority?: string;
  category?: string;
  page?: number;
  pageSize?: number;
  keyword?: string;
}) => {
  const response = await ticketApi.get(ENDPOINTS.TICKETS, { params });
  return response;
};

// 获取单个工单
export const getTicket = async (id: number) => {
  const response = await ticketApi.get(ENDPOINTS.TICKET(id));
  return response;
};

// 创建工单
export const createTicket = async (ticketData: {
  title: string;
  description?: string;
  priority: string;
  category?: string;
  contactPhone?: string;
  address?: string;
}) => {
  const response = await ticketApi.post(ENDPOINTS.TICKETS, ticketData);
  return response;
};

// 保存工单（更新）
export const saveTicket = async (id: number, ticketData: {
  title?: string;
  description?: string;
  priority?: string;
  category?: string;
  contactPhone?: string;
  address?: string;
}) => {
  const response = await ticketApi.post(ENDPOINTS.TICKET(id), ticketData);
  return response;
};

// 获取工单选项（枚举值）
export const getTicketOptions = async () => {
  const response = await ticketApi.get(ENDPOINTS.OPTIONS);
  return response;
};

// 派单
export const dispatchTicket = async (id: number, assigneeId: number, remark?: string) => {
  const response = await ticketApi.post(ENDPOINTS.DISPATCH(id), {
    assigneeId,
    remark
  });
  return response;
};

// 接单
export const acceptTicket = async (id: number) => {
  const response = await ticketApi.post(ENDPOINTS.ACCEPT(id));
  return response;
};

// 拒单
export const rejectTicket = async (id: number, reason: string) => {
  const response = await ticketApi.post(ENDPOINTS.REJECT(id), { reason });
  return response;
};

// 处理中
export const progressTicket = async (id: number, remark?: string) => {
  const response = await ticketApi.post(ENDPOINTS.PROGRESS(id), { remark });
  return response;
};

// 完成
export const finishTicket = async (id: number, solution?: string) => {
  const response = await ticketApi.post(ENDPOINTS.FINISH(id), { solution });
  return response;
};

// 确认完工
export const confirmTicket = async (id: number) => {
  const response = await ticketApi.post(ENDPOINTS.CONFIRM(id));
  return response;
};

// 评价
export const rateTicket = async (id: number, rating: number, comment?: string) => {
  const response = await ticketApi.post(ENDPOINTS.RATE(id), { rating, comment });
  return response;
};

export default {
  getTickets,
  getTicket,
  createTicket,
  saveTicket,
  getTicketOptions,
  dispatchTicket,
  acceptTicket,
  rejectTicket,
  progressTicket,
  finishTicket,
  confirmTicket,
  rateTicket,
};