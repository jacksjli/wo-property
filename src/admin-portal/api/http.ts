import axios from 'axios'
import { ElMessage } from 'element-plus'
import { getServiceUrl, SERVICES } from './config'

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
    (error: any) => {
      if (error.response?.status === 401) {
        ElMessage.error('登录已过期，请重新登录')
        localStorage.removeItem('token')
        window.location.href = '/login'
      } else {
        const message = error.response?.data?.message || error.message || '请求失败'
        ElMessage.error(message)
      }
      return Promise.reject(error)
    }
  )

  return client
}

// 导出各服务API客户端
export const authApi = createHttpClient(getServiceUrl('auth'))
export const ticketApi = createHttpClient(getServiceUrl('ticket'))
export const materialApi = createHttpClient(getServiceUrl('material'))
export const notificationApi = createHttpClient(getServiceUrl('notification'))
export const contractApi = createHttpClient(getServiceUrl('contract'))
export const financeApi = createHttpClient(getServiceUrl('finance'))
export const inspectionApi = createHttpClient(getServiceUrl('inspection'))
export const complaintApi = createHttpClient(getServiceUrl('complaint'))
export const keyApi = createHttpClient(getServiceUrl('key'))
export const visitorApi = createHttpClient(getServiceUrl('visitor'))
export const statisticsApi = createHttpClient(getServiceUrl('statistics'))
export const mobileApi = createHttpClient(getServiceUrl('mobile'))

export { createHttpClient }
export default createHttpClient
