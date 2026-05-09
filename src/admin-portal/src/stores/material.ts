import { ref } from 'vue'

// 物料分类
export type MaterialCategory = 'repair_parts' | 'cleaning' | 'security' | 'office' | 'other'

// 物料单位
export type MaterialUnit = '个' | '件' | '套' | '米' | '升' | '公斤' | '卷' | '盒' | '箱'

// 物料状态
export type MaterialStatus = 'normal' | 'low_stock' | 'out_of_stock' | 'expired' | 'disabled'

// 入库记录
export interface InboundRecord {
  id: number
  materialId: number
  date: string
  quantity: number
  unit: MaterialUnit
  handler: string
  supplier: string
  remark: string
}

// 出库记录
export interface OutboundRecord {
  id: number
  materialId: number
  date: string
  quantity: number
  unit: MaterialUnit
  handler: string
  useFor: string      // 用于什么（如：工单编号）
  remark: string
}

// 保养记录
export interface MaintenanceRecord {
  id: number
  materialId: number
  date: string
  type: 'check' | 'clean' | 'replace'
  description: string
  handler: string
  remark: string
}

// 物料信息
export interface Material {
  id: number
  materialNo: string      // 物料编号
  name: string           // 物料名称
  category: MaterialCategory  // 分类
  spec: string           // 规格型号
  unit: MaterialUnit     // 单位
  quantity: number        // 当前库存
  minQuantity: number    // 最低库存警戒
  price: number          // 单价
  location: string       // 存放位置
  status: MaterialStatus  // 状态
  supplier: string        // 供应商
  purchaseDate: string   // 采购日期
  expirationDate: string  // 有效期（如有）
  lastCheckDate: string  // 最近盘点日期
  nextCheckDate: string  // 下次盘点日期
  checkCycle: string     // 盘点周期
  remark: string         // 备注
  inboundRecords: InboundRecord[]    // 入库记录
  outboundRecords: OutboundRecord[]   // 出库记录
  maintenanceRecords: MaintenanceRecord[]  // 保养记录
}

// 分类标签
export const categoryLabels: Record<MaterialCategory, string> = {
  'repair_parts': '维修配件',
  'cleaning': '清洁用品',
  'security': '安保器材',
  'office': '办公用品',
  'other': '其他'
}

// 状态标签
export const statusLabels: Record<MaterialStatus, string> = {
  'normal': '正常',
  'low_stock': '库存不足',
  'out_of_stock': '已用完',
  'expired': '已过期',
  'disabled': '已停用'
}

// 单位选项
export const unitOptions: MaterialUnit[] = ['个', '件', '套', '米', '升', '公斤', '卷', '盒', '箱']

// 存储键名
const STORAGE_KEY = 'wo_materials'

// 从 localStorage 加载数据
const loadFromStorage = (): Material[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载物料数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Material[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存物料数据失败:', error)
  }
}

// 更新物料状态（根据库存数量）
const updateMaterialStatus = (material: Material) => {
  if (material.status === 'disabled') return
  
  if (material.quantity <= 0) {
    material.status = 'out_of_stock'
  } else if (material.quantity <= material.minQuantity) {
    material.status = 'low_stock'
  } else {
    material.status = 'normal'
  }
  
  // 检查是否过期
  if (material.expirationDate) {
    const today = new Date()
    const expDate = new Date(material.expirationDate)
    if (expDate < today) {
      material.status = 'expired'
    }
  }
}

// 物料数据
const materials = ref<Material[]>(loadFromStorage())

