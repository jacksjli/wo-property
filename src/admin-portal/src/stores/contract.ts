import { ref } from 'vue'

// 合同类型
export type ContractType = 'rental' | 'service' | 'procurement' | 'construction' | 'maintenance' | 'other'

// 合同状态
export type ContractStatus = 'draft' | 'active' | 'expired' | 'terminated' | 'renewed'

// 付款方式
export type PaymentMethod = 'one_time' | 'monthly' | 'quarterly' | 'yearly' | 'custom'

// 付款记录
export interface PaymentRecord {
  id: number
  contractId: number
  date: string
  amount: number
  method: PaymentMethod
  invoiceNo: string
  handler: string
  remark: string
}

// 合同文件
export interface ContractFile {
  id: number
  contractId: number
  name: string
  type: string
  url: string
  uploadDate: string
}

// 合同信息
export interface Contract {
  id: number
  contractNo: string      // 合同编号
  name: string           // 合同名称
  type: ContractType      // 合同类型
  partyA: string         // 甲方
  partyB: string         // 乙方
  contactPersonA: string  // 甲方联系人
  contactPersonB: string // 乙方联系人
  phoneA: string         // 甲方电话
  phoneB: string         // 乙方电话
  addressA: string        // 甲方地址
  addressB: string        // 乙方地址
  amount: number          // 合同金额
  paymentMethod: PaymentMethod  // 付款方式
  signDate: string        // 签订日期
  startDate: string       // 开始日期
  endDate: string         // 结束日期
  status: ContractStatus // 状态
  autoRenew: boolean      // 自动续约
  renewalPeriod: number   // 续约周期（月）
  nextRenewDate: string   // 下次续约日期
  description: string     // 合同描述
  remark: string         // 备注
  createdBy: string      // 创建人
  createdAt: string      // 创建时间
  paymentRecords: PaymentRecord[]   // 付款记录
  files: ContractFile[]  // 合同文件
}

// 类型标签
export const contractTypeLabels: Record<ContractType, string> = {
  'rental': '租赁合同',
  'service': '服务合同',
  'procurement': '采购合同',
  'construction': '施工合同',
  'maintenance': '维保合同',
  'other': '其他合同'
}

// 状态标签
export const contractStatusLabels: Record<ContractStatus, string> = {
  'draft': '草稿',
  'active': '执行中',
  'expired': '已到期',
  'terminated': '已终止',
  'renewed': '已续约'
}

// 付款方式标签
export const paymentMethodLabels: Record<PaymentMethod, string> = {
  'one_time': '一次性付款',
  'monthly': '月付',
  'quarterly': '季付',
  'yearly': '年付',
  'custom': '自定义'
}

// 存储键名
const STORAGE_KEY = 'wo_contracts'

// 从 localStorage 加载数据
const loadFromStorage = (): Contract[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载合同数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Contract[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存合同数据失败:', error)
  }
}

// 更新合同状态（根据日期）
const updateContractStatus = (contract: Contract) => {
  if (contract.status === 'terminated' || contract.status === 'renewed') return
  
  const today = new Date()
  const endDate = new Date(contract.endDate)
  
  if (endDate < today) {
    contract.status = 'expired'
  } else {
    contract.status = 'active'
  }
}

// 合同数据
const contracts = ref<Contract[]>(loadFromStorage())

