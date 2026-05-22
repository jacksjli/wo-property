// API 基础配置
const API_BASE_URL = 'http://localhost'

// 服务端口配置（Phase 1 + Phase 2 改造后的端口）
export const SERVICES = {
  auth: 5106,           // AuthService (Phase 0)
  center: 5016,          // CenterService (项目中心)
  masterData: 5019,     // MasterDataService (Phase 0)
  person: 5018,          // PersonService (Phase 0)
  ticket: 5102,          // TicketService (Phase 0)
  ticketType: 5102,     // TicketService (工单类型API)
  announcement: 5511,
  delivery: 5017,     // AnnouncementService (Phase 1)
  cleaning: 5516,        // CleaningService (Phase 1)
  express: 5517,         // ExpressService (Phase 1)
  key: 5512,             // KeyService (Phase 1)
  parking: 5525,         // ParkingService (Phase 1)
  community: 5522,       // CommunityService (Phase 1)
  renovation: 5521,      // RenovationService (Phase 1)
  inspection: 5510,      // InspectionService (Phase 1)
  visitor: 5513,         // VisitorService (Phase 1)
  contract: 5501,        // ContractService (Phase 1)
  notification: 5129,   // NotificationService (Phase 1)
  material: 5504,        // MaterialService (Phase 1)
  finance: 5509,         // FinanceService (Phase 1)
  device: 5530,           // DeviceService (Phase 2)
  payment: 5507,          // PaymentService (Phase 2)
  mobile: 5526,          // MobileService (Phase 2)
  statistics: 5241,     // StatisticsService (Phase 1)
  projectTracking: 5520,  // ProjectTrackingService (Phase 1)
}

// 获取服务URL
export const getServiceUrl = (service: keyof typeof SERVICES): string => {
  return `${API_BASE_URL}:${SERVICES[service]}`
}

export default SERVICES