// 如果没有数据，使用默认数据
if (materials.value.length === 0) {
  materials.value = [
    {
      id: 1,
      materialNo: 'MAT-001',
      name: '灯泡',
      category: 'repair_parts',
      spec: 'E27 15W',
      unit: '个',
      quantity: 50,
      minQuantity: 10,
      price: 8,
      location: '仓库A区',
      status: 'normal',
      supplier: '五金批发店',
      purchaseDate: '2024-01-15',
      expirationDate: '',
      lastCheckDate: '2024-04-01',
      nextCheckDate: '2024-05-01',
      checkCycle: 'monthly',
      remark: '常规使用耗材',
      inboundRecords: [],
      outboundRecords: [],
      maintenanceRecords: []
    },
    {
      id: 2,
      materialNo: 'MAT-002',
      name: '门锁',
      category: 'repair_parts',
      spec: 'C级防盗锁',
      unit: '套',
      quantity: 5,
      minQuantity: 3,
      price: 128,
      location: '仓库A区',
      status: 'normal',
      supplier: '锁具批发市场',
      purchaseDate: '2024-02-01',
      expirationDate: '',
      lastCheckDate: '2024-04-15',
      nextCheckDate: '2024-05-15',
      checkCycle: 'monthly',
      remark: '高端防盗锁芯',
      inboundRecords: [],
      outboundRecords: [],
      maintenanceRecords: []
    },
    {
      id: 3,
      materialNo: 'MAT-003',
      name: '清洁剂',
      category: 'cleaning',
      spec: '多功能清洁剂 500ml',
      unit: '瓶',
      quantity: 20,
      minQuantity: 5,
      price: 25,
      location: '仓库B区',
      status: 'normal',
      supplier: '日化用品公司',
      purchaseDate: '2024-03-01',
      expirationDate: '2025-03-01',
      lastCheckDate: '2024-04-10',
      nextCheckDate: '2024-05-10',
      checkCycle: 'monthly',
      remark: '注意避光保存',
      inboundRecords: [],
      outboundRecords: [],
      maintenanceRecords: []
    },
    {
      id: 4,
      materialNo: 'MAT-004',
      name: '保安制服',
      category: 'security',
      spec: '夏装  XL',
      unit: '件',
      quantity: 2,
      minQuantity: 5,
      price: 180,
      location: '仓库B区',
      status: 'low_stock',
      supplier: '服装定制厂',
      purchaseDate: '2024-02-15',
      expirationDate: '',
      lastCheckDate: '2024-04-05',
      nextCheckDate: '2024-05-05',
      checkCycle: 'quarterly',
      remark: '需补充库存',
      inboundRecords: [],
      outboundRecords: [],
      maintenanceRecords: []
    },
    {
      id: 5,
      materialNo: 'MAT-005',
      name: '打印纸',
      category: 'office',
      spec: 'A4 70g',
      unit: '箱',
      quantity: 3,
      minQuantity: 2,
      price: 45,
      location: '办公室储藏间',
      status: 'normal',
      supplier: '文具批发商',
      purchaseDate: '2024-03-15',
      expirationDate: '',
      lastCheckDate: '2024-04-20',
      nextCheckDate: '2024-05-20',
      checkCycle: 'monthly',
      remark: '',
      inboundRecords: [],
      outboundRecords: [],
      maintenanceRecords: []
    }
  ]
  saveToStorage(materials.value)
}

let materialIdCounter = Math.max(...materials.value.map(m => m.id), 0) + 1

// 获取所有物料
export const getAllMaterials = () => materials.value

// 获取指定物料
export const getMaterialById = (id: number) => materials.value.find(m => m.id === id)

// 按分类获取物料
export const getMaterialsByCategory = (category: MaterialCategory) => materials.value.filter(m => m.category === category)

// 按状态获取物料
export const getMaterialsByStatus = (status: MaterialStatus) => materials.value.filter(m => m.status === status)

// 获取库存不足的物料
export const getLowStockMaterials = () => materials.value.filter(m => m.quantity <= m.minQuantity)

// 获取需要盘点的物料
export const getMaterialsForCheck = () => {
  const today = new Date()
  return materials.value.filter(m => {
    if (m.status === 'disabled') return false
    if (!m.nextCheckDate) return false
    const nextDate = new Date(m.nextCheckDate)
    return nextDate <= today
  })
}

