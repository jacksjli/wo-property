import { ref } from 'vue'

// 人员角色类型
export type StaffRole = 'primary' | 'backup' | 'supervisor'

// 领导信息
export interface Leader {
  id: number
  staffId: number
  staffName: string
  level: number
}

// 人员信息
export interface StaffMember {
  id: number
  name: string
  phone: string
  specialty: string[]
  role: StaffRole
  status: 'Active' | 'Inactive'
  leaders: Leader[]
}

// 人员数据
const staffMembers = ref<StaffMember[]>([
  { 
    id: 1, 
    name: '张师傅', 
    phone: '13800138001', 
    specialty: ['电梯', '设备维修'], 
    role: 'primary', 
    status: 'Active', 
    leaders: [
      { id: 1, staffId: 3, staffName: '王师傅', level: 1 },
      { id: 2, staffId: 4, staffName: '赵班长', level: 2 }
    ]
  },
  { 
    id: 2, 
    name: '李师傅', 
    phone: '13800138002', 
    specialty: ['水电', '日常维修'], 
    role: 'primary', 
    status: 'Active', 
    leaders: [
      { id: 3, staffId: 3, staffName: '王师傅', level: 1 }
    ]
  },
  { 
    id: 3, 
    name: '王师傅', 
    phone: '13800138003', 
    specialty: ['消防', '设备维修'], 
    role: 'backup', 
    status: 'Active', 
    leaders: [
      { id: 4, staffId: 4, staffName: '赵班长', level: 1 }
    ]
  },
  { 
    id: 4, 
    name: '赵班长', 
    phone: '13800138004', 
    specialty: ['电梯', '水电', '消防'], 
    role: 'supervisor', 
    status: 'Active', 
    leaders: []
  },
])

// ID计数器
let nextId = 5

// ==================== 人员管理 ====================

// 获取所有人员
export const getAllStaff = () => staffMembers.value

// 获取在职人员
export const getActiveStaff = () => staffMembers.value.filter(s => s.status === 'Active')

// 按角色获取人员
export const getStaffByRole = (role: StaffRole) => 
  staffMembers.value.filter(s => s.role === role && s.status === 'Active')

// 按专长获取人员
export const getStaffBySpecialty = (specialty: string) =>
  staffMembers.value.filter(s => 
    s.status === 'Active' && s.specialty.includes(specialty)
  )

// 添加人员
export const addStaff = (staff: Omit<StaffMember, 'id'>): StaffMember => {
  const newStaff: StaffMember = {
    ...staff,
    id: nextId++,
    leaders: staff.leaders || []
  }
  staffMembers.value.push(newStaff)
  return newStaff
}

// 更新人员
export const updateStaff = (id: number, updates: Partial<StaffMember>) => {
  const index = staffMembers.value.findIndex(s => s.id === id)
  if (index !== -1) {
    staffMembers.value[index] = { ...staffMembers.value[index], ...updates }
  }
}

// 删除人员
export const deleteStaff = (id: number) => {
  const index = staffMembers.value.findIndex(s => s.id === id)
  if (index !== -1) {
    staffMembers.value.splice(index, 1)
  }
}

// 切换人员状态
export const toggleStaffStatus = (id: number) => {
  const staff = staffMembers.value.find(s => s.id === id)
  if (staff) {
    staff.status = staff.status === 'Active' ? 'Inactive' : 'Active'
  }
}

// ==================== 领导链管理 ====================

let leaderIdCounter = 100

// 添加领导
export const addLeader = (staffId: number, leaderStaffId: number): Leader | null => {
  const staff = staffMembers.value.find(s => s.id === staffId)
  const leader = staffMembers.value.find(s => s.id === leaderStaffId)
  if (!staff || !leader) return null
  
  if (staff.leaders.some(l => l.staffId === leaderStaffId)) {
    return null
  }
  
  const newLeader: Leader = {
    id: leaderIdCounter++,
    staffId: leaderStaffId,
    staffName: leader.name,
    level: staff.leaders.length + 1
  }
  
  staff.leaders.push(newLeader)
  return newLeader
}

// 移除领导
export const removeLeader = (staffId: number, leaderId: number): boolean => {
  const staff = staffMembers.value.find(s => s.id === staffId)
  if (!staff) return false
  
  const index = staff.leaders.findIndex(l => l.id === leaderId)
  if (index !== -1) {
    staff.leaders.splice(index, 1)
    staff.leaders.forEach((l, i) => { l.level = i + 1 })
    return true
  }
  return false
}

// 调整领导顺序
export const reorderLeaders = (staffId: number, fromIndex: number, toIndex: number): boolean => {
  const staff = staffMembers.value.find(s => s.id === staffId)
  if (!staff) return false
  
  const [leader] = staff.leaders.splice(fromIndex, 1)
  staff.leaders.splice(toIndex, 0, leader)
  staff.leaders.forEach((l, i) => { l.level = i + 1 })
  return true
}

// 获取人员领导链
export const getLeaders = (staffId: number): Leader[] => {
  const staff = staffMembers.value.find(s => s.id === staffId)
  return staff?.leaders || []
}

// 获取第N级领导
export const getNthLeader = (staffId: number, level: number): Leader | null => {
  const leaders = getLeaders(staffId)
  return leaders.find(l => l.level === level) || null
}

// ==================== 导出 ====================

export const staffStore = {
  staffMembers,
  getAllStaff,
  getActiveStaff,
  getStaffByRole,
  getStaffBySpecialty,
  addStaff,
  updateStaff,
  deleteStaff,
  toggleStaffStatus,
  addLeader,
  removeLeader,
  reorderLeaders,
  getLeaders,
  getNthLeader
}
