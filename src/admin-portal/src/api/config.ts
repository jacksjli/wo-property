// API 基础配置
const API_BASE_URL = 'http://localhost'

// 服务端口配置
export const SERVICES = {
  auth: 5006,
  material: 5004,
  notification: 5005,
  contract: 5001,
  finance: 5009,
  inspection: 5010,
  ticket: 5002,
  device: 5003,
  accessControl: 5006,
  announcement: 5011,
  person: 5018,
  cleaning: 5016,
  statistics: 5014,
  community: 5022,
  express: 5017,
  parking: 5025,
  payment: 5007,
  renovation: 5021,
}

// 获取服务URL
export const getServiceUrl = (service: keyof typeof SERVICES): string => {
  return `${API_BASE_URL}:${SERVICES[service]}`
}

export default SERVICES
