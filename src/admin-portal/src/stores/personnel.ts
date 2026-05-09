import { ref } from 'vue'

// 人员角色类型
export type PersonnelRole = 'operator' | 'supervisor' | 'manager' | 'department_head' | 'company_head'

// 人员状态
export type PersonnelStatus = 'active' | 'inactive' | 'on_leave' | 'probation' | 'resigned'

// 性别
export type Gender = 'male' | 'female' | 'other'

// 学历
export type Education = 'high_school' | 'college' | 'bachelor' | 'master' | 'doctor'

// 编制类型
export type EmploymentType = 'full_time' | 'part_time' | 'contract' | 'intern'

// 紧急联系人
export interface EmergencyContact {
  name: string
  relationship: string
  phone: string
}

// 备份人员
export interface BackupStaff {
  staffId: number
  staffName: string
}

// 部门
export interface Department {
  id: number
  name: string
  managerId?: number
}

// 人员信息
export interface Personnel {
  id: number
  employeeNo: string          // 工号
  name: string              // 姓名
  avatar?: string           // 头像
  gender: Gender           // 性别
  birthday?: string        // 出生日期
  idCard: string          // 身份证号
  phone: string           // 联系电话
  emergencyContact?: EmergencyContact  // 紧急联系人
  email?: string          // 邮箱
  address?: string        // 住址
  education: Education   // 学历
  graduateSchool?: string  // 毕业院校
  major?: string          // 专业
  role: PersonnelRole   // 角色
  departmentId?: number  // 部门ID
  departmentName?: string // 部门名称
  position: string        // 职位
  employmentType: EmploymentType  // 编制类型
  hireDate: string       // 入职日期
  contractStart?: string // 合同开始日期
  contractEnd?: string   // 合同结束日期
  salary?: number        // 月薪
  bankAccount?: string   // 银行账号
  socialSecurityNo?: string  // 社保账号
  status: PersonnelStatus  // 状态
  specialties: string[] // 专业技能/专长
  backups: BackupStaff[]  // 备份人员列表
  attendanceCount: number  // 出勤天数
  overtimeHours: number   // 加班时长
  leaveDays: number      // 请假天数
  performanceScore?: number  // 绩效评分
  trainingCount: number  // 培训次数
  remark?: string        // 备注
  createdAt: string    // 创建时间
  updatedAt: string    // 更新时间
}

// 角色等级
export const roleLevels: Record<PersonnelRole, number> = {
  'operator': 1,
  'supervisor': 2,
  'manager': 3,
  'department_head': 4,
  'company_head': 5
}

// 角色标签
export const roleLabels: Record<PersonnelRole, string> = {
  'operator': '操作人员',
  'supervisor': '主管',
  'manager': '经理',
  'department_head': '部门负责人',
  'company_head': '公司负责人'
}

// 状态标签
export const statusLabels: Record<PersonnelStatus, string> = {
  'active': '在职',
  'inactive': '离职',
  'on_leave': '休假中',
  'probation': '试用期',
  'resigned': '已辞职'
}

// 性别标签
export const genderLabels: Record<Gender, string> = {
  'male': '男',
  'female': '女',
  'other': '其他'
}

// 学历标签
export const educationLabels: Record<Education, string> = {
  'high_school': '高中',
  'college': '大专',
  'bachelor': '本科',
  'master': '硕士',
  'doctor': '博士'
}

// 编制类型标签
export const employmentTypeLabels: Record<EmploymentType, string> = {
  'full_time': '全职',
  'part_time': '兼职',
  'contract': '合同制',
  'intern': '实习生'
}

// 专业技能选项
export const specialtyOptions = [
  '电梯维修', '水电维修', '消防设备', '门禁系统', '监控系统',
  '空调维修', '给排水', '强弱电', '日常保养', '设备巡检',
  '应急处理', '客户服务', '安全管理', '环境清洁', '绿化养护'
]

