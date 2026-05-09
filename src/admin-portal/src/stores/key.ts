import { ref } from 'vue'

// 钥匙类型
export type KeyType = 'door_key' | 'room_key' | 'card' | 'password' | 'remote' | 'other'

// 钥匙状态
export type KeyStatus = 'available' | 'borrowed' | 'lost' | 'damaged' | 'returned' | 'disabled'

// 借用记录
export interface BorrowRecord {
  id: number
  keyId: number
  borrower: string       // 借用人
  borrowerPhone: string   // 联系电话
  purpose: string        // 借用用途
  borrowTime: string     // 借用时间
  expectReturnTime: string  // 预计归还时间
  actualReturnTime: string  // 实际归还时间
  handler: string        // 经办人
  status: 'borrowed' | 'returned' | 'overdue'
  remark: string
}

// 钥匙信息
export interface Key {
  id: number
  keyNo: string         // 钥匙编号
  name: string          // 钥匙名称
  type: KeyType        // 钥匙类型
  location: string      // 存放位置
  building: string      // 楼栋
  floor: string        // 楼层
  doorNo: string       // 门牌号
  quantity: number     // 数量
  status: KeyStatus    // 状态
  holder: string       // 当前持有人
  holderPhone: string  // 持有人电话
  lastBorrowTime: string  // 最近借用时间
  borrowCount: number  // 借用次数
  photo: string       // 照片
  remark: string      // 备注
  records: BorrowRecord[]  // 借用记录
}

// 类型标签
export const keyTypeLabels: Record<KeyType, string> = {
  'door_key': '入户门钥匙',
  'room_key': '房门钥匙',
  'card': '门禁卡',
  'password': '密码',
  'remote': '遥控器',
  'other': '其他'
}

// 状态标签
export const keyStatusLabels: Record<KeyStatus, string> = {
  'available': '可用',
  'borrowed': '已借出',
  'lost': '已遗失',
  'damaged': '已损坏',
  'returned': '已归还',
  'disabled': '已停用'
}

// 存储键名
const STORAGE_KEY = 'wo_keys'

// 从 localStorage 加载数据
const loadFromStorage = (): Key[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载钥匙数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Key[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存钥匙数据失败:', error)
  }
}

// 数据
const keys = ref<Key[]>(loadFromStorage())

// 默认数据
if (keys.value.length === 0) {
  keys.value = [
    {
      id: 1,
      keyNo: 'KEY-A101',
      name: 'A栋101室钥匙',
      type: 'room_key',
      location: '前台钥匙柜',
      building: 'A栋',
      floor: '1楼',
      doorNo: '101',
      quantity: 2,
      status: 'available',
      holder: '',
      holderPhone: '',
      lastBorrowTime: '',
      borrowCount: 15,
      photo: '',
      remark: '备用钥匙',
      records: []
    },
    {
      id: 2,
      keyNo: 'KEY-A102',
      name: 'A栋102室钥匙',
      type: 'room_key',
      location: '前台钥匙柜',
      building: 'A栋',
      floor: '1楼',
      doorNo: '102',
      quantity: 2,
      status: 'borrowed',
      holder: '李师傅',
      holderPhone: '13800138001',
      lastBorrowTime: '2024-04-15 10:00',
      borrowCount: 8,
      photo: '',
      remark: '',
      records: [
        {
          id: 1,
          keyId: 2,
          borrower: '李师傅',
          borrowerPhone: '13800138001',
          purpose: '维修水管',
          borrowTime: '2024-04-15 10:00',
          expectReturnTime: '2024-04-15 18:00',
          actualReturnTime: '',
          handler: '张主管',
          status: 'borrowed',
          remark: ''
        }
      ]
    },
    {
      id: 3,
      keyNo: 'KEY-ELEV',
      name: '电梯机房钥匙',
      type: 'door_key',
      location: '工程部办公室',
      building: 'A栋',
      floor: '屋顶',
      doorNo: '电梯机房',
      quantity: 1,
      status: 'available',
      holder: '',
      holderPhone: '',
      lastBorrowTime: '',
      borrowCount: 3,
      photo: '',
      remark: '需持证人员使用',
      records: []
    },
    {
      id: 4,
      keyNo: 'KEY-CARD-001',
      name: '门禁卡-李明',
      type: 'card',
      location: '前台',
      building: '公共',
      floor: '1楼',
      doorNo: '大堂',
      quantity: 1,
      status: 'available',
      holder: '李明',
      holderPhone: '13900001111',
      lastBorrowTime: '2024-04-10 09:00',
      borrowCount: 25,
      photo: '',
      remark: '业主卡',
      records: []
    },
    {
      id: 5,
      keyNo: 'KEY-PARK',
      name: '停车场道闸遥控器',
      type: 'remote',
      location: '保安室',
      building: '停车场',
      floor: '入口',
      doorNo: '道闸',
      quantity: 3,
      status: 'available',
      holder: '',
      holderPhone: '',
      lastBorrowTime: '',
      borrowCount: 50,
      photo: '',
      remark: '',
      records: []
    }
  ]
  saveToStorage(keys.value)
}

