import { ref } from 'vue'

// 外卖订单状态
export type TakeoutStatus = '待取餐' | '配送中' | '已送达' | '已取消' | '异常'
export type TakeoutType = '中餐' | '西餐' | '快餐' | '甜品' | '饮品' | '其他'

// 外卖订单接口
export interface TakeoutOrder {
  id: number
  orderNo: string           // 订单编号
  restaurantName: string    // 餐厅名称
  foodType: TakeoutType     // 餐食类型
  residentName: string      // 住户姓名
  roomNo: string           // 房号
  phone: string            // 联系电话
  deliveryPerson: string   // 配送员姓名
  deliveryPhone: string    // 配送员电话
  deliveryTime: string     // 送达时间
  status: TakeoutStatus    // 订单状态
  totalAmount: number      // 总金额
  remark?: string          // 备注
  createTime: string       // 创建时间
  updateTime: string       // 更新时间
}

// 存储键名
const STORAGE_KEY = 'wo_takeout_orders'

// 从 localStorage 加载
const loadFromStorage = (): TakeoutOrder[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      return JSON.parse(saved)
    }
  } catch (e) {
    console.error('加载外卖订单数据失败:', e)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: TakeoutOrder[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (e) {
    console.error('保存外卖订单数据失败:', e)
  }
}

// 状态标签类型
export const getStatusType = (status: TakeoutStatus): string => {
  const map: Record<TakeoutStatus, string> = {
    '待取餐': 'warning',
    '配送中': 'primary',
    '已送达': 'success',
    '已取消': 'info',
    '异常': 'danger'
  }
  return map[status] || 'info'
}

// 类型标签
export const getTypeTag = (type: TakeoutType): string => {
  const map: Record<TakeoutType, string> = {
    '中餐': '',
    '西餐': 'success',
    '快餐': 'warning',
    '甜品': 'danger',
    '饮品': 'info',
    '其他': ''
  }
  return map[type] || ''
}

// 订单数据
const orders = ref<TakeoutOrder[]>(loadFromStorage())

// 如果没有数据，使用示例数据
if (orders.value.length === 0) {
  orders.value = [
    {
      id: 1,
      orderNo: 'TK-2026-001',
      restaurantName: '麦当劳',
      foodType: '快餐',
      residentName: '张三',
      roomNo: 'A栋101',
      phone: '13800138001',
      deliveryPerson: '李师傅',
      deliveryPhone: '13900139001',
      deliveryTime: '2026-04-22 12:30',
      status: '待取餐',
      totalAmount: 45.5,
      remark: '',
      createTime: '2026-04-22 10:30',
      updateTime: '2026-04-22 10:30'
    },
    {
      id: 2,
      orderNo: 'TK-2026-002',
      restaurantName: '肯德基',
      foodType: '快餐',
      residentName: '李四',
      roomNo: 'B栋201',
      phone: '13800138002',
      deliveryPerson: '王师傅',
      deliveryPhone: '13900139002',
      deliveryTime: '2026-04-22 12:45',
      status: '配送中',
      totalAmount: 68.0,
      remark: '',
      createTime: '2026-04-22 10:45',
      updateTime: '2026-04-22 11:00'
    },
    {
      id: 3,
      orderNo: 'TK-2026-003',
      restaurantName: 'Pizza Hut',
      foodType: '西餐',
      residentName: '王五',
      roomNo: 'C栋301',
      phone: '13800138003',
      deliveryPerson: '赵师傅',
      deliveryPhone: '13900139003',
      deliveryTime: '2026-04-22 13:00',
      status: '已送达',
      totalAmount: 128.0,
      remark: '',
      createTime: '2026-04-22 11:00',
      updateTime: '2026-04-22 13:15'
    },
    {
      id: 4,
      orderNo: 'TK-2026-004',
      restaurantName: '一点点',
      foodType: '饮品',
      residentName: '赵六',
      roomNo: 'A栋102',
      phone: '13800138004',
      deliveryPerson: '钱师傅',
      deliveryPhone: '13900139004',
      deliveryTime: '2026-04-22 13:15',
      status: '异常',
      totalAmount: 32.0,
      remark: '联系不上住户，已暂存外卖柜',
      createTime: '2026-04-22 11:15',
      updateTime: '2026-04-22 13:20'
    },
    {
      id: 5,
      orderNo: 'TK-2026-005',
      restaurantName: '沙县小吃',
      foodType: '中餐',
      residentName: '钱七',
      roomNo: 'B栋202',
      phone: '13800138005',
      deliveryPerson: '孙师傅',
      deliveryPhone: '13900139005',
      deliveryTime: '2026-04-22 13:30',
      status: '已取消',
      totalAmount: 25.0,
      remark: '住户取消订单',
      createTime: '2026-04-22 11:30',
      updateTime: '2026-04-22 12:00'
    }
  ]
  saveToStorage(orders.value)
}

// 生成订单编号
const generateOrderNo = (): string => {
  const date = new Date()
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const count = orders.value.filter(o => o.orderNo.startsWith(`TK-${year}`)).length + 1
  return `TK-${year}-${String(count).padStart(3, '0')}`
}

// 获取所有订单
export const getAllOrders = () => orders.value

// 按ID获取订单
export const getOrderById = (id: number) => orders.value.find(o => o.id === id)

// 按状态获取订单
export const getOrdersByStatus = (status: TakeoutStatus) => orders.value.filter(o => o.status === status)

// 按类型获取订单
export const getOrdersByType = (type: TakeoutType) => orders.value.filter(o => o.foodType === type)

// 添加订单
export const addOrder = (order: Omit<TakeoutOrder, 'id' | 'orderNo' | 'createTime' | 'updateTime'>): TakeoutOrder => {
  const now = new Date().toISOString().split('T')[0]
  const newOrder: TakeoutOrder = {
    ...order,
    id: Math.max(...orders.value.map(o => o.id), 0) + 1,
    orderNo: generateOrderNo(),
    createTime: now,
    updateTime: now
  }
  orders.value.push(newOrder)
  saveToStorage(orders.value)
  return newOrder
}

// 更新订单
export const updateOrder = (id: number, updates: Partial<TakeoutOrder>) => {
  const index = orders.value.findIndex(o => o.id === id)
  if (index !== -1) {
    const now = new Date().toISOString().split('T')[0]
    orders.value[index] = {
      ...orders.value[index],
      ...updates,
      updateTime: now
    }
    saveToStorage(orders.value)
  }
}

// 删除订单
export const deleteOrder = (id: number) => {
  const index = orders.value.findIndex(o => o.id === id)
  if (index !== -1) {
    orders.value.splice(index, 1)
    saveToStorage(orders.value)
  }
}

// 更新订单状态
export const updateOrderStatus = (id: number, status: TakeoutStatus) => {
  updateOrder(id, { status })
}

// 获取统计
export const getStats = () => {
  const total = orders.value.length
  const pending = orders.value.filter(o => o.status === '待取餐').length
  const delivering = orders.value.filter(o => o.status === '配送中').length
  const delivered = orders.value.filter(o => o.status === '已送达').length
  const abnormal = orders.value.filter(o => o.status === '异常').length
  const cancelled = orders.value.filter(o => o.status === '已取消').length
  return { total, pending, delivering, delivered, abnormal, cancelled }
}

// 导出 store
export const takeoutStore = {
  orders,
  getAllOrders,
  getOrderById,
  getOrdersByStatus,
  getOrdersByType,
  addOrder,
  updateOrder,
  deleteOrder,
  updateOrderStatus,
  getStats,
  getStatusType,
  getTypeTag
}
