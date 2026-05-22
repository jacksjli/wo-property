import { deviceApi } from './http'

// API 端点
const ENDPOINTS = {
  DEVICES: '/api/tenant/devices',
  DEVICE: (id: number) => `/api/tenant/devices/${id}`,
  DEVICE_CATEGORIES: '/api/tenant/device-categories',
  LOCATIONS: '/api/tenant/locations',
  STATISTICS: '/api/tenant/devices/statistics',
  MAINTENANCE_HISTORY: (deviceId: number) => `/api/tenant/devices/${deviceId}/maintenance-history`,
  MAINTENANCE: (deviceId: number) => `/api/tenant/devices/${deviceId}/maintenance`,
}

// 获取设备列表
export const getDevices = async (params?: {
  categoryId?: number;
  status?: string;
}) => {
  const response = await deviceApi.get(ENDPOINTS.DEVICES, { params });
  return response.data;
};

// 获取单个设备
export const getDevice = async (id: number) => {
  const response = await deviceApi.get(ENDPOINTS.DEVICE(id));
  return response.data;
};

// 创建设备
export const createDevice = async (data: any) => {
  const response = await deviceApi.post(ENDPOINTS.DEVICES, data);
  return response.data;
};

// 更新设备
export const updateDevice = async (id: number, data: any) => {
  const response = await deviceApi.put(ENDPOINTS.DEVICE(id), data);
  return response.data;
};

// 删除设备
export const deleteDevice = async (id: number) => {
  const response = await deviceApi.delete(ENDPOINTS.DEVICE(id));
  return response.data;
};

// 获取设备分类
export const getDeviceCategories = async () => {
  const response = await deviceApi.get(ENDPOINTS.DEVICE_CATEGORIES);
  return response.data;
};

// 获取位置信息
export const getLocations = async () => {
  const response = await deviceApi.get(ENDPOINTS.LOCATIONS);
  return response.data;
};

// 获取设备统计
export const getDeviceStatistics = async () => {
  const response = await deviceApi.get(ENDPOINTS.STATISTICS);
  return response.data;
};

// 获取设备维护历史
export const getMaintenanceHistory = async (deviceId: number) => {
  const response = await deviceApi.get(ENDPOINTS.MAINTENANCE_HISTORY(deviceId));
  return response.data;
};

// 创建维护记录
export const createMaintenanceRecord = async (deviceId: number, data: any) => {
  const response = await deviceApi.post(ENDPOINTS.MAINTENANCE(deviceId), data);
  return response.data;
};

// 类型定义
export interface Device {
  id: number;
  code: string;
  name: string;
  model?: string;
  serialNumber?: string;
  categoryId: number;
  locationId?: number;
  purchaseDate?: string;
  warrantyEndDate?: string;
  status: string;
  currentStatus: string;
  notes?: string;
}

export interface DeviceCategory {
  id: number;
  name: string;
  code: string;
  description?: string;
}

export interface Location {
  id: number;
  name: string;
  type: string;
  parentId?: number;
}

export interface MaintenanceRecord {
  id: number;
  deviceId: number;
  maintenanceType: string;
  maintenanceDate: string;
  description: string;
  technician: string;
  cost: number;
  hours: number;
  notes?: string;
}

export interface CreateDeviceRequest {
  code: string;
  name: string;
  model?: string;
  serialNumber?: string;
  categoryId: number;
  locationId?: number;
  purchaseDate?: string;
  warrantyEndDate?: string;
  status?: string;
  currentStatus?: string;
  notes?: string;
}

export interface UpdateDeviceRequest {
  name?: string;
  model?: string;
  serialNumber?: string;
  categoryId?: number;
  locationId?: number;
  purchaseDate?: string;
  warrantyEndDate?: string;
  status?: string;
  currentStatus?: string;
  notes?: string;
}

export interface CreateMaintenanceRecordRequest {
  maintenanceType: string;
  maintenanceDate: string;
  description: string;
  technician: string;
  cost?: number;
  hours?: number;
  notes?: string;
}

export interface DeviceQueryParams {
  categoryId?: number;
  status?: string;
  page?: number;
  pageSize?: number;
}

export default {
  getDevices,
  getDevice,
  createDevice,
  updateDevice,
  deleteDevice,
  getDeviceCategories,
  getLocations,
  getDeviceStatistics,
  getMaintenanceHistory,
  createMaintenanceRecord
};
