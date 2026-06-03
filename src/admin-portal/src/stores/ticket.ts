import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { currentProject } from './project';
import {
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
} from '@/api/ticket';

export interface Ticket {
  id: number;
  ticketCode: string;
  title: string;
  description?: string;
  status: string;
  statusName?: string;
  priority: string;
  priorityName?: string;
  category: string;
  categoryName?: string;
  contactPhone?: string;
  address?: string;
  createdBy: number;
  creatorName?: string;
  assignedTo?: number;
  assigneeName?: string;
  createdAt: string;
  updatedAt?: string;
  completedAt?: string;
}

export interface TicketOptions {
  ticketTypes: Array<{ value: string; label: string }>;
  priorities: Array<{ value: string; label: string }>;
  categories: Array<{ value: string; label: string }>;
}

export const useTicketStore = defineStore('ticket', () => {
  // 状态
  const tickets = ref<Ticket[]>([]);
  const currentTicket = ref<Ticket | null>(null);
  const ticketOptions = ref<TicketOptions | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const pagination = ref({
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0
  });

  // 计算属性
  const ticketCount = computed(() => tickets.value.length);
  const openTickets = computed(() => tickets.value.filter(t => t.status === 'New' || t.status === 'InProgress'));
  const closedTickets = computed(() => tickets.value.filter(t => t.status === 'Resolved' || t.status === 'Closed'));
  const highPriorityTickets = computed(() => tickets.value.filter(t => t.priority === 'High'));

  // 获取工单选项
  const fetchTicketOptions = async () => {
    try {
      const response = await getTicketOptions();
      if (response.success && response.data) {
        ticketOptions.value = response.data;
        return { success: true, data: response.data };
      }
      return { success: false, message: response.message };
    } catch (err: any) {
      error.value = err.message || '获取工单选项失败';
      return { success: false, message: error.value };
    }
  };

  // 获取工单列表
  const fetchTickets = async (params?: {
    status?: string;
    priority?: string;
    category?: string;
    page?: number;
    pageSize?: number;
    keyword?: string;
  }) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await getTickets(params);

      if (response.success && response.data) {
        tickets.value = response.data.list || response.data;

        if (response.pagination) {
          pagination.value = response.pagination;
        } else if (response.data.total !== undefined) {
          pagination.value = {
            page: params?.page || 1,
            pageSize: params?.pageSize || 20,
            totalCount: response.data.total,
            totalPages: Math.ceil(response.data.total / (params?.pageSize || 20))
          };
        }

        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取工单列表失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取单个工单
  const fetchTicket = async (id: number) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await getTicket(id);

      if (response.success && response.data) {
        currentTicket.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取工单详情失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 创建工单
  const createNewTicket = async (data: {
    title: string;
    description?: string;
    priority: string;
    category?: string;
    contactPhone?: string;
    address?: string;
  }) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await createTicket({ ...data, projectId: currentProject.value?.id || 0 });

      if (response.success && response.data) {
        tickets.value.unshift(response.data);
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '创建工单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 保存工单
  const saveExistingTicket = async (id: number, data: {
    title?: string;
    description?: string;
    priority?: string;
    category?: string;
    contactPhone?: string;
    address?: string;
  }) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await saveTicket(id, data);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }

        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }

        return { success: true, data: response.data };
      } else {
        error.value = response.message || '保存工单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 派单
  const dispatchExistingTicket = async (id: number, assigneeId: number, remark?: string) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await dispatchTicket(id, assigneeId, remark);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '派单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 接单
  const acceptExistingTicket = async (id: number) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await acceptTicket(id);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '接单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 拒单
  const rejectExistingTicket = async (id: number, reason: string) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await rejectTicket(id, reason);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '拒单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 处理中
  const progressExistingTicket = async (id: number, remark?: string) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await progressTicket(id, remark);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '更新失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 完成
  const finishExistingTicket = async (id: number, solution?: string) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await finishTicket(id, solution);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '完成失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 确认完工
  const confirmExistingTicket = async (id: number) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await confirmTicket(id);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '确认失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 评价
  const rateExistingTicket = async (id: number, rating: number, comment?: string) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await rateTicket(id, rating, comment);

      if (response.success && response.data) {
        const index = tickets.value.findIndex(t => t.id === id);
        if (index !== -1) {
          tickets.value[index] = response.data;
        }
        if (currentTicket.value?.id === id) {
          currentTicket.value = response.data;
        }
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '评价失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 清除错误
  const clearError = () => {
    error.value = null;
  };

  // 清除当前工单
  const clearCurrentTicket = () => {
    currentTicket.value = null;
  };

  return {
    // 状态
    tickets,
    currentTicket,
    ticketOptions,
    loading,
    error,
    pagination,

    // 计算属性
    ticketCount,
    openTickets,
    closedTickets,
    highPriorityTickets,

    // 方法
    fetchTicketOptions,
    fetchTickets,
    fetchTicket,
    createNewTicket,
    saveExistingTicket,
    dispatchExistingTicket,
    acceptExistingTicket,
    rejectExistingTicket,
    progressExistingTicket,
    finishExistingTicket,
    confirmExistingTicket,
    rateExistingTicket,
    clearError,
    clearCurrentTicket,
  };
});