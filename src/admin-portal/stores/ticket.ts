import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { 
  getTickets, 
  getTicket, 
  createTicket, 
  updateTicket, 
  deleteTicket,
  assignTicket,
  getTicketStatistics 
} from '@/api/ticket';
import type { 
  Ticket, 
  CreateTicketRequest, 
  UpdateTicketRequest,
  TicketQueryParams 
} from '@/api/ticket';

export const useTicketStore = defineStore('ticket', () => {
  // 状态
  const tickets = ref<Ticket[]>([]);
  const currentTicket = ref<Ticket | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const pagination = ref({
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0
  });
  const statistics = ref<any>(null);

  // 计算属性
  const ticketCount = computed(() => tickets.value.length);
  const openTickets = computed(() => tickets.value.filter(t => t.status === 'New' || t.status === 'InProgress'));
  const closedTickets = computed(() => tickets.value.filter(t => t.status === 'Resolved' || t.status === 'Closed'));
  const highPriorityTickets = computed(() => tickets.value.filter(t => t.priority === 'High'));

  // 获取工单列表
  const fetchTickets = async (params?: TicketQueryParams) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getTickets(params);
      
      if (response.success && response.data) {
        tickets.value = response.data;
        
        if (response.pagination) {
          pagination.value = response.pagination;
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
  const createNewTicket = async (data: CreateTicketRequest) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await createTicket(data);
      
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

  // 更新工单
  const updateExistingTicket = async (id: number, data: UpdateTicketRequest) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await updateTicket(id, data);
      
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
        error.value = response.message || '更新工单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 删除工单
  const deleteExistingTicket = async (id: number) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await deleteTicket(id);
      
      if (response.success) {
        tickets.value = tickets.value.filter(t => t.id !== id);
        
        if (currentTicket.value?.id === id) {
          currentTicket.value = null;
        }
        
        return { success: true };
      } else {
        error.value = response.message || '删除工单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 分配工单
  const assignExistingTicket = async (id: number, assignedTo: number) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await assignTicket(id, assignedTo);
      
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
        error.value = response.message || '分配工单失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取工单统计
  const fetchStatistics = async () => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getTicketStatistics();
      
      if (response.success && response.data) {
        statistics.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取统计失败';
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
    loading,
    error,
    pagination,
    statistics,
    
    // 计算属性
    ticketCount,
    openTickets,
    closedTickets,
    highPriorityTickets,
    
    // 方法
    fetchTickets,
    fetchTicket,
    createNewTicket,
    updateExistingTicket,
    deleteExistingTicket,
    assignExistingTicket,
    fetchStatistics,
    clearError,
    clearCurrentTicket
  };
});
