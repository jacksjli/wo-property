import { ref } from 'vue'
import { masterDataApi, type FieldDefinition } from '../api/masterDataService'

// ============================================================
// 本地类型定义（与后端 FieldDefinition 对齐，并保留前端扩展字段）
// ============================================================

/** 字段类型 */
export type FieldType = 'text' | 'number' | 'date' | 'select' | 'textarea'

/** 字段分类：shared（共享）、private（私有）、system（系统） */
export type FieldClassification = 'shared' | 'private' | 'system'

/** 字段配置项（前端扩展版） */
export interface FieldConfig {
  id: number
  module: string
  name: string
  key: string
  type: FieldType
  defaultValue: any
  required: boolean
  status: 'Active' | 'Inactive'
  options?: string[]
  width?: number
  align?: 'left' | 'center' | 'right'
  classification?: FieldClassification
  source?: string         // 来源服务：MasterDataService / PersonService / 各微服务
  aliasOf?: string        // 别名：指向共享字段的原始 key（从 ALIAS_MAP 转换）
  isShared?: boolean      // 是否为共享字段（来自 API）
  fieldDefinitionId?: number // 对应后端 FieldDefinition.id
}

// ============================================================
// 第一层：共享字段注册表（所有模块共用的字段定义）
// ============================================================
const SHARED_FIELDS = {
  // --- 系统共享字段（所有模块都有）---
  status: {
    key: 'status',
    name: '状态',
    type: 'select' as FieldType,
    classification: 'system' as FieldClassification,
    source: 'System',
    options: ['正常', '停用', '草稿', '已发布', '处理中', '已解决', '已关闭', '空闲', '已占用'],
  },
  remark: {
    key: 'remark',
    name: '备注',
    type: 'textarea' as FieldType,
    classification: 'system' as FieldClassification,
    source: 'System',
  },
  createdAt: {
    key: 'createdAt',
    name: '创建时间',
    type: 'date' as FieldType,
    classification: 'system' as FieldClassification,
    source: 'System',
  },
  updatedAt: {
    key: 'updatedAt',
    name: '更新时间',
    type: 'date' as FieldType,
    classification: 'system' as FieldClassification,
    source: 'System',
  },

  // --- MasterDataService 共享字段 ---
  roomNo: {
    key: 'roomNo',
    name: '房号',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },
  location: {
    key: 'location',
    name: '位置',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },
  buildingId: {
    key: 'buildingId',
    name: '楼栋',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },
  floor: {
    key: 'floor',
    name: '楼层',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },
  address: {
    key: 'address',
    name: '地址',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },

  // --- PersonService 共享字段 ---
  name: {
    key: 'name',
    name: '姓名',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  phone: {
    key: 'phone',
    name: '联系电话',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  staffId: {
    key: 'staffId',
    name: '员工编号',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  department: {
    key: 'department',
    name: '部门',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
    options: ['工程部', '客服部', '安保部', '保洁部', '绿化部', '行政部', '财务部', '人事部'],
  },
  role: {
    key: 'role',
    name: '职位',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
    options: ['operator', 'supervisor', 'manager', 'department_head', 'company_head'],
  },
  gender: {
    key: 'gender',
    name: '性别',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
    options: ['男', '女'],
  },
  email: {
    key: 'email',
    name: '邮箱',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  idCard: {
    key: 'idCard',
    name: '身份证号',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  joinDate: {
    key: 'joinDate',
    name: '入职日期',
    type: 'date' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  emergencyContact: {
    key: 'emergencyContact',
    name: '紧急联系人',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },
  emergencyPhone: {
    key: 'emergencyPhone',
    name: '紧急电话',
    type: 'text' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'PersonService',
  },

  // --- MasterDataService 枚举共享字段 ---
  type: {
    key: 'type',
    name: '类型',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },
  priority: {
    key: 'priority',
    name: '优先级',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
    options: ['低', '中', '高', '紧急', 'High', 'Normal', 'Low'],
  },
  category: {
    key: 'category',
    name: '分类',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
  },
  level: {
    key: 'level',
    name: '优先级',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
    options: ['低', '普通', '重要', '紧急'],
  },
  unit: {
    key: 'unit',
    name: '单位',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
    options: ['个', '件', '套', '盒', '瓶', '卷', '米', '千克', '升'],
  },
  cycle: {
    key: 'cycle',
    name: '周期',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
    options: ['日检', '周检', '月检', '季检', '年检', '月', '季', '年', '一次性'],
  },
  education: {
    key: 'education',
    name: '学历',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
    options: ['高中', '中专', '大专', '本科', '硕士', '博士'],
  },
  employmentType: {
    key: 'employmentType',
    name: '用工类型',
    type: 'select' as FieldType,
    classification: 'shared' as FieldClassification,
    source: 'MasterDataService',
    options: ['正式员工', '合同工', '临时工', '实习生'],
  },
} as const

// 别名映射表：同一实体在不同模块的字段名
// key = module.fieldKey，value = 共享字段的 key
const ALIAS_MAP: Record<string, string> = {
  // 工单模块
  'ticket.creatorName': 'name',
  'ticket.assigneeName': 'name',
  'ticket.contactName': 'name',
  'ticket.contactPhone': 'phone',

  // 设备模块
  'device.responsible': 'name',

  // 合同模块
  'contract.responsible': 'name',

  // 财务管理模块
  'finance.residentName': 'name',
  'finance.handledBy': 'name',

  // 投诉模块
  'complaint.complainant': 'name',
  'complaint.handler': 'name',

  // 钥匙模块
  'key.borrower': 'name',

  // 访客模块
  'visitor.visitedResident': 'name',

  // 通知模块
  'notification.publisher': 'name',

  // 住户模块
  'resident.name': 'name',

  // 车位模块
  'parking.ownerName': 'name',
  'parking.ownerPhone': 'phone',

  // 缴费模块
  'payment.residentName': 'name',
  'payment.phone': 'phone',

  // 人员模块
  'personnel.name': 'name',

  // 外卖模块
  'takeout.residentName': 'name',
  'takeout.phone': 'phone',
  'takeout.deliveryPerson': 'name',
  'takeout.deliveryPhone': 'phone',

  // 巡检模块
  'inspection.responsible': 'name',
}

// 辅助函数：从别名获取共享字段定义
function resolveSharedField(
  module: string,
  fieldKey: string
): (typeof SHARED_FIELDS)[keyof typeof SHARED_FIELDS] | null {
  const aliasKey = `${module}.${fieldKey}`
  const sharedKey = ALIAS_MAP[aliasKey]
  if (sharedKey && SHARED_FIELDS[sharedKey as keyof typeof SHARED_FIELDS]) {
    return SHARED_FIELDS[sharedKey as keyof typeof SHARED_FIELDS]
  }
  return null
}

// ============================================================
// 第二层：模块默认字段配置
// ============================================================

// 工单模块的默认字段配置
const TICKET_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'ticket', name: '工单编号', key: 'ticketNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 130, classification: 'private', source: 'TicketService' },
  { id: 2, module: 'ticket', name: '工单标题', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200, classification: 'private', source: 'TicketService' },
  { id: 6, module: 'ticket', name: '工单描述', key: 'description', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'private', source: 'TicketService' },
  { id: 10, module: 'ticket', name: '处理时间', key: 'handleTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'TicketService' },
  { id: 11, module: 'ticket', name: '完成时间', key: 'completeTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'TicketService' },
  { id: 3, module: 'ticket', name: '工单类型', key: 'type', type: 'select', defaultValue: 'Repair', required: true, status: 'Active', width: 100, options: ['Repair', 'Access', 'Cleaning', 'Security', 'Other'], classification: 'shared', source: 'MasterDataService', aliasOf: 'type' },
  { id: 4, module: 'ticket', name: '优先级', key: 'priority', type: 'select', defaultValue: 'Normal', required: true, status: 'Active', width: 80, options: ['High', 'Normal', 'Low'], classification: 'shared', source: 'MasterDataService', aliasOf: 'priority' },
  { id: 5, module: 'ticket', name: '工单状态', key: 'status', type: 'select', defaultValue: 'Open', required: true, status: 'Active', width: 100, options: ['Open', 'Processing', 'Resolved', 'Closed'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 7, module: 'ticket', name: '创建人', key: 'creatorName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 8, module: 'ticket', name: '创建时间', key: 'createTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'system', source: 'System', aliasOf: 'createdAt' },
  { id: 9, module: 'ticket', name: '指派人', key: 'assigneeName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 12, module: 'ticket', name: '联系人', key: 'contactName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 13, module: 'ticket', name: '联系电话', key: 'contactPhone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 14, module: 'ticket', name: '位置', key: 'location', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 15, module: 'ticket', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 设备管理模块的默认字段配置
const DEVICE_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'device', name: '设备编号', key: 'deviceNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'DeviceService' },
  { id: 2, module: 'device', name: '设备名称', key: 'deviceName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'DeviceService' },
  { id: 3, module: 'device', name: '设备类型', key: 'deviceType', type: 'select', defaultValue: '其他', required: true, status: 'Active', width: 100, options: ['电梯', '消防', '监控', '门禁', '空调', '照明', '给排水', '供电', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 4, module: 'device', name: '设备型号', key: 'model', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'DeviceService' },
  { id: 5, module: 'device', name: '设备品牌', key: 'brand', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'private', source: 'DeviceService' },
  { id: 6, module: 'device', name: '安装位置', key: 'location', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 7, module: 'device', name: '设备状态', key: 'status', type: 'select', defaultValue: '正常', required: true, status: 'Active', width: 100, options: ['正常', '维修中', '已报废', '待报废'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 8, module: 'device', name: '巡检周期', key: 'inspectionCycle', type: 'select', defaultValue: '月检', required: true, status: 'Active', width: 100, options: ['日检', '周检', '月检', '季检', '年检'], classification: 'shared', source: 'MasterDataService', aliasOf: 'cycle' },
  { id: 9, module: 'device', name: '购买日期', key: 'purchaseDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'DeviceService' },
  { id: 10, module: 'device', name: '维保截止', key: 'warrantyEndDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'DeviceService' },
  { id: 11, module: 'device', name: '供应商', key: 'supplier', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'DeviceService' },
  { id: 12, module: 'device', name: '负责人', key: 'responsible', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 13, module: 'device', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 物料管理模块的默认字段配置
const MATERIAL_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'material', name: '物料编号', key: 'materialNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'MaterialService' },
  { id: 2, module: 'material', name: '物料名称', key: 'materialName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'MaterialService' },
  { id: 3, module: 'material', name: '物料分类', key: 'category', type: 'select', defaultValue: '其他', required: true, status: 'Active', width: 100, options: ['办公用品', '清洁用品', '维修工具', '五金配件', '安防器材', '绿化物资', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 4, module: 'material', name: '规格型号', key: 'specification', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'MaterialService' },
  { id: 5, module: 'material', name: '单位', key: 'unit', type: 'select', defaultValue: '个', required: true, status: 'Active', width: 80, options: ['个', '件', '套', '盒', '瓶', '卷', '米', '千克', '升'], classification: 'shared', source: 'MasterDataService', aliasOf: 'unit' },
  { id: 6, module: 'material', name: '库存数量', key: 'quantity', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 100, classification: 'private', source: 'MaterialService' },
  { id: 7, module: 'material', name: '库存上限', key: 'maxStock', type: 'number', defaultValue: 100, required: false, status: 'Active', width: 100, classification: 'private', source: 'MaterialService' },
  { id: 8, module: 'material', name: '库存下限', key: 'minStock', type: 'number', defaultValue: 10, required: false, status: 'Active', width: 100, classification: 'private', source: 'MaterialService' },
  { id: 9, module: 'material', name: '单价', key: 'unitPrice', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100, classification: 'private', source: 'MaterialService' },
  { id: 10, module: 'material', name: '供应商', key: 'supplier', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'MaterialService' },
  { id: 11, module: 'material', name: '存放位置', key: 'location', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 12, module: 'material', name: '物料状态', key: 'status', type: 'select', defaultValue: '正常', required: true, status: 'Active', width: 100, options: ['正常', '库存不足', '已用完', '已停用'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 13, module: 'material', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 合同管理模块的默认字段配置
const CONTRACT_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'contract', name: '合同编号', key: 'contractNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ContractService' },
  { id: 2, module: 'contract', name: '合同名称', key: 'contractName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200, classification: 'private', source: 'ContractService' },
  { id: 3, module: 'contract', name: '合同类型', key: 'contractType', type: 'select', defaultValue: '服务', required: true, status: 'Active', width: 100, options: ['服务', '采购', '租赁', '维修', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 4, module: 'contract', name: '甲方', key: 'partyA', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'ContractService' },
  { id: 5, module: 'contract', name: '乙方', key: 'partyB', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'ContractService' },
  { id: 6, module: 'contract', name: '合同金额', key: 'amount', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 120, classification: 'private', source: 'ContractService' },
  { id: 7, module: 'contract', name: '签订日期', key: 'signDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ContractService' },
  { id: 8, module: 'contract', name: '开始日期', key: 'startDate', type: 'date', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ContractService' },
  { id: 9, module: 'contract', name: '结束日期', key: 'endDate', type: 'date', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ContractService' },
  { id: 10, module: 'contract', name: '付款方式', key: 'paymentMethod', type: 'select', defaultValue: '月付', required: false, status: 'Active', width: 100, options: ['月付', '季付', '半年付', '年付', '一次性'], classification: 'private', source: 'ContractService' },
  { id: 11, module: 'contract', name: '付款状态', key: 'paymentStatus', type: 'select', defaultValue: '未付款', required: true, status: 'Active', width: 100, options: ['未付款', '部分付款', '已付款'], classification: 'private', source: 'ContractService' },
  { id: 12, module: 'contract', name: '合同状态', key: 'status', type: 'select', defaultValue: '执行中', required: true, status: 'Active', width: 100, options: ['草稿', '执行中', '已到期', '已终止'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 13, module: 'contract', name: '负责人', key: 'responsible', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 14, module: 'contract', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 财务管理模块的默认字段配置
const FINANCE_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'finance', name: '记录编号', key: 'recordNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'FinanceService' },
  { id: 2, module: 'finance', name: '记录日期', key: 'transactionDate', type: 'date', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'FinanceService' },
  { id: 3, module: 'finance', name: '收支类型', key: 'type', type: 'select', defaultValue: '收入', required: true, status: 'Active', width: 80, options: ['收入', '支出'], classification: 'shared', source: 'MasterDataService', aliasOf: 'type' },
  { id: 4, module: 'finance', name: '交易类型', key: 'category', type: 'select', defaultValue: '其他', required: true, status: 'Active', width: 100, options: ['物业费', '停车费', '广告费', '维修费', '绿化费', '保洁费', '安保费', '水电费', '办公费', '其他收入', '其他支出'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 5, module: 'finance', name: '金额', key: 'amount', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 120, classification: 'private', source: 'FinanceService' },
  { id: 6, module: 'finance', name: '付款方式', key: 'paymentMethod', type: 'select', defaultValue: '现金', required: true, status: 'Active', width: 100, options: ['现金', '银行转账', '微信', '支付宝', '其他'], classification: 'private', source: 'FinanceService' },
  { id: 7, module: 'finance', name: '关联对象', key: 'relatedObject', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'FinanceService' },
  { id: 8, module: 'finance', name: '房号', key: 'roomNo', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'MasterDataService', aliasOf: 'roomNo' },
  { id: 9, module: 'finance', name: '住户姓名', key: 'residentName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 10, module: 'finance', name: '交易状态', key: 'status', type: 'select', defaultValue: '已完成', required: true, status: 'Active', width: 100, options: ['待确认', '已完成', '已取消', '已退款'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 11, module: 'finance', name: '经手人', key: 'handledBy', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 12, module: 'finance', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 巡检管理模块的默认字段配置
const INSPECTION_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'inspection', name: '计划编号', key: 'planNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'InspectionService' },
  { id: 2, module: 'inspection', name: '计划名称', key: 'planName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'InspectionService' },
  { id: 3, module: 'inspection', name: '巡检区域', key: 'zone', type: 'select', defaultValue: '公共区域', required: true, status: 'Active', width: 100, options: ['公共区域', '地下车库', '楼层', '外墙', '屋顶', '设备间', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 4, module: 'inspection', name: '巡检周期', key: 'cycle', type: 'select', defaultValue: '日检', required: true, status: 'Active', width: 100, options: ['日检', '周检', '月检', '季检', '年检'], classification: 'shared', source: 'MasterDataService', aliasOf: 'cycle' },
  { id: 5, module: 'inspection', name: '开始时间', key: 'startTime', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'InspectionService' },
  { id: 6, module: 'inspection', name: '结束时间', key: 'endTime', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'InspectionService' },
  { id: 7, module: 'inspection', name: '负责人', key: 'responsible', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 8, module: 'inspection', name: '计划状态', key: 'status', type: 'select', defaultValue: '启用', required: true, status: 'Active', width: 100, options: ['启用', '停用', '草稿'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 9, module: 'inspection', name: '巡检点数', key: 'pointCount', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100, classification: 'private', source: 'InspectionService' },
  { id: 10, module: 'inspection', name: '已完成数', key: 'completedCount', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100, classification: 'private', source: 'InspectionService' },
  { id: 11, module: 'inspection', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 投诉管理模块的默认字段配置
const COMPLAINT_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'complaint', name: '投诉编号', key: 'complaintNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ComplaintService' },
  { id: 2, module: 'complaint', name: '投诉标题', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200, classification: 'private', source: 'ComplaintService' },
  { id: 3, module: 'complaint', name: '投诉类型', key: 'type', type: 'select', defaultValue: '服务投诉', required: true, status: 'Active', width: 100, options: ['服务投诉', '设施投诉', '环境投诉', '安全投诉', '其他投诉'], classification: 'shared', source: 'MasterDataService', aliasOf: 'type' },
  { id: 4, module: 'complaint', name: '投诉来源', key: 'source', type: 'select', defaultValue: '电话', required: true, status: 'Active', width: 100, options: ['电话', '微信', '邮件', '来访', '其他'], classification: 'shared', source: 'MasterDataService' },
  { id: 5, module: 'complaint', name: '投诉人', key: 'complainant', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 6, module: 'complaint', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 7, module: 'complaint', name: '房号', key: 'roomNo', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'MasterDataService', aliasOf: 'roomNo' },
  { id: 8, module: 'complaint', name: '投诉内容', key: 'content', type: 'textarea', defaultValue: '', required: true, status: 'Active', classification: 'private', source: 'ComplaintService' },
  { id: 9, module: 'complaint', name: '优先级', key: 'priority', type: 'select', defaultValue: '中', required: true, status: 'Active', width: 80, options: ['低', '中', '高', '紧急'], classification: 'shared', source: 'MasterDataService', aliasOf: 'priority' },
  { id: 10, module: 'complaint', name: '处理状态', key: 'handleStatus', type: 'select', defaultValue: '待处理', required: true, status: 'Active', width: 100, options: ['待处理', '处理中', '已处理', '已关闭'], classification: 'system', source: 'System' },
  { id: 11, module: 'complaint', name: '处理人', key: 'handler', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 12, module: 'complaint', name: '处理时间', key: 'handleTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ComplaintService' },
  { id: 13, module: 'complaint', name: '处理结果', key: 'result', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'private', source: 'ComplaintService' },
  { id: 14, module: 'complaint', name: '关闭时间', key: 'closeTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ComplaintService' },
  { id: 15, module: 'complaint', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 钥匙管理模块的默认字段配置
const KEY_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'key', name: '钥匙编号', key: 'keyNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'KeyService' },
  { id: 2, module: 'key', name: '钥匙名称', key: 'keyName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'KeyService' },
  { id: 3, module: 'key', name: '钥匙类型', key: 'keyType', type: 'select', defaultValue: '房间钥匙', required: true, status: 'Active', width: 100, options: ['房间钥匙', '公共区域钥匙', '设备间钥匙', '门禁卡', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 4, module: 'key', name: '关联位置', key: 'location', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 5, module: 'key', name: '钥匙状态', key: 'status', type: 'select', defaultValue: '可用', required: true, status: 'Active', width: 100, options: ['可用', '已借出', '维修中', '已报废'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 6, module: 'key', name: '借用人', key: 'borrower', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 7, module: 'key', name: '借用时间', key: 'borrowTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'KeyService' },
  { id: 8, module: 'key', name: '预计归还', key: 'expectedReturn', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'KeyService' },
  { id: 9, module: 'key', name: '实际归还', key: 'actualReturn', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'KeyService' },
  { id: 10, module: 'key', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 访客管理模块的默认字段配置
const VISITOR_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'visitor', name: '访客编号', key: 'visitorNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'VisitorService' },
  { id: 2, module: 'visitor', name: '访客姓名', key: 'visitorName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'private', source: 'VisitorService' },
  { id: 3, module: 'visitor', name: '访客类型', key: 'visitorType', type: 'select', defaultValue: '普通访客', required: true, status: 'Active', width: 100, options: ['普通访客', 'VIP访客', '快递员', '外卖员', '维修人员', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 4, module: 'visitor', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 5, module: 'visitor', name: '身份证号', key: 'idCard', type: 'text', defaultValue: '', required: false, status: 'Active', width: 180, classification: 'shared', source: 'PersonService', aliasOf: 'idCard' },
  { id: 6, module: 'visitor', name: '受访住户', key: 'visitedResident', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 7, module: 'visitor', name: '受访房号', key: 'roomNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'MasterDataService', aliasOf: 'roomNo' },
  { id: 8, module: 'visitor', name: '来访事由', key: 'purpose', type: 'textarea', defaultValue: '', required: true, status: 'Active', classification: 'private', source: 'VisitorService' },
  { id: 9, module: 'visitor', name: '来访时间', key: 'checkInTime', type: 'date', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'VisitorService' },
  { id: 10, module: 'visitor', name: '离开时间', key: 'checkOutTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'VisitorService' },
  { id: 11, module: 'visitor', name: '访客状态', key: 'status', type: 'select', defaultValue: '登记中', required: true, status: 'Active', width: 100, options: ['登记中', '访问中', '已离开', '已取消'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 12, module: 'visitor', name: '放行签字', key: 'signature', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'VisitorService' },
  { id: 13, module: 'visitor', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 通知管理模块的默认字段配置
const NOTIFICATION_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'notification', name: '通知编号', key: 'notificationNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'NotificationService' },
  { id: 2, module: 'notification', name: '通知标题', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200, classification: 'private', source: 'NotificationService' },
  { id: 3, module: 'notification', name: '通知类型', key: 'type', type: 'select', defaultValue: '系统通知', required: true, status: 'Active', width: 100, options: ['系统通知', '活动通知', '缴费通知', '设备维护', '安全提醒', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'type' },
  { id: 4, module: 'notification', name: '优先级', key: 'level', type: 'select', defaultValue: '普通', required: true, status: 'Active', width: 80, options: ['低', '普通', '重要', '紧急'], classification: 'shared', source: 'MasterDataService', aliasOf: 'level' },
  { id: 5, module: 'notification', name: '通知内容', key: 'content', type: 'textarea', defaultValue: '', required: true, status: 'Active', classification: 'private', source: 'NotificationService' },
  { id: 6, module: 'notification', name: '发布状态', key: 'status', type: 'select', defaultValue: '草稿', required: true, status: 'Active', width: 100, options: ['草稿', '已发布', '已下线'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 7, module: 'notification', name: '发送方式', key: 'sendMethod', type: 'select', defaultValue: '系统通知', required: true, status: 'Active', width: 100, options: ['系统通知', '短信', '微信', '公告', '全部'], classification: 'private', source: 'NotificationService' },
  { id: 8, module: 'notification', name: '发布时间', key: 'publishTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'NotificationService' },
  { id: 9, module: 'notification', name: '发布人', key: 'publisher', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 10, module: 'notification', name: '截止时间', key: 'endTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'NotificationService' },
  { id: 11, module: 'notification', name: '浏览次数', key: 'viewCount', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100, classification: 'private', source: 'NotificationService' },
  { id: 12, module: 'notification', name: '是否置顶', key: 'isPinned', type: 'select', defaultValue: '否', required: true, status: 'Active', width: 80, options: ['是', '否'], classification: 'private', source: 'NotificationService' },
  { id: 13, module: 'notification', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 住户管理模块的默认字段配置
const RESIDENT_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'resident', name: '住户姓名', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 2, module: 'resident', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 3, module: 'resident', name: '房号', key: 'unit', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'MasterDataService', aliasOf: 'roomNo' },
  { id: 4, module: 'resident', name: '楼层', key: 'floor', type: 'text', defaultValue: '', required: false, status: 'Active', width: 80, classification: 'shared', source: 'MasterDataService', aliasOf: 'floor' },
  { id: 5, module: 'resident', name: '住户状态', key: 'status', type: 'select', defaultValue: 'Active', required: true, status: 'Active', width: 100, options: ['Active', 'Inactive'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 6, module: 'resident', name: '身份证号', key: 'idCard', type: 'text', defaultValue: '', required: false, status: 'Active', width: 180, classification: 'shared', source: 'PersonService', aliasOf: 'idCard' },
  { id: 7, module: 'resident', name: '入住日期', key: 'moveInDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'PersonService' },
  { id: 8, module: 'resident', name: '迁出日期', key: 'moveOutDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'PersonService' },
  { id: 9, module: 'resident', name: '紧急联系人', key: 'emergencyContact', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'emergencyContact' },
  { id: 10, module: 'resident', name: '紧急电话', key: 'emergencyPhone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'emergencyPhone' },
  { id: 11, module: 'resident', name: '车辆信息', key: 'carInfo', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'PersonService' },
  { id: 12, module: 'resident', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 车位管理模块的默认字段配置
const PARKING_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'parking', name: '车位编号', key: 'spaceNumber', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ParkingService' },
  { id: 2, module: 'parking', name: '位置', key: 'location', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 3, module: 'parking', name: '车位类型', key: 'type', type: 'select', defaultValue: '固定', required: true, status: 'Active', width: 100, options: ['固定', '临时', 'VIP'], classification: 'shared', source: 'MasterDataService', aliasOf: 'type' },
  { id: 4, module: 'parking', name: '车位状态', key: 'status', type: 'select', defaultValue: '空闲', required: true, status: 'Active', width: 100, options: ['空闲', '已占用', '已预约', '维修中'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 5, module: 'parking', name: '车牌号', key: 'licensePlate', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ParkingService' },
  { id: 6, module: 'parking', name: '车主姓名', key: 'ownerName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 7, module: 'parking', name: '车主电话', key: 'ownerPhone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 8, module: 'parking', name: '物业名称', key: 'propertyName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ParkingService' },
  { id: 9, module: 'parking', name: '启用日期', key: 'startDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ParkingService' },
  { id: 10, module: 'parking', name: '到期日期', key: 'endDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'ParkingService' },
  { id: 11, module: 'parking', name: '月费', key: 'monthlyFee', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 100, classification: 'private', source: 'ParkingService' },
  { id: 12, module: 'parking', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 缴费管理模块的默认字段配置
const PAYMENT_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'payment', name: '收费项目', key: 'itemName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'PaymentService' },
  { id: 2, module: 'payment', name: '收费类型', key: 'feeType', type: 'select', defaultValue: '物业费', required: true, status: 'Active', width: 100, options: ['物业费', '水电费', '停车费', '垃圾费', '电梯费', '维修费', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 3, module: 'payment', name: '收费周期', key: 'cycle', type: 'select', defaultValue: '月', required: true, status: 'Active', width: 80, options: ['月', '季', '年', '一次性'], classification: 'shared', source: 'MasterDataService', aliasOf: 'cycle' },
  { id: 4, module: 'payment', name: '标准金额', key: 'standardAmount', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 100, classification: 'private', source: 'PaymentService' },
  { id: 5, module: 'payment', name: '收费单位', key: 'unit', type: 'text', defaultValue: '', required: false, status: 'Active', width: 80, classification: 'shared', source: 'MasterDataService', aliasOf: 'unit' },
  { id: 6, module: 'payment', name: '房号', key: 'roomNo', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'MasterDataService', aliasOf: 'roomNo' },
  { id: 7, module: 'payment', name: '住户姓名', key: 'residentName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 8, module: 'payment', name: '住户电话', key: 'phone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 9, module: 'payment', name: '上次缴费', key: 'lastPaymentDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'PaymentService' },
  { id: 10, module: 'payment', name: '下次缴费', key: 'nextPaymentDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'PaymentService' },
  { id: 11, module: 'payment', name: '账户余额', key: 'balance', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100, classification: 'private', source: 'PaymentService' },
  { id: 12, module: 'payment', name: '状态', key: 'status', type: 'select', defaultValue: '正常', required: true, status: 'Active', width: 80, options: ['正常', '欠费', '已结清'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 13, module: 'payment', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 超时设置模块的默认字段配置
const TIMEOUT_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'timeout', name: '规则名称', key: 'ruleName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150, classification: 'private', source: 'TimeoutService' },
  { id: 2, module: 'timeout', name: '超时等级', key: 'color', type: 'select', defaultValue: 'blue', required: true, status: 'Active', width: 100, options: ['green', 'blue', 'orange', 'red'], classification: 'private', source: 'TimeoutService' },
  { id: 3, module: 'timeout', name: '处理角色', key: 'role', type: 'select', defaultValue: 'operator', required: true, status: 'Active', width: 120, options: ['operator', 'supervisor', 'manager', 'department_head', 'company_head'], classification: 'shared', source: 'MasterDataService', aliasOf: 'role' },
  { id: 4, module: 'timeout', name: '超时时限', key: 'hours', type: 'number', defaultValue: 24, required: true, status: 'Active', width: 100, classification: 'private', source: 'TimeoutService' },
  { id: 5, module: 'timeout', name: '是否启用', key: 'enabled', type: 'select', defaultValue: '是', required: true, status: 'Active', width: 80, options: ['是', '否'], classification: 'private', source: 'TimeoutService' },
  { id: 6, module: 'timeout', name: '颜色说明', key: 'colorLabel', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'TimeoutService' },
  { id: 7, module: 'timeout', name: '角色说明', key: 'roleLabel', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'private', source: 'TimeoutService' },
  { id: 8, module: 'timeout', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 项目跟踪模块的默认字段配置
const PROJECT_TRACKING_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'projectTracking', name: '项目编号', key: 'projectNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ProjectTrackingService' },
  { id: 2, module: 'projectTracking', name: '项目名称', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200, classification: 'private', source: 'ProjectTrackingService' },
  { id: 3, module: 'projectTracking', name: '项目类型', key: 'type', type: 'select', defaultValue: '投标', required: true, status: 'Active', width: 100, options: ['招标', '投标', '意向'], classification: 'shared', source: 'MasterDataService', aliasOf: 'type' },
  { id: 4, module: 'projectTracking', name: '客户名称', key: 'client', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'ProjectTrackingService' },
  { id: 5, module: 'projectTracking', name: '项目预算', key: 'budget', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 100, classification: 'private', source: 'ProjectTrackingService' },
  { id: 6, module: 'projectTracking', name: '投标金额', key: 'bidAmount', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100, classification: 'private', source: 'ProjectTrackingService' },
  { id: 7, module: 'projectTracking', name: '项目所在地', key: 'location', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'MasterDataService', aliasOf: 'location' },
  { id: 8, module: 'projectTracking', name: '报名截止', key: 'registerDeadline', type: 'date', defaultValue: '', required: false, status: 'Active', width: 110, classification: 'private', source: 'ProjectTrackingService' },
  { id: 9, module: 'projectTracking', name: '投标截止', key: 'bidDeadline', type: 'date', defaultValue: '', required: false, status: 'Active', width: 110, classification: 'private', source: 'ProjectTrackingService' },
  { id: 10, module: 'projectTracking', name: '开标日期', key: 'bidOpenDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 110, classification: 'private', source: 'ProjectTrackingService' },
  { id: 11, module: 'projectTracking', name: '文件状态', key: 'fileStatus', type: 'select', defaultValue: '未获取', required: false, status: 'Active', width: 100, options: ['未获取', '已获取', '已购买'], classification: 'private', source: 'ProjectTrackingService' },
  { id: 12, module: 'projectTracking', name: '项目状态', key: 'status', type: 'select', defaultValue: '意向', required: true, status: 'Active', width: 100, options: ['意向', '跟踪中', '报名', '已投标', '开标', '公告', '中标', '落标'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 13, module: 'projectTracking', name: '成功率', key: 'successRate', type: 'number', defaultValue: 50, required: false, status: 'Active', width: 100, classification: 'private', source: 'ProjectTrackingService' },
  { id: 14, module: 'projectTracking', name: '项目描述', key: 'description', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'private', source: 'ProjectTrackingService' },
  { id: 15, module: 'projectTracking', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 人员管理模块的默认字段配置
const PERSONNEL_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'personnel', name: '员工编号', key: 'staffId', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'staffId' },
  { id: 2, module: 'personnel', name: '员工姓名', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 3, module: 'personnel', name: '性别', key: 'gender', type: 'select', defaultValue: '男', required: true, status: 'Active', width: 80, options: ['男', '女'], classification: 'shared', source: 'PersonService', aliasOf: 'gender' },
  { id: 4, module: 'personnel', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 5, module: 'personnel', name: '邮箱', key: 'email', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'shared', source: 'PersonService', aliasOf: 'email' },
  { id: 6, module: 'personnel', name: '部门', key: 'department', type: 'select', defaultValue: '工程部', required: true, status: 'Active', width: 120, options: ['工程部', '客服部', '安保部', '保洁部', '绿化部', '行政部', '财务部', '人事部'], classification: 'shared', source: 'PersonService', aliasOf: 'department' },
  { id: 7, module: 'personnel', name: '职位', key: 'role', type: 'select', defaultValue: 'operator', required: true, status: 'Active', width: 120, options: ['operator', 'supervisor', 'manager', 'department_head', 'company_head'], classification: 'shared', source: 'PersonService', aliasOf: 'role' },
  { id: 8, module: 'personnel', name: '入职日期', key: 'joinDate', type: 'date', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'joinDate' },
  { id: 9, module: 'personnel', name: '学历', key: 'education', type: 'select', defaultValue: '大专', required: false, status: 'Active', width: 100, options: ['高中', '中专', '大专', '本科', '硕士', '博士'], classification: 'shared', source: 'MasterDataService', aliasOf: 'education' },
  { id: 10, module: 'personnel', name: '专业', key: 'specialty', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'PersonService' },
  { id: 11, module: 'personnel', name: '用工类型', key: 'employmentType', type: 'select', defaultValue: '正式员工', required: true, status: 'Active', width: 100, options: ['正式员工', '合同工', '临时工', '实习生'], classification: 'shared', source: 'MasterDataService', aliasOf: 'employmentType' },
  { id: 12, module: 'personnel', name: '员工状态', key: 'status', type: 'select', defaultValue: '在职', required: true, status: 'Active', width: 100, options: ['在职', '离职', '休假', '停职'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 13, module: 'personnel', name: '身份证号', key: 'idCard', type: 'text', defaultValue: '', required: false, status: 'Active', width: 180, classification: 'shared', source: 'PersonService', aliasOf: 'idCard' },
  { id: 14, module: 'personnel', name: '紧急联系人', key: 'emergencyContact', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'emergencyContact' },
  { id: 15, module: 'personnel', name: '紧急电话', key: 'emergencyPhone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'emergencyPhone' },
  { id: 16, module: 'personnel', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// 外卖管理模块的默认字段配置
const TAKEOUT_DEFAULT_FIELDS: FieldConfig[] = [
  { id: 1, module: 'takeout', name: '订单编号', key: 'orderNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 140, classification: 'private', source: 'TakeoutService' },
  { id: 2, module: 'takeout', name: '餐厅名称', key: 'restaurantName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'private', source: 'TakeoutService' },
  { id: 3, module: 'takeout', name: '餐食类型', key: 'foodType', type: 'select', defaultValue: '快餐', required: true, status: 'Active', width: 100, options: ['中餐', '西餐', '快餐', '甜品', '饮品', '其他'], classification: 'shared', source: 'MasterDataService', aliasOf: 'category' },
  { id: 4, module: 'takeout', name: '住户姓名', key: 'residentName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 5, module: 'takeout', name: '房号', key: 'roomNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100, classification: 'shared', source: 'MasterDataService', aliasOf: 'roomNo' },
  { id: 6, module: 'takeout', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 7, module: 'takeout', name: '配送员', key: 'deliveryPerson', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100, classification: 'shared', source: 'PersonService', aliasOf: 'name' },
  { id: 8, module: 'takeout', name: '配送员电话', key: 'deliveryPhone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120, classification: 'shared', source: 'PersonService', aliasOf: 'phone' },
  { id: 9, module: 'takeout', name: '送达时间', key: 'deliveryTime', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150, classification: 'private', source: 'TakeoutService' },
  { id: 10, module: 'takeout', name: '订单状态', key: 'status', type: 'select', defaultValue: '待取餐', required: true, status: 'Active', width: 100, options: ['待取餐', '配送中', '已送达', '已取消', '异常'], classification: 'system', source: 'System', aliasOf: 'status' },
  { id: 11, module: 'takeout', name: '总金额', key: 'totalAmount', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 100, classification: 'private', source: 'TakeoutService' },
  { id: 12, module: 'takeout', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', classification: 'system', source: 'System', aliasOf: 'remark' },
]

// ============================================================
// 存储与初始化
// ============================================================

const STORAGE_KEY = 'wo_field_configs'
const SCHEMA_VERSION = 3
const STORAGE_META_KEY = 'wo_field_configs_meta'

const MODULE_SCHEMA_VERSIONS: Record<string, number> = {
  projectTracking: 1,
  device: 1,
  material: 1,
  contract: 1,
  finance: 1,
  inspection: 1,
  complaint: 1,
  key: 1,
  visitor: 1,
  notification: 1,
  resident: 1,
  parking: 1,
  payment: 1,
  timeout: 1,
  ticket: 3,
  personnel: 1,
  takeout: 1,
}

const ALL_MODULE_DEFAULTS: Record<string, FieldConfig[]> = {
  projectTracking: PROJECT_TRACKING_DEFAULT_FIELDS,
  device: DEVICE_DEFAULT_FIELDS,
  material: MATERIAL_DEFAULT_FIELDS,
  contract: CONTRACT_DEFAULT_FIELDS,
  finance: FINANCE_DEFAULT_FIELDS,
  inspection: INSPECTION_DEFAULT_FIELDS,
  complaint: COMPLAINT_DEFAULT_FIELDS,
  key: KEY_DEFAULT_FIELDS,
  visitor: VISITOR_DEFAULT_FIELDS,
  notification: NOTIFICATION_DEFAULT_FIELDS,
  resident: RESIDENT_DEFAULT_FIELDS,
  parking: PARKING_DEFAULT_FIELDS,
  payment: PAYMENT_DEFAULT_FIELDS,
  timeout: TIMEOUT_DEFAULT_FIELDS,
  ticket: TICKET_DEFAULT_FIELDS,
  personnel: PERSONNEL_DEFAULT_FIELDS,
  takeout: TAKEOUT_DEFAULT_FIELDS,
}

// 从 localStorage 加载数据
const loadFromStorage = (): Record<string, FieldConfig[]> => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (parsed && typeof parsed === 'object') return parsed
    }
  } catch (error) {
    console.error('加载字段配置数据失败:', error)
  }
  return {}
}

// 获取元数据
const loadMeta = () => {
  try {
    const meta = localStorage.getItem(STORAGE_META_KEY)
    return meta ? JSON.parse(meta) : {}
  } catch {
    return {}
  }
}

// 保存元数据
const saveMeta = (meta: object) => {
  localStorage.setItem(STORAGE_META_KEY, JSON.stringify(meta))
}

// 保存到 localStorage
const saveToStorage = (data: Record<string, FieldConfig[]>) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存字段配置数据失败:', error)
  }
}

// 强制重置字段配置
const forceResetConfig = (): Record<string, FieldConfig[]> => {
  console.log('🔄 强制重置字段配置 (v3: 字段分类+别名)...')

  const meta = loadMeta()
  const currentVersion = meta.schemaVersion ?? 0
  const loadedConfigs = loadFromStorage()

  if (currentVersion < SCHEMA_VERSION) {
    console.log(`📋 配置版本过时 (当前:${currentVersion} → 最新:${SCHEMA_VERSION})，强制重置所有模块`)

    for (const [moduleName, defaultFields] of Object.entries(ALL_MODULE_DEFAULTS)) {
      const moduleVersion = MODULE_SCHEMA_VERSIONS[moduleName] ?? 1
      const savedModuleVersion = meta.moduleVersions?.[moduleName] ?? 0

      if (savedModuleVersion < moduleVersion) {
        console.log(`  🔸 ${moduleName} 重置为默认 (v${savedModuleVersion} → v${moduleVersion})`)
        loadedConfigs[moduleName] = defaultFields
      } else if (!loadedConfigs[moduleName] || loadedConfigs[moduleName].length === 0) {
        loadedConfigs[moduleName] = defaultFields
      }
    }

    saveMeta({ schemaVersion: SCHEMA_VERSION, moduleVersions: MODULE_SCHEMA_VERSIONS })
    saveToStorage(loadedConfigs)
  } else {
    for (const [moduleName, defaultFields] of Object.entries(ALL_MODULE_DEFAULTS)) {
      if (!loadedConfigs[moduleName] || loadedConfigs[moduleName].length === 0) {
        console.log(`  🔸 ${moduleName} 从未配置，初始化为默认`)
        loadedConfigs[moduleName] = defaultFields
      }
    }
    saveToStorage(loadedConfigs)
  }

  Object.keys(MODULE_SCHEMA_VERSIONS).forEach(module => {
    const fields = loadedConfigs[module]
    const sharedCount = fields?.filter(f => f.classification === 'shared' || f.classification === 'system').length ?? 0
    const privateCount = fields?.filter(f => f.classification === 'private').length ?? 0
    console.log(`✅ ${module}: ${fields?.length ?? 0} 个字段 (共享/系统:${sharedCount}, 私有:${privateCount})`)
  })

  return loadedConfigs
}

// ============================================================
// API 集成：将后端 FieldDefinition 转换为前端 FieldConfig
// ============================================================

/** 将后端 FieldDefinition 转换为前端 FieldConfig */
function convertApiToFieldConfig(def: FieldDefinition, module: string): FieldConfig {
  // 尝试从 ALIAS_MAP 获取 aliasOf
  const aliasKey = `${module}.${def.fieldKey}`
  const aliasOf = ALIAS_MAP[aliasKey] ?? undefined

  return {
    id: def.id,
    module,
    name: def.displayName,
    key: def.fieldKey,
    type: def.fieldType,
    defaultValue: def.defaultValue,
    required: def.isRequired,
    status: def.status,
    options: def.options ?? [],
    width: def.width ?? 120,
    classification: def.isShared ? 'shared' : 'private',
    source: def.source,
    isShared: def.isShared,
    fieldDefinitionId: def.id,
    aliasOf,
  }
}

/** 从 API 加载模块字段（静默后台刷新） */
async function loadFieldsFromAPI(module: string): Promise<FieldConfig[]> {
  try {
    const response = await masterDataApi.getFieldsByModule(module)
    return (response.data as FieldDefinition[]).map(d => convertApiToFieldConfig(d, module))
  } catch (error) {
    console.warn(`[fieldConfig] API 加载失败 module=${module}，使用本地缓存:`, error)
    return []
  }
}

/** 初始化：从 API 加载，失败则回退到 localStorage */
async function initializeConfigs(): Promise<Record<string, FieldConfig[]>> {
  // 先尝试从 API 加载
  try {
    const modules = Object.keys(MODULE_SCHEMA_VERSIONS)
    const results = await Promise.allSettled(modules.map(m => loadFieldsFromAPI(m)))

    // 检查是否所有模块都从 API 成功加载
    const allSuccess = results.every(r => r.status === 'fulfilled' && (r.value as FieldConfig[]).length > 0)

    if (allSuccess) {
      const configs: Record<string, FieldConfig[]> = {}
      results.forEach((r, i) => {
        if (r.status === 'fulfilled') {
          configs[modules[i]] = r.value as FieldConfig[]
        }
      })
      console.log('✅ 字段配置从 API 加载成功')
      return configs
    }
  } catch (error) {
    console.warn('[fieldConfig] API 初始化失败，回退到 localStorage:', error)
  }

  // 回退到 localStorage
  return forceResetConfig()
}

// ============================================================
// 模块字段配置（响应式）
// ============================================================

// 初始化（同步获取 localStorage，异步尝试 API 刷新）
const fieldConfigs = ref<Record<string, FieldConfig[]>>(forceResetConfig())

// API 数据缓存（用于静默后台刷新）
let apiCache: Record<string, FieldConfig[]> = {}

// 是否已初始化完成
let initialized = false

/** 尝试从 API 静默刷新所有模块的字段配置（每 5 分钟调用一次） */
async function refreshFromAPI() {
  try {
    const modules = Object.keys(MODULE_SCHEMA_VERSIONS)
    const results = await Promise.allSettled(modules.map(m => loadFieldsFromAPI(m)))

    const newCache: Record<string, FieldConfig[]> = {}
    results.forEach((r, i) => {
      if (r.status === 'fulfilled' && (r.value as FieldConfig[]).length > 0) {
        newCache[modules[i]] = r.value as FieldConfig[]
      }
    })

    if (Object.keys(newCache).length > 0) {
      apiCache = newCache
      // 合并到 fieldConfigs（保留 localStorage 的用户自定义）
      for (const [module, apiFields] of Object.entries(newCache)) {
        const localFields = fieldConfigs.value[module]
        if (!localFields || localFields.length === 0) {
          // 本地没有，直接用 API 数据
          fieldConfigs.value[module] = apiFields
        }
        // 如果本地有，API 数据只做补充/更新（不覆盖用户已保存的自定义）
      }
      console.log('[fieldConfig] 后台 API 刷新完成')
    }
  } catch (error) {
    // 静默失败，不影响用户
    console.warn('[fieldConfig] 后台 API 刷新失败:', error)
  }
}

// 启动时异步初始化（不阻塞 UI）
initializeConfigs().then(configs => {
  // 如果 API 返回了有效数据，合并进去
  for (const [module, fields] of Object.entries(configs)) {
    if (fields.length > 0) {
      // 只有当本地为空时才用 API 数据（避免覆盖用户自定义）
      if (!fieldConfigs.value[module] || fieldConfigs.value[module].length === 0) {
        fieldConfigs.value[module] = fields
      }
    }
  }
  initialized = true

  // 启动后台定时刷新（每 5 分钟）
  setInterval(refreshFromAPI, 5 * 60 * 1000)
})

// ============================================================
// 导出函数（保持原有所有导出）
// ============================================================

/** 获取所有模块的字段配置 */
export const getAllFieldConfigs = () => fieldConfigs.value

/** 获取指定模块的字段配置 */
export const getFieldsByModule = (module: string) => fieldConfigs.value[module] || []

/** 获取指定模块的字段配置（别名：保留旧函数名） */
export const getModuleFields = (module: string) => fieldConfigs.value[module] || []

/** 获取指定模块的别名指向（用于调试和分析） */
export const getFieldAlias = (module: string, fieldKey: string): string | null => {
  const aliasKey = `${module}.${fieldKey}`
  return ALIAS_MAP[aliasKey] ?? null
}

/** 获取指定模块启用的字段配置 */
export const getActiveFields = (module: string) => {
  const fields = fieldConfigs.value[module] || []
  return fields.filter(f => f.status === 'Active')
}

/** 获取指定模块的共享字段 */
export const getSharedFields = (module: string) => {
  const fields = fieldConfigs.value[module] || []
  return fields.filter(f => f.classification === 'shared' || f.classification === 'system')
}

/** 获取指定模块的私有字段 */
export const getPrivateFields = (module: string) => {
  const fields = fieldConfigs.value[module] || []
  return fields.filter(f => f.classification === 'private')
}

/** 添加字段 */
export const addField = (module: string, field: Omit<FieldConfig, 'id'>) => {
  if (!fieldConfigs.value[module]) {
    fieldConfigs.value[module] = []
  }
  const fields = fieldConfigs.value[module]
  const newId = fields.length > 0 ? Math.max(...fields.map(f => f.id)) + 1 : 1
  const newField = { ...field, id: newId }
  fields.push(newField)
  saveToStorage(fieldConfigs.value)
  return newField
}

/** 更新字段 */
export const updateField = (module: string, id: number, updates: Partial<FieldConfig>) => {
  const fields = fieldConfigs.value[module]
  if (fields) {
    const index = fields.findIndex(f => f.id === id)
    if (index !== -1) {
      fields[index] = { ...fields[index], ...updates }
      saveToStorage(fieldConfigs.value)
      return fields[index]
    }
  }
  return null
}

/** 删除字段 */
export const deleteField = (module: string, id: number) => {
  const fields = fieldConfigs.value[module]
  if (fields) {
    const index = fields.findIndex(f => f.id === id)
    if (index !== -1) {
      fields.splice(index, 1)
      saveToStorage(fieldConfigs.value)
      return true
    }
  }
  return false
}

/** 切换字段状态 */
export const toggleFieldStatus = (module: string, id: number) => {
  const fields = fieldConfigs.value[module]
  if (fields) {
    const index = fields.findIndex(f => f.id === id)
    if (index !== -1) {
      fields[index].status = fields[index].status === 'Active' ? 'Inactive' : 'Active'
      saveToStorage(fieldConfigs.value)
      return fields[index].status
    }
  }
  return null
}

/** 自动生成字段标识 */
export const autoGenerateKey = (name: string) => {
  return name
    .replace(/[^\u4e00-\u9fa5a-zA-Z0-9]/g, '_')
    .replace(/_+/g, '_')
    .replace(/^_|_$/g, '')
    .toLowerCase()
}

// ============================================================
// 常量导出
// ============================================================

/** 字段类型选项 */
export const fieldTypeOptions = [
  { value: 'text', label: '文本' },
  { value: 'number', label: '数字' },
  { value: 'date', label: '日期' },
  { value: 'select', label: '下拉选择' },
  { value: 'textarea', label: '多行文本' },
]

/** 字段分类选项（用于 UI 下拉） */
export const fieldClassificationOptions = [
  { value: 'private', label: '私有字段（模块独有）' },
  { value: 'shared', label: '共享字段（跨模块复用）' },
  { value: 'system', label: '系统字段（所有模块都有）' },
]

/** 来源服务选项 */
export const sourceServiceOptions = [
  { value: 'System', label: '系统' },
  { value: 'MasterDataService', label: '基础数据服务' },
  { value: 'PersonService', label: '人员服务' },
  { value: 'TicketService', label: '工单服务' },
  { value: 'DeviceService', label: '设备服务' },
  { value: 'MaterialService', label: '物料服务' },
  { value: 'ContractService', label: '合同服务' },
  { value: 'FinanceService', label: '财务服务' },
  { value: 'ComplaintService', label: '投诉服务' },
  { value: 'VisitorService', label: '访客服务' },
  { value: 'NotificationService', label: '通知服务' },
  { value: 'KeyService', label: '钥匙服务' },
  { value: 'ParkingService', label: '车位服务' },
  { value: 'PaymentService', label: '缴费服务' },
  { value: 'InspectionService', label: '巡检服务' },
  { value: 'ProjectTrackingService', label: '项目跟踪服务' },
]

export { fieldConfigs, SHARED_FIELDS, ALIAS_MAP }