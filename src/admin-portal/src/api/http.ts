import axios from 'axios'
import { getServiceUrl, SERVICES } from './config'

// 是否正在刷新 Token
let isRefreshing = false
// 等待刷新的请求队列
let refreshSubscribers: Array<(token: string) => void> = []

// 添加到刷新队列
function subscribeTokenRefresh(callback: (token: string) => void) {
  refreshSubscribers.push(callback)
}

// 通知所有等待的请求
function onRefreshComplete(newToken: string) {
  refreshSubscribers.forEach(callback => callback(newToken))
  refreshSubscribers = []
}

// 保存 Token
function saveTokens(token: string, refreshToken: string) {
  localStorage.setItem('token', token)
  localStorage.setItem('refreshToken', refreshToken)
}

// 获取 RefreshToken
function getRefreshToken(): string | null {
  return localStorage.getItem('refreshToken')
}

// 调用刷新接口
async function refreshToken(): Promise<string | null> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) return null
  
  try {
    const response = await axios.post(
      getServiceUrl('auth') + '/api/auth/refresh',
      { refreshToken },
      { headers: { 'Content-Type': 'application/json' } }
    )
    if (response.data?.success && response.data?.data) {
      const { token, refreshToken: newRefreshToken } = response.data.data
      saveTokens(token, newRefreshToken)
      return token
    }
    return null
  } catch (error) {
    console.error('Refresh token failed:', error)
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    return null
  }
}

// 创建 axios 实例
const createHttpClient = (baseURL: string) => {
  const client = axios.create({
    baseURL,
    timeout: 15000,
    headers: {
      'Content-Type': 'application/json'
    }
  })

  // 请求拦截器
  client.interceptors.request.use(
    (config) => {
      const token = localStorage.getItem('token')
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      // 租户隔离：每次请求带上 X-Tenant header（兼容旧系统）
      const tenantCode = localStorage.getItem('tenantCode')
      if (tenantCode) {
        config.headers['X-Tenant'] = tenantCode
      }
      // 单租户多项目：每次请求带上 X-Project header
      const projectJson = localStorage.getItem('currentProject')
      if (projectJson) {
        try {
          const project = JSON.parse(projectJson)
          if (project.code) {
            config.headers['X-Project'] = project.code
          }
        } catch (e) {
          // ignore parse error
        }
      }
      return config
    },
    (error) => {
      return Promise.reject(error)
    }
  )

  // 响应拦截器
  client.interceptors.response.use(
    (response) => {
      return response.data
    },
    async (error: any) => {
      // 网络错误（无响应）不显示错误消息，让组件自行处理
      if (!error.response) {
        console.warn('网络请求失败，将使用模拟数据')
        return Promise.reject(error)
      }
      
      const originalRequest = error.config
      
      // 401 且未尝试过刷新
      if (error.response?.status === 401 && !originalRequest._retry) {
        if (isRefreshing) {
          // 正在刷新，加入队列等待
          return new Promise((resolve, reject) => {
            subscribeTokenRefresh((token: string) => {
              originalRequest.headers.Authorization = `Bearer ${token}`
              resolve(client(originalRequest))
            })
          })
        }
        
        originalRequest._retry = true
        isRefreshing = true
        
        try {
          const newToken = await refreshToken()
          if (newToken) {
            isRefreshing = false
            onRefreshComplete(newToken)
            originalRequest.headers.Authorization = `Bearer ${newToken}`
            return client(originalRequest)
          } else {
            throw new Error('Refresh failed')
          }
        } catch (refreshError) {
          isRefreshing = false
          ElMessage.error('登录已过期，请重新登录')
          localStorage.removeItem('token')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login'
          return Promise.reject(error)
        }
      } else {
        if (error.response?.status === 401) {
          ElMessage.error('登录已过期，请重新登录')
          localStorage.removeItem('token')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login'
        } else {
          const message = error.response?.data?.message || error.message || '请求失败'
          ElMessage.error(message)
        }
      }
      return Promise.reject(error)
    }
  )

  return client
}

// 导出各服务API客户端
export const authApi = createHttpClient(getServiceUrl('auth'))
export const ticketApi = createHttpClient(getServiceUrl('ticket'))
export const ticketTypeApi = createHttpClient(getServiceUrl('ticketType'))
// MaterialService via Gateway /api/materials
export const notificationApi = createHttpClient(getServiceUrl('notification'))
export const contractApi = createHttpClient(getServiceUrl('contract'))
export const financeApi = createHttpClient(getServiceUrl('finance'))
export const inspectionApi = createHttpClient(getServiceUrl('inspection'))
export const deviceApi = createHttpClient(getServiceUrl('device'))
export const materialApi = createHttpClient(getServiceUrl('material') + '/api/tenant/material')
export const accessControlApi = createHttpClient(getServiceUrl('accessControl'))
export const announcementApi = createHttpClient(getServiceUrl('announcement'))
export const cleaningApi = createHttpClient(getServiceUrl('cleaning'))
export const communityApi = createHttpClient(getServiceUrl('community'))
export const expressApi = createHttpClient(getServiceUrl('express'))
export const parkingApi = createHttpClient(getServiceUrl('parking'))
export const paymentApi = createHttpClient(getServiceUrl('payment'))
export const renovationApi = createHttpClient(getServiceUrl('renovation'))
// All services route through API Gateway (port 5000)
export const statisticsApi = createHttpClient(getServiceUrl("statistics"))
export const personApi = createHttpClient(getServiceUrl('person'))
export const masterApi = createHttpClient(getServiceUrl('masterData') + '/api')
// CenterService for project management
export const centerApi = createHttpClient(getServiceUrl('center'))
// Alias for backward compatibility
export const departmentApi = masterApi

export { createHttpClient }
export default createHttpClient