import { ref } from 'vue'

// 巡检区域类型
export type InspectionZone = 'building' | 'parking' | 'garden' | 'facility' | 'security' | 'other'

// 巡检点类型
export type InspectionPointType = 'fire_equipment' | 'elevator' | 'access_control' | 'camera' | 'lighting' | 'water_electricity' | 'other'

// 巡检计划状态
export type PlanStatus = 'draft' | 'active' | 'paused' | 'completed'

// 巡检任务状态
export type TaskStatus = 'pending' | 'in_progress' | 'completed' | 'abnormal' | 'cancelled'

// 巡检计划
export interface InspectionPlan {
  id: number
  planNo: string        // 计划编号
  name: string         // 计划名称
  zone: InspectionZone  // 巡检区域
  cycle: 'daily' | 'weekly' | 'monthly' | 'quarterly'  // 巡检周期
  startDate: string    // 开始日期
  endDate: string      // 结束日期
  frequency: number    // 每日巡检次数
  estimatedDuration: number  // 预计时长（分钟）
  inspectorIds: number[] // 巡检人员ID列表
  pointIds: number[]   // 巡检点ID列表
  status: PlanStatus   // 状态
  createdBy: string   // 创建人
  createdAt: string    // 创建时间
  remark: string      // 备注
}

// 巡检点
export interface InspectionPoint {
  id: number
  pointNo: string      // 点位编号
  name: string         // 点位名称
  type: InspectionPointType  // 点位类型
  location: string     // 位置描述
  building: string     // 楼栋
  floor: string        // 楼层
  photo: string        // 点位照片
  standard: string     // 巡检标准
  checkItems: string[] // 检查项目列表
  status: 'normal' | 'abnormal' | 'disabled'
  lastCheckDate: string  // 最近检查日期
  remark: string
}

// 巡检任务
export interface InspectionTask {
  id: number
  taskNo: string        // 任务编号
  planId: number        // 关联计划ID
  planName: string     // 计划名称
  zone: InspectionZone  // 巡检区域
  inspectorId: number  // 巡检人员ID
  inspectorName: string
  scheduledDate: string // 计划日期
  scheduledTime: string // 计划时间
  startTime: string    // 实际开始时间
  endTime: string      // 实际结束时间
  status: TaskStatus   // 状态
  points: number[]     // 巡检点ID列表
  completedPoints: { pointId: number; status: 'normal' | 'abnormal'; result: string; photo?: string; checkTime: string }[]
  abnormalCount: number // 异常数量
  remark: string
}

// 巡检记录（每日的实际巡检结果）
export interface InspectionRecord {
  id: number
  recordNo: string     // 记录编号
  taskId: number       // 关联任务ID
  taskNo: string      // 任务编号
  planId: number       // 关联计划ID
  planName: string    // 计划名称
  zone: InspectionZone // 巡检区域
  inspectorId: number  // 巡检人员ID
  inspectorName: string
  date: string        // 巡检日期
  startTime: string   // 开始时间
  endTime: string     // 结束时间
  duration: number    // 巡检时长（分钟）
  totalPoints: number // 总巡检点
  completedPoints: number // 完成点数
  abnormalPoints: number  // 异常点数
  abnormalDetails: { pointId: number; pointName: string; description: string; photo?: string }[]
  status: 'completed' | 'abnormal' | 'incomplete'
  handleStatus: 'pending' | 'processing' | 'resolved'  // 异常处理状态
  handleResult: string  // 处理结果
  handleTime: string   // 处理时间
  remark: string
}

// 区域标签
export const zoneLabels: Record<InspectionZone, string> = {
  'building': '楼栋',
  'parking': '停车场',
  'garden': '园林',
  'facility': '公共设施',
  'security': '安保区域',
  'other': '其他'
}

// 点位类型标签
export const pointTypeLabels: Record<InspectionPointType, string> = {
  'fire_equipment': '消防设备',
  'elevator': '电梯',
  'access_control': '门禁',
  'camera': '监控摄像头',
  'lighting': '照明设备',
  'water_electricity': '水电设施',
  'other': '其他'
}

// 周期标签
export const cycleLabels: Record<string, string> = {
  'daily': '每日',
  'weekly': '每周',
  'monthly': '每月',
  'quarterly': '每季度'
}

// 状态标签
export const statusLabels: Record<string, string> = {
  'draft': '草稿',
  'active': '进行中',
  'paused': '已暂停',
  'completed': '已完成',
  'pending': '待巡检',
  'in_progress': '巡检中',
  'normal': '正常',
  'abnormal': '异常',
  'cancelled': '已取消'
}

// 存储键名
const STORAGE_KEY_PLANS = 'wo_inspection_plans'
const STORAGE_KEY_POINTS = 'wo_inspection_points'
const STORAGE_KEY_TASKS = 'wo_inspection_tasks'
const STORAGE_KEY_RECORDS = 'wo_inspection_records'