// 如果没有数据，使用默认数据
if (contracts.value.length === 0) {
  contracts.value = [
    {
      id: 1,
      contractNo: 'CT-2024-001',
      name: '办公楼租赁合同',
      type: 'rental',
      partyA: '万科物业管理有限公司',
      partyB: '某某科技有限公司',
      contactPersonA: '张经理',
      contactPersonB: '李总',
      phoneA: '021-12345678',
      phoneB: '138-0000-1111',
      addressA: '浦东新区张江路100号',
      addressB: '徐汇区漕河泾开发区50号',
      amount: 500000,
      paymentMethod: 'yearly',
      signDate: '2024-01-01',
      startDate: '2024-02-01',
      endDate: '2025-01-31',
      status: 'active',
      autoRenew: true,
      renewalPeriod: 12,
      nextRenewDate: '2025-01-01',
      description: '办公楼租赁合同，面积为500平方米',
      remark: '含车位2个',
      createdBy: 'admin',
      createdAt: '2024-01-01',
      paymentRecords: [],
      files: []
    },
    {
      id: 2,
      contractNo: 'CT-2024-002',
      name: '电梯维保服务合同',
      type: 'maintenance',
      partyA: '万科物业管理有限公司',
      partyB: '奥的斯电梯维保公司',
      contactPersonA: '王主管',
      contactPersonB: '陈工程师',
      phoneA: '021-12345678',
      phoneB: '139-0000-2222',
      addressA: '浦东新区张江路100号',
      addressB: '静安区南京西路200号',
      amount: 120000,
      paymentMethod: 'quarterly',
      signDate: '2024-03-01',
      startDate: '2024-04-01',
      endDate: '2025-03-31',
      status: 'active',
      autoRenew: false,
      renewalPeriod: 12,
      nextRenewDate: '',
      description: '包含5部电梯的季度维保服务',
      remark: '每季度巡检4次',
      createdBy: 'admin',
      createdAt: '2024-03-01',
      paymentRecords: [],
      files: []
    },
    {
      id: 3,
      contractNo: 'CT-2024-003',
      name: '保洁服务合同',
      type: 'service',
      partyA: '万科物业管理有限公司',
      partyB: '洁邦保洁服务公司',
      contactPersonA: '王主管',
      contactPersonB: '刘经理',
      phoneA: '021-12345678',
      phoneB: '137-0000-3333',
      addressA: '浦东新区张江路100号',
      addressB: '浦东新区金科路300号',
      amount: 180000,
      paymentMethod: 'monthly',
      signDate: '2024-02-01',
      startDate: '2024-03-01',
      endDate: '2025-02-28',
      status: 'active',
      autoRenew: true,
      renewalPeriod: 12,
      nextRenewDate: '2025-02-01',
      description: '日常保洁服务，每天2次',
      remark: '包含节假日加班',
      createdBy: 'admin',
      createdAt: '2024-02-01',
      paymentRecords: [],
      files: []
    },
    {
      id: 4,
      contractNo: 'CT-2023-005',
      name: '消防设备采购合同',
      type: 'procurement',
      partyA: '万科物业管理有限公司',
      partyB: '海康威视科技有限公司',
      contactPersonA: '张经理',
      contactPersonB: '赵销售',
      phoneA: '021-12345678',
      phoneB: '136-0000-4444',
      addressA: '浦东新区张江路100号',
      addressB: '杭州市滨江区江南大道100号',
      amount: 350000,
      paymentMethod: 'one_time',
      signDate: '2023-06-01',
      startDate: '2023-07-01',
      endDate: '2024-06-30',
      status: 'expired',
      autoRenew: false,
      renewalPeriod: 0,
      nextRenewDate: '',
      description: '采购消防监控设备一批',
      remark: '已验收完成',
      createdBy: 'admin',
      createdAt: '2023-06-01',
      paymentRecords: [],
      files: []
    }
  ]
  saveToStorage(contracts.value)
}

let contractIdCounter = Math.max(...contracts.value.map(c => c.id), 0) + 1

// 获取所有合同
export const getAllContracts = () => contracts.value

// 获取指定合同
export const getContractById = (id: number) => contracts.value.find(c => c.id === id)

// 按类型获取合同
export const getContractsByType = (type: ContractType) => contracts.value.filter(c => c.type === type)

// 按状态获取合同
export const getContractsByStatus = (status: ContractStatus) => contracts.value.filter(c => c.status === status)

// 获取即将到期的合同（30天内）
export const getExpiringContracts = () => {
  const today = new Date()
  const thirtyDaysLater = new Date()
  thirtyDaysLater.setDate(today.getDate() + 30)
  
  return contracts.value.filter(c => {
    if (c.status !== 'active') return false
    const endDate = new Date(c.endDate)
    return endDate >= today && endDate <= thirtyDaysLater
  })
}

// 获取已到期的合同
export const getExpiredContracts = () => contracts.value.filter(c => c.status === 'expired')

