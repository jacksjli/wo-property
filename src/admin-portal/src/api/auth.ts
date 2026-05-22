import axios from 'axios'

// CenterService (5016) - 单租户多项目认证
const CENTER_BASE_URL = 'http://localhost:5000'

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
  const response = await authHttp.post('/api/auth/login', { username, password })
  console.log('[Auth] Login response:', JSON.stringify(response))
  // 保存 token 和 user - response 已经是解包后的数据（中间件已处理）
  if (response.success && response.token) {
    localStorage.setItem('token', response.token)
    localStorage.setItem('refreshToken', response.refreshToken || '')
    localStorage.setItem('user', JSON.stringify(response.user || response))
    console.log('[Auth] Token saved:', response.token?.substring(0, 20) + '...')
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