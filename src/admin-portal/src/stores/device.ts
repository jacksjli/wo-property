import { ref } from 'vue'

// 设备类型
export type DeviceType = 'fire_protection' | 'surveillance' | 'access_control' | 'elevator' | 'parking' | 'other'

// 设备状态
export type DeviceStatus = 'normal' | 'maintenance' | 'fault' | 'disabled'

// 巡检计划周期
export type InspectionCycle = 'daily' | 'weekly' | 'monthly' | 'quarterly' | 'yearly'

// 设备信息
export interface Device {
  id: number
  deviceNo: string        // 设备编号
  name: string           // 设备名称
  type: DeviceType       // 设备类型
  model: string          // 型号
  manufacturer: string    // 制造商
  location: string        // 安装位置
  installDate: string     // 安装日期
  status: DeviceStatus    // 状态
  lastMaintenanceDate: string  // 最近保养日期
  nextMaintenanceDate: string  // 下次保养日期
  inspectionCycle: InspectionCycle  // 巡检周期
  lastInspectionDate: string     // 最近巡检日期
  nextInspectionDate: string     // 下次巡检日期
  assignedStaffId: number | null  // 负责人ID
  remark: string         // 备注
  maintenanceRecords: MaintenanceRecord[]  // 维修记录
  inspectionRecords: InspectionRecord[]    // 巡检记录
}

// 维修记录
export interface MaintenanceRecord {
  id: number
  deviceId: number
  date: string
  type: 'repair' | 'replace' | 'check'
  description: string
  handler: string
  cost: number
  remark: string
}

// 巡检记录
export interface InspectionRecord {
  id: number
  deviceId: number
  date: string
  inspector: string
  result: 'normal' | 'abnormal'
  issue: string
  remark: string
}

// 设备类型标签
export const deviceTypeLabels: Record<DeviceType, string> = {
  'fire_protection': '消防设备',
  'surveillance': '监控设备',
  'access_control': '门禁设备',
  'elevator': '电梯设备',
  'parking': '停车设备',
  'other': '其他设备'
}

// 设备状态标签
export const deviceStatusLabels: Record<DeviceStatus, string> = {
  'normal': '正常',
  'maintenance': '维修中',
  'fault': '故障',
  'disabled': '停用'
}

// 巡检周期标签
export const inspectionCycleLabels: Record<InspectionCycle, string> = {
  'daily': '每日',
  'weekly': '每周',
  'monthly': '每月',
  'quarterly': '每季度',
  'yearly': '每年'
}

// 存储键名
const STORAGE_KEY = 'wo_devices'

// 从 localStorage 加载数据
const loadFromStorage = (): Device[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载设备数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Device[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存设备数据失败:', error)
  }
}

// 设备数据
const devices = ref<Device[]>(loadFromStorage())

// 如果没有数据，使用默认数据
if (devices.value.length === 0) {
  devices.value = [
    {
      id: 1,
      deviceNo: 'CAM-001',
      name: 'A栋监控摄像头',
      type: 'surveillance',
      model: 'Hikvision DS-2CD3T86F',
      manufacturer: '海康威视',
      location: 'A栋大堂',
      installDate: '2024-01-15',
      status: 'normal',
      lastMaintenanceDate: '2024-03-01',
      nextMaintenanceDate: '2024-06-01',
      inspectionCycle: 'monthly',
      lastInspectionDate: '2024-04-01',
      nextInspectionDate: '2024-05-01',
      assignedStaffId: 1,
      remark: '高清红外摄像头',
      maintenanceRecords: [],
      inspectionRecords: []
    },
    {
      id: 2,
      deviceNo: 'ACC-001',
      name: 'A栋门禁系统',
      type: 'access_control',
      model: 'ZKTeco C3-400',
      manufacturer: '中控智慧',
      location: 'A栋入口',
      installDate: '2024-02-01',
      status: 'normal',
      lastMaintenanceDate: '2024-03-15',
      nextMaintenanceDate: '2024-06-15',
      inspectionCycle: 'weekly',
      lastInspectionDate: '2024-04-15',
      nextInspectionDate: '2024-04-22',
      assignedStaffId: 2,
      remark: '支持指纹、密码、刷卡',
      maintenanceRecords: [],
      inspectionRecords: []
    },
    {
      id: 3,
      deviceNo: 'FIR-001',
      name: 'A栋消防栓',
      type: 'fire_protection',
      model: 'SN65消火栓',
      manufacturer: '上海淹城',
      location: 'A栋每层走廊',
      installDate: '2023-06-01',
      status: 'normal',
      lastMaintenanceDate: '2024-02-01',
      nextMaintenanceDate: '2024-05-01',
      inspectionCycle: 'quarterly',
      lastInspectionDate: '2024-04-01',
      nextInspectionDate: '2024-07-01',
      assignedStaffId: 1,
      remark: '每层楼2个',
      maintenanceRecords: [],
      inspectionRecords: []
    },
    {
      id: 4,
      deviceNo: 'ELV-001',
      name: '1号电梯',
      type: 'elevator',
      model: 'OTIS Gen2',
      manufacturer: '奥的斯',
      location: 'A栋电梯井',
      installDate: '2022-01-01',
      status: 'normal',
      lastMaintenanceDate: '2024-04-01',
      nextMaintenanceDate: '2024-05-01',
      inspectionCycle: 'monthly',
      lastInspectionDate: '2024-04-15',
      nextInspectionDate: '2024-05-15',
      assignedStaffId: 3,
      remark: '需持证人员维护',
      maintenanceRecords: [],
      inspectionRecords: []
    },
    {
      id: 5,
      deviceNo: 'CAM-002',
      name: 'B栋监控摄像头',
      type: 'surveillance',
      model: 'Hikvision DS-2CD3T86F',
      manufacturer: '海康威视',
      location: 'B栋停车场',
      installDate: '2024-03-01',
      status: 'fault',
      lastMaintenanceDate: '2024-03-01',
      nextMaintenanceDate: '2024-04-15',
      inspectionCycle: 'monthly',
      lastInspectionDate: '2024-04-10',
      nextInspectionDate: '2024-05-10',
      assignedStaffId: 2,
      remark: '摄像头故障，已报修',
      maintenanceRecords: [],
      inspectionRecords: []
    }
  ]
  saveToStorage(devices.value)
}

