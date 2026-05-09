import { ref } from 'vue'

// 访客类型
export type VisitorType = 'family' | 'friend' | 'delivery' | 'service' | 'business' | 'interview' | 'other'

// 访客状态
export type VisitorStatus = 'registered' | 'checked_in' | 'checked_out' | 'cancelled'

// 访客记录
export interface Visitor {
  id: number
  visitorNo: string         // 访客编号
  name: string            // 访客姓名
  idCard: string          // 身份证号
  phone: string           // 联系电话
  type: VisitorType       // 访客类型
  plateNo: string         // 车牌号
  visitCount: number      // 拜访次数
  residentName: string   // 被访住户姓名
  residentRoom: string   // 被访住户房号
  visitPurpose: string    // 访问事由
  visitTime: string     // 登记时间
  checkInTime: string   // 进入时间
  checkOutTime: string  // 离开时间
  companion: string     // 同行人数
  handler: string      // 登记人
  photos: string[]     // 照片
  temperature?: number  // 体温
  healthCode?: string  // 健康码状态
  remark: string       // 备注
  records: VisitRecord[]  // 来访记录
}

// 访问记录
export interface VisitRecord {
  id: number
  visitorId: number
  visitDate: string
  visitTime: string
  residentRoom: string
  residentName: string
  purpose: string
  checkInTime: string
  checkOutTime: string
  handler: string
}

// 类型标签
export const visitorTypeLabels: Record<VisitorType, string> = {
  'family': '探亲',
  'friend': '访友',
  'delivery': '快递/外卖',
  'service': '服务人员',
  'business': '商务来访',
  'interview': '面试',
  'other': '其他'
}

// 状态标签
export const visitorStatusLabels: Record<VisitorStatus, string> = {
  'registered': '已登记',
  'checked_in': '已进入',
  'checked_out': '已离开',
  'cancelled': '已取消'
}

// 存储键名
const STORAGE_KEY = 'wo_visitors'

// 从 localStorage 加载数据
const loadFromStorage = (): Visitor[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载访客数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Visitor[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存访客数据失败:', error)
  }
}

// 数据
const visitors = ref<Visitor[]>(loadFromStorage())

// 默认数据
if (visitors.value.length === 0) {
  visitors.value = [
    {
      id: 1,
      visitorNo: 'V-2024-001',
      name: '张先生',
      idCard: '310101199001011234',
      phone: '138-0000-1111',
      type: 'family',
      plateNo: '沪A12345',
      visitCount: 5,
      residentName: '王阿姨',
      residentRoom: 'A栋101',
      visitPurpose: '看望老人',
      visitTime: '2024-04-15 14:00',
      checkInTime: '2024-04-15 14:05',
      checkOutTime: '',
      companion: '2人',
      handler: '前台小李',
      photos: [],
      remark: '',
      records: []
    },
    {
      id: 2,
      visitorNo: 'V-2024-002',
      name: '李师傅',
      idCard: '310101199002022345',
      phone: '139-0000-2222',
      type: 'service',
      plateNo: '沪B67890',
      visitCount: 20,
      residentName: '物业工程部',
      residentRoom: 'A栋地下室',
      visitPurpose: '电梯维修',
      visitTime: '2024-04-15 09:00',
      checkInTime: '2024-04-15 09:10',
      checkOutTime: '2024-04-15 17:30',
      companion: '1人',
      handler: '前台小王',
      photos: [],
      remark: '维修单号：WX20240415',
      records: []
    },
    {
      id: 3,
      visitorNo: 'V-2024-003',
      name: '陈快递',
      idCard: '',
      phone: '137-0000-3333',
      type: 'delivery',
      plateNo: '',
      visitCount: 100,
      residentName: 'A栋202',
      residentRoom: 'A栋202',
      visitPurpose: '送快递',
      visitTime: '2024-04-15 11:00',
      checkInTime: '2024-04-15 11:02',
      checkOutTime: '2024-04-15 11:15',
      companion: '',
      handler: '前台小李',
      photos: [],
      remark: '',
      records: []
    }
  ]
  saveToStorage(visitors.value)
}

let visitorIdCounter = Math.max(...visitors.value.map(v => v.id), 0) + 1

// 获取统计数据
export const getVisitorStats = () => {
  const today = new Date().toISOString().split('T')[0]
  const todayVisitors = visitors.value.filter(v => v.visitTime.split('T')[0] === today)
  
  return {
    total: visitors.value.length,
    today: todayVisitors.length,
    checkedIn: visitors.value.filter(v => v.checkInTime && !v.checkOutTime).length,
    checkedOut: visitors.value.filter(v => v.checkOutTime).length,
    registered: visitors.value.filter(v => !v.checkInTime).length,
    totalVisits: visitors.value.reduce((sum, v) => sum + v.visitCount, 0)
  }
}

// 获取所有访客
export const getAllVisitors = () => visitors.value

// 获取指定访客
export const getVisitorById = (id: number) => visitors.value.find(v => v.id === id)

// 按状态获取访客
export const getVisitorsByStatus = (status: VisitorStatus) => visitors.value.filter(v => v.status === status)

// 按类型获取访客
export const getVisitorsByType = (type: VisitorType) => visitors.value.filter(v => v.type === type)

// 添加访客
export const addVisitor = (visitor: Omit<Visitor, 'id' | 'records' | 'visitCount'>): Visitor => {
  const newVisitor: Visitor = {
    ...visitor,
    id: visitorIdCounter++,
    visitCount: 1,
    records: []
  }
  visitors.value.push(newVisitor)
  saveToStorage(visitors.value)
  return newVisitor
}

// 更新访客
export const updateVisitor = (id: number, updates: Partial<Visitor>) => {
  const index = visitors.value.findIndex(v => v.id === id)
  if (index !== -1) {
    visitors.value[index] = { ...visitors.value[index], ...updates }
    saveToStorage(visitors.value)
  }
}

// 删除访客
export const deleteVisitor = (id: number) => {
  const index = visitors.value.findIndex(v => v.id === id)
  if (index !== -1) {
    visitors.value.splice(index, 1)
    saveToStorage(visitors.value)
  }
}

// 登记进入
export const checkInVisitor = (id: number) => {
  const visitor = visitors.value.find(v => v.id === id)
  if (visitor) {
    visitor.checkInTime = new Date().toISOString().slice(0, 16).replace('T', ' ')
    visitor.visitCount++
    saveToStorage(visitors.value)
  }
}

// 登记离开
export const checkOutVisitor = (id: number) => {
  const visitor = visitors.value.find(v => v.id === id)
  if (visitor) {
    visitor.checkOutTime = new Date().toISOString().slice(0, 16).replace('T', ' ')
    saveToStorage(visitors.value)
  }
}

// 获取状态颜色
export const getStatusColor = (status: VisitorStatus) => {
  const colors: Record<VisitorStatus, string> = {
    registered: '#409EFF',
    checked_in: '#E6A23C',
    checked_out: '#67C23A',
    cancelled: '#909399'
  }
  return colors[status]
}

// 获取状态类型
export const getStatusType = (status: VisitorStatus) => {
  const types: Record<VisitorStatus, string> = {
    registered: 'primary',
    checked_in: 'warning',
    checked_out: 'success',
    cancelled: 'info'
  }
  return types[status]
}

export const visitorStore = {
  visitors,
  getVisitorStats,
  getAllVisitors,
  getVisitorById,
  getVisitorsByStatus,
  getVisitorsByType,
  addVisitor,
  updateVisitor,
  deleteVisitor,
  checkInVisitor,
  checkOutVisitor,
  getStatusColor,
  getStatusType
}