// 从 localStorage 加载数据
const loadFromStorage = <T>(key: string, defaultValue: T): T => {
  try {
    const saved = localStorage.getItem(key)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed) || typeof parsed === 'object') {
        return parsed
      }
    }
  } catch (error) {
    console.error(`加载${key}数据失败:`, error)
  }
  return defaultValue
}

// 保存到 localStorage
const saveToStorage = <T>(key: string, data: T) => {
  try {
    localStorage.setItem(key, JSON.stringify(data))
  } catch (error) {
    console.error(`保存${key}数据失败:`, error)
  }
}

// 数据
const plans = ref<InspectionPlan[]>(loadFromStorage(STORAGE_KEY_PLANS, []))
const points = ref<InspectionPoint[]>(loadFromStorage(STORAGE_KEY_POINTS, []))
const tasks = ref<InspectionTask[]>(loadFromStorage(STORAGE_KEY_TASKS, []))
const records = ref<InspectionRecord[]>(loadFromStorage(STORAGE_KEY_RECORDS, []))

// 默认数据
if (plans.value.length === 0) {
  plans.value = [
    {
      id: 1,
      planNo: 'IP-2024-001',
      name: 'A栋日常巡检',
      zone: 'building',
      cycle: 'daily',
      startDate: '2024-04-01',
      endDate: '2025-03-31',
      frequency: 2,
      estimatedDuration: 30,
      inspectorIds: [1, 2],
      pointIds: [1, 2, 3],
      status: 'active',
      createdBy: 'admin',
      createdAt: '2024-04-01',
      remark: '每天上午下午各巡检一次'
    },
    {
      id: 2,
      planNo: 'IP-2024-002',
      name: '消防设备月度巡检',
      zone: 'security',
      cycle: 'monthly',
      startDate: '2024-04-01',
      endDate: '2025-03-31',
      frequency: 1,
      estimatedDuration: 120,
      inspectorIds: [3],
      pointIds: [4, 5, 6],
      status: 'active',
      createdBy: 'admin',
      createdAt: '2024-04-01',
      remark: '每月第一个周一进行'
    }
  ]
  saveToStorage(STORAGE_KEY_PLANS, plans.value)
}

if (points.value.length === 0) {
  points.value = [
    {
      id: 1,
      pointNo: 'P-A-001',
      name: 'A栋1单元大堂监控',
      type: 'camera',
      location: 'A栋1单元1楼大堂',
      building: 'A栋',
      floor: '1楼',
      photo: '',
      standard: '画面清晰，无遮挡',
      checkItems: ['画面质量', '角度', '录像存储'],
      status: 'normal',
      lastCheckDate: '2024-04-15',
      remark: ''
    },
    {
      id: 2,
      pointNo: 'P-A-002',
      name: 'A栋电梯监控',
      type: 'elevator',
      location: 'A栋电梯内部',
      building: 'A栋',
      floor: '电梯',
      photo: '',
      standard: '运行正常，有监控',
      checkItems: ['运行状态', '通话功能', '监控'],
      status: 'normal',
      lastCheckDate: '2024-04-15',
      remark: ''
    },
    {
      id: 3,
      pointNo: 'P-A-003',
      name: 'A栋1单元门禁',
      type: 'access_control',
      location: 'A栋1单元入口',
      building: 'A栋',
      floor: '1楼',
      photo: '',
      standard: '刷卡正常，开门顺畅',
      checkItems: ['刷卡功能', '密码功能', '开门速度'],
      status: 'normal',
      lastCheckDate: '2024-04-15',
      remark: ''
    },
    {
      id: 4,
      pointNo: 'P-F-001',
      name: 'A栋消防栓',
      type: 'fire_equipment',
      location: 'A栋每层走廊',
      building: 'A栋',
      floor: '各层',
      photo: '',
      standard: '配件齐全，压力正常',
      checkItems: ['配件齐全', '压力表', '外观检查'],
      status: 'normal',
      lastCheckDate: '2024-04-01',
      remark: ''
    },
    {
      id: 5,
      pointNo: 'P-F-002',
      name: 'B栋灭火器',
      type: 'fire_equipment',
      location: 'B栋每层走廊',
      building: 'B栋',
      floor: '各层',
      photo: '',
      standard: '压力正常，在有效期内',
      checkItems: ['压力表', '有效期', '外观'],
      status: 'normal',
      lastCheckDate: '2024-04-01',
      remark: ''
    }
  ]
  saveToStorage(STORAGE_KEY_POINTS, points.value)
}

let idCounters = {
  plan: Math.max(...plans.value.map(p => p.id), 0) + 1,
  point: Math.max(...points.value.map(p => p.id), 0) + 1,
  task: Math.max(...tasks.value.map(t => t.id), 0) + 1,
  record: Math.max(...records.value.map(r => r.id), 0) + 1
}

