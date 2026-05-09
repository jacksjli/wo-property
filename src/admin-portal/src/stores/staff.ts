import { ref } from 'vue'

// 人员角色类型
export type StaffRole = 'operator' | 'supervisor' | 'manager' | 'department_head' | 'company_head'

// 角色等级（数字越大等级越高）
export const roleLevels: Record<StaffRole, number> = {
  'operator': 1,
  'supervisor': 2,
  'manager': 3,
  'department_head': 4,
  'company_head': 5
}

// 角色标签
export const roleLabels: Record<StaffRole, string> = {
  'operator': '操作人员',
  'supervisor': '主管',
  'manager': '经理',
  'department_head': '部门负责人',
  'company_head': '公司负责人'
}

// 备份人员
export interface BackupStaff {
  staffId: number
  staffName: string
}

// 人员信息
export interface StaffMember {
  id: number
  name: string
  phone: string
  specialty: string[]
  role: StaffRole
  status: 'Active' | 'Inactive'
  backups: BackupStaff[]  // 备份人员列表
}

// 存储键名
const STORAGE_KEY = 'wo_staff_members'

// 从 localStorage 加载数据
const loadFromStorage = (): StaffMember[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      // 确保数据格式正确
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed.map(s => ({
          ...s,
          status: s.status || 'Active',
          backups: s.backups || []
        }))
      }
    }
  } catch (error) {
    console.error('加载人员数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: StaffMember[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存人员数据失败:', error)
  }
}

// 人员数据
const staffMembers = ref<StaffMember[]>(loadFromStorage())

// 如果没有数据，使用默认数据
if (staffMembers.value.length === 0) {
  staffMembers.value = [
    { 
      id: 1, 
      name: '张师傅', 
      phone: '13800138001', 
      specialty: ['电梯', '设备维修'], 
      role: 'operator', 
      status: 'Active', 
      backups: [
        { staffId: 3, staffName: '王师傅' },
        { staffId: 4, staffName: '赵班长' }
      ]
    },
    { 
      id: 2, 
      name: '李师傅', 
      phone: '13800138002', 
      specialty: ['水电', '日常维修'], 
      role: 'operator', 
      status: 'Active', 
      backups: [
        { staffId: 3, staffName: '王师傅' }
      ]
    },
    { 
      id: 3, 
      name: '王师傅', 
      phone: '13800138003', 
      specialty: ['消防', '门禁'], 
      role: 'supervisor', 
      status: 'Active', 
      backups: [
        { staffId: 4, staffName: '赵班长' }
      ]
    },
    { 
      id: 4, 
      name: '赵班长', 
      phone: '13800138004', 
      specialty: ['管理', '协调'], 
      role: 'supervisor', 
      status: 'Active', 
      backups: [
        { staffId: 5, staffName: '刘经理' }
      ]
    },
    { 
      id: 5, 
      name: '刘经理', 
      phone: '13800138005', 
      specialty: ['管理'], 
      role: 'manager', 
      status: 'Active', 
      backups: [
        { staffId: 6, staffName: '陈总监' }
      ]
    },
    { 
      id: 6, 
      name: '陈总监', 
      phone: '13800138006', 
      specialty: ['管理'], 
      role: 'department_head', 
      status: 'Active', 
      backups: [
        { staffId: 7, staffName: '李总' }
      ]
    },
    { 
      id: 7, 
      name: '李总', 
      phone: '13800138007', 
      specialty: ['管理'], 
      role: 'company_head', 
      status: 'Active', 
      backups: []
    }
  ]
  saveToStorage(staffMembers.value)
}

let staffIdCounter = Math.max(...staffMembers.value.map(s => s.id), 0) + 1

// 获取所有人员
export const getAllStaff = () => staffMembers.value

// 获取启用人员
export const getActiveStaff = () => staffMembers.value.filter(s => s.status === 'Active')

// 添加人员
export const addStaff = (staff: Omit<StaffMember, 'id'>): StaffMember => {
  const newStaff: StaffMember = { ...staff, id: staffIdCounter++ }
  staffMembers.value.push(newStaff)
  saveToStorage(staffMembers.value)  // 自动保存
  return newStaff
}

