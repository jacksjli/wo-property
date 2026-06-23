// API 基础配置
// 部署环境：http://139.196.195.93（走 nginx 统一入口）
const API_BASE_URL = 'http://139.196.195.93'

// nginx 路径路由（/api/xxx 形式）— 前端通过这些路径访问后端服务
// 格式: 'serviceName': '/api/xxx'
const NGINX_ROUTES: Record<string, string> = {
  auth:           '',  // 空字符串 → baseURL 直接用 API_BASE_URL
  ticket:         '/api/tickets',
  ticketType:     '/api/ticket-types',
  person:         '/api/persons',
  device:         '/api/devices',
  inspection:     '/api/inspections',
  complaint:      '/api/complaints',
  visitor:        '/api/visitors',
  payment:        '/api/payments',
  notification:   '/api/notifications',
  material:       '/api/materials',
  contract:       '/api/contracts',
  finance:        '/api/finance',
  dispatch:       '/api/dispatch',
  parking:        '/api/parkings',
  announcement:   '/api/announcements',
  cleaning:       '/api/cleanings',
  delivery:       '/api/deliveries',
  express:        '/api/express',
  renovation:     '/api/renovations',
  community:      '/api/community',
  mobile:         '/api/mobile',
  key:            '/api/keys',
  // MasterDataService 通用路由（field-definitions, field-equivalences, areas, buildings 等）
  masterData:     '/api/master',
}

// 直连端口服务（未在 nginx 配置，需要直接 IP:port 访问）
// 这些服务要么端口已对公网开放，要么内网调用
const DIRECT_PORTS: Record<string, number> = {
  center:         5016,
  statistics:     5250,
  projectTracking: 5520,
}

// 兼容性别名（支持旧的 port-based 用法）
export const SERVICES = { ...NGINX_ROUTES, ...DIRECT_PORTS } as Record<string, string | number>

// 获取服务URL
export const getServiceUrl = (service: keyof typeof SERVICES): string => {
  const route = NGINX_ROUTES[service]
  if (route) {
    // nginx 路由 → /api/xxx（无端口）
    return API_BASE_URL + route
  }
  // 直连 → IP:port
  const port = DIRECT_PORTS[service]
  if (port) {
    return `${API_BASE_URL}:${port}`
  }
  // fallback: 假设是端口号
  return `${API_BASE_URL}:${SERVICES[service]}`
}

export default SERVICES
