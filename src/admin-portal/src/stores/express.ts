import { ref } from 'vue'

// 快递状态类型
export type ExpressStatus = '待取件' | '已入库' | '配送中' | '已签收' | '异常'

// 快递公司类型
export type ExpressCompany = '顺丰' | '中通' | '韵达' | '圆通' | '申通' | '邮政' | '京东' | '其他'

// 快递记录接口
export interface Express {
  id: number
  expressNo: string          // 快递单号
  company: ExpressCompany   // 快递公司
  senderName: string        // 发件人姓名
  senderPhone: string       // 发件人电话
  senderAddress: string      // 发件地址
  receiverName: string      // 收件人姓名
  receiverPhone: string      // 收件人电话
  receiverRoom: string      // 收件房号
  status: ExpressStatus     // 快递状态
  weight?: number           // 重量(kg)
  remark?: string           // 备注
  createTime: string        // 入库时间
  pickupTime?: string       // 取件时间
  courier?: string          // 配送员
  courierPhone?: string      // 配送员电话
}

// 存储键名
const STORAGE_KEY = 'wo_express_orders'

// 从 localStorage 加载
const loadFromStorage = (): Express[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      return JSON.parse(saved)
    }
  } catch (e) {
    console.error('加载快递数据失败:', e)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Express[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (e) {
    console.error('保存快递数据失败:', e)
  }
}

// 获取状态标签类型
export const getStatusType = (status: ExpressStatus): string => {
  const map: Record<ExpressStatus, string> = {
    '待取件': 'warning',
    '已入库': 'primary',
    '配送中': 'info',
    '已签收': 'success',
    '异常': 'danger'
  }
  return map[status] || 'info'
}

// 获取状态标签颜色
export const getStatusTag = (status: ExpressStatus): string => {
  const map: Record<ExpressStatus, string> = {
    '待取件': 'warning',
    '已入库': 'primary',
    '配送中': 'info',
    '已签收': 'success',
    '异常': 'danger'
  }
  return map[status] || 'info'
}

// 快递公司列表
export const expressCompanies: ExpressCompany[] = ['顺丰', '中通', '韵达', '圆通', '申通', '邮政', '京东', '其他']

// 状态列表
export const expressStatuses: ExpressStatus[] = ['待取件', '已入库', '配送中', '已签收', '异常']

// 生成快递单号
const generateExpressNo = (): string => {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  const random = Math.floor(Math.random() * 10000).toString().padStart(4, '0')
  return `EXP${year}${month}${day}${random}`
}

// 数据
const expressList = ref<Express[]>(loadFromStorage())

// 保存初始数据（如果有的话）
if (expressList.value.length === 0) {
  // 添加一些示例数据
  const sampleData: Express[] = [
    {
      id: 1,
      expressNo: 'EXP202604220001',
      company: '顺丰',
      senderName: '张三天猫店',
      senderPhone: '13800138001',
      senderAddress: '深圳市南山区',
      receiverName: '王五',
      receiverPhone: '13900139001',
      receiverRoom: 'A栋101',
      status: '待取件',
      weight: 1.2,
      remark: '生鲜物品，注意保鲜',
      createTime: '2026-04-22 09:30:00'
    },
    {
      id: 2,
      expressNo: 'EXP202604220002',
      company: '中通',
      senderName: '李四淘宝店',
      senderPhone: '13700137001',
      senderAddress: '广州市天河区',
      receiverName: '赵六',
      receiverPhone: '13800138002',
      receiverRoom: 'B栋203',
      status: '已入库',
      weight: 0.8,
      createTime: '2026-04-22 10:15:00'
    },
    {
      id: 3,
      expressNo: 'EXP202604220003',
      company: '韵达',
      senderName: '王五京东店',
      senderPhone: '13600136001',
      senderAddress: '北京市朝阳区',
      receiverName: '孙七',
      receiverPhone: '13700137002',
      receiverRoom: 'C栋305',
      status: '配送中',
      weight: 2.5,
      courier: '小张',
      courierPhone: '15800001111',
      createTime: '2026-04-22 11:00:00'
    },
    {
      id: 4,
      expressNo: 'EXP202604220004',
      company: '邮政',
      senderName: '官方文件',
      senderPhone: '11111111',
      senderAddress: '北京市邮政局',
      receiverName: '周八',
      receiverPhone: '13800138003',
      receiverRoom: 'A栋502',
      status: '已签收',
      weight: 0.3,
      pickupTime: '2026-04-22 14:00:00',
      createTime: '2026-04-22 08:00:00'
    }
  ]
  expressList.value = sampleData
  saveToStorage(sampleData)
}

// 获取统计数据
export const getExpressStats = () => {
  const total = expressList.value.length
  const pending = expressList.value.filter(e => e.status === '待取件').length
  const stored = expressList.value.filter(e => e.status === '已入库').length
  const delivering = expressList.value.filter(e => e.status === '配送中').length
  const delivered = expressList.value.filter(e => e.status === '已签收').length
  const abnormal = expressList.value.filter(e => e.status === '异常').length
  return { total, pending, stored, delivering, delivered, abnormal }
}

// 获取所有快递
export const getAllExpress = () => expressList.value

// 根据ID获取快递
export const getExpressById = (id: number) => expressList.value.find(e => e.id === id)

// 根据状态获取快递
export const getExpressByStatus = (status: ExpressStatus) => expressList.value.filter(e => e.status === status)

// 添加快递
export const addExpress = (express: Omit<Express, 'id' | 'expressNo' | 'createTime'>): Express => {
  const now = new Date().toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }).replace(/\//g, '-')
  const newExpress: Express = {
    id: Date.now(),
    expressNo: generateExpressNo(),
    createTime: now,
    ...express
  }
  expressList.value.push(newExpress)
  saveToStorage(expressList.value)
  return newExpress
}

// 更新快递
export const updateExpress = (id: number, updates: Partial<Express>) => {
  const index = expressList.value.findIndex(e => e.id === id)
  if (index !== -1) {
    expressList.value[index] = { ...expressList.value[index], ...updates }
    saveToStorage(expressList.value)
  }
}

// 删除快递
export const deleteExpress = (id: number) => {
  const index = expressList.value.findIndex(e => e.id === id)
  if (index !== -1) {
    expressList.value.splice(index, 1)
    saveToStorage(expressList.value)
  }
}

// 更新快递状态
export const updateExpressStatus = (id: number, status: ExpressStatus) => {
  const express = expressList.value.find(e => e.id === id)
  if (express) {
    express.status = status
    if (status === '已签收') {
      express.pickupTime = new Date().toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }).replace(/\//g, '-')
    }
    saveToStorage(expressList.value)
  }
}

// 快递 store
export const expressStore = {
  expressList,
  getAllExpress,
  getExpressById,
  getExpressByStatus,
  addExpress,
  updateExpress,
  deleteExpress,
  updateExpressStatus,
  getExpressStats
}