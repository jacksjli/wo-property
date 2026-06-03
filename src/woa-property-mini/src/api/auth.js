import ENV from '../config/env.js'

const BASE_URL = ENV.AUTH_API

/**
 * 微信登录
 * @param {string} code - wx.login() 返回的 code
 */
export function wechatLogin(code) {
  return new Promise((resolve, reject) => {
    wx.request({
      // 注意：BASE_URL 已经包含 /api，所以这里直接 /auth/wechat-login
      url: `${BASE_URL}/auth/wechat-login`,
      method: 'POST',
      data: { code },
      header: {
        'Content-Type': 'application/json',
        'Tenant-Code': 'wo_property'
      },
      success: (res) => {
        if (res.data.success) {
          wx.setStorageSync('token', res.data.data.token)
          wx.setStorageSync('userInfo', res.data.data.userInfo)
          wx.setStorageSync('personId', res.data.data.userInfo.personId || 0)
          resolve(res.data)
        } else {
          reject(new Error(res.data.message || '登录失败'))
        }
      },
      fail: (err) => {
        reject(err)
      }
    })
  })
}

/**
 * 获取当前用户信息
 */
export function getUserInfo() {
  return new Promise((resolve, reject) => {
    wx.request({
      url: `${BASE_URL}/auth/userinfo`,
      method: 'GET',
      header: {
        'Authorization': `Bearer ${wx.getStorageSync('token')}`,
        'Tenant-Code': 'wo_property'
      },
      success: (res) => {
        if (res.data.success) {
          resolve(res.data.data)
        } else {
          reject(new Error(res.data.message || '获取用户信息失败'))
        }
      },
      fail: (err) => {
        reject(err)
      }
    })
  })
}

/**
 * 获取微信手机号
 * @param {string} code - 手机号授权码
 */
export function getWeChatPhone(code) {
  return new Promise((resolve, reject) => {
    wx.request({
      url: `${BASE_URL}/auth/wechat-phone`,
      method: 'POST',
      data: { code },
      header: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${wx.getStorageSync('token')}`,
        'Tenant-Code': 'wo_property'
      },
      success: (res) => {
        if (res.data.success) {
          resolve(res.data.data)
        } else {
          reject(new Error(res.data.message || '获取手机号失败'))
        }
      },
      fail: (err) => {
        reject(err)
      }
    })
  })
}

/**
 * 退出登录
 */
export function logout() {
  wx.removeStorageSync('token')
  wx.removeStorageSync('userInfo')
}