// 获取合同总金额
export const getContractStats = () => {
  const total = contracts.value.length
  const active = contracts.value.filter(c => c.status === 'active').length
  const expiring = getExpiringContracts().length
  const expired = contracts.value.filter(c => c.status === 'expired').length
  const totalAmount = contracts.value.reduce((sum, c) => sum + c.amount, 0)
  const activeAmount = contracts.value.filter(c => c.status === 'active').reduce((sum, c) => sum + c.amount, 0)
  
  return { total, active, expiring, expired, totalAmount, activeAmount }
}

// 添加合同
export const addContract = (contract: Omit<Contract, 'id' | 'paymentRecords' | 'files'>): Contract => {
  const newContract: Contract = {
    ...contract,
    id: contractIdCounter++,
    paymentRecords: [],
    files: []
  }
  updateContractStatus(newContract)
  contracts.value.push(newContract)
  saveToStorage(contracts.value)
  return newContract
}

// 更新合同
export const updateContract = (id: number, updates: Partial<Contract>) => {
  const index = contracts.value.findIndex(c => c.id === id)
  if (index !== -1) {
    contracts.value[index] = { ...contracts.value[index], ...updates }
    updateContractStatus(contracts.value[index])
    saveToStorage(contracts.value)
  }
}

// 删除合同
export const deleteContract = (id: number) => {
  const index = contracts.value.findIndex(c => c.id === id)
  if (index !== -1) {
    contracts.value.splice(index, 1)
    saveToStorage(contracts.value)
  }
}

// 终止合同
export const terminateContract = (id: number, reason: string) => {
  const contract = contracts.value.find(c => c.id === id)
  if (contract) {
    contract.status = 'terminated'
    contract.remark = contract.remark + `\n终止原因：${reason}`
    saveToStorage(contracts.value)
  }
}

// 续约合同
export const renewContract = (id: number, newEndDate: string, newAmount?: number) => {
  const contract = contracts.value.find(c => c.id === id)
  if (contract) {
    contract.status = 'renewed'
    // 创建新合同
    const newContract: Contract = {
      ...contract,
      id: contractIdCounter++,
      contractNo: contract.contractNo + '-R',
      startDate: new Date(new Date(contract.endDate).getTime() + 86400000).toISOString().split('T')[0],
      endDate: newEndDate,
      status: 'active',
      amount: newAmount || contract.amount,
      createdAt: new Date().toISOString().split('T')[0],
      paymentRecords: [],
      files: []
    }
    if (contract.autoRenew && contract.renewalPeriod > 0) {
      const renewDate = new Date(newEndDate)
      renewDate.setMonth(renewDate.getMonth() - 1)
      newContract.nextRenewDate = renewDate.toISOString().split('T')[0]
    }
    contracts.value.push(newContract)
    saveToStorage(contracts.value)
    return newContract
  }
  return null
}

// 添加付款记录
export const addPaymentRecord = (contractId: number, record: Omit<PaymentRecord, 'id' | 'contractId'>) => {
  const contract = contracts.value.find(c => c.id === contractId)
  if (contract) {
    const newRecord: PaymentRecord = {
      ...record,
      id: Date.now(),
      contractId
    }
    contract.paymentRecords.push(newRecord)
    saveToStorage(contracts.value)
    return newRecord
  }
  return null
}

// 删除付款记录
export const deletePaymentRecord = (contractId: number, recordId: number) => {
  const contract = contracts.value.find(c => c.id === contractId)
  if (contract) {
    const index = contract.paymentRecords.findIndex(r => r.id === recordId)
    if (index !== -1) {
      contract.paymentRecords.splice(index, 1)
      saveToStorage(contracts.value)
    }
  }
}

// 格式化金额
export const formatAmount = (amount: number) => {
  return `¥${amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

export const contractStore = {
  contracts,
  getAllContracts,
  getContractById,
  getContractsByType,
  getContractsByStatus,
  getExpiringContracts,
  getExpiredContracts,
  getContractStats,
  addContract,
  updateContract,
  deleteContract,
  terminateContract,
  renewContract,
  addPaymentRecord,
  deletePaymentRecord,
  formatAmount
}