// 部门列表
export const departments: Department[] = [
  { id: 1, name: '工程部' },
  { id: 2, name: '安保部' },
  { id: 3, name: '客服部' },
  { id: 4, name: '财务部' },
  { id: 5, name: '行政部' },
  { id: 6, name: '保洁部' },
  { id: 7, name: '绿化部' }
]

// 存储键名
const STORAGE_KEY = 'wo_personnel'

// 从 localStorage 加载数据
const loadFromStorage = (): Personnel[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载人员数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Personnel[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存人员数据失败:', error)
  }
}

// 数据
const personnelList = ref<Personnel[]>(loadFromStorage())

// 默认数据
if (personnelList.value.length === 0) {
  const today = new Date().toISOString().split('T')[0]
  personnelList.value = [
    {
      id: 1,
      employeeNo: 'EMP001',
      name: '张师傅',
      gender: 'male',
      birthday: '1980-05-15',
      idCard: '310101198005151234',
      phone: '138-0000-1111',
      emergencyContact: { name: '张小梅', relationship: '配偶', phone: '139-0000-1112' },
      email: 'zhangsf@woproperty.com',
      address: '浦东新区张江路100号',
      education: 'high_school',
      graduateSchool: '',
      major: '',
      role: 'operator',
      departmentId: 1,
      departmentName: '工程部',
      position: '维修技师',
      employmentType: 'full_time',
      hireDate: '2020-01-15',
      contractStart: '2020-01-15',
      contractEnd: '2025-01-14',
      salary: 8000,
      bankAccount: '6222021234567890',
      socialSecurityNo: '3101234567',
      status: 'active',
      specialties: ['电梯维修', '水电维修', '设备巡检'],
      backups: [
        { staffId: 2, staffName: '李师傅' },
        { staffId: 3, staffName: '王师傅' }
      ],
      attendanceCount: 95,
      overtimeHours: 12,
      leaveDays: 3,
      performanceScore: 4.5,
      trainingCount: 4,
      remark: '高级技师',
      createdAt: '2020-01-15',
      updatedAt: today
    },
    {
      id: 2,
      employeeNo: 'EMP002',
      name: '李师傅',
      gender: 'male',
      birthday: '1985-08-20',
      idCard: '310101198508201234',
      phone: '138-0000-2222',
      emergencyContact: { name: '李小红', relationship: '配偶', phone: '139-0000-2223' },
      education: 'college',
      role: 'operator',
      departmentId: 1,
      departmentName: '工程部',
      position: '维修技师',
      employmentType: 'full_time',
      hireDate: '2021-03-01',
      salary: 7000,
      status: 'active',
      specialties: ['水电维修', '给排水', '应急处理'],
      backups: [
        { staffId: 1, staffName: '张师傅' }
      ],
      attendanceCount: 90,
      overtimeHours: 8,
      leaveDays: 5,
      performanceScore: 4.2,
      trainingCount: 3,
      remark: '',
      createdAt: '2021-03-01',
      updatedAt: today
    },
    {
      id: 3,
      employeeNo: 'EMP003',
      name: '王主管',
      gender: 'male',
      birthday: '1978-03-10',
      idCard: '310101197803101234',
      phone: '138-0000-3333',
      emergencyContact: { name: '王小丽', relationship: '配偶', phone: '139-0000-3334' },
      education: 'bachelor',
      graduateSchool: '上海大学',
      major: '机电一体化',
      role: 'supervisor',
      departmentId: 1,
      departmentName: '工程部',
      position: '工程主管',
      employmentType: 'full_time',
      hireDate: '2019-06-01',
      salary: 12000,
      status: 'active',
      specialties: ['消防设备', '门禁系统', '监控系统', '安全管理'],
      backups: [
        { staffId: 4, staffName: '赵经理' }
      ],
      attendanceCount: 98,
      overtimeHours: 20,
      leaveDays: 2,
      performanceScore: 4.8,
      trainingCount: 6,
      remark: '注册安全工程师',
      createdAt: '2019-06-01',
      updatedAt: today
    },
    {
      id: 4,
      employeeNo: 'EMP004',
      name: '赵经理',
      gender: 'male',
      birthday: '1975-11-25',
      idCard: '310101197511251234',
      phone: '138-0000-4444',
      education: 'master',
      graduateSchool: '复旦大学',
      major: '企业管理',
      role: 'manager',
      departmentId: 1,
      departmentName: '工程部',
      position: '工程部经理',
      employmentType: 'full_time',
      hireDate: '2018-01-01',
      salary: 20000,
      status: 'active',
      specialties: ['团队管理', '设备管理', '安全管理', '客户服务'],
      backups: [
        { staffId: 5, staffName: '陈总监' }
      ],
      attendanceCount: 100,
      overtimeHours: 30,
      leaveDays: 0,
      performanceScore: 4.9,
      trainingCount: 8,
      remark: 'MBA',
      createdAt: '2018-01-01',
      updatedAt: today
    },
    {
      id: 5,
      employeeNo: 'EMP005',
      name: '陈总监',
      gender: 'male',
      birthday: '1970-06-18',
      idCard: '310101197006181234',
      phone: '138-0000-5555',
      education: 'master',
      graduateSchool: '中欧国际工商学院',
      major: '工商管理',
      role: 'department_head',
      departmentId: 1,
      departmentName: '工程部',
      position: '工程部总监',
      employmentType: 'full_time',
      hireDate: '2015-01-01',
      salary: 35000,
      status: 'active',
      specialties: ['战略规划', '团队建设', '资源整合', '风险控制'],
      backups: [
        { staffId: 6, staffName: '李总' }
      ],
      attendanceCount: 100,
      overtimeHours: 50,
      leaveDays: 0,
      performanceScore: 5.0,
      trainingCount: 12,
      remark: '',
      createdAt: '2015-01-01',
      updatedAt: today
    },
    {
      id: 6,
      employeeNo: 'EMP006',
      name: '李总',
      gender: 'male',
      birthday: '1965-02-28',
      idCard: '310101196502281234',
      phone: '138-0000-6666',
      education: 'doctor',
      graduateSchool: '清华大学',
      major: '物业管理',
      role: 'company_head',
      departmentId: 1,
      departmentName: '工程部',
      position: '总经理',
      employmentType: 'full_time',
      hireDate: '2010-01-01',
      salary: 80000,
      status: 'active',
      specialties: ['战略管理', '品牌建设', '资本运营'],
      backups: [],
      attendanceCount: 100,
      overtimeHours: 0,
      leaveDays: 0,
      performanceScore: 5.0,
      trainingCount: 20,
      remark: '公司创始人',
      createdAt: '2010-01-01',
      updatedAt: today
    },
    {
      id: 7,
      employeeNo: 'EMP007',
      name: '小林',
      gender: 'female',
      birthday: '1995-09-10',
      idCard: '310101199509101234',
      phone: '138-0000-7777',
      education: 'bachelor',
      graduateSchool: '华东师范大学',
      major: '物业管理',
      role: 'operator',
      departmentId: 3,
      departmentName: '客服部',
      position: '客服专员',
      employmentType: 'full_time',
      hireDate: '2023-07-01',
      salary: 6000,
      status: 'probation',
      specialties: ['客户服务', '投诉处理'],
      backups: [],
      attendanceCount: 30,
      overtimeHours: 2,
      leaveDays: 1,
      performanceScore: 3.8,
      trainingCount: 2,
      remark: '试用期',
      createdAt: '2023-07-01',
      updatedAt: today
    }
  ]
  saveToStorage(personnelList.value)
}

