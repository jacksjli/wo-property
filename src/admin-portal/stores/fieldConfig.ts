import { ref } from 'vue'

// 字段类型
export type FieldType = 'text' | 'number' | 'date' | 'select' | 'textarea'

// 字段配置项
export interface FieldConfig {
  id: number
  module: string          // 模块标识
  name: string            // 字段显示名称
  key: string             // 字段标识（英文）
  type: FieldType         // 字段类型
  defaultValue: any       // 默认值
  required: boolean        // 是否必填
  status: 'Active' | 'Inactive'  // 状态
  options?: string[]      // 选项（用于select类型）
  width?: number          // 表格列宽
  align?: 'left' | 'center' | 'right'  // 对齐方式
}

// 模块字段配置
const fieldConfigs = ref<Record<string, FieldConfig[]>>({
  // 缴费管理 - 已完成示例
  payment: [
    { id: 1, module: 'payment', name: '物业费', key: 'propertyFee', type: 'number', defaultValue: 350, required: true, status: 'Active' },
    { id: 2, module: 'payment', name: '车位费', key: 'parkingFee', type: 'number', defaultValue: 500, required: false, status: 'Active' },
    { id: 3, module: 'payment', name: '水费', key: 'waterFee', type: 'number', defaultValue: 0, required: false, status: 'Active' },
    { id: 4, module: 'payment', name: '电费', key: 'electricFee', type: 'number', defaultValue: 0, required: false, status: 'Active' },
  ],
  
  // 工单管理 - 默认字段
  ticket: [
    { id: 1, module: 'ticket', name: '工单编号', key: 'ticketNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 2, module: 'ticket', name: '标题', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200 },
    { id: 3, module: 'ticket', name: '类型', key: 'type', type: 'select', defaultValue: '报修', required: true, status: 'Active', width: 100, options: ['报修', '投诉', '咨询', '建议'] },
    { id: 4, module: 'ticket', name: '地点', key: 'location', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120 },
    { id: 5, module: 'ticket', name: '状态', key: 'status', type: 'select', defaultValue: '待处理', required: true, status: 'Active', width: 100, options: ['待处理', '处理中', '已解决', '已关闭'] },
    { id: 6, module: 'ticket', name: '优先级', key: 'priority', type: 'select', defaultValue: '普通', required: false, status: 'Active', width: 80, options: ['紧急', '重要', '普通', '低'] },
    { id: 7, module: 'ticket', name: '创建人', key: 'creator', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 8, module: 'ticket', name: '创建时间', key: 'createTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120 },
  ],
  
  // 设备管理 - 默认字段
  device: [
    { id: 1, module: 'device', name: '设备编号', key: 'deviceNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 2, module: 'device', name: '设备名称', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 3, module: 'device', name: '设备类型', key: 'type', type: 'select', defaultValue: '监控', required: true, status: 'Active', width: 100, options: ['监控', '门禁', '消防', '电梯', '其他'] },
    { id: 4, module: 'device', name: '状态', key: 'status', type: 'select', defaultValue: '正常', required: true, status: 'Active', width: 100, options: ['正常', '维修中', '故障', '停用'] },
    { id: 5, module: 'device', name: '位置', key: 'location', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150 },
    { id: 6, module: 'device', name: '负责人', key: 'manager', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 7, module: 'device', name: '购买日期', key: 'buyDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120 },
  ],
  
  // 物料管理 - 默认字段
  material: [
    { id: 1, module: 'material', name: '物料编码', key: 'materialNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 2, module: 'material', name: '物料名称', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 3, module: 'material', name: '规格', key: 'spec', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 4, module: 'material', name: '单位', key: 'unit', type: 'text', defaultValue: '个', required: false, status: 'Active', width: 80 },
    { id: 5, module: 'material', name: '库存数量', key: 'quantity', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 100 },
    { id: 6, module: 'material', name: '单价', key: 'price', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100 },
    { id: 7, module: 'material', name: '状态', key: 'status', type: 'select', defaultValue: '充足', required: false, status: 'Active', width: 100, options: ['充足', '不足', '缺货'] },
  ],
  
  // 合同管理 - 默认字段
  contract: [
    { id: 1, module: 'contract', name: '合同编号', key: 'contractNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 2, module: 'contract', name: '合同名称', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200 },
    { id: 3, module: 'contract', name: '甲方', key: 'partyA', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 4, module: 'contract', name: '乙方', key: 'partyB', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 5, module: 'contract', name: '金额', key: 'amount', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 120 },
    { id: 6, module: 'contract', name: '签订日期', key: 'signDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120 },
    { id: 7, module: 'contract', name: '到期日期', key: 'expireDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120 },
    { id: 8, module: 'contract', name: '状态', key: 'status', type: 'select', defaultValue: '执行中', required: false, status: 'Active', width: 100, options: ['执行中', '已到期', '已终止'] },
  ],
  
  // 住户管理 - 默认字段
  resident: [
    { id: 1, module: 'resident', name: '房号', key: 'roomNo', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 2, module: 'resident', name: '业主姓名', key: 'name', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 3, module: 'resident', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 130 },
    { id: 4, module: 'resident', name: '身份证号', key: 'idCard', type: 'text', defaultValue: '', required: false, status: 'Active', width: 180 },
    { id: 5, module: 'resident', name: '家庭成员', key: 'familyMembers', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100 },
    { id: 6, module: 'resident', name: '入住日期', key: 'checkInDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120 },
    { id: 7, module: 'resident', name: '状态', key: 'status', type: 'select', defaultValue: '入住', required: false, status: 'Active', width: 100, options: ['入住', '未入住', '已迁出'] },
  ],
  
  // 车位管理 - 默认字段
  parking: [
    { id: 1, module: 'parking', name: '车位编号', key: 'spaceNumber', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 2, module: 'parking', name: '楼层', key: 'floor', type: 'select', defaultValue: '地下一层', required: true, status: 'Active', width: 100, options: ['地面', '地下一层', '地下二层', '地下三层'] },
    { id: 3, module: 'parking', name: '类型', key: 'type', type: 'select', defaultValue: '固定', required: true, status: 'Active', width: 80, options: ['固定', '临时'] },
    { id: 4, module: 'parking', name: '状态', key: 'status', type: 'select', defaultValue: '空闲', required: true, status: 'Active', width: 80, options: ['空闲', '已租用', '已占用', '已预定'] },
    { id: 5, module: 'parking', name: '车牌号', key: 'plateNumber', type: 'text', defaultValue: '', required: false, status: 'Active', width: 120 },
    { id: 6, module: 'parking', name: '车主姓名', key: 'ownerName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 7, module: 'parking', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: false, status: 'Active', width: 130 },
    { id: 8, module: 'parking', name: '月租费', key: 'monthlyFee', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100 },
    { id: 9, module: 'parking', name: '租金', key: 'rentalFee', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 100 },
    { id: 10, module: 'parking', name: '面积', key: 'area', type: 'number', defaultValue: 0, required: false, status: 'Active', width: 80 },
    { id: 11, module: 'parking', name: '备注', key: 'remark', type: 'textarea', defaultValue: '', required: false, status: 'Active', width: 200 },
  ],

  // 访客管理 - 默认字段
  visitor: [
    { id: 1, module: 'visitor', name: '访客姓名', key: 'visitorName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 2, module: 'visitor', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 130 },
    { id: 3, module: 'visitor', name: '访问单元', key: 'visitUnit', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 4, module: 'visitor', name: '访问原因', key: 'visitReason', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150 },
    { id: 5, module: 'visitor', name: '被访人', key: 'hostName', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 6, module: 'visitor', name: '进入时间', key: 'checkInTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 160 },
    { id: 7, module: 'visitor', name: '离开时间', key: 'checkOutTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 160 },
    { id: 8, module: 'visitor', name: '状态', key: 'status', type: 'select', defaultValue: '访问中', required: false, status: 'Active', width: 100, options: ['访问中', '已离开'] },
  ],

  // 钥匙管理 - 默认字段
  key: [
    { id: 1, module: 'key', name: '钥匙编号', key: 'keyNumber', type: 'text', defaultValue: '', required: true, status: 'Active', width: 110 },
    { id: 2, module: 'key', name: '钥匙名称', key: 'keyName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 3, module: 'key', name: '存放位置', key: 'location', type: 'text', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 4, module: 'key', name: '借用人', key: 'borrower', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 5, module: 'key', name: '借用时间', key: 'borrowTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 160 },
    { id: 6, module: 'key', name: '归还时间', key: 'returnTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 160 },
    { id: 7, module: 'key', name: '状态', key: 'status', type: 'select', defaultValue: '可用', required: false, status: 'Active', width: 100, options: ['可用', '已借出', '遗失'] },
  ],

  // 巡检管理 - 默认字段
  inspection: [
    { id: 1, module: 'inspection', name: '计划名称', key: 'planName', type: 'text', defaultValue: '', required: true, status: 'Active', width: 180 },
    { id: 2, module: 'inspection', name: '巡检路线', key: 'route', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 3, module: 'inspection', name: '巡检人员', key: 'inspector', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 4, module: 'inspection', name: '计划日期', key: 'scheduledDate', type: 'date', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 5, module: 'inspection', name: '实际日期', key: 'actualDate', type: 'date', defaultValue: '', required: false, status: 'Active', width: 120 },
    { id: 6, module: 'inspection', name: '巡检结果', key: 'result', type: 'textarea', defaultValue: '', required: false, status: 'Active', width: 200 },
    { id: 7, module: 'inspection', name: '状态', key: 'status', type: 'select', defaultValue: '待执行', required: false, status: 'Active', width: 100, options: ['待执行', '执行中', '已完成', '异常'] },
  ],

  // 通知管理 - 默认字段
  notification: [
    { id: 1, module: 'notification', name: '标题', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 200 },
    { id: 2, module: 'notification', name: '内容', key: 'content', type: 'textarea', defaultValue: '', required: true, status: 'Active', width: 300 },
    { id: 3, module: 'notification', name: '类型', key: 'type', type: 'select', defaultValue: '公告', required: true, status: 'Active', width: 100, options: ['公告', '提醒', '警告', '活动'] },
    { id: 4, module: 'notification', name: '发布人', key: 'publisher', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 5, module: 'notification', name: '发布时间', key: 'publishTime', type: 'date', defaultValue: '', required: false, status: 'Active', width: 160 },
    { id: 6, module: 'notification', name: '状态', key: 'status', type: 'select', defaultValue: '草稿', required: false, status: 'Active', width: 100, options: ['草稿', '已发布', '已下线'] },
  ],

  // 财务管理 - 默认字段
  finance: [
    { id: 1, module: 'finance', name: '账单编号', key: 'billNumber', type: 'text', defaultValue: '', required: true, status: 'Active', width: 130 },
    { id: 2, module: 'finance', name: '账单名称', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 180 },
    { id: 3, module: 'finance', name: '类别', key: 'category', type: 'select', defaultValue: '收入', required: true, status: 'Active', width: 100, options: ['收入', '支出'] },
    { id: 4, module: 'finance', name: '类型', key: 'type', type: 'select', defaultValue: '其他', required: true, status: 'Active', width: 80, options: ['物业费', '车位费', '维修费', '其他'] },
    { id: 5, module: 'finance', name: '金额', key: 'amount', type: 'number', defaultValue: 0, required: true, status: 'Active', width: 120 },
    { id: 6, module: 'finance', name: '日期', key: 'billDate', type: 'date', defaultValue: '', required: true, status: 'Active', width: 120 },
    { id: 7, module: 'finance', name: '付款方', key: 'payer', type: 'text', defaultValue: '', required: false, status: 'Active', width: 150 },
    { id: 8, module: 'finance', name: '状态', key: 'status', type: 'select', defaultValue: '未支付', required: false, status: 'Active', width: 100, options: ['未支付', '已支付', '已逾期'] },
  ],

  // 投诉管理 - 默认字段
  complaint: [
    { id: 1, module: 'complaint', name: '投诉编号', key: 'complaintNumber', type: 'text', defaultValue: '', required: true, status: 'Active', width: 130 },
    { id: 2, module: 'complaint', name: '投诉标题', key: 'title', type: 'text', defaultValue: '', required: true, status: 'Active', width: 150 },
    { id: 3, module: 'complaint', name: '类型', key: 'type', type: 'select', defaultValue: '服务投诉', required: true, status: 'Active', width: 100, options: ['服务投诉', '环境投诉', '设施投诉', '其他'] },
    { id: 4, module: 'complaint', name: '投诉人', key: 'complainant', type: 'text', defaultValue: '', required: true, status: 'Active', width: 100 },
    { id: 5, module: 'complaint', name: '联系电话', key: 'phone', type: 'text', defaultValue: '', required: true, status: 'Active', width: 130 },
    { id: 6, module: 'complaint', name: '投诉内容', key: 'content', type: 'textarea', defaultValue: '', required: true, status: 'Active', width: 250 },
    { id: 7, module: 'complaint', name: '处理人', key: 'handler', type: 'text', defaultValue: '', required: false, status: 'Active', width: 100 },
    { id: 8, module: 'complaint', name: '处理结果', key: 'result', type: 'textarea', defaultValue: '', required: false, status: 'Active', width: 200 },
    { id: 9, module: 'complaint', name: '状态', key: 'status', type: 'select', defaultValue: '待处理', required: false, status: 'Active', width: 100, options: ['待处理', '处理中', '已解决', '已关闭'] },
  ],
})

// 获取模块的字段配置
export const getModuleFields = (module: string) => {
  return fieldConfigs.value[module] || []
}

// 获取启用的字段
export const getActiveFields = (module: string) => {
  return getModuleFields(module).filter(f => f.status === 'Active')
}

// 添加字段
export const addField = (module: string, field: Omit<FieldConfig, 'id' | 'module' | 'status'>) => {
  if (!fieldConfigs.value[module]) {
    fieldConfigs.value[module] = []
  }
  const maxId = Math.max(...fieldConfigs.value[module].map(f => f.id), 0)
  fieldConfigs.value[module].push({
    ...field,
    id: maxId + 1,
    module,
    status: 'Active'
  })
}

// 更新字段
export const updateField = (module: string, id: number, updates: Partial<FieldConfig>) => {
  const fields = fieldConfigs.value[module]
  if (fields) {
    const index = fields.findIndex(f => f.id === id)
    if (index !== -1) {
      fields[index] = { ...fields[index], ...updates }
    }
  }
}

// 删除字段
export const deleteField = (module: string, id: number) => {
  const fields = fieldConfigs.value[module]
  if (fields) {
    const index = fields.findIndex(f => f.id === id)
    if (index !== -1) {
      fields.splice(index, 1)
    }
  }
}

// 切换字段状态
export const toggleFieldStatus = (module: string, id: number) => {
  const fields = fieldConfigs.value[module]
  if (fields) {
    const field = fields.find(f => f.id === id)
    if (field) {
      field.status = field.status === 'Active' ? 'Inactive' : 'Active'
    }
  }
}

// 获取字段类型选项
export const fieldTypeOptions = [
  { value: 'text', label: '文本' },
  { value: 'number', label: '数字' },
  { value: 'date', label: '日期' },
  { value: 'select', label: '下拉选择' },
  { value: 'textarea', label: '多行文本' },
]

// 获取对齐方式选项
export const alignOptions = [
  { value: 'left', label: '左对齐' },
  { value: 'center', label: '居中' },
  { value: 'right', label: '右对齐' },
]

// 自动生成key
export const autoGenerateKey = (name: string) => {
  return name
    .toLowerCase()
    .replace(/[^\u4e00-\u9fa5a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
}

export { fieldConfigs }
