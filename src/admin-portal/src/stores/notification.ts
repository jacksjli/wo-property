import { ref } from 'vue'

// 通知类型
export type NotificationType = 'system' | 'notice' | 'alert' | 'reminder' | 'announcement'

// 通知级别
export type NotificationLevel = 'info' | 'warning' | 'important' | 'urgent'

// 通知状态
export type NotificationStatus = 'draft' | 'published' | 'cancelled' | 'expired'

// 发送方式
export type SendMethod = 'app' | 'sms' | 'wechat' | 'email' | 'screen' | 'all'

// 发送范围
export type SendScope = 'all' | 'building' | 'floor' | 'room' | 'custom'

// 通知记录
export interface Notification {
  id: number
  notificationNo: string       // 通知编号
  title: string              // 通知标题
  type: NotificationType    // 通知类型
  level: NotificationLevel  // 通知级别
  content: string           // 通知内容
  sendMethod: SendMethod    // 发送方式
  sendScope: SendScope      // 发送范围
  targetBuildings?: string[] // 目标楼栋
  targetFloors?: string[]   // 目标楼层
  targetRooms?: string[]     // 目标房号
  publisher: string         // 发布人
  publishTime?: string       // 发布时间
  startTime?: string        // 开始时间
  endTime?: string         // 结束时间
  status: NotificationStatus  // 状态
  readCount: number        // 已读人数
  unreadCount: number     // 未读人数
  totalCount: number      // 发送总人数
  attachments: string[]    // 附件
  isPinned: boolean        // 是否置顶
  isDeleted: boolean      // 是否删除
  createdAt: string       // 创建时间
  remark: string          // 备注
}

// 阅读记录
export interface ReadRecord {
  id: number
  notificationId: number
  userName: string
  userRoom: string
  readTime: string
}

// 类型标签
export const notificationTypeLabels: Record<NotificationType, string> = {
  'system': '系统通知',
  'notice': '温馨提示',
  'alert': '预警通知',
  'reminder': '提醒通知',
  'announcement': '公告'
}

// 级别标签
export const notificationLevelLabels: Record<NotificationLevel, string> = {
  'info': '普通',
  'warning': '重要',
  'important': '非常重要',
  'urgent': '紧急'
}

// 状态标签
export const notificationStatusLabels: Record<NotificationStatus, string> = {
  'draft': '草稿',
  'published': '已发布',
  'cancelled': '已撤回',
  'expired': '已过期'
}

// 发送方式标签
export const sendMethodLabels: Record<SendMethod, string> = {
  'app': 'APP推送',
  'sms': '短信',
  'wechat': '微信公众号',
  'email': '邮件',
  'screen': '电子屏',
  'all': '全部'
}

// 存储键名
const STORAGE_KEY = 'wo_notifications'

// 从 localStorage 加载数据
const loadFromStorage = (): Notification[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      if (Array.isArray(parsed)) {
        return parsed
      }
    }
  } catch (error) {
    console.error('加载通知数据失败:', error)
  }
  return []
}

