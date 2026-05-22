import { ticketApi } from './http'

// API 端点（匹配后端 TenantTicketController）
const ENDPOINTS = {
  TICKETS: '/api/tenant/tickets',
  TICKET: (id: number) => `/api/tenant/tickets/${id}`,
  OVERDUE: '/api/tenant/tickets/overdue',
  OPTIONS: '/api/tenant/tickets/options',
  DISPATCH: (id: number) => `/api/tenant/tickets/${id}/dispatch`,
  ACCEPT: (id: number) => `/api/tenant/tickets/${id}/accept`,
  REJECT: (id: number) => `/api/tenant/tickets/${id}/reject`,
  REASSIGN: (id: number) => `/api/tenant/tickets/${id}/reassign`,
  PROGRESS: (id: number) => `/api/tenant/tickets/${id}/progress`,
  FINISH: (id: number) => `/api/tenant/tickets/${id}/finish`,
  CONFIRM: (id: number) => `/api/tenant/tickets/${id}/confirm`,
  RATE: (id: number) => `/api/tenant/tickets/${id}/rate`,
}

// 获取工单列表
export const getTickets = async (params?: {
  status?: string;
  priority?: string;
  category?: string;
  page?: number;
  pageSize?: number;
  keyword?: string;
  areaId?: number;
  buildingId?: number;
  roomId?: number;
}) => {
  const response = await ticketApi.get(ENDPOINTS.TICKETS, { params });
  return response;
};

// 获取单个工单
export const getTicket = async (id: number) => {
  const response = await ticketApi.get(ENDPOINTS.TICKET(id));
  return response;
};

// 获取 Overdue 工单列表
export const getOverdueTickets = async () => {
  const response = await ticketApi.get(ENDPOINTS.OVERDUE);
  return response;
};

// 创建工单
export const createTicket = async (ticketData: {
  title: string;
  description?: string;
  priority: string;
  ticketTypeId?: number;
  category?: string;
  contactPhone?: string;
  address?: string;
  areaId?: number;
  buildingId?: number;
  roomId?: number;
  projectId?: number;
}) => {
  const response = await ticketApi.post(ENDPOINTS.TICKETS, ticketData);
  return response;
};

// 保存工单（更新）
export const saveTicket = async (id: number, ticketData: {
  title?: string;
  description?: string;
  priority?: string;
  status?: string;
}) => {
  const response = await ticketApi.put(ENDPOINTS.TICKET(id), ticketData);
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
  const response = await ticketApi.put(ENDPOINTS.REJECT(id), { reason });
  return response;
};

// 重新指派
export const reassignTicket = async (id: number, personId: number, reason?: string) => {
  const response = await ticketApi.put(ENDPOINTS.REASSIGN(id), {
    personId,
    reason
  });
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
  getOverdueTickets,
  createTicket,
  saveTicket,
  getTicketOptions,
  dispatchTicket,
  acceptTicket,
  rejectTicket,
  reassignTicket,
  progressTicket,
  finishTicket,
  confirmTicket,
  rateTicket,
};