let personnelIdCounter = Math.max(...personnelList.value.map(p => p.id), 0) + 1

// 获取统计数据
export const getPersonnelStats = () => {
  return {
    total: personnelList.value.length,
    active: personnelList.value.filter(p => p.status === 'active').length,
    inactive: personnelList.value.filter(p => p.status === 'inactive').length,
    onLeave: personnelList.value.filter(p => p.status === 'on_leave').length,
    probation: personnelList.value.filter(p => p.status === 'probation').length,
    byRole: {
      operator: personnelList.value.filter(p => p.role === 'operator').length,
      supervisor: personnelList.value.filter(p => p.role === 'supervisor').length,
      manager: personnelList.value.filter(p => p.role === 'manager').length,
      departmentHead: personnelList.value.filter(p => p.role === 'department_head').length,
      companyHead: personnelList.value.filter(p => p.role === 'company_head').length
    }
  }
}

// 获取所有人员
export const getAllPersonnel = () => personnelList.value

// 获取指定人员
export const getPersonnelById = (id: number) => personnelList.value.find(p => p.id === id)

// 按角色获取人员
export const getPersonnelByRole = (role: PersonnelRole) => 
  personnelList.value.filter(p => p.role === role)

// 按部门获取人员
export const getPersonnelByDepartment = (departmentId: number) => 
  personnelList.value.filter(p => p.departmentId === departmentId)

