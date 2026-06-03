import axios from 'axios'

// CenterService (5106) - 单租户多项目认证
import { getServiceUrl } from './config'
const CENTER_BASE_URL = getServiceUrl('auth')

// 创建认证服务HTTP客户端
const authHttp = axios.create({
  baseURL: CENTER_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器
authHttp.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// 响应拦截器
authHttp.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// 登录 - 调用 CenterService
export const login = async (username: string, password: string) => {
  const rawResponse = await authHttp.post('/api/auth/login', { username, password, tenantCode: 'wo_property' })
  // rawResponse = axios response, .data = { success, message, data: { token, projects, user } }
  const response = rawResponse
  console.log('[Auth] Login response:', JSON.stringify(response))
  
  // 检查外层 success
  if (response.success) {
    // token/project/user 在内层 data 中，需要展开到外层供 LoginView 使用
    const data = response.data || {}
    if (data.token) {
      localStorage.setItem('token', data.token)
      localStorage.setItem('refreshToken', data.refreshToken || '')
      localStorage.setItem('user', JSON.stringify(data.user || response))
      console.log('[Auth] Token saved:', data.token?.substring(0, 20) + '...')
      // 展开 data 到外层，保持与 LoginView 的兼容性
      response.token = data.token
      response.projects = data.projects || []
      response.user = data.user
      response.tenantId = data.tenantId
      response.tenantCode = data.tenantCode
    }
  } else {
    console.log('[Auth] Login failed - response:', JSON.stringify(response))
  }
  return response
}

// 获取当前用户
export const getCurrentUser = async () => {
  const response = await authHttp.get('/api/auth/me')
  return response
}

// 获取存储的用户信息
export const getStoredUserInfo = () => {
  const user = localStorage.getItem('user')
  return user ? JSON.parse(user) : null
}

// 登出
export const logout = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('user')
  localStorage.removeItem('currentProject')
  localStorage.removeItem('currentProjectName')
}

// 获取存储的令牌
export const getStoredToken = () => localStorage.getItem('token')

// 检查是否已登录
export const isAuthenticated = () => !!localStorage.getItem('token')

export const auth = {
  login,
  logout,
  getCurrentUser,
  getStoredUserInfo,
  getStoredToken,
  isAuthenticated
}

export default auth