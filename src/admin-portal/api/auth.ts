import axios, { AxiosInstance } from 'axios';
import { API_CONFIG } from './config';

// 创建统一认证服务HTTP客户端
const authHttp: AxiosInstance = axios.create({
  baseURL: API_CONFIG.AUTH_SERVICE.BASE_URL,
  timeout: API_CONFIG.REQUEST_CONFIG.TIMEOUT,
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
      localStorage.removeItem('auth_token');
      localStorage.removeItem('user_info');
      // 重定向到登录页面
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// 登录
export const login = async (username: string, password: string) => {
  const response = await authHttp.post(API_CONFIG.AUTH_SERVICE.ENDPOINTS.LOGIN, {
    username,
    password
  });
  
  if (response.data.success && response.data.data.token) {
    // 保存令牌和用户信息
    localStorage.setItem('auth_token', response.data.data.token);
    localStorage.setItem('user_info', JSON.stringify(response.data.data.user));
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
  const response = await authHttp.post(API_CONFIG.AUTH_SERVICE.ENDPOINTS.REGISTER, userData);
  return response.data;
};

// 获取当前用户信息
export const getCurrentUser = async () => {
  const response = await authHttp.get(API_CONFIG.AUTH_SERVICE.ENDPOINTS.ME);
  return response.data;
};

// 验证令牌
export const validateToken = async (token: string) => {
  const response = await authHttp.post(API_CONFIG.AUTH_SERVICE.ENDPOINTS.VALIDATE, {
    token
  });
  return response.data;
};

// 登出
export const logout = () => {
  localStorage.removeItem('auth_token');
  localStorage.removeItem('user_info');
};

// 获取存储的用户信息
export const getStoredUserInfo = () => {
  const userInfo = localStorage.getItem('user_info');
  return userInfo ? JSON.parse(userInfo) : null;
};

// 获取存储的令牌
export const getStoredToken = () => {
  return localStorage.getItem('auth_token');
};

// 检查是否已登录
export const isAuthenticated = () => {
  return !!localStorage.getItem('auth_token');
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
