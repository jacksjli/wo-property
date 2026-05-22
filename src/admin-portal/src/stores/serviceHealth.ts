import { ref, computed } from 'vue'
import { SERVICES, getServiceUrl } from '@/api/config'

export interface ServiceHealth {
  key: string
  name: string
  port: number
  healthy: boolean
  message: string
  lastChecked: Date | null
}

// Module to service mapping
const MODULE_SERVICE_MAP: Record<string, string> = {
  '工单管理': 'ticket',
  '工单类型': 'ticket',
  '设备管理': 'device',
  '物料管理': 'material',
  '财务管理': 'finance',
  '合同管理': 'contract',
  '巡检管理': 'inspection',
  '通知管理': 'notification',
  '数据统计': 'statistics',
  '基础数据': 'masterData',
  '人员管理': 'person',
  '住户管理': 'person',
  '车位管理': 'parking',
  '清洁管理': 'cleaning',
  '快递管理': 'express',
  '访客管理': 'visitor',
  '装修管理': 'renovation',
  '社区管理': 'community',
  '钥匙管理': 'key',
  '门禁管理': 'accessControl',
  '公告管理': 'announcement',
  '支付管理': 'payment',
  '配送管理': 'delivery',
}

// Services that must always be healthy for the app to function
const CORE_SERVICES = ['auth', 'gateway']

// All available services
const ALL_SERVICES = Object.keys(SERVICES).filter(k => !['masterData'].includes(k)) // filter internal-only

// Direct export for use in other modules
const serviceHealthMap = ref<Map<string, ServiceHealth>>(new Map())
const isChecking = ref(false)
const lastGlobalCheck = ref<Date | null>(null)

function getServiceList(): ServiceHealth[] {
  return Object.entries(SERVICES).map(([key, port]) => ({
    key,
    name: getServiceDisplayName(key),
    port,
    healthy: false,
    message: '',
    lastChecked: null
  }))
}

function getServiceDisplayName(key: string): string {
  const names: Record<string, string> = {
    auth: '认证服务',
    masterData: '基础数据服务',
    person: '人员服务',
    ticket: '工单服务',
    announcement: '公告服务',
    cleaning: '清洁服务',
    express: '快递服务',
    key: '钥匙服务',
    parking: '车位服务',
    community: '社区服务',
    renovation: '装修服务',
    inspection: '巡检服务',
    visitor: '访客服务',
    contract: '合同服务',
    notification: '通知服务',
    material: '物料服务',
    finance: '财务服务',
    device: '设备服务',
    payment: '支付服务',
    mobile: '移动服务',
    statistics: '统计服务',
    accessControl: '门禁服务',
    gateway: 'API网关'
  }
  return names[key] || key
}

async function checkServiceHealth(serviceKey: string): Promise<ServiceHealth> {
  const port = SERVICES[serviceKey as keyof typeof SERVICES]
  const url = `http://localhost:${port}/health`

  const result: ServiceHealth = {
    key: serviceKey,
    name: getServiceDisplayName(serviceKey),
    port,
    healthy: false,
    message: '',
    lastChecked: new Date()
  }

  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
      signal: AbortSignal.timeout(5000)
    })

    if (response.ok) {
      const data = await response.json()
      result.healthy = data.status === 'healthy'
      result.message = result.healthy ? '正常' : '服务异常'
    } else {
      result.healthy = false
      result.message = `HTTP ${response.status}`
    }
  } catch (error: any) {
    result.healthy = false
    result.message = error.name === 'TimeoutError' ? '连接超时' : '无法连接'
  }

  return result
}

async function checkAllServices(): Promise<void> {
  if (isChecking.value) return

  isChecking.value = true
  lastGlobalCheck.value = new Date()

  const services = Object.keys(SERVICES).filter(k => !['masterData'].includes(k) && k !== 'gateway')

  const results = await Promise.all(
    services.map(key => checkServiceHealth(key))
  )

  results.forEach(result => {
    serviceHealthMap.value.set(result.key, result)
  })

  // Check gateway specifically
  try {
    const gwResponse = await fetch('http://localhost:5000/health', {
      signal: AbortSignal.timeout(5000)
    })
    serviceHealthMap.value.set('gateway', {
      key: 'gateway',
      name: 'API网关',
      port: 5000,
      healthy: gwResponse.ok,
      message: gwResponse.ok ? '正常' : '服务异常',
      lastChecked: new Date()
    })
  } catch {
    serviceHealthMap.value.set('gateway', {
      key: 'gateway',
      name: 'API网关',
      port: 5000,
      healthy: false,
      message: '无法连接',
      lastChecked: new Date()
    })
  }

  isChecking.value = false
}

async function checkProjectServices(moduleNames: string[]): Promise<{
  healthy: boolean
  unhealthyServices: ServiceHealth[]
}> {
  // Get unique service keys needed for the modules
  const neededServices = new Set<string>()

  moduleNames.forEach(module => {
    const serviceKey = MODULE_SERVICE_MAP[module]
    if (serviceKey) {
      neededServices.add(serviceKey)
    }
  })

  // Always check core services
  CORE_SERVICES.forEach(s => neededServices.add(s))

  // Check each needed service
  const results = await Promise.all(
    Array.from(neededServices).map(key => checkServiceHealth(key))
  )

  const unhealthy = results.filter(r => !r.healthy)

  return {
    healthy: unhealthy.length === 0,
    unhealthyServices: unhealthy
  }
}

function getUnhealthyServices(): ServiceHealth[] {
  return Array.from(serviceHealthMap.value.values()).filter(s => !s.healthy)
}

function isServiceHealthy(key: string): boolean {
  return serviceHealthMap.value.get(key)?.healthy ?? false
}

// Computed
const allServicesHealthy = computed(() => {
  const services = Array.from(serviceHealthMap.value.values())
  if (services.length === 0) return true
  return services.every(s => s.healthy)
})

const coreServicesHealthy = computed(() => {
  return CORE_SERVICES.every(key => isServiceHealthy(key))
})

export function useServiceHealth() {
  return {
    serviceHealthMap,
    isChecking,
    lastGlobalCheck,
    allServicesHealthy,
    coreServicesHealthy,
    checkServiceHealth,
    checkAllServices,
    checkProjectServices,
    getUnhealthyServices,
    isServiceHealthy,
    getServiceList
  }
}

export default useServiceHealth

// Direct exports for use in other modules
export { serviceHealthMap, isChecking, lastGlobalCheck, allServicesHealthy, coreServicesHealthy }