// 获取在职人员
export const getActivePersonnel = () => 
  personnelList.value.filter(p => p.status === 'active' || p.status === 'on_leave')

// 添加人员
export const addPersonnel = (person: Omit<Personnel, 'id' | 'createdAt' | 'updatedAt'>): Personnel => {
  const now = new Date().toISOString()
  const newPerson: Personnel = {
    ...person,
    id: personnelIdCounter++,
    createdAt: now,
    updatedAt: now,
    attendanceCount: 0,
    overtimeHours: 0,
    leaveDays: 0,
    trainingCount: 0
  }
  personnelList.value.push(newPerson)
  saveToStorage(personnelList.value)
  return newPerson
}

// 更新人员
export const updatePersonnel = (id: number, updates: Partial<Personnel>) => {
  const index = personnelList.value.findIndex(p => p.id === id)
  if (index !== -1) {
    personnelList.value[index] = { 
      ...personnelList.value[index], 
      ...updates,
      updatedAt: new Date().toISOString()
    }
    saveToStorage(personnelList.value)
  }
}

// 删除人员
export const deletePersonnel = (id: number) => {
  const index = personnelList.value.findIndex(p => p.id === id)
  if (index !== -1) {
    personnelList.value.splice(index, 1)
    saveToStorage(personnelList.value)
  }
}

// 添加备份人员
export const addBackup = (staffId: number, backupStaffId: number, backupStaffName: string) => {
  const person = personnelList.value.find(p => p.id === staffId)
  if (person) {
    const exists = person.backups.some(b => b.staffId === backupStaffId)
    if (!exists) {
      person.backups.push({ staffId: backupStaffId, staffName: backupStaffName })
      saveToStorage(personnelList.value)
    }
  }
}

// 移除备份人员
export const removeBackup = (staffId: number, backupStaffId: number) => {
  const person = personnelList.value.find(p => p.id === staffId)
  if (person) {
    const index = person.backups.findIndex(b => b.staffId === backupStaffId)
    if (index !== -1) {
      person.backups.splice(index, 1)
      saveToStorage(personnelList.value)
    }
  }
}

// 获取状态颜色
export const getStatusColor = (status: PersonnelStatus) => {
  const colors: Record<PersonnelStatus, string> = {
    active: '#67C23A',
    inactive: '#909399',
    on_leave: '#E6A23C',
    probation: '#409EFF',
    resigned: '#F56C6C'
  }
  return colors[status]
}

// 获取状态类型
export const getStatusType = (status: PersonnelStatus) => {
  const types: Record<PersonnelStatus, string> = {
    active: 'success',
    inactive: 'info',
    on_leave: 'warning',
    probation: 'primary',
    resigned: 'danger'
  }
  return types[status]
}

export const personnelStore = {
  personnelList,
  getPersonnelStats,
  getAllPersonnel,
  getPersonnelById,
  getPersonnelByRole,
  getPersonnelByDepartment,
  getActivePersonnel,
  addPersonnel,
  updatePersonnel,
  deletePersonnel,
  addBackup,
  removeBackup,
  getStatusColor,
  getStatusType
}