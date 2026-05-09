import { defineStore } from 'pinia';
import { login as authLogin, logout as authLogout, getStoredUserInfo, getStoredToken, isAuthenticated as checkAuth, getCurrentUser } from '../api/auth';

interface User {
  id: number;
  username: string;
  fullName: string;
  email: string;
  phone?: string;
  role: string;
  status: string;
}

interface AuthState {
  token: string | null;
  user: User | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: getStoredToken(),
    user: getStoredUserInfo(),
    isAuthenticated: checkAuth(),
    loading: false,
    error: null
  }),
  
  getters: {
    isAdmin: (state) => state.user?.role === 'Administrator',
    isTechnician: (state) => state.user?.role === 'Technician',
    isUser: (state) => state.user?.role === 'User',
    currentUser: (state) => state.user
  },
  
  actions: {
    async login(username: string, password: string) {
      this.loading = true;
      this.error = null;
      try {
        const result = await authLogin(username, password);
        this.token = result.data.token;
        this.user = result.data.user;
        this.isAuthenticated = true;
        return result;
      } catch (error: any) {
        this.error = error.message || '登录失败';
        this.logout();
        throw error;
      } finally {
        this.loading = false;
      }
    },
    
    async initUser() {
      try {
        const result = await getCurrentUser();
        if (result.success && result.data) {
          this.user = result.data;
        }
      } catch (error) {
        console.error('初始化用户信息失败:', error);
      }
    },
    
    logout() {
      authLogout();
      this.token = null;
      this.user = null;
      this.isAuthenticated = false;
      this.error = null;
    },
    
    checkAuth() {
      return checkAuth();
    },
    
    hasPermission(requiredRole: string): boolean {
      if (!this.user) return false;
      
      if (requiredRole === 'Admin') {
        return this.user.role === 'Administrator';
      }
      
      return this.user.role === requiredRole || this.user.role === 'Administrator';
    }
  }
});
