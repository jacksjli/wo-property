<template>
  <div class="notification-center-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h1>通知中心</h1>
        <p>管理系统通知、公告和消息模板</p>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="showCreateNotificationDialog = true">
          <el-icon><Plus /></el-icon>
          发送通知
        </el-button>
        <el-button @click="showCreateAnnouncementDialog = true">
          <el-icon><Bell /></el-icon>
          发布公告
        </el-button>
      </div>
    </div>
    
    <!-- 统计卡片 -->
    <div class="stats-cards">
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon total">
            <el-icon><Bell /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.totalNotifications }}</div>
            <div class="stat-label">通知总数</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon unread">
            <el-icon><Message /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.unreadNotifications }}</div>
            <div class="stat-label">未读通知</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon announcements">
            <el-icon><ChatLineSquare /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.activeAnnouncements }}</div>
            <div class="stat-label">活动公告</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon templates">
            <el-icon><Document /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.messageTemplatesCount }}</div>
            <div class="stat-label">消息模板</div>
          </div>
        </div>
      </el-card>
    </div>
    
    <!-- 标签页 -->
    <el-card class="notifications-card" shadow="never">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <!-- 通知列表 -->
        <el-tab-pane label="通知列表" name="notifications">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-select
                v-model="filterNotificationType"
                placeholder="通知类型"
                clearable
                @change="loadNotifications"
              >
                <el-option label="工单通知" value="Ticket" />
                <el-option label="设备通知" value="Device" />
                <el-option label="物料通知" value="Material" />
                <el-option label="系统通知" value="System" />
                <el-option label="公告通知" value="Announcement" />
              </el-select>
              
              <el-select
                v-model="filterNotificationPriority"
                placeholder="优先级"
                clearable
                @change="loadNotifications"
              >
                <el-option label="紧急" value="Urgent" />
                <el-option label="高" value="High" />
                <el-option label="普通" value="Normal" />
                <el-option label="低" value="Low" />
              </el-select>
              
              <el-select
                v-model="filterReadStatus"
                placeholder="阅读状态"
                clearable
                @change="loadNotifications"
              >
                <el-option label="未读" value="false" />
                <el-option label="已读" value="true" />
              </el-select>
            </div>
            
            <div class="toolbar-right">
              <el-button 
                v-if="statistics.unreadNotifications > 0"
                @click="markAllAsRead"
              >
                <el-icon><Check /></el-icon>
                全部标记为已读
              </el-button>
              <el-button @click="loadNotifications">
                <el-icon><Refresh /></el-icon>
                刷新
              </el-button>
            </div>
          </div>
          
          <div class="notifications-list" v-loading="loadingNotifications">
            <div 
              v-for="notification in notifications" 
              :key="notification.id"
              class="notification-item"
              :class="{ unread: !notification.isRead, read: notification.isRead }"
              @click="handleNotificationClick(notification)"
            >
              <div class="notification-icon">
                <el-icon v-if="notification.type === 'Ticket'"><Ticket /></el-icon>
                <el-icon v-else-if="notification.type === 'Device'"><Monitor /></el-icon>
                <el-icon v-else-if="notification.type === 'Material'"><Box /></el-icon>
                <el-icon v-else-if="notification.type === 'Announcement'"><ChatLineSquare /></el-icon>
                <el-icon v-else><Bell /></el-icon>
              </div>
              
              <div class="notification-content">
                <div class="notification-header">
                  <span class="notification-title">{{ notification.title }}</span>
                  <span 
                    class="notification-priority"
                    :class="getPriorityClass(notification.priority)"
                  >
                    {{ getPriorityLabel(notification.priority) }}
                  </span>
                </div>
                <div class="notification-body">{{ notification.content }}</div>
                <div class="notification-footer">
                  <span class="notification-time">{{ formatDate(notification.createdAt) }}</span>
                  <span v-if="!notification.isRead" class="unread-badge">未读</span>
                </div>
              </div>
              
              <div class="notification-actions">
                <el-button 
                  v-if="!notification.isRead"
                  type="text" 
                  size="small"
                  @click.stop="markAsRead(notification.id)"
                >
                  标记已读
                </el-button>
              </div>
            </div>
            
            <el-empty 
              v-if="notifications.length === 0 && !loadingNotifications" 
              description="暂无通知" 
            />
          </div>
        </el-tab-pane>
        
        <!-- 系统公告 -->
        <el-tab-pane label="系统公告" name="announcements">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-select
                v-model="filterAnnouncementType"
                placeholder="公告类型"
                clearable
                @change="loadAnnouncements"
              >
                <el-option label="系统公告" value="System" />
                <el-option label="功能更新" value="Feature" />
                <el-option label="节假日通知" value="Holiday" />
                <el-option label="维护通知" value="Maintenance" />
              </el-select>
              
              <el-select
                v-model="filterAnnouncementStatus"
                placeholder="状态"
                clearable
                @change="loadAnnouncements"
              >
                <el-option label="活动中" value="true" />
                <el-option label="已过期" value="false" />
              </el-select>
            </div>
            
            <div class="toolbar-right">
              <el-button @click="loadAnnouncements">
                <el-icon><Refresh /></el-icon>
                刷新
              </el-button>
            </div>
          </div>
          
          <div class="announcements-list" v-loading="loadingAnnouncements">
            <div 
              v-for="announcement in announcements" 
              :key="announcement.id"
              class="announcement-item"
              :class="{ top: announcement.isTop }"
            >
              <div class="announcement-header">
                <div class="announcement-title-row">
                  <el-tag v-if="announcement.isTop" type="danger" size="small">置顶</el-tag>
                  <el-tag :type="getAnnouncementTypeTag(announcement.type)" size="small">
                    {{ getAnnouncementTypeLabel(announcement.type) }}
                  </el-tag>
                  <span class="announcement-title">{{ announcement.title }}</span>
                </div>
                <span class="announcement-time">{{ formatDate(announcement.createdAt) }}</span>
              </div>
              
              <div class="announcement-body">{{ announcement.content }}</div>
              
              <div class="announcement-footer">
                <span class="announcement-dates">
                  {{ formatDate(announcement.startDate) }}
                  <template v-if="announcement.endDate">
                    至 {{ formatDate(announcement.endDate) }}
                  </template>
                </span>
                <el-tag 
                  :type="announcement.isActive ? 'success' : 'info'" 
                  size="small"
                >
                  {{ announcement.isActive ? '活动中' : '已过期' }}
                </el-tag>
              </div>
            </div>
            
            <el-empty 
              v-if="announcements.length === 0 && !loadingAnnouncements" 
              description="暂无公告" 
            />
          </div>
        </el-tab-pane>
        
        <!-- 消息模板 -->
        <el-tab-pane label="消息模板" name="templates">
          <div class="tab-toolbar">
            <div class="toolbar-left">
              <el-select
                v-model="filterTemplateType"
                placeholder="模板类型"
                clearable
                @change="loadTemplates"
              >
                <el-option label="工单模板" value="Ticket" />
                <el-option label="设备模板" value="Device" />
                <el-option label="物料模板" value="Material" />
                <el-option label="公告模板" value="Announcement" />
              </el-select>
            </div>
            
            <div class="toolbar-right">
              <el-button @click="loadTemplates">
                <el-icon><Refresh /></el-icon>
                刷新
              </el-button>
            </div>
          </div>
          
          <div class="templates-list" v-loading="loadingTemplates">
            <el-table :data="templates" style="width: 100%">
              <el-table-column prop="name" label="模板名称" width="180" />
              <el-table-column prop="type" label="类型" width="120">
                <template #default="scope">
                  <el-tag size="small">{{ scope.row.type }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="subject" label="主题" />
              <el-table-column prop="variables" label="变量" width="200">
                <template #default="scope">
                  <span v-if="scope.row.variables" class="variables-text">
                    {{ scope.row.variables }}
                  </span>
                  <span v-else>-</span>
                </template>
              </el-table-column>
            </el-table>
            
            <el-empty 
              v-if="templates.length === 0 && !loadingTemplates" 
              description="暂无模板" 
            />
          </div>
        </el-tab-pane>
        
        <!-- 报表中心 -->
        <el-tab-pane label="报表中心" name="reports">
          <div class="reports-section">
            <h3>报表生成</h3>
            
            <div class="report-cards">
              <el-card class="report-card" shadow="never">
                <template #header>
                  <div class="report-card-header">
                    <span>工单统计报表</span>
                    <el-button type="primary" size="small" @click="generateTicketReport">
                      生成报表
                    </el-button>
                  </div>
                </template>
                <div v-if="ticketReport" class="report-content">
                  <div class="report-period">
                    报表周期: {{ ticketReport.period.start }} 至 {{ ticketReport.period.end }}
                  </div>
                  <div class="report-stats">
                    <div class="report-stat">
                      <span class="stat-label">工单总数</span>
                      <span class="stat-value">{{ ticketReport.totalTickets }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">新建工单</span>
                      <span class="stat-value new">{{ ticketReport.newTickets }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">处理中</span>
                      <span class="stat-value in-progress">{{ ticketReport.inProgressTickets }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">已解决</span>
                      <span class="stat-value resolved">{{ ticketReport.resolvedTickets }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">已关闭</span>
                      <span class="stat-value closed">{{ ticketReport.closedTickets }}</span>
                    </div>
                  </div>
                  <div class="report-generation-time">
                    生成时间: {{ ticketReport.generationTime }}
                  </div>
                </div>
                <div v-else class="report-placeholder">
                  点击"生成报表"按钮获取工单统计数据
                </div>
              </el-card>
              
              <el-card class="report-card" shadow="never">
                <template #header>
                  <div class="report-card-header">
                    <span>设备统计报表</span>
                    <el-button type="primary" size="small" @click="generateDeviceReport">
                      生成报表
                    </el-button>
                  </div>
                </template>
                <div v-if="deviceReport" class="report-content">
                  <div class="report-period">
                    报表周期: {{ deviceReport.period.start }} 至 {{ deviceReport.period.end }}
                  </div>
                  <div class="report-stats">
                    <div class="report-stat">
                      <span class="stat-label">设备总数</span>
                      <span class="stat-value">{{ deviceReport.totalDevices }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">活动中</span>
                      <span class="stat-value active">{{ deviceReport.activeDevices }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">维护中</span>
                      <span class="stat-value maintenance">{{ deviceReport.maintenanceDevices }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">维护记录</span>
                      <span class="stat-value">{{ deviceReport.totalMaintenanceRecords }}</span>
                    </div>
                  </div>
                  <div class="report-generation-time">
                    生成时间: {{ deviceReport.generationTime }}
                  </div>
                </div>
                <div v-else class="report-placeholder">
                  点击"生成报表"按钮获取设备统计数据
                </div>
              </el-card>
              
              <el-card class="report-card" shadow="never">
                <template #header>
                  <div class="report-card-header">
                    <span>物料统计报表</span>
                    <el-button type="primary" size="small" @click="generateMaterialReport">
                      生成报表
                    </el-button>
                  </div>
                </template>
                <div v-if="materialReport" class="report-content">
                  <div class="report-period">
                    报表周期: {{ materialReport.period.start }} 至 {{ materialReport.period.end }}
                  </div>
                  <div class="report-stats">
                    <div class="report-stat">
                      <span class="stat-label">物料种类</span>
                      <span class="stat-value">{{ materialReport.totalMaterials }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">低库存</span>
                      <span class="stat-value low">{{ materialReport.lowStockMaterials }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">缺货</span>
                      <span class="stat-value out">{{ materialReport.outOfStockMaterials }}</span>
                    </div>
                    <div class="report-stat">
                      <span class="stat-label">库存价值</span>
                      <span class="stat-value">¥{{ materialReport.totalStockValue.toLocaleString() }}</span>
                    </div>
                  </div>
                  <div class="report-generation-time">
                    生成时间: {{ materialReport.generationTime }}
                  </div>
                </div>
                <div v-else class="report-placeholder">
                  点击"生成报表"按钮获取物料统计数据
                </div>
              </el-card>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>
    
    <!-- 发送通知对话框 -->
    <el-dialog
      v-model="showCreateNotificationDialog"
      title="发送通知"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="notificationFormRef"
        :model="notificationForm"
        :rules="notificationRules"
        label-width="100px"
      >
        <el-form-item label="通知类型" prop="type">
          <el-select v-model="notificationForm.type" placeholder="请选择通知类型" style="width: 100%">
            <el-option label="工单通知" value="Ticket" />
            <el-option label="设备通知" value="Device" />
            <el-option label="物料通知" value="Material" />
            <el-option label="系统通知" value="System" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="目标用户" prop="userId">
          <el-select v-model="notificationForm.userId" placeholder="请选择目标用户" style="width: 100%">
            <el-option label="全体用户" :value="0" />
            <el-option label="管理员" :value="1" />
            <el-option label="技术人员" :value="2" />
            <el-option label="普通用户" :value="3" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="优先级" prop="priority">
          <el-select v-model="notificationForm.priority" placeholder="请选择优先级" style="width: 100%">
            <el-option label="紧急" value="Urgent" />
            <el-option label="高" value="High" />
            <el-option label="普通" value="Normal" />
            <el-option label="低" value="Low" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="通知标题" prop="title">
          <el-input v-model="notificationForm.title" placeholder="请输入通知标题" />
        </el-form-item>
        
        <el-form-item label="通知内容" prop="content">
          <el-input
            v-model="notificationForm.content"
            type="textarea"
            :rows="4"
            placeholder="请输入通知内容"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showCreateNotificationDialog = false">取消</el-button>
          <el-button type="primary" @click="createNotification" :loading="creatingNotification">
            发送
          </el-button>
        </span>
      </template>
    </el-dialog>
    
    <!-- 发布公告对话框 -->
    <el-dialog
      v-model="showCreateAnnouncementDialog"
      title="发布公告"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="announcementFormRef"
        :model="announcementForm"
        :rules="announcementRules"
        label-width="100px"
      >
        <el-form-item label="公告类型" prop="type">
          <el-select v-model="announcementForm.type" placeholder="请选择公告类型" style="width: 100%">
            <el-option label="系统公告" value="System" />
            <el-option label="功能更新" value="Feature" />
            <el-option label="节假日通知" value="Holiday" />
            <el-option label="维护通知" value="Maintenance" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="公告标题" prop="title">
          <el-input v-model="announcementForm.title" placeholder="请输入公告标题" />
        </el-form-item>
        
        <el-form-item label="公告内容" prop="content">
          <el-input
            v-model="announcementForm.content"
            type="textarea"
            :rows="5"
            placeholder="请输入公告内容"
          />
        </el-form-item>
        
        <el-form-item label="优先级">
          <el-select v-model="announcementForm.priority" placeholder="请选择优先级" style="width: 100%">
            <el-option label="高" value="High" />
            <el-option label="普通" value="Normal" />
            <el-option label="低" value="Low" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="置顶">
          <el-switch v-model="announcementForm.isTop" />
        </el-form-item>
        
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="开始日期">
              <el-date-picker
                v-model="announcementForm.startDate"
                type="datetime"
                placeholder="选择开始日期"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束日期">
              <el-date-picker
                v-model="announcementForm.endDate"
                type="datetime"
                placeholder="选择结束日期（可选）"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showCreateAnnouncementDialog = false">取消</el-button>
          <el-button type="primary" @click="createAnnouncement" :loading="creatingAnnouncement">
            发布
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { 
  Plus, Bell, Message, ChatLineSquare, Document, Ticket, 
  Monitor, Box, Refresh, Check 
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';
import { notificationApi, type Notification, type Announcement, type MessageTemplate, type TicketReport, type DeviceReport, type MaterialReport } from '@/api/notification';

const authStore = useAuthStore();

// 统计数据
const statistics = ref({
  totalNotifications: 0,
  unreadNotifications: 0,
  activeAnnouncements: 0,
  messageTemplatesCount: 0
});

// 通知数据
const notifications = ref<Notification[]>([]);
const announcements = ref<Announcement[]>([]);
const templates = ref<MessageTemplate[]>([]);

// 报表数据
const ticketReport = ref<TicketReport | null>(null);
const deviceReport = ref<DeviceReport | null>(null);
const materialReport = ref<MaterialReport | null>(null);

// 加载状态
const loadingNotifications = ref(false);
const loadingAnnouncements = ref(false);
const loadingTemplates = ref(false);

// 标签页
const activeTab = ref('notifications');

// 筛选条件
const filterNotificationType = ref('');
const filterNotificationPriority = ref('');
const filterReadStatus = ref('');
const filterAnnouncementType = ref('');
const filterAnnouncementStatus = ref('');
const filterTemplateType = ref('');

// 对话框状态
const showCreateNotificationDialog = ref(false);
const showCreateAnnouncementDialog = ref(false);
const creatingNotification = ref(false);
const creatingAnnouncement = ref(false);

// 通知表单
const notificationFormRef = ref<FormInstance>();
const notificationForm = ref({
  userId: 0,
  title: '',
  content: '',
  type: 'System',
  priority: 'Normal',
  relatedEntityType: '',
  relatedEntityId: undefined as number | undefined
});

// 公告表单
const announcementFormRef = ref<FormInstance>();
const announcementForm = ref({
  title: '',
  content: '',
  type: 'System',
  priority: 'Normal',
  isTop: false,
  startDate: new Date(),
  endDate: undefined as Date | undefined,
  createdBy: 1
});

// 表单验证规则
const notificationRules: FormRules = {
  userId: [
    { required: true, message: '请选择目标用户', trigger: 'change' }
  ],
  title: [
    { required: true, message: '请输入通知标题', trigger: 'blur' },
    { min: 2, message: '标题至少2个字符', trigger: 'blur' }
  ],
  content: [
    { required: true, message: '请输入通知内容', trigger: 'blur' }
  ],
  type: [
    { required: true, message: '请选择通知类型', trigger: 'change' }
  ]
};

const announcementRules: FormRules = {
  title: [
    { required: true, message: '请输入公告标题', trigger: 'blur' },
    { min: 2, message: '标题至少2个字符', trigger: 'blur' }
  ],
  content: [
    { required: true, message: '请输入公告内容', trigger: 'blur' }
  ],
  type: [
    { required: true, message: '请选择公告类型', trigger: 'change' }
  ]
};

// 获取优先级标签
const getPriorityLabel = (priority: string) => {
  const labels: Record<string, string> = {
    Urgent: '紧急',
    High: '高',
    Normal: '普通',
    Low: '低'
  };
  return labels[priority] || priority;
};

// 获取优先级类
const getPriorityClass = (priority: string) => {
  const classes: Record<string, string> = {
    Urgent: 'priority-urgent',
    High: 'priority-high',
    Normal: 'priority-normal',
    Low: 'priority-low'
  };
  return classes[priority] || '';
};

// 获取公告类型标签
const getAnnouncementTypeLabel = (type: string) => {
  const labels: Record<string, string> = {
    System: '系统',
    Feature: '功能',
    Holiday: '假日',
    Maintenance: '维护'
  };
  return labels[type] || type;
};

// 获取公告类型标签样式
const getAnnouncementTypeTag = (type: string) => {
  const tags: Record<string, string> = {
    System: '',
    Feature: 'success',
    Holiday: 'warning',
    Maintenance: 'info'
  };
  return tags[type] || '';
};

// 格式化日期
const formatDate = (dateString: string) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 加载统计数据
const loadStatistics = async () => {
  try {
    const response = await notificationApi.getStatistics();
    if (response.success) {
      statistics.value = response.data.statistics;
    }
  } catch (error) {
    console.error('加载统计失败:', error);
  }
};

// 加载通知列表
const loadNotifications = async () => {
  loadingNotifications.value = true;
  try {
    const params: any = {};
    if (filterNotificationType.value) params.type = filterNotificationType.value;
    if (filterNotificationPriority.value) params.priority = filterNotificationPriority.value;
    if (filterReadStatus.value) params.isRead = filterReadStatus.value === 'true';
    
    const response = await notificationApi.getNotifications(params);
    if (response.success) {
      notifications.value = response.data.notifications;
    }
  } catch (error) {
    console.error('加载通知失败:', error);
  } finally {
    loadingNotifications.value = false;
  }
};

// 加载公告列表
const loadAnnouncements = async () => {
  loadingAnnouncements.value = true;
  try {
    const params: any = {};
    if (filterAnnouncementType.value) params.type = filterAnnouncementType.value;
    if (filterAnnouncementStatus.value) params.isActive = filterAnnouncementStatus.value === 'true';
    
    const response = await notificationApi.getAnnouncements(params);
    if (response.success) {
      announcements.value = response.data.announcements;
    }
  } catch (error) {
    console.error('加载公告失败:', error);
  } finally {
    loadingAnnouncements.value = false;
  }
};

// 加载消息模板
const loadTemplates = async () => {
  loadingTemplates.value = true;
  try {
    const params: any = {};
    if (filterTemplateType.value) params.type = filterTemplateType.value;
    
    const response = await notificationApi.getMessageTemplates(params);
    if (response.success) {
      templates.value = response.data.templates;
    }
  } catch (error) {
    console.error('加载模板失败:', error);
  } finally {
    loadingTemplates.value = false;
  }
};

// 标记单条通知为已读
const markAsRead = async (id: number) => {
  try {
    const response = await notificationApi.markAsRead(id);
    if (response.success) {
      ElMessage.success('已标记为已读');
      loadNotifications();
      loadStatistics();
    }
  } catch (error) {
    ElMessage.error('操作失败');
  }
};

// 标记所有通知为已读
const markAllAsRead = async () => {
  try {
    const response = await notificationApi.markAllAsRead(1); // 使用用户ID 1作为示例
    if (response.success) {
      ElMessage.success(`已标记 ${response.data.count} 条通知为已读`);
      loadNotifications();
      loadStatistics();
    }
  } catch (error) {
    ElMessage.error('操作失败');
  }
};

// 处理通知点击
const handleNotificationClick = (notification: Notification) => {
  if (!notification.isRead) {
    markAsRead(notification.id);
  }
  
  // 根据通知类型导航到相关页面
  if (notification.relatedEntityType === 'Ticket' && notification.relatedEntityId) {
    // 导航到工单详情
  } else if (notification.relatedEntityType === 'Device' && notification.relatedEntityId) {
    // 导航到设备详情
  } else if (notification.relatedEntityType === 'Announcement' && notification.relatedEntityId) {
    // 导航到公告详情
  }
};

// 创建通知
const createNotification = async () => {
  if (!notificationFormRef.value) return;
  
  try {
    await notificationFormRef.value.validate();
    creatingNotification.value = true;
    
    const response = await notificationApi.createNotification(notificationForm.value);
    
    if (response.success) {
      ElMessage.success('通知发送成功');
      showCreateNotificationDialog.value = false;
      notificationFormRef.value.resetFields();
      loadNotifications();
      loadStatistics();
    } else {
      ElMessage.error(response.message || '发送失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    creatingNotification.value = false;
  }
};

// 创建公告
const createAnnouncement = async () => {
  if (!announcementFormRef.value) return;
  
  try {
    await announcementFormRef.value.validate();
    creatingAnnouncement.value = true;
    
    const response = await notificationApi.createAnnouncement({
      title: announcementForm.value.title,
      content: announcementForm.value.content,
      type: announcementForm.value.type,
      isTop: announcementForm.value.isTop,
      priority: announcementForm.value.priority,
      startDate: announcementForm.value.startDate?.toISOString(),
      endDate: announcementForm.value.endDate?.toISOString(),
      createdBy: authStore.user?.id || 1
    });
    
    if (response.success) {
      ElMessage.success('公告发布成功');
      showCreateAnnouncementDialog.value = false;
      announcementFormRef.value.resetFields();
      loadAnnouncements();
      loadStatistics();
    } else {
      ElMessage.error(response.message || '发布失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    creatingAnnouncement.value = false;
  }
};

// 生成工单报表
const generateTicketReport = async () => {
  try {
    const response = await notificationApi.getTicketReport();
    if (response.success) {
      ticketReport.value = response.data.report;
      ElMessage.success('工单报表生成成功');
    }
  } catch (error) {
    ElMessage.error('报表生成失败');
  }
};

// 生成设备报表
const generateDeviceReport = async () => {
  try {
    const response = await notificationApi.getDeviceReport();
    if (response.success) {
      deviceReport.value = response.data.report;
      ElMessage.success('设备报表生成成功');
    }
  } catch (error) {
    ElMessage.error('报表生成失败');
  }
};

// 生成物料报表
const generateMaterialReport = async () => {
  try {
    const response = await notificationApi.getMaterialReport();
    if (response.success) {
      materialReport.value = response.data.report;
      ElMessage.success('物料报表生成成功');
    }
  } catch (error) {
    ElMessage.error('报表生成失败');
  }
};

// 处理标签页切换
const handleTabChange = (tabName: string) => {
  switch (tabName) {
    case 'notifications':
      loadNotifications();
      break;
    case 'announcements':
      loadAnnouncements();
      break;
    case 'templates':
      loadTemplates();
      break;
    case 'reports':
      // 报表数据在点击按钮时加载
      break;
  }
};

// 组件挂载时加载数据
onMounted(() => {
  loadStatistics();
  loadNotifications();
});
</script>

<style scoped>
.notification-center-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 页面标题 */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 8px;
}

.header-left h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px 0;
}

.header-left p {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

.header-right {
  display: flex;
  gap: 12px;
}

/* 统计卡片 */
.stats-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.stat-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.stat-icon .el-icon {
  color: white;
}

.total {
  background-color: #3b82f6;
}

.unread {
  background-color: #f59e0b;
}

.announcements {
  background-color: #10b981;
}

.templates {
  background-color: #8b5cf6;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: #1f2937;
  line-height: 1;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  margin-top: 4px;
}

/* 通知卡片 */
.notifications-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

/* 标签页工具栏 */
.tab-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  flex-wrap: wrap;
  gap: 12px;
}

.toolbar-left {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-right {
  display: flex;
  gap: 12px;
}

/* 通知列表 */
.notifications-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.notification-item {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 16px;
  border-radius: 8px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
  cursor: pointer;
  transition: all 0.3s;
}

.notification-item:hover {
  border-color: #3b82f6;
}

.notification-item.unread {
  background-color: #eff6ff;
  border-color: #bfdbfe;
}

.notification-item.read {
  opacity: 0.8;
}

.notification-icon {
  width: 40px;
  height: 40px;
  border-radius: 8px;
  background-color: #3b82f6;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
}

.notification-icon .el-icon {
  color: white;
}

.notification-content {
  flex: 1;
}

.notification-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.notification-title {
  font-weight: 600;
  color: #1f2937;
}

.notification-priority {
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
}

.priority-urgent {
  background-color: #fee2e2;
  color: #991b1b;
}

.priority-high {
  background-color: #ffedd5;
  color: #9a3412;
}

.priority-normal {
  background-color: #dbeafe;
  color: #1e40af;
}

.priority-low {
  background-color: #f3f4f6;
  color: #4b5563;
}

.notification-body {
  font-size: 14px;
  color: #6b7280;
  margin-bottom: 8px;
  line-height: 1.5;
}

.notification-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.notification-time {
  font-size: 12px;
  color: #9ca3af;
}

.unread-badge {
  font-size: 11px;
  padding: 2px 6px;
  border-radius: 4px;
  background-color: #ef4444;
  color: white;
}

.notification-actions {
  flex-shrink: 0;
}

/* 公告列表 */
.announcements-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.announcement-item {
  padding: 16px;
  border-radius: 8px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
}

.announcement-item.top {
  background-color: #fff7ed;
  border-color: #fed7aa;
}

.announcement-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 12px;
}

.announcement-title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.announcement-title {
  font-weight: 600;
  color: #1f2937;
  font-size: 16px;
}

.announcement-time {
  font-size: 12px;
  color: #9ca3af;
}

.announcement-body {
  font-size: 14px;
  color: #6b7280;
  line-height: 1.6;
  margin-bottom: 12px;
}

.announcement-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.announcement-dates {
  font-size: 12px;
  color: #9ca3af;
}

/* 消息模板 */
.variables-text {
  font-size: 12px;
  color: #6b7280;
  font-family: monospace;
}

/* 报表部分 */
.reports-section h3 {
  margin: 0 0 16px 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.report-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 16px;
}

.report-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.report-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.report-card-header span {
  font-weight: 600;
  color: #1f2937;
}

.report-content {
  padding: 8px 0;
}

.report-period {
  font-size: 12px;
  color: #6b7280;
  margin-bottom: 12px;
}

.report-stats {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  margin-bottom: 12px;
}

.report-stat {
  display: flex;
  flex-direction: column;
  padding: 12px;
  background-color: #f9fafb;
  border-radius: 8px;
}

.report-stat .stat-label {
  font-size: 12px;
  color: #6b7280;
  margin-bottom: 4px;
}

.report-stat .stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #1f2937;
}

.report-stat .stat-value.new {
  color: #3b82f6;
}

.report-stat .stat-value.in-progress {
  color: #f59e0b;
}

.report-stat .stat-value.resolved {
  color: #10b981;
}

.report-stat .stat-value.closed {
  color: #6b7280;
}

.report-stat .stat-value.active {
  color: #10b981;
}

.report-stat .stat-value.maintenance {
  color: #f59e0b;
}

.report-stat .stat-value.low {
  color: #f59e0b;
}

.report-stat .stat-value.out {
  color: #ef4444;
}

.report-generation-time {
  font-size: 11px;
  color: #9ca3af;
  text-align: right;
}

.report-placeholder {
  text-align: center;
  padding: 32px;
  color: #9ca3af;
  font-size: 14px;
}

/* 对话框 */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