// 添加物料
export const addMaterial = (material: Omit<Material, 'id' | 'inboundRecords' | 'outboundRecords' | 'maintenanceRecords'>): Material => {
  const newMaterial: Material = {
    ...material,
    id: materialIdCounter++,
    inboundRecords: [],
    outboundRecords: [],
    maintenanceRecords: []
  }
  updateMaterialStatus(newMaterial)
  materials.value.push(newMaterial)
  saveToStorage(materials.value)
  return newMaterial
}

// 更新物料
export const updateMaterial = (id: number, updates: Partial<Material>) => {
  const index = materials.value.findIndex(m => m.id === id)
  if (index !== -1) {
    materials.value[index] = { ...materials.value[index], ...updates }
    updateMaterialStatus(materials.value[index])
    saveToStorage(materials.value)
  }
}

// 删除物料
export const deleteMaterial = (id: number) => {
  const index = materials.value.findIndex(m => m.id === id)
  if (index !== -1) {
    materials.value.splice(index, 1)
    saveToStorage(materials.value)
  }
}

// 入库
export const addInbound = (materialId: number, record: Omit<InboundRecord, 'id' | 'materialId'>) => {
  const material = materials.value.find(m => m.id === materialId)
  if (material) {
    const newRecord: InboundRecord = {
      ...record,
      id: Date.now(),
      materialId
    }
    material.inboundRecords.push(newRecord)
    material.quantity += record.quantity
    updateMaterialStatus(material)
    saveToStorage(materials.value)
    return newRecord
  }
  return null
}

// 出库
export const addOutbound = (materialId: number, record: Omit<OutboundRecord, 'id' | 'materialId'>) => {
  const material = materials.value.find(m => m.id === materialId)
  if (material) {
    if (material.quantity < record.quantity) {
      return null // 库存不足
    }
    const newRecord: OutboundRecord = {
      ...record,
      id: Date.now(),
      materialId
    }
    material.outboundRecords.push(newRecord)
    material.quantity -= record.quantity
    updateMaterialStatus(material)
    saveToStorage(materials.value)
    return newRecord
  }
  return null
}

// 添加保养记录
export const addMaintenanceRecord = (materialId: number, record: Omit<MaintenanceRecord, 'id' | 'materialId'>) => {
  const material = materials.value.find(m => m.id === materialId)
  if (material) {
    const newRecord: MaintenanceRecord = {
      ...record,
      id: Date.now(),
      materialId
    }
    material.maintenanceRecords.push(newRecord)
    material.lastCheckDate = record.date
    saveToStorage(materials.value)
    return newRecord
  }
  return null
}

// 获取物料统计
export const getMaterialStats = () => {
  const total = materials.value.length
  const normal = materials.value.filter(m => m.status === 'normal').length
  const lowStock = materials.value.filter(m => m.status === 'low_stock').length
  const outOfStock = materials.value.filter(m => m.status === 'out_of_stock').length
  const expired = materials.value.filter(m => m.status === 'expired').length
  const disabled = materials.value.filter(m => m.status === 'disabled').length
  const totalValue = materials.value.reduce((sum, m) => sum + m.quantity * m.price, 0)
  
  return { total, normal, lowStock, outOfStock, expired, disabled, totalValue }
}

// 重置为默认
export const resetMaterials = () => {
  materials.value = []
  materialIdCounter = 1
  saveToStorage(materials.value)
}

export const materialStore = {
  materials,
  getAllMaterials,
  getMaterialById,
  getMaterialsByCategory,
  getMaterialsByStatus,
  getLowStockMaterials,
  getMaterialsForCheck,
  addMaterial,
  updateMaterial,
  deleteMaterial,
  addInbound,
  addOutbound,
  addMaintenanceRecord,
  getMaterialStats,
  resetMaterials
}