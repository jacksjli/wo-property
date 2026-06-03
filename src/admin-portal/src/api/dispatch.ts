import http from '@/api/http'

// ============ 类型定义 ============
export type DispatchStatus = 'Pending' | 'WReceived' | 'Completed' | 'Confirmed' | 'Transferred' | 'Cancelled'

export interface DispatchRecord {
  id: number
  ticketId: number
  ticketCode: string
  title?: string
  content?: string
  dispatchTime: string
  fromPersonId: number
  fromPersonName: string
  toPersonId: number
  toPersonName: string
  status: DispatchStatus
  source: string
  completedAt?: string
  confirmedAt?: string
  tenantCode: string
  projectId: number
  areaId?: number
  areaName?: string
  buildingId?: number
  buildingName?: string
  roomId?: number
  roomName?: string
}

export interface TransferRequest {
  id: number
  dispatchRecordId: number
  ticketId: number
  ticketCode: string
  fromPersonId: number
  fromPersonName: string
  toPersonId: number
  toPersonName: string
  reason: string
  status: 'Pending' | 'Approved' | 'Rejected'
  transferDispatchId?: number
  createdAt: string
  approvedAt?: string
  tenantCode: string
  projectId: number
}

export interface SatisfactionRating {
  id: number
  ticketId: number
  ticketCode: string
  dispatchRecordId: number
  raterId: number
  raterName: string
  rateeId: number
  rateeName: string
  qualityScore: number
  attitudeScore: number
  timelinessScore: number
  overallScore: number
  comment?: string
  ratedAt: string
  isAutoRated: boolean
}

export interface TimeoutAlert {
  id: number
  ticketId: number
  ticketCode: string
  dispatchRecordId: number
  alertType: string
  expectedTime: string
  actualTime: string
  timeoutMinutes: number
  level: number
  notifyTargetId: number
  notifyTargetName: string
  status: 'Pending' | 'Processed'
  processedAt?: string
  createdAt: string
  tenantCode: string
  projectId: number
}

export interface EscalationRecord {
  id: number
  level: number
  fromPersonId: number
  fromPersonName: string
  toPersonId: number
  toPersonName: string
  toRole: string
  escalatedAt: string
  status: string
}

// ============ API 方法 ============

const dispatchApi = {
  // 待处理工单
  getPending: (page = 1, pageSize = 10) =>
    http.get<{ success: boolean; data: DispatchRecord[]; total: number; page: number; pageSize: number }>(
      `/api/tenant/dispatch/pending?page=${page}&pageSize=${pageSize}`
    ),

  // 接收工单
  receive: (id: number) =>
    http.post<{ success: boolean; message: string }>(
      `/api/tenant/dispatch/${id}/receive`
    ),

  // 完工提交
  complete: (id: number, data: { completionRemark?: string }) =>
    http.post<{ success: boolean; message: string }>(
      `/api/tenant/dispatch/${id}/complete`,
      data
    ),

  // 确认并评价
  confirm: (id: number, data: {
    raterId: number
    raterName: string
    qualityScore: number
    attitudeScore: number
    timelinessScore: number
    overallScore: number
    comment?: string
  }) =>
    http.post<{ success: boolean; data: { ratingId: number }; message: string }>(
      `/api/tenant/dispatch/${id}/confirm`,
      data
    ),

  // 派单历史
  getHistory: (ticketId: number) =>
    http.get<{ success: boolean; data: DispatchRecord[] }>(
      `/api/tenant/dispatch/history/${ticketId}`
    ),

  // 获取工单升级状态
  getEscalations: (ticketId: number) =>
    http.get<{
      success: boolean;
      data: {
        currentLevel: number;
        currentStatus: string;
        nextEscalationRole: string | null;
        escalations: EscalationRecord[];
        dispatchRecords: DispatchRecord[];
      }
    }>(
      `/api/tenant/dispatch/escalations/${ticketId}`
    ),

  // 转单申请
  transfer: (data: {
    dispatchRecordId: number
    toPersonId: number
    toPersonName: string
    reason: string
  }) =>
    http.post<{ success: boolean; data: { transferRequestId: number }; message: string }>(
      `/api/tenant/dispatch/transfer`,
      data
    ),

  // 待审批转单列表
  getPendingTransfers: (page = 1, pageSize = 10) =>
    http.get<{ success: boolean; data: TransferRequest[]; total: number; page: number; pageSize: number }>(
      `/api/tenant/dispatch/transfer/pending?page=${page}&pageSize=${pageSize}`
    ),

  // 审批转单
  approveTransfer: (id: number, action: 'approve' | 'reject', reason?: string) =>
    http.put<{ success: boolean; message: string }>(
      `/api/tenant/dispatch/transfer/${id}`,
      { Action: action, Reason: reason }
    ),

  // 转单历史
  getTransferHistory: (page = 1, pageSize = 10) =>
    http.get<{ success: boolean; data: TransferRequest[]; total: number; page: number; pageSize: number }>(
      `/api/tenant/dispatch/transfer/history?page=${page}&pageSize=${pageSize}`
    ),

  // 工单评价
  getRating: (ticketId: number) =>
    http.get<{ success: boolean; data: SatisfactionRating | null }>(
      `/api/tenant/dispatch/rating/${ticketId}`
    ),

  // 评价统计
  getRatingStats: (personId: number) =>
    http.get<{ success: boolean; data: {
      personId: number
      totalCount: number
      avgQuality: number
      avgAttitude: number
      avgTimeliness: number
      avgOverall: number
    } }>(
      `/api/tenant/dispatch/rating/stats?personId=${personId}`
    ),

  // 超时告警列表
  getAlerts: (page = 1, pageSize = 10, status?: string) =>
    http.get<{ success: boolean; data: TimeoutAlert[]; total: number; page: number; pageSize: number }>(
      `/api/tenant/dispatch/alerts?page=${page}&pageSize=${pageSize}${status ? `&status=${status}` : ''}`
    ),

  // 处理告警
  processAlert: (id: number) =>
    http.put<{ success: boolean; message: string }>(
      `/api/tenant/dispatch/alerts/${id}/process`
    ),
}

export default dispatchApi