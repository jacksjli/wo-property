// API 基础配置
const API_BASE_URL = 'http://localhost'

// 服务端口配置
export const SERVICES = {
  auth: 5006,
  material: 5004,
  notification: 5005,
  contract: 5008,
  finance: 5009,
  inspection: 5010,
  complaint: 5011,
  key: 5012,
  visitor: 5013,
  statistics: 5014,
  mobile: 5015,
  ticket: 5002,
  device: 5007,
  accessControl: 5001,
  announcement: 5016,
  person: 5018,
  cleaning: 5021,
  community: 5022,
  delivery: 5023,
  express: 5024,
  parking: 5025,
  payment: 5026,
  projectConfig: 5027,
  renovation: 5028,
}

// 获取服务URL
export const getServiceUrl = (service: keyof typeof SERVICES): string => {
  return `${API_BASE_URL}:${SERVICES[service]}`
}

export default SERVICES
