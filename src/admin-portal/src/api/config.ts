// API 基础配置
// 手机/外部访问时使用代理路径（通过 Vite server.proxy 转发到后端）
// Vite 开发服务器配置在 vite.config.ts 中将 /api 路径代理到后端服务
const API_BASE_URL = ''  // 空字符串 = 使用同源（经过 Vite 代理）

// 如果需要直接访问后端（不推荐），取消下面这行注释并填入服务器IP
// const API_BASE_URL = 'http://192.168.1.3'

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
  notification: 5105,   // NotificationService (Phase 1)
  material: 5504,        // MaterialService (Phase 1)
  finance: 5509,         // FinanceService (Phase 1)
  device: 5530,           // DeviceService (Phase 2)
  payment: 5109,          // PaymentService (Phase 2)
  mobile: 5526,          // MobileService (Phase 2)
  statistics: 5250,     // StatisticsService (Phase 1)
  projectTracking: 5520,  // ProjectTrackingService (Phase 1)
}

// 获取服务URL
export const getServiceUrl = (service: keyof typeof SERVICES): string => {
  if (API_BASE_URL) {
    return `${API_BASE_URL}:${SERVICES[service]}`
  }
  return `http://192.168.1.3:${SERVICES[service]}`
}

export default SERVICES