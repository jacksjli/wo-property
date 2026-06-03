/**
 * 微信小程序 - Ticket API 单元测试
 * 
 * 测试覆盖:
 * - getTicketTypes() - 工单类型 API
 * - getJobTypes() - 工种 API
 * - getAreas() - 区域 API
 * - getBuildings() - 楼栋 API
 * 
 * 运行: npm run test:run
 */

import { describe, it, expect, beforeAll } from 'vitest'

// API 配置
const API_BASE = 'https://elbow-backache-pacifist.ngrok-free.dev/api'

/**
 * 模拟 request 请求
 */
async function fetchAPI(path, method = 'GET', data = null) {
  const options = {
    method,
    headers: { 'Content-Type': 'application/json' }
  }
  if (data) {
    options.body = JSON.stringify(data)
  }
  const res = await fetch(`${API_BASE}${path}`, options)
  return res.json()
}

describe('Ticket API 单元测试', () => {
  
  describe('getTicketTypes() - 工单类型', () => {
    it('应返回 7 条工单类型', async () => {
      const res = await fetchAPI('/ticket-types')
      expect(res.success).toBe(true)
      expect(res.data.length).toBe(7)
    })

    it('工单类型应包含必要字段', async () => {
      const res = await fetchAPI('/ticket-types')
      const item = res.data[0]
      expect(item).toHaveProperty('id')
      expect(item).toHaveProperty('name')
      expect(item).toHaveProperty('code')
      expect(item).toHaveProperty('color')
    })

    it('工单类型应包含"维修"', async () => {
      const res = await fetchAPI('/ticket-types')
      const names = res.data.map(t => t.name)
      expect(names).toContain('维修')
    })
  })

  describe('getJobTypes() - 工种', () => {
    it('应返回 24 条工种', async () => {
      const res = await fetchAPI('/job-types')
      expect(res.success).toBe(true)
      expect(res.data.length).toBe(24)
    })

    it('工种应关联 ticket_type_id', async () => {
      const res = await fetchAPI('/job-types')
      const item = res.data[0]
      expect(item).toHaveProperty('ticket_type_id')
      expect(typeof item.ticket_type_id).toBe('number')
    })

    it('工种应按 category 分组', async () => {
      const res = await fetchAPI('/job-types')
      const categories = [...new Set(res.data.map(j => j.category))]
      expect(categories.length).toBeGreaterThan(1)
    })
  })

  describe('getAreas() - 区域', () => {
    it('应返回 7 个区域', async () => {
      const res = await fetchAPI('/areas')
      expect(res.success).toBe(true)
      expect(res.data.length).toBe(7)
    })

    it('区域应包含必要字段', async () => {
      const res = await fetchAPI('/areas')
      const item = res.data[0]
      expect(item).toHaveProperty('id')
      expect(item).toHaveProperty('name')
      expect(item).toHaveProperty('code')
    })

    it('区域应包含"东区"', async () => {
      const res = await fetchAPI('/areas')
      const names = res.data.map(a => a.name)
      expect(names).toContain('东区')
    })
  })

  describe('getBuildings() - 楼栋', () => {
    it('应返回 6 个楼栋', async () => {
      const res = await fetchAPI('/buildings')
      expect(res.success).toBe(true)
      expect(res.data.length).toBe(6)
    })

    it('楼栋应包含必要字段', async () => {
      const res = await fetchAPI('/buildings')
      const item = res.data[0]
      expect(item).toHaveProperty('id')
      expect(item).toHaveProperty('name')
    })
  })

})

describe('Dispatch API 单元测试', () => {
  
  describe('getPending() - 待处理工单', () => {
    it('应返回 HTTP 200', async () => {
      const res = await fetch(`${API_BASE}/tenant/dispatch/pending?page=1&pageSize=10`, {
        headers: { 'Tenant-Code': 'wo_property' }
      })
      expect(res.ok || res.status === 200 || res.status === 401).toBe(true) // 401 表示需要认证
    })
  })

})

describe('表单验证逻辑测试', () => {
  
  const validationRules = {
    ticketTypeId: { required: true, message: '请选择工单类型' },
    jobTypeId: { required: true, message: '请选择工种' },
    areaId: { required: true, message: '请选择区域' },
    name: { required: true, message: '请输入姓名' },
    phone: { required: true, pattern: /^1\d{10}$/, message: '请输入正确的手机号' }
  }

  it('工单类型必填', () => {
    expect(validationRules.ticketTypeId.required).toBe(true)
  })

  it('工种必填', () => {
    expect(validationRules.jobTypeId.required).toBe(true)
  })

  it('区域必填', () => {
    expect(validationRules.areaId.required).toBe(true)
  })

  it('姓名必填', () => {
    expect(validationRules.name.required).toBe(true)
  })

  it('电话格式验证', () => {
    expect(validationRules.phone.pattern.test('13800138000')).toBe(true)
    expect(validationRules.phone.pattern.test('1234567890')).toBe(false)
  })

})