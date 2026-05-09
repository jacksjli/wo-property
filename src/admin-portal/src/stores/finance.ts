import { ref } from 'vue'

// 收支类型
export type TransactionType = 'income' | 'expense'

// 收支分类
export type TransactionCategory = 
  | 'property_fee'        // 物业费
  | 'parking_fee'         // 车位费
  | 'water_fee'           // 水费
  | 'electric_fee'         // 电费
  | 'gas_fee'             // 燃气费
  | 'heating_fee'         // 暖气费
  | 'public_fee'          // 公摊费
  | 'maintenance_fee'      // 维修费
  | 'cleaning_fee'        // 清洁费
  | 'security_fee'         // 安保费
  | 'landscape_fee'       // 绿化费
  | 'office_fee'          // 办公费
  | 'salary'              // 工资
  | 'material'            // 材料费
  | 'equipment'           // 设备费
  | 'other'               // 其他

// 支付方式
export type PaymentMethod = 'cash' | 'transfer' | 'wechat' | 'alipay' | 'card' | 'other'

// 状态
export type TransactionStatus = 'pending' | 'completed' | 'cancelled' | 'refunded'

// 账单记录
export interface BillRecord {
  id: number
  billNo: string           // 单据编号
  residentName: string     // 住户名称
  roomNo: string          // 房号
  period: string          // 账期（如：2024-01）
  categories: {            // 各费用项
    category: TransactionCategory
    amount: number
  }[]
  totalAmount: number      // 总金额
  paidAmount: number       // 已付金额
  status: 'unpaid' | 'partial' | 'paid' | 'overdue'
  dueDate: string         // 缴费截止日期
  paidDate: string       // 实际缴费日期
  remark: string
}

// 收支记录
export interface TransactionRecord {
  id: number
  transactionNo: string    // 交易编号
  type: TransactionType   // 收支类型
  category: TransactionCategory  // 分类
  amount: number          // 金额
  balance: number         // 余额
  paymentMethod: PaymentMethod  // 支付方式
  date: string           // 日期
  handler: string         // 经办人
  relatedParty: string    // 对方单位/个人
  contractNo: string     // 关联合同编号
  billNo: string         // 关联账单编号
  description: string     // 说明
  receiptNo: string       // 收据/发票号
  status: TransactionStatus  // 状态
  remark: string         // 备注
  attachments: string[]   // 附件
}

// 分类标签
export const categoryLabels: Record<TransactionCategory, string> = {
  'property_fee': '物业费',
  'parking_fee': '车位费',
  'water_fee': '水费',
  'electric_fee': '电费',
  'gas_fee': '燃气费',
  'heating_fee': '暖气费',
  'public_fee': '公摊费',
  'maintenance_fee': '维修费',
  'cleaning_fee': '清洁费',
  'security_fee': '安保费',
  'landscape_fee': '绿化费',
  'office_fee': '办公费',
  'salary': '工资',
  'material': '材料费',
  'equipment': '设备费',
  'other': '其他'
}

// 收支类型标签
export const typeLabels: Record<TransactionType, string> = {
  'income': '收入',
  'expense': '支出'
}

// 支付方式标签
export const paymentMethodLabels: Record<PaymentMethod, string> = {
  'cash': '现金',
  'transfer': '银行转账',
  'wechat': '微信支付',
  'alipay': '支付宝',
  'card': '刷卡',
  'other': '其他'
}

// 状态标签
export const statusLabels: Record<TransactionStatus, string> = {
  'pending': '待处理',
  'completed': '已完成',
  'cancelled': '已取消',
  'refunded': '已退款'
}

// 存储键名
const STORAGE_KEY = 'wo_finance_transactions'
const BILLS_KEY = 'wo_finance_bills'

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

// 收支记录数据
const transactions = ref<TransactionRecord[]>(loadFromStorage(STORAGE_KEY, []))

// 账单数据
const bills = ref<BillRecord[]>(loadFromStorage(BILLS_KEY, []))

// 如果没有数据，使用默认数据
if (transactions.value.length === 0) {
  const today = new Date().toISOString().split('T')[0]
  transactions.value = [
    {
      id: 1,
      transactionNo: 'TR-2024-001',
      type: 'income',
      category: 'property_fee',
      amount: 3500,
      balance: 0,
      paymentMethod: 'transfer',
      date: '2024-04-15',
      handler: '张会计',
      relatedParty: 'A栋101住户',
      contractNo: '',
      billNo: 'BILL-2024-001',
      description: '2024年第一季度物业费',
      receiptNo: 'RCP-2024-001',
      status: 'completed',
      remark: '',
      attachments: []
    },
    {
      id: 2,
      transactionNo: 'TR-2024-002',
      type: 'expense',
      category: 'maintenance_fee',
      amount: 800,
      balance: 0,
      paymentMethod: 'wechat',
      date: '2024-04-10',
      handler: '李出纳',
      relatedParty: '电梯维修公司',
      contractNo: 'CT-2024-002',
      billNo: '',
      description: '1号电梯维修费',
      receiptNo: 'INV-2024-002',
      status: 'completed',
      remark: '',
      attachments: []
    },
    {
      id: 3,
      transactionNo: 'TR-2024-003',
      type: 'income',
      category: 'parking_fee',
      amount: 500,
      balance: 0,
      paymentMethod: 'alipay',
      date: '2024-04-12',
      handler: '张会计',
      relatedParty: 'A栋住户-王先生',
      contractNo: '',
      billNo: 'BILL-2024-002',
      description: '4月车位费',
      receiptNo: 'RCP-2024-003',
      status: 'completed',
      remark: '',
      attachments: []
    },
    {
      id: 4,
      transactionNo: 'TR-2024-004',
      type: 'expense',
      category: 'salary',
      amount: 15000,
      balance: 0,
      paymentMethod: 'transfer',
      date: '2024-04-05',
      handler: '王经理',
      relatedParty: '保安部员工',
      contractNo: '',
      billNo: '',
      description: '4月保安部工资',
      receiptNo: 'SAL-2024-004',
      status: 'completed',
      remark: '',
      attachments: []
    }
  ]
  saveToStorage(STORAGE_KEY, transactions.value)
}

