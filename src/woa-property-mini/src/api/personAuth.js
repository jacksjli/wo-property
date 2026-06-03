import ENV from '../config/env.js'
const BASE_URL = ENV.AUTH_API

export function personLogin(name, phone) {
  return new Promise((resolve, reject) => {
    wx.request({
      url: `${BASE_URL}/auth/login-by-person`,
      method: 'POST',
      data: {
        name,
        phone,
        tenantCode: 'wo_property'
      },
      header: {
        'Content-Type': 'application/json',
        'Tenant-Code': 'wo_property'
      },
      success: (res) => {
        if (res.data.success) {
          const data = res.data.data
          wx.setStorageSync('token', data.token)
          wx.setStorageSync('employeeType', data.employeeType)
          wx.setStorageSync('personId', data.personId || 0)
          wx.setStorageSync('projects', data.projects || [])
          // 设置当前项目（默认第一个）
          const projects = data.projects || []
          wx.setStorageSync('currentProject', projects.length > 0 ? projects[0] : null)
          wx.setStorageSync('userInfo', {
            name: data.name,
            phone: data.phone,
            role: data.role,
            employeeType: data.employeeType,
            areaIds: data.areaIds,
            buildingIds: data.buildingIds,
            specialtyIds: data.specialtyIds,
            projects: data.projects || []
          })
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
 * 获取当前用户信息（从本地缓存）
 */
export function getUserInfo() {
  const userInfo = wx.getStorageSync('userInfo') || {}
  const employeeType = wx.getStorageSync('employeeType') || 'Guest'
  return {
    ...userInfo,
    employeeType
  }
}

/**
 * 获取当前用户员工类型
 */
export function getEmployeeType() {
  return wx.getStorageSync('employeeType') || 'Guest'
}

/**
 * 退出登录
 */
export function logout() {
  wx.removeStorageSync('token')
  wx.removeStorageSync('userInfo')
  wx.removeStorageSync('employeeType')
  wx.removeStorageSync('personId')
  wx.removeStorageSync('projects')
  wx.removeStorageSync('currentProject')
}