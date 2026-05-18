import axios from 'axios';
import { getServiceUrl } from './config';

// Auth 服务基础 URL - Phase 0 使用 5106（独立多租户）
const AUTH_BASE_URL = 'http://localhost:5106';

// 创建统一认证服务HTTP客户端
const authHttp = axios.create({
  baseURL: AUTH_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// 请求拦截器 - 添加令牌
authHttp.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 响应拦截器 - 处理错误
authHttp.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
    // 清除本地存储的令牌
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    // 重定向到登录页面
    window.location.href = '/login';
  }
  return Promise.reject(error);
  }
);

// 登录
const LOGIN_ENDPOINT = '/api/auth/login'
const REGISTER_ENDPOINT = '/api/auth/register'
const ME_ENDPOINT = '/api/auth/me'

export const login = async (username: string, password: string, tenantCode?: string) => {
  const response = await authHttp.post(LOGIN_ENDPOINT, {
    username,
    password,
    ...(tenantCode ? { tenantCode } : {})
  });

  if (response.data.success && response.data.data.token) {
    // 保存令牌和用户信息
    localStorage.setItem('token', response.data.data.token);
    localStorage.setItem('refreshToken', response.data.data.refreshToken || '');
    localStorage.setItem('user', JSON.stringify(response.data.data.user));
    return response.data;
  }

  throw new Error(response.data.message || '登录失败');
};

// 注册
export const register = async (userData: {
  username: string;
  email: string;
  fullName: string;
  password: string;
  phone?: string;
  role?: string;
}) => {
  const response = await authHttp.post(REGISTER_ENDPOINT, userData);
  return response.data;
};

// 获取当前用户信息
export const getCurrentUser = async () => {
  const response = await authHttp.get(ME_ENDPOINT);
  return response.data;
};

// 验证令牌
export const validateToken = async (token: string) => {
  const response = await authHttp.post('/api/auth/validate', {
    token
  });
  return response.data;
};

// 登出
export const logout = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
};

// 获取存储的用户信息
export const getStoredUserInfo = () => {
  const userInfo = localStorage.getItem('user');
  return userInfo ? JSON.parse(userInfo) : null;
};

// 获取存储的令牌
export const getStoredToken = () => {
  return localStorage.getItem('token');
};

// 检查是否已登录
export const isAuthenticated = () => {
  return !!localStorage.getItem('token');
};

export default {
  login,
  register,
  getCurrentUser,
  validateToken,
  logout,
  getStoredUserInfo,
  getStoredToken,
  isAuthenticated
};

// Named export alias for convenience
export const auth = {
  login,
  register,
  getCurrentUser,
  validateToken,
  logout,
  getStoredUserInfo,
  getStoredToken,
  isAuthenticated
};
