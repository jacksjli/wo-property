import { materialApi as materialClient } from './http';
import type { ApiResponse } from './config';

// 物料分类接口
export interface MaterialCategory {
  id: number;
  name: string;
  description?: string;
  parentCategoryId?: number;
  createdAt: string;
}

// 物料接口
export interface Material {
  id: number;
  code: string;
  name: string;
  description?: string;
  categoryId: number;
  unit: string;
  unitPrice: number;
  safetyStock: number;
  maxStock: number;
  currentStock: number;
  location?: string;
  supplier?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

// 库存交易接口
export interface StockTransaction {
  id: number;
  materialId: number;
  transactionType: string;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  referenceNumber?: string;
  ticketId?: number;
  notes?: string;
  createdBy: number;
  createdAt: string;
}

// 采购订单接口
export interface PurchaseOrder {
  id: number;
  orderNumber: string;
  supplier: string;
  status: string;
  totalAmount: number;
  createdBy: number;
  createdAt: string;
  approvedAt?: string;
  completedAt?: string;
  items: PurchaseOrderItem[];
}

export interface PurchaseOrderItem {
  id: number;
  purchaseOrderId: number;
  materialId: number;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  notes?: string;
}

// 请求接口
export interface CreateMaterialRequest {
  code: string;
  name: string;
  description?: string;
  categoryId: number;
  unit: string;
  unitPrice: number;
  safetyStock?: number;
  maxStock?: number;
  location?: string;
  supplier?: string;
  notes?: string;
}

export interface UpdateMaterialRequest {
  name: string;
  description?: string;
  categoryId: number;
  unit: string;
  unitPrice: number;
  safetyStock: number;
  maxStock: number;
  location?: string;
  supplier?: string;
  notes?: string;
}

export interface StockInRequest {
  quantity: number;
  unitPrice?: number;
  referenceNumber?: string;
  notes?: string;
  createdBy: number;
}

export interface StockOutRequest {
  quantity: number;
  unitPrice?: number;
  referenceNumber?: string;
  ticketId?: number;
  notes?: string;
  createdBy: number;
}

// 统计接口
export interface MaterialStatistics {
  totalMaterials: number;
  lowStockMaterials: number;
  outOfStockMaterials: number;
  totalStockValue: number;
}

// API函数
export const materialApi = {
  // 物料分类
  getCategories: (): Promise<ApiResponse<{ categories: MaterialCategory[] }>> => {
    return materialClient.get('/api/material-categories');
  },

  createCategory: (data: { name: string; description?: string; parentCategoryId?: number }): Promise<ApiResponse<{ category: MaterialCategory }>> => {
    return materialClient.post('/api/material-categories', data);
  },

  // 物料管理
  getMaterials: (params?: { categoryId?: number; search?: string }): Promise<ApiResponse<{ materials: Material[] }>> => {
    return materialClient.get('/api/materials', { params });
  },

  getMaterial: (id: number): Promise<ApiResponse<{ material: Material }>> => {
    return materialClient.get(`/api/materials/${id}`);
  },

  createMaterial: (data: CreateMaterialRequest): Promise<ApiResponse<{ material: Material }>> => {
    return materialClient.post('/api/materials', data);
  },

  updateMaterial: (id: number, data: UpdateMaterialRequest): Promise<ApiResponse<{ material: Material }>> => {
    return materialClient.put(`/api/materials/${id}`, data);
  },

  // 库存管理
  stockIn: (materialId: number, data: StockInRequest): Promise<ApiResponse<{ material: Material; transaction: StockTransaction }>> => {
    return materialClient.post(`/api/materials/${materialId}/stock-in`, data);
  },

  stockOut: (materialId: number, data: StockOutRequest): Promise<ApiResponse<{ material: Material; transaction: StockTransaction }>> => {
    return materialClient.post(`/api/materials/${materialId}/stock-out`, data);
  },

  // 采购管理
  getPurchaseOrders: (): Promise<ApiResponse<{ orders: PurchaseOrder[] }>> => {
    return materialClient.get('/api/purchase-orders');
  },

  createPurchaseOrder: (data: { supplier: string; createdBy: number }): Promise<ApiResponse<{ order: PurchaseOrder }>> => {
    return materialClient.post('/api/purchase-orders', data);
  },

  // 统计
  getStatistics: (): Promise<ApiResponse<{ statistics: MaterialStatistics; recentTransactions: StockTransaction[] }>> => {
    return materialClient.get('/api/materials/statistics');
  }
};