let keyIdCounter = Math.max(...keys.value.map(k => k.id), 0) + 1

// 获取统计数据
export const getKeyStats = () => {
  return {
    total: keys.value.length,
    available: keys.value.filter(k => k.status === 'available').length,
    borrowed: keys.value.filter(k => k.status === 'borrowed').length,
    lost: keys.value.filter(k => k.status === 'lost').length,
    damaged: keys.value.filter(k => k.status === 'damaged').length,
    totalBorrows: keys.value.reduce((sum, k) => sum + k.borrowCount, 0),
    overdueCount: keys.value.filter(k => 
      k.status === 'borrowed' && k.records.some(r => r.status === 'borrowed')
    ).length
  }
}

// 获取所有钥匙
export const getAllKeys = () => keys.value

// 获取指定钥匙
export const getKeyById = (id: number) => keys.value.find(k => k.id === id)

// 按状态获取钥匙
export const getKeysByStatus = (status: KeyStatus) => keys.value.filter(k => k.status === status)

// 按类型获取钥匙
export const getKeysByType = (type: KeyType) => keys.value.filter(k => k.type === type)

// 添加钥匙
export const addKey = (key: Omit<Key, 'id' | 'records' | 'borrowCount' | 'lastBorrowTime'>): Key => {
  const newKey: Key = {
    ...key,
    id: keyIdCounter++,
    borrowCount: 0,
    lastBorrowTime: '',
    records: []
  }
  keys.value.push(newKey)
  saveToStorage(keys.value)
  return newKey
}

// 更新钥匙
export const updateKey = (id: number, updates: Partial<Key>) => {
  const index = keys.value.findIndex(k => k.id === id)
  if (index !== -1) {
    keys.value[index] = { ...keys.value[index], ...updates }
    saveToStorage(keys.value)
  }
}

// 删除钥匙
export const deleteKey = (id: number) => {
  const index = keys.value.findIndex(k => k.id === id)
  if (index !== -1) {
    keys.value.splice(index, 1)
    saveToStorage(keys.value)
  }
}

// 借用钥匙
export const borrowKey = (keyId: number, record: Omit<BorrowRecord, 'id' | 'keyId'>) => {
  const key = keys.value.find(k => k.id === keyId)
  if (key) {
    const newRecord: BorrowRecord = {
      ...record,
      id: Date.now(),
      keyId
    }
    key.records.push(newRecord)
    key.status = 'borrowed'
    key.holder = record.borrower
    key.holderPhone = record.borrowerPhone
    key.lastBorrowTime = record.borrowTime
    key.borrowCount++
    saveToStorage(keys.value)
    return newRecord
  }
  return null
}

// 归还钥匙
export const returnKey = (keyId: number, recordId: number) => {
  const key = keys.value.find(k => k.id === keyId)
  if (key) {
    const record = key.records.find(r => r.id === recordId)
    if (record) {
      record.status = 'returned'
      record.actualReturnTime = new Date().toISOString().slice(0, 16).replace('T', ' ')
      key.status = 'available'
      key.holder = ''
      key.holderPhone = ''
      saveToStorage(keys.value)
    }
  }
}

// 获取状态颜色
export const getStatusColor = (status: KeyStatus) => {
  const colors: Record<KeyStatus, string> = {
    available: '#67C23A',
    borrowed: '#E6A23C',
    lost: '#F56C6C',
    damaged: '#909399',
    returned: '#409EFF',
    disabled: '#C0C4CC'
  }
  return colors[status]
}

// 获取状态类型
export const getStatusType = (status: KeyStatus) => {
  const types: Record<KeyStatus, string> = {
    available: 'success',
    borrowed: 'warning',
    lost: 'danger',
    damaged: 'info',
    returned: 'primary',
    disabled: 'info'
  }
  return types[status]
}

export const keyStore = {
  keys,
  getKeyStats,
  getAllKeys,
  getKeyById,
  getKeysByStatus,
  getKeysByType,
  addKey,
  updateKey,
  deleteKey,
  borrowKey,
  returnKey,
  getStatusColor,
  getStatusType
}