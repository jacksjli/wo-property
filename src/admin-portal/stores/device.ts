import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { 
  getDevices, 
  getDevice, 
  createDevice, 
  updateDevice, 
  deleteDevice,
  getDeviceCategories,
  getLocations,
  getMaintenanceHistory,
  createMaintenanceRecord,
  getDeviceStatistics
} from '@/api/device';
import type { 
  Device, 
  DeviceCategory, 
  Location, 
  MaintenanceRecord,
  CreateDeviceRequest, 
  UpdateDeviceRequest,
  CreateMaintenanceRecordRequest,
  DeviceQueryParams 
} from '@/api/device';

export const useDeviceStore = defineStore('device', () => {
  // 状态
  const devices = ref<Device[]>([]);
  const currentDevice = ref<Device | null>(null);
  const deviceCategories = ref<DeviceCategory[]>([]);
  const locations = ref<Location[]>([]);
  const maintenanceHistory = ref<MaintenanceRecord[]>([]);
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
  const deviceCount = computed(() => devices.value.length);
  const activeDevices = computed(() => devices.value.filter(d => d.status === 'Active'));
  const maintenanceDevices = computed(() => devices.value.filter(d => d.status === 'Maintenance'));
  const warningDevices = computed(() => devices.value.filter(d => d.currentStatus === 'Warning'));
  const faultDevices = computed(() => devices.value.filter(d => d.currentStatus === 'Fault'));

  // 获取设备列表
  const fetchDevices = async (params?: DeviceQueryParams) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getDevices(params);
      
      if (response.success && response.data) {
        devices.value = response.data;
        
        if (response.pagination) {
          pagination.value = response.pagination;
        }
        
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取设备列表失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取单个设备
  const fetchDevice = async (id: number) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getDevice(id);
      
      if (response.success && response.data) {
        currentDevice.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取设备详情失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 创建设备
  const createNewDevice = async (data: CreateDeviceRequest) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await createDevice(data);
      
      if (response.success && response.data) {
        devices.value.unshift(response.data);
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '创建设备失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 更新设备
  const updateExistingDevice = async (id: number, data: UpdateDeviceRequest) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await updateDevice(id, data);
      
      if (response.success && response.data) {
        const index = devices.value.findIndex(d => d.id === id);
        if (index !== -1) {
          devices.value[index] = response.data;
        }
        
        if (currentDevice.value?.id === id) {
          currentDevice.value = response.data;
        }
        
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '更新设备失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 删除设备
  const deleteExistingDevice = async (id: number) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await deleteDevice(id);
      
      if (response.success) {
        devices.value = devices.value.filter(d => d.id !== id);
        
        if (currentDevice.value?.id === id) {
          currentDevice.value = null;
        }
        
        return { success: true };
      } else {
        error.value = response.message || '删除设备失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取设备分类
  const fetchDeviceCategories = async () => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getDeviceCategories();
      
      if (response.success && response.data) {
        deviceCategories.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取设备分类失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取位置列表
  const fetchLocations = async () => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getLocations();
      
      if (response.success && response.data) {
        locations.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取位置列表失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取维护历史
  const fetchMaintenanceHistory = async (deviceId: number) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getMaintenanceHistory(deviceId);
      
      if (response.success && response.data) {
        maintenanceHistory.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取维护历史失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 创建维护记录
  const createNewMaintenanceRecord = async (deviceId: number, data: CreateMaintenanceRecordRequest) => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await createMaintenanceRecord(deviceId, data);
      
      if (response.success && response.data) {
        maintenanceHistory.value.unshift(response.data);
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '创建维护记录失败';
        return { success: false, message: response.message };
      }
    } catch (err: any) {
      error.value = err.message || '请求失败';
      return { success: false, message: error.value };
    } finally {
      loading.value = false;
    }
  };

  // 获取设备统计
  const fetchStatistics = async () => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await getDeviceStatistics();
      
      if (response.success && response.data) {
        statistics.value = response.data;
        return { success: true, data: response.data };
      } else {
        error.value = response.message || '获取设备统计失败';
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

  // 清除当前设备
  const clearCurrentDevice = () => {
    currentDevice.value = null;
  };

  // 清除维护历史
  const clearMaintenanceHistory = () => {
    maintenanceHistory.value = [];
  };

  return {
    // 状态
    devices,
    currentDevice,
    deviceCategories,
    locations,
    maintenanceHistory,
    loading,
    error,
    pagination,
    statistics,
    
    // 计算属性
    deviceCount,
    activeDevices,
    maintenanceDevices,
    warningDevices,
    faultDevices,
    
    // 方法
    fetchDevices,
    fetchDevice,
    createNewDevice,
    updateExistingDevice,
    deleteExistingDevice,
    fetchDeviceCategories,
    fetchLocations,
    fetchMaintenanceHistory,
    createNewMaintenanceRecord,
    fetchStatistics,
    clearError,
    clearCurrentDevice,
    clearMaintenanceHistory
  };
});