// 更新人员
export const updateStaff = (id: number, updates: Partial<StaffMember>) => {
  const index = staffMembers.value.findIndex(s => s.id === id)
  if (index !== -1) {
    staffMembers.value[index] = { ...staffMembers.value[index], ...updates }
    saveToStorage(staffMembers.value)  // 自动保存
  }
}

// 删除人员
export const deleteStaff = (id: number) => {
  const index = staffMembers.value.findIndex(s => s.id === id)
  if (index !== -1) {
    staffMembers.value.splice(index, 1)
    saveToStorage(staffMembers.value)  // 自动保存
  }
}

// 切换人员状态
export const toggleStaffStatus = (id: number) => {
  const staff = staffMembers.value.find(s => s.id === id)
  if (staff) {
    staff.status = staff.status === 'Active' ? 'Inactive' : 'Active'
    saveToStorage(staffMembers.value)  // 自动保存
  }
}

// 添加备份人员
export const addBackup = (staffId: number, backupStaffId: number, backupStaffName: string) => {
  const staff = staffMembers.value.find(s => s.id === staffId)
  if (staff) {
    // 检查是否已存在
    const exists = staff.backups.some(b => b.staffId === backupStaffId)
    if (!exists) {
      staff.backups.push({ staffId: backupStaffId, staffName: backupStaffName })
      saveToStorage(staffMembers.value)  // 自动保存
    }
  }
}

// 移除备份人员
export const removeBackup = (staffId: number, backupStaffId: number) => {
  const staff = staffMembers.value.find(s => s.id === staffId)
  if (staff) {
    const index = staff.backups.findIndex(b => b.staffId === backupStaffId)
    if (index !== -1) {
      staff.backups.splice(index, 1)
      saveToStorage(staffMembers.value)  // 自动保存
    }
  }
}

// 获取按角色筛选的人员
export const getStaffByRole = (role: StaffRole) => {
  return staffMembers.value.filter(s => s.role === role && s.status === 'Active')
}

// 重置为默认
export const resetStaff = () => {
  staffMembers.value = [
    { 
      id: 1, 
      name: '张师傅', 
      phone: '13800138001', 
      specialty: ['电梯', '设备维修'], 
      role: 'operator', 
      status: 'Active', 
      backups: [
        { staffId: 3, staffName: '王师傅' },
        { staffId: 4, staffName: '赵班长' }
      ]
    },
    { 
      id: 2, 
      name: '李师傅', 
      phone: '13800138002', 
      specialty: ['水电', '日常维修'], 
      role: 'operator', 
      status: 'Active', 
      backups: [
        { staffId: 3, staffName: '王师傅' }
      ]
    },
    { 
      id: 3, 
      name: '王师傅', 
      phone: '13800138003', 
      specialty: ['消防', '门禁'], 
      role: 'supervisor', 
      status: 'Active', 
      backups: [
        { staffId: 4, staffName: '赵班长' }
      ]
    },
    { 
      id: 4, 
      name: '赵班长', 
      phone: '13800138004', 
      specialty: ['管理', '协调'], 
      role: 'supervisor', 
      status: 'Active', 
      backups: [
        { staffId: 5, staffName: '刘经理' }
      ]
    },
    { 
      id: 5, 
      name: '刘经理', 
      phone: '13800138005', 
      specialty: ['管理'], 
      role: 'manager', 
      status: 'Active', 
      backups: [
        { staffId: 6, staffName: '陈总监' }
      ]
    },
    { 
      id: 6, 
      name: '陈总监', 
      phone: '13800138006', 
      specialty: ['管理'], 
      role: 'department_head', 
      status: 'Active', 
      backups: [
        { staffId: 7, staffName: '李总' }
      ]
    },
    { 
      id: 7, 
      name: '李总', 
      phone: '13800138007', 
      specialty: ['管理'], 
      role: 'company_head', 
      status: 'Active', 
      backups: []
    }
  ]
  staffIdCounter = 8
  saveToStorage(staffMembers.value)
}

export const staffStore = {
  staffMembers,
  getAllStaff,
  getActiveStaff,
  addStaff,
  updateStaff,
  deleteStaff,
  toggleStaffStatus,
  addBackup,
  removeBackup,
  getStaffByRole,
  resetStaff
}