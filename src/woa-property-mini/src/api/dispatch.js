import env from '@/config/env'

function request(url, method, data) {
  return new Promise((resolve, reject) => {
    uni.request({
      url: env.DISPATCH_API + url,
      method,
      data,
      header: {
        'Content-Type': 'application/json',
        'Tenant-Code': uni.getStorageSync('tenantCode') || 'wo_property',
        'X-Project': uni.getStorageSync('currentProject')?.projectCode || '',
        'Authorization': 'Bearer ' + (uni.getStorageSync('token') || '')
      },
      success: (res) => {
        if (res.statusCode === 200) {
          resolve(res.data)
        } else if (res.statusCode === 401) {
          uni.removeStorageSync('token')
          uni.switchTab({ url: '/pages/login/index' })
          reject(new Error('未登录'))
        } else {
          reject(new Error(res.data?.message || '请求失败'))
        }
      },
      fail: (err) => reject(err)
    })
  })
}

export default {
  // 获取待处理工单（工程师端）
  getPending(page = 1, pageSize = 20, personId) {
    let url = `/pending?page=${page}&pageSize=${pageSize}`
    if (personId) url += `&personId=${personId}`
    return request(url, 'GET')
  },
  
  // 接收工单
  receive(id) {
    return request(`/${id}/receive`, 'POST')
  },
  
  // 完工提交（同时评价）
  confirm(id, data) {
    return request(`/${id}/confirm`, 'POST', data)
  },
  
  // 完工提交（不评价）
  complete(id, remark) {
    return request(`/${id}/complete`, 'POST', { completionRemark: remark })
  },
  
  // 转单申请
  transfer(data) {
    return request('/transfer', 'POST', data)
  },
  
  // 待审批转单列表
  getPendingTransfers(page = 1, pageSize = 20) {
    return request(`/transfer/pending?page=${page}&pageSize=${pageSize}`, 'GET')
  },
  
  // 审批转单
  approveTransfer(id, action, reason) {
    return request(`/transfer/${id}`, 'PUT', { Action: action, Reason: reason })
  },
  
  // 获取评价统计
  // 获取工单评价详情
  getRating(ticketId) {
    return request(`/rating/${ticketId}`, 'GET')
  },

  getRatingStats(personId) {
    return request(`/rating/stats?personId=${personId}`, 'GET')
  },

  // 转单申请
  transferDispatch(data) {
    return request('/transfer', 'POST', data)
  },

  // 接受转单
  acceptDispatch(id) {
    return request(`/${id}/accept`, 'PUT')
  },

  // 拒绝转单
  rejectDispatch(id, reason) {
    return request(`/${id}/reject`, 'PUT', { reason })
  },

  // 获取我的待接受转单
  getMyPendingTransfers(personId) {
    return request(`/transfer/pending-for-me?personId=${personId}`, 'GET')
  },

  // 获取工单当前活跃的派单记录（用于转单）
  getActiveDispatch(ticketCode) {
    return request(`/active/${ticketCode}`, 'GET')
  },
  
  // 获取管理员工单列表
  getAdminTickets(page = 1, pageSize = 20, status) {
    let url = `/tickets?page=${page}&pageSize=${pageSize}`
    if (status) url += `&status=${status}`
    return request(url, 'GET')
  },
  
  // 获取超时工单
  getTimeoutTickets() {
    return request('/timeout-tickets', 'GET')
  },

  // 获取超时告警列表
  getAlerts(page = 1, pageSize = 20, status = '', personId = null) {
    let url = `/alerts?page=${page}&pageSize=${pageSize}`
    if (status) url += `&status=${status}`
    if (personId) url += `&personId=${personId}`
    return request(url, 'GET')
  },

  // 处理告警
  processAlert(id) {
    return request(`/alerts/${id}/process`, 'PUT')
  },

  // 获取工单升级状态
  getEscalations(ticketId) {
    return request(`/escalations/${ticketId}`, 'GET')
  }
}