let deviceIdCounter = Math.max(...devices.value.map(d => d.id), 0) + 1

// 获取所有设备
export const getAllDevices = () => devices.value

// 获取指定设备
export const getDeviceById = (id: number) => devices.value.find(d => d.id === id)

// 按类型获取设备
export const getDevicesByType = (type: DeviceType) => devices.value.filter(d => d.type === type)

// 按状态获取设备
export const getDevicesByStatus = (status: DeviceStatus) => devices.value.filter(d => d.status === status)

// 获取需要巡检的设备
export const getDevicesForInspection = () => {
  const today = new Date()
  return devices.value.filter(d => {
    if (d.status === 'disabled') return false
    const nextDate = new Date(d.nextInspectionDate)
    return nextDate <= today
  })
}

// 添加设备
export const addDevice = (device: Omit<Device, 'id' | 'maintenanceRecords' | 'inspectionRecords'>): Device => {
  const newDevice: Device = {
    ...device,
    id: deviceIdCounter++,
    maintenanceRecords: [],
    inspectionRecords: []
  }
  devices.value.push(newDevice)
  saveToStorage(devices.value)
  return newDevice
}

// 更新设备
export const updateDevice = (id: number, updates: Partial<Device>) => {
  const index = devices.value.findIndex(d => d.id === id)
  if (index !== -1) {
    devices.value[index] = { ...devices.value[index], ...updates }
    saveToStorage(devices.value)
  }
}

// 删除设备
export const deleteDevice = (id: number) => {
  const index = devices.value.findIndex(d => d.id === id)
  if (index !== -1) {
    devices.value.splice(index, 1)
    saveToStorage(devices.value)
  }
}

// 添加维修记录
export const addMaintenanceRecord = (deviceId: number, record: Omit<MaintenanceRecord, 'id' | 'deviceId'>) => {
  const device = devices.value.find(d => d.id === deviceId)
  if (device) {
    const newRecord: MaintenanceRecord = {
      ...record,
      id: Date.now(),
      deviceId
    }
    device.maintenanceRecords.push(newRecord)
    device.lastMaintenanceDate = record.date
    saveToStorage(devices.value)
    return newRecord
  }
  return null
}

// 添加巡检记录
export const addInspectionRecord = (deviceId: number, record: Omit<InspectionRecord, 'id' | 'deviceId'>) => {
  const device = devices.value.find(d => d.id === deviceId)
  if (device) {
    const newRecord: InspectionRecord = {
      ...record,
      id: Date.now(),
      deviceId
    }
    device.inspectionRecords.push(newRecord)
    device.lastInspectionDate = record.date
    saveToStorage(devices.value)
    return newRecord
  }
  return null
}

// 获取设备统计
export const getDeviceStats = () => {
  const total = devices.value.length
  const normal = devices.value.filter(d => d.status === 'normal').length
  const maintenance = devices.value.filter(d => d.status === 'maintenance').length
  const fault = devices.value.filter(d => d.status === 'fault').length
  const disabled = devices.value.filter(d => d.status === 'disabled').length
  const needInspection = getDevicesForInspection().length
  
  return { total, normal, maintenance, fault, disabled, needInspection }
}

// 重置为默认
export const resetDevices = () => {
  devices.value = []
  deviceIdCounter = 1
  saveToStorage(devices.value)
}

export const deviceStore = {
  devices,
  getAllDevices,
  getDeviceById,
  getDevicesByType,
  getDevicesByStatus,
  getDevicesForInspection,
  addDevice,
  updateDevice,
  deleteDevice,
  addMaintenanceRecord,
  addInspectionRecord,
  getDeviceStats,
  resetDevices
}