// 获取统计数据
export const getInspectionStats = () => {
  const today = new Date().toISOString().split('T')[0]
  const todayTasks = tasks.value.filter(t => t.scheduledDate === today)
  const completedToday = todayTasks.filter(t => t.status === 'completed').length
  const abnormalToday = todayTasks.filter(t => t.status === 'abnormal').length
  
  return {
    totalPlans: plans.value.length,
    activePlans: plans.value.filter(p => p.status === 'active').length,
    totalPoints: points.value.length,
    normalPoints: points.value.filter(p => p.status === 'normal').length,
    abnormalPoints: points.value.filter(p => p.status === 'abnormal').length,
    todayTasks: todayTasks.length,
    completedToday,
    abnormalToday,
    pendingTasks: tasks.value.filter(t => t.status === 'pending').length
  }
}

// 获取所有计划
export const getAllPlans = () => plans.value
export const getPlanById = (id: number) => plans.value.find(p => p.id === id)
export const getActivePlans = () => plans.value.filter(p => p.status === 'active')

// 添加计划
export const addPlan = (plan: Omit<InspectionPlan, 'id'>): InspectionPlan => {
  const newPlan: InspectionPlan = { ...plan, id: idCounters.plan++ }
  plans.value.push(newPlan)
  saveToStorage(STORAGE_KEY_PLANS, plans.value)
  return newPlan
}

// 更新计划
export const updatePlan = (id: number, updates: Partial<InspectionPlan>) => {
  const index = plans.value.findIndex(p => p.id === id)
  if (index !== -1) {
    plans.value[index] = { ...plans.value[index], ...updates }
    saveToStorage(STORAGE_KEY_PLANS, plans.value)
  }
}

// 删除计划
export const deletePlan = (id: number) => {
  const index = plans.value.findIndex(p => p.id === id)
  if (index !== -1) {
    plans.value.splice(index, 1)
    saveToStorage(STORAGE_KEY_PLANS, plans.value)
  }
}

// 获取所有巡检点
export const getAllPoints = () => points.value
export const getPointById = (id: number) => points.value.find(p => p.id === id)
export const getPointsByZone = (zone: InspectionZone) => points.value.filter(p => {
  // 需要通过计划关联区域，这里简化处理
  return true
})

// 添加巡检点
export const addPoint = (point: Omit<InspectionPoint, 'id'>): InspectionPoint => {
  const newPoint: InspectionPoint = { ...point, id: idCounters.point++ }
  points.value.push(newPoint)
  saveToStorage(STORAGE_KEY_POINTS, points.value)
  return newPoint
}

// 更新巡检点
export const updatePoint = (id: number, updates: Partial<InspectionPoint>) => {
  const index = points.value.findIndex(p => p.id === id)
  if (index !== -1) {
    points.value[index] = { ...points.value[index], ...updates }
    saveToStorage(STORAGE_KEY_POINTS, points.value)
  }
}

// 删除巡检点
export const deletePoint = (id: number) => {
  const index = points.value.findIndex(p => p.id === id)
  if (index !== -1) {
    points.value.splice(index, 1)
    saveToStorage(STORAGE_KEY_POINTS, points.value)
  }
}

// 获取所有任务
export const getAllTasks = () => tasks.value
export const getTaskById = (id: number) => tasks.value.find(t => t.id === id)
export const getTasksByDate = (date: string) => tasks.value.filter(t => t.scheduledDate === date)

// 添加任务
export const addTask = (task: Omit<InspectionTask, 'id'>): InspectionTask => {
  const newTask: InspectionTask = { ...task, id: idCounters.task++ }
  tasks.value.push(newTask)
  saveToStorage(STORAGE_KEY_TASKS, tasks.value)
  return newTask
}

// 更新任务
export const updateTask = (id: number, updates: Partial<InspectionTask>) => {
  const index = tasks.value.findIndex(t => t.id === id)
  if (index !== -1) {
    tasks.value[index] = { ...tasks.value[index], ...updates }
    saveToStorage(STORAGE_KEY_TASKS, tasks.value)
  }
}

// 完成巡检任务
export const completeTask = (id: number, completedPoints: InspectionTask['completedPoints']) => {
  const task = tasks.value.find(t => t.id === id)
  if (task) {
    task.status = 'completed'
    task.completedPoints = completedPoints
    task.endTime = new Date().toISOString()
    task.abnormalCount = completedPoints.filter(p => p.status === 'abnormal').length
    saveToStorage(STORAGE_KEY_TASKS, tasks.value)
  }
}

// 获取所有记录
export const getAllRecords = () => records.value
export const getRecordById = (id: number) => records.value.find(r => r.id === id)

// 添加记录
export const addRecord = (record: Omit<InspectionRecord, 'id'>): InspectionRecord => {
  const newRecord: InspectionRecord = { ...record, id: idCounters.record++ }
  records.value.push(newRecord)
  saveToStorage(STORAGE_KEY_RECORDS, records.value)
  return newRecord
}

export const inspectionStore = {
  plans,
  points,
  tasks,
  records,
  getInspectionStats,
  getAllPlans,
  getPlanById,
  getActivePlans,
  addPlan,
  updatePlan,
  deletePlan,
  getAllPoints,
  getPointById,
  addPoint,
  updatePoint,
  deletePoint,
  getAllTasks,
  getTaskById,
  getTasksByDate,
  addTask,
  updateTask,
  completeTask,
  getAllRecords,
  getRecordById,
  addRecord
}