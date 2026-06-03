import env from '@/config/env'

function getCommonHeader() {
  const currentProject = uni.getStorageSync('currentProject') || {}
  return {
    'Content-Type': 'application/json',
    'Tenant-Code': uni.getStorageSync('tenantCode') || 'wo_property',
    'X-Project': currentProject.projectCode || '',
    'Authorization': 'Bearer ' + (uni.getStorageSync('token') || '')
  }
}

function request(url, method, data) {
  return new Promise((resolve, reject) => {
    uni.request({
      url: env.TICKET_API + url,
      method,
      data,
      timeout: 10000,
      header: getCommonHeader(),
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

// 调用 MasterDataService 的基础数据（区域/楼栋）
function masterRequest(url, method, data) {
  return new Promise((resolve, reject) => {
    uni.request({
      url: env.MASTERDATA_API + url,
      method,
      data,
      timeout: 10000,
      header: getCommonHeader(),
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
  // 获取工单类型
  getTicketTypes() {
    return request('/ticket-types', 'GET')
  },
  
  // 获取工种（调用 MasterDataService）
  getJobTypes() {
    return masterRequest('/job-types', 'GET')
  },
  
  // 获取项目列表
  getProjects() {
    return masterRequest('/projects', 'GET')
  },
  
  // 获取区域（调用 MasterDataService）
  getAreas() {
    return masterRequest('/areas', 'GET')
  },
  
  // 获取楼栋（调用 MasterDataService）
  getBuildings(areaId) {
    // MasterDataService 不支持 areaId 参数，返回全部后前端按区域名过滤
    return masterRequest('/buildings', 'GET')
  },
  
  // 创建工单
  createTicket(data) {
    const personId = uni.getStorageSync('personId')
    return request('/tenant/tickets', 'POST', { ...data, creatorPersonId: personId })
  },
  
  // 我的工单列表（业主端）
  getMyTickets(page = 1, pageSize = 20, status) {
    const personId = uni.getStorageSync('personId')
    let url = `/tenant/tickets?page=${page}&pageSize=${pageSize}`
    if (personId) url += `&creatorPersonId=${personId}`
    if (status) url += `&status=${status}`
    return request(url, 'GET')
  },
  
  // 管理员工单列表（支持区域/楼栋/类型筛选）
  getTickets(page = 1, pageSize = 20, status, projectId, areaId, buildingId, ticketTypeId) {
    let url = `/tenant/tickets?page=${page}&pageSize=${pageSize}`
    if (status) url += `&status=${status}`
    if (projectId) url += `&projectId=${projectId}`
    if (areaId) url += `&areaId=${areaId}`
    if (buildingId) url += `&buildingId=${buildingId}`
    if (ticketTypeId) url += `&ticketTypeId=${ticketTypeId}`
    return request(url, 'GET')
  },
  
  // 工单详情
  getTicketDetail(id) {
    return request(`/tenant/tickets/${id}`, 'GET')
  },
  
  // 催单
  remindTicket(id) {
    return request(`/tenant/tickets/${id}/remind`, 'POST')
  },
  
  // 评价工单
  rateTicket(id, data) {
    return request(`/tenant/tickets/${id}/rate`, 'POST', data)
  },
  
  // 获取工程师的待处理工单（按 assigneePersonId 过滤）
  getAssignedTickets(page = 1, pageSize = 20) {
    const personId = uni.getStorageSync('personId')
    let url = `/tenant/tickets?page=${page}&pageSize=${pageSize}`
    if (personId) url += `&assigneePersonId=${personId}`
    // 只显示 Dispatched/Accepted/InProgress 状态的工单
    url += `&dispatchStatus=Dispatched&dispatchStatus=Accepted&dispatchStatus=Pending`
    return request(url, 'GET')
  },
  
  // 开始处理（status: Dispatched → InProgress）
  progressTicket(id) {
    return request(`/tenant/tickets/${id}/progress`, 'POST', {})
  },
  
  // 接单（Dispatched → Accepted）
  acceptTicket(id) {
    return request(`/tenant/tickets/${id}/accept`, 'POST', {})
  },

  // 完成工单（status: InProgress → Finished）
  finishTicket(id) {
    return request(`/tenant/tickets/${id}/finish`, 'POST', { finishedAt: new Date().toISOString() })
  },
  
  // 管理页面：按负责范围筛选（assignedByMe）
  getAssignedByMeTickets(page = 1, pageSize = 20, status, areaId, buildingId, ticketTypeId) {
    let url = `/tenant/tickets?page=${page}&pageSize=${pageSize}&assignedByMe=true`
    if (status) url += `&status=${status}`
    if (areaId) url += `&areaId=${areaId}`
    if (buildingId) url += `&buildingId=${buildingId}`
    if (ticketTypeId) url += `&ticketTypeId=${ticketTypeId}`
    return request(url, 'GET')
  },
  
  // 获取工程师的待处理工单（旧方法，兼容）
  getEngineerTickets(page = 1, pageSize = 20, status) {
    const personId = uni.getStorageSync('personId')
    let url = `/tenant/dispatch/pending?page=${page}&pageSize=${pageSize}`
    if (personId) url += `&personId=${personId}`
    return request(url, 'GET')
  }
}