let transactionIdCounter = Math.max(...transactions.value.map(t => t.id), 0) + 1

// 收入统计
export const getIncomeStats = (startDate?: string, endDate?: string) => {
  let filtered = transactions.value.filter(t => t.type === 'income' && t.status === 'completed')
  if (startDate) {
    filtered = filtered.filter(t => t.date >= startDate)
  }
  if (endDate) {
    filtered = filtered.filter(t => t.date <= endDate)
  }
  
  const total = filtered.reduce((sum, t) => sum + t.amount, 0)
  const byCategory: Record<string, number> = {}
  filtered.forEach(t => {
    byCategory[t.category] = (byCategory[t.category] || 0) + t.amount
  })
  
  return { total, byCategory, count: filtered.length }
}

// 支出统计
export const getExpenseStats = (startDate?: string, endDate?: string) => {
  let filtered = transactions.value.filter(t => t.type === 'expense' && t.status === 'completed')
  if (startDate) {
    filtered = filtered.filter(t => t.date >= startDate)
  }
  if (endDate) {
    filtered = filtered.filter(t => t.date <= endDate)
  }
  
  const total = filtered.reduce((sum, t) => sum + t.amount, 0)
  const byCategory: Record<string, number> = {}
  filtered.forEach(t => {
    byCategory[t.category] = (byCategory[t.category] || 0) + t.amount
  })
  
  return { total, byCategory, count: filtered.length }
}

// 获取统计汇总
export const getFinanceStats = (startDate?: string, endDate?: string) => {
  const income = getIncomeStats(startDate, endDate)
  const expense = getExpenseStats(startDate, endDate)
  const balance = income.total - expense.total
  
  return {
    totalIncome: income.total,
    totalExpense: expense.total,
    balance,
    incomeCount: income.count,
    expenseCount: expense.count,
    incomeByCategory: income.byCategory,
    expenseByCategory: expense.byCategory
  }
}

// 获取所有记录
export const getAllTransactions = () => transactions.value

// 按类型获取记录
export const getTransactionsByType = (type: TransactionType) => 
  transactions.value.filter(t => t.type === type)

// 按分类获取记录
export const getTransactionsByCategory = (category: TransactionCategory) => 
  transactions.value.filter(t => t.category === category)

// 按日期范围获取记录
export const getTransactionsByDateRange = (startDate: string, endDate: string) =>
  transactions.value.filter(t => t.date >= startDate && t.date <= endDate)

// 添加收支记录
export const addTransaction = (record: Omit<TransactionRecord, 'id'>): TransactionRecord => {
  const newRecord: TransactionRecord = {
    ...record,
    id: transactionIdCounter++
  }
  transactions.value.push(newRecord)
  saveToStorage(STORAGE_KEY, transactions.value)
  return newRecord
}

// 更新收支记录
export const updateTransaction = (id: number, updates: Partial<TransactionRecord>) => {
  const index = transactions.value.findIndex(t => t.id === id)
  if (index !== -1) {
    transactions.value[index] = { ...transactions.value[index], ...updates }
    saveToStorage(STORAGE_KEY, transactions.value)
  }
}

// 删除收支记录
export const deleteTransaction = (id: number) => {
  const index = transactions.value.findIndex(t => t.id === id)
  if (index !== -1) {
    transactions.value.splice(index, 1)
    saveToStorage(STORAGE_KEY, transactions.value)
  }
}

// 获取账单
export const getAllBills = () => bills.value

// 按状态获取账单
export const getBillsByStatus = (status: BillRecord['status']) =>
  bills.value.filter(b => b.status === status)

// 添加账单
export const addBill = (bill: Omit<BillRecord, 'id'>): BillRecord => {
  const newBill: BillRecord = { ...bill, id: Date.now() }
  bills.value.push(newBill)
  saveToStorage(BILLS_KEY, bills.value)
  return newBill
}

// 更新账单
export const updateBill = (id: number, updates: Partial<BillRecord>) => {
  const index = bills.value.findIndex(b => b.id === id)
  if (index !== -1) {
    bills.value[index] = { ...bills.value[index], ...updates }
    saveToStorage(BILLS_KEY, bills.value)
  }
}

// 缴费
export const payBill = (id: number, paidAmount: number, paidDate: string) => {
  const bill = bills.value.find(b => b.id === id)
  if (bill) {
    bill.paidAmount += paidAmount
    bill.paidDate = paidDate
    if (bill.paidAmount >= bill.totalAmount) {
      bill.status = 'paid'
    } else if (bill.paidAmount > 0) {
      bill.status = 'partial'
    }
    saveToStorage(BILLS_KEY, bills.value)
  }
}

// 格式化金额
export const formatMoney = (amount: number) => {
  return `¥${amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

export const financeStore = {
  transactions,
  bills,
  getAllTransactions,
  getTransactionsByType,
  getTransactionsByCategory,
  getTransactionsByDateRange,
  addTransaction,
  updateTransaction,
  deleteTransaction,
  getAllBills,
  getBillsByStatus,
  addBill,
  updateBill,
  payBill,
  getFinanceStats,
  getIncomeStats,
  getExpenseStats,
  formatMoney
}