// 保存到 localStorage
const saveToStorage = (data: Notification[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch (error) {
    console.error('保存通知数据失败:', error)
  }
}

// 数据
const notifications = ref<Notification[]>(loadFromStorage())

// 默认数据
if (notifications.value.length === 0) {
  const now = new Date().toISOString()
  const today = now.split('T')[0]
  
  notifications.value = [
    {
      id: 1,
      notificationNo: 'NOT-2024-001',
      title: '小区电梯维保通知',
      type: 'notice',
      level: 'warning',
      content: '各位业主：\n\n为确保电梯安全运行，物业将安排专业维保人员对小区内所有电梯进行月度维保工作。\n\n维保时间：4月15日 9:00-17:00\n受影响电梯：A栋、B栋电梯\n\n届时电梯将暂停使用，给您带来不便敬请谅解！\n\n物业服务中心',
      sendMethod: 'all',
      sendScope: 'all',
      publisher: '物业管理员',
      publishTime: now,
      startTime: today + 'T09:00:00',
      endTime: today + 'T18:00:00',
      status: 'published',
      readCount: 45,
      unreadCount: 15,
      totalCount: 60,
      attachments: [],
      isPinned: true,
      isDeleted: false,
      createdAt: now,
      remark: ''
    },
    {
      id: 2,
      notificationNo: 'NOT-2024-002',
      title: '五一劳动节放假安排',
      type: 'announcement',
      level: 'info',
      content: '各位业主/住户：\n\n根据国家法定节假日安排，小区物业服务中心五一假期安排如下：\n\n放假时间：5月1日-5月5日\n值班电话：021-12345678\n\n假期期间物业服务中心将安排人员值班，如需帮助请致电。\n\n祝大家节日快乐！',
      sendMethod: 'all',
      sendScope: 'all',
      publisher: '物业经理',
      publishTime: now,
      startTime: today + 'T00:00:00',
      status: 'published',
      readCount: 30,
      unreadCount: 30,
      totalCount: 60,
      attachments: [],
      isPinned: false,
      isDeleted: false,
      createdAt: now,
      remark: ''
    },
    {
      id: 3,
      notificationNo: 'NOT-2024-003',
      title: '水费缴费提醒',
      type: 'reminder',
      level: 'warning',
      content: '尊敬的业主：\n\n您2024年3月的水费账单已生成，请于4月20日前完成缴费。逾期未缴将按相关规定处理。\n\n如有疑问请联系物业服务中心。',
      sendMethod: 'app',
      sendScope: 'custom',
      targetBuildings: ['A栋', 'B栋'],
      publisher: '财务部',
      publishTime: now,
      startTime: today + 'T08:00:00',
      endTime: '2024-04-20T23:59:59',
      status: 'published',
      readCount: 20,
      unreadCount: 25,
      totalCount: 45,
      attachments: [],
      isPinned: false,
      isDeleted: false,
      createdAt: now,
      remark: ''
    },
    {
      id: 4,
      notificationNo: 'NOT-2024-004',
      title: '关于规范电动车停放的通知',
      type: 'alert',
      level: 'important',
      content: '各位业主：\n\n为加强小区安全管理，预防电动车火灾事故，现将电动车停放规范通知如下：\n\n1. 电动车需停放在指定停车区域\n2. 严禁在楼道、公共区域充电\n3. 违规停放将被移至指定地点\n\n请大家配合！',
      sendMethod: 'all',
      sendScope: 'all',
      publisher: '安全管理部',
      publishTime: now,
      status: 'published',
      readCount: 50,
      unreadCount: 10,
      totalCount: 60,
      attachments: [],
      isPinned: true,
      isDeleted: false,
      createdAt: now,
      remark: '重要安全通知'
    }
  ]
  saveToStorage(notifications.value)
}

let notificationIdCounter = Math.max(...notifications.value.map(n => n.id), 0) + 1

// 获取统计数据
export const getNotificationStats = () => {
  return {
    total: notifications.value.filter(n => !n.isDeleted).length,
    published: notifications.value.filter(n => n.status === 'published' && !n.isDeleted).length,
    draft: notifications.value.filter(n => n.status === 'draft' && !n.isDeleted).length,
    pinned: notifications.value.filter(n => n.isPinned && n.status === 'published' && !n.isDeleted).length,
    totalReads: notifications.value.reduce((sum, n) => sum + n.readCount, 0)
  }
}

// 获取所有通知
export const getAllNotifications = () => notifications.value.filter(n => !n.isDeleted)

// 获取指定通知
export const getNotificationById = (id: number) => notifications.value.find(n => n.id === id)

// 按状态获取通知
export const getNotificationsByStatus = (status: NotificationStatus) => 
  notifications.value.filter(n => n.status === status && !n.isDeleted)

// 获取置顶通知
export const getPinnedNotifications = () => 
  notifications.value.filter(n => n.isPinned && n.status === 'published' && !n.isDeleted)

// 添加通知
export const addNotification = (notification: Omit<Notification, 'id'>): Notification => {
  const newNotification: Notification = {
    ...notification,
    id: notificationIdCounter++,
    readCount: 0,
    unreadCount: 0,
    totalCount: 0,
    isDeleted: false,
    createdAt: new Date().toISOString()
  }
  notifications.value.unshift(newNotification)
  saveToStorage(notifications.value)
  return newNotification
}

// 更新通知
export const updateNotification = (id: number, updates: Partial<Notification>) => {
  const index = notifications.value.findIndex(n => n.id === id)
  if (index !== -1) {
    notifications.value[index] = { ...notifications.value[index], ...updates }
    saveToStorage(notifications.value)
  }
}

// 删除通知
export const deleteNotification = (id: number) => {
  const notification = notifications.value.find(n => n.id === id)
  if (notification) {
    notification.isDeleted = true
    saveToStorage(notifications.value)
  }
}

// 发布通知
export const publishNotification = (id: number) => {
  const notification = notifications.value.find(n => n.id === id)
  if (notification) {
    notification.status = 'published'
    notification.publishTime = new Date().toISOString()
    saveToStorage(notifications.value)
  }
}

// 撤回通知
export const cancelNotification = (id: number) => {
  const notification = notifications.value.find(n => n.id === id)
  if (notification) {
    notification.status = 'cancelled'
    saveToStorage(notifications.value)
  }
}

// 置顶/取消置顶
export const togglePinNotification = (id: number) => {
  const notification = notifications.value.find(n => n.id === id)
  if (notification) {
    notification.isPinned = !notification.isPinned
    saveToStorage(notifications.value)
  }
}

// 获取级别颜色
export const getLevelColor = (level: NotificationLevel) => {
  const colors: Record<NotificationLevel, string> = {
    info: '#409EFF',
    warning: '#E6A23C',
    important: '#F56C6C',
    urgent: '#F56C6C'
  }
  return colors[level]
}

// 获取状态类型
export const getStatusType = (status: NotificationStatus) => {
  const types: Record<NotificationStatus, string> = {
    draft: 'info',
    published: 'success',
    cancelled: 'warning',
    expired: ''
  }
  return types[status]
}

export const notificationStore = {
  notifications,
  getNotificationStats,
  getAllNotifications,
  getNotificationById,
  getNotificationsByStatus,
  getPinnedNotifications,
  addNotification,
  updateNotification,
  deleteNotification,
  publishNotification,
  cancelNotification,
  togglePinNotification,
  getLevelColor,
  getStatusType
}