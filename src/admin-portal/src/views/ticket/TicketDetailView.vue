<template>
  <div class="ticket-detail-view">
    <!-- 返回按钮 -->
    <div class="back-button">
      <el-button type="text" @click="$router.push('/tickets')">
        <el-icon><ArrowLeft /></el-icon>
        返回工单列表
      </el-button>
    </div>
    
    <!-- 工单头部 -->
    <el-card class="ticket-header-card" shadow="never">
      <div class="ticket-header">
        <div class="ticket-title">
          <h1>{{ ticketStore.currentTicket?.title }}</h1>
          <div class="ticket-meta">
            <el-tag :type="getStatusType(ticketStore.currentTicket?.status)" size="large">
              {{ getStatusText(ticketStore.currentTicket?.status) }}
            </el-tag>
            <el-tag :type="getPriorityType(ticketStore.currentTicket?.priority)" size="large">
              {{ ticketStore.currentTicket?.priority }}
            </el-tag>
            <span class="ticket-code">{{ ticketStore.currentTicket?.ticketCode }}</span>
          </div>
        </div>
        
        <div class="ticket-actions">
          <el-button 
            type="primary" 
            @click="editMode = !editMode"
            v-if="authStore.isAdmin || authStore.isTechnician"
          >
            <el-icon><Edit /></el-icon>
            {{ editMode ? '取消编辑' : '编辑工单' }}
          </el-button>
          <el-button 
            type="danger" 
            @click="handleDelete"
            v-if="authStore.isAdmin"
          >
            <el-icon><Delete /></el-icon>
            删除工单
          </el-button>
        </div>
      </div>
    </el-card>
    
    <div class="detail-content">
      <!-- 左侧：工单详情 -->
      <div class="left-column">
        <!-- 工单信息卡片 -->
        <el-card class="info-card" shadow="never">
          <template #header>
            <h3>工单信息</h3>
          </template>
          
          <el-form
            ref="ticketFormRef"
            :model="ticketForm"
            :rules="ticketRules"
            label-width="100px"
            :disabled="!editMode"
          >
            <el-form-item label="工单标题" prop="title">
              <el-input v-model="ticketForm.title" />
            </el-form-item>
            
            <el-form-item label="工单描述">
              <el-input
                v-model="ticketForm.description"
                type="textarea"
                :rows="4"
                placeholder="请输入工单描述"
                maxlength="1000"
                show-word-limit
              />
            </el-form-item>
            
            <el-form-item label="工单分类">
              <el-select v-model="ticketForm.category" placeholder="请选择分类">
                <el-option
                  v-for="type in ticketTypeStore.ticketTypes"
                  :key="type.id"
                  :label="type.name"
                  :value="type.name"
                />
              </el-select>
            </el-form-item>
            
            <el-row :gutter="20">
              <el-col :span="12">
                <el-form-item label="优先级">
                  <el-select v-model="ticketForm.priority" placeholder="请选择优先级">
                    <el-option label="高" value="High" />
                    <el-option label="中" value="Medium" />
                    <el-option label="低" value="Low" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="状态">
                  <el-select v-model="ticketForm.status" placeholder="请选择状态">
                    <el-option label="新建" value="New" />
                    <el-option label="进行中" value="InProgress" />
                    <el-option label="已解决" value="Resolved" />
                    <el-option label="已关闭" value="Closed" />
                  </el-select>
                </el-form-item>
              </el-col>
            </el-row>
            
            <el-form-item label="分配给" v-if="authStore.isAdmin || authStore.isTechnician">
              <el-select 
                v-model="ticketForm.assignedTo" 
                placeholder="请选择负责人"
                clearable
              >
                <el-option label="未分配" :value="undefined" />
                <el-option label="技术人员" value="2" />
                <el-option label="管理员" value="1" />
              </el-select>
            </el-form-item>
            
            <div v-if="editMode" class="form-actions">
              <el-button @click="cancelEdit">取消</el-button>
              <el-button type="primary" @click="saveTicket" :loading="saving">
                保存更改
              </el-button>
            </div>
          </el-form>
        </el-card>
        
        <!-- 操作记录卡片 -->
        <el-card class="activity-card" shadow="never">
          <template #header>
            <h3>操作记录</h3>
          </template>
          
          <div class="activity-list">
            <div class="activity-item">
              <div class="activity-icon">
                <el-icon><Plus /></el-icon>
              </div>
              <div class="activity-content">
                <div class="activity-title">工单创建</div>
                <div class="activity-desc">工单已创建</div>
                <div class="activity-time">
                  {{ formatDate(ticketStore.currentTicket?.createdAt) }}
                </div>
              </div>
            </div>
            
            <div v-if="ticketStore.currentTicket?.updatedAt" class="activity-item">
              <div class="activity-icon">
                <el-icon><Edit /></el-icon>
              </div>
              <div class="activity-content">
                <div class="activity-title">工单更新</div>
                <div class="activity-desc">工单信息已更新</div>
                <div class="activity-time">
                  {{ formatDate(ticketStore.currentTicket.updatedAt) }}
                </div>
              </div>
            </div>
            
            <div v-if="ticketStore.currentTicket?.assignedTo" class="activity-item">
              <div class="activity-icon">
                <el-icon><User /></el-icon>
              </div>
              <div class="activity-content">
                <div class="activity-title">工单分配</div>
                <div class="activity-desc">工单已分配给负责人</div>
                <div class="activity-time">
                  {{ formatDate(ticketStore.currentTicket.updatedAt) }}
                </div>
              </div>
            </div>
            
            <div class="activity-item" v-if="ticketStore.currentTicket?.status === 'Resolved'">
              <div class="activity-icon">
                <el-icon><Check /></el-icon>
              </div>
              <div class="activity-content">
                <div class="activity-title">工单解决</div>
                <div class="activity-desc">工单已标记为已解决</div>
                <div class="activity-time">
                  {{ formatDate(ticketStore.currentTicket.updatedAt) }}
                </div>
              </div>
            </div>
            
            <div class="activity-item" v-if="ticketStore.currentTicket?.status === 'Closed'">
              <div class="activity-icon">
                <el-icon><CircleClose /></el-icon>
              </div>
              <div class="activity-content">
                <div class="activity-title">工单关闭</div>
                <div class="activity-desc">工单已关闭</div>
                <div class="activity-time">
                  {{ formatDate(ticketStore.currentTicket.updatedAt) }}
                </div>
              </div>
            </div>
          </div>
        </el-card>
      </div>
      
      <!-- 右侧：统计和操作 -->
      <div class="right-column">
        <!-- 工单统计卡片 -->
        <el-card class="stats-card" shadow="never">
          <template #header>
            <h3>工单统计</h3>
          </template>
          
          <div class="stats-content">
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Calendar /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-label">创建时间</div>
                <div class="stat-value">
                  {{ formatDate(ticketStore.currentTicket?.createdAt) }}
                </div>
              </div>
            </div>
            
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Clock /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-label">处理时长</div>
                <div class="stat-value">{{ calculateProcessingTime() }}</div>
              </div>
            </div>
            
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><User /></el-icon>
              </div>
              <div class="stat-info">
            </div>
            
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Check /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-label">负责人</div>
                <div class="stat-value">
                  {{ ticketStore.currentTicket?.assignedTo ? `用户 ${ticketStore.currentTicket.assignedTo}` : '未分配' }}
                </div>
              </div>
            </div>
          </div>
        </el-card>
        
        <!-- 快速操作卡片 -->
        <el-card class="quick-actions-card" shadow="never">
          <template #header>
            <h3>快速操作</h3>
          </template>
          
          <div class="quick-actions">
            <!-- 派单按钮 -->
            <el-button
              type="primary"
              @click="showDispatchDialog = true"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'New' && authStore.isAdmin"
            >
              <el-icon><Position /></el-icon>
              派单
            </el-button>

            <!-- 接单按钮 -->
            <el-button
              type="success"
              @click="handleAccept"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'Dispatched' && authStore.isTechnician"
            >
              <el-icon><Check /></el-icon>
              接单
            </el-button>

            <!-- 拒单按钮 -->
            <el-button
              type="warning"
              @click="showRejectDialog = true"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'Dispatched' && authStore.isTechnician"
            >
              <el-icon><Close /></el-icon>
              拒单
            </el-button>

            <!-- 处理中按钮 -->
            <el-button
              type="primary"
              @click="handleProgress"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'Accepted'"
            >
              <el-icon><VideoPlay /></el-icon>
              开始处理
            </el-button>

            <!-- 完成按钮 -->
            <el-button
              type="success"
              @click="showFinishDialog = true"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'InProgress'"
            >
              <el-icon><CircleCheck /></el-icon>
              完成
            </el-button>

            <!-- 确认完工按钮 -->
            <el-button
              type="primary"
              @click="handleConfirm"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'Finished'"
            >
              <el-icon><Medal /></el-icon>
              确认完工
            </el-button>

            <!-- 评价按钮 -->
            <el-button
              type="warning"
              @click="showRateDialog = true"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'Confirmed'"
            >
              <el-icon><Star /></el-icon>
              评价
            </el-button>

            <!-- 分配给我 -->
            <el-button
              @click="assignToMe"
              class="action-button"
              v-if="!ticketStore.currentTicket?.assignedTo && authStore.isAdmin && ticketStore.currentTicket?.status === 'New'"
            >
              <el-icon><User /></el-icon>
              分配给我
            </el-button>
          </div>
        </el-card>
        
        <!-- 工单信息卡片 -->
        <el-card class="info-summary-card" shadow="never">
          <template #header>
            <h3>工单摘要</h3>
          </template>
          
          <div class="info-summary">
            <div class="summary-item">
              <span class="summary-label">工单编号</span>
              <span class="summary-value">
                {{ ticketStore.currentTicket?.ticketCode }}
              </span>
            </div>
            
            <div class="summary-item">
              <span class="summary-label">创建时间</span>
              <span class="summary-value">
                {{ formatDateTime(ticketStore.currentTicket?.createdAt) }}
              </span>
            </div>
            
            <div class="summary-item">
              <span class="summary-label">最后更新</span>
              <span class="summary-value">
                {{ formatDateTime(ticketStore.currentTicket?.updatedAt) || '从未更新' }}
              </span>
            </div>
            
            <div class="summary-item">
              <span class="summary-label">工单分类</span>
              <span class="summary-value">
                {{ ticketStore.currentTicket?.categoryName || ticketStore.currentTicket?.category || '未分类' }}
              </span>
            </div>
            
            <div class="summary-item">
            </div>
            
            <div class="summary-item">
              <span class="summary-label">负责人</span>
              <span class="summary-value">
                {{ ticketStore.currentTicket?.assigneeName || (ticketStore.currentTicket?.assignedTo ? `用户${ticketStore.currentTicket.assignedTo}` : '未分配') }}
              </span>
            </div>
          </div>
        </el-card>

        <!-- 跨模块关联记录 -->
        <el-card class="linked-records-card" shadow="never" v-if="linkedRecords.length > 0">
          <template #header>
            <div class="linked-header">
              <h3>关联记录</h3>
              <span class="linked-badge">{{ linkedRecords.length }} 条记录</span>
            </div>
          </template>
          <div class="linked-list">
            <div v-for="group in linkedRecords" :key="group.module" class="linked-group">
              <div class="linked-group-title">
                <el-icon><<component :is="getModuleIcon(group.module)" /></el-icon>
                {{ group.moduleName }}
                <el-tag size="small" type="info">{{ group.records.length }} 条</el-tag>
              </div>
              <div class="linked-items">
                <div v-for="record in group.records" :key="record.id" class="linked-item">
                  <span class="linked-label">{{ record.label }}</span>
                  <span class="linked-value">{{ record.value }}</span>
                </div>
              </div>
            </div>
          </div>
        </el-card>
      </div>
    </div>

    <!-- 派单对话框 -->
    <el-dialog
      v-model="showDispatchDialog"
      title="派单"
      width="500px"
    >
      <el-form label-width="80px">
        <el-form-item label="选择员工" required>
          <el-select
            v-model="dispatchForm.assigneeId"
            placeholder="请选择员工"
            style="width: 100%"
          >
            <el-option
              v-for="person in personnelList"
              :key="person.id"
              :label="person.name"
              :value="person.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input
            v-model="dispatchForm.remark"
            type="textarea"
            :rows="3"
            placeholder="可选填写备注"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDispatchDialog = false">取消</el-button>
        <el-button type="primary" @click="submitDispatch" :disabled="!dispatchForm.assigneeId">
          确认派单
        </el-button>
      </template>
    </el-dialog>

    <!-- 升级状态卡片 -->
    <el-card v-if="escalationData" class="escalation-card" shadow="never">
      <template #header>
        <h3>
          <el-icon><WarningFilled /></el-icon>
          升级状态
          <el-tag v-if="escalationData.currentLevel > 0" type="danger" size="small" style="margin-left: 8px;">
            L{{ escalationData.currentLevel }}
          </el-tag>
        </h3>
      </template>
      
      <div class="escalation-summary">
        <div class="escalation-item">
          <span class="label">当前状态:</span>
          <span class="value">{{ escalationData.currentStatus }}</span>
        </div>
        <div class="escalation-item" v-if="escalationData.nextEscalationRole">
          <span class="label">下一升级:</span>
          <span class="value escalation-role">{{ escalationData.nextEscalationRole }}</span>
        </div>
      </div>

      <el-divider content-position="left">升级历史</el-divider>

      <el-timeline v-if="escalationData.escalations?.length > 0">
        <el-timeline-item
          v-for="esc in escalationData.escalations"
          :key="esc.id"
          :type="esc.level >= 3 ? 'danger' : esc.level >= 2 ? 'warning' : 'primary'"
          :timestamp="formatDateTime(esc.escalatedAt)"
        >
          <div class="escalation-timeline-item">
            <div class="esc-level">级别 L{{ esc.level }}</div>
            <div class="esc-detail">
              <span class="esc-from">{{ esc.fromPersonName }}</span>
              <span class="esc-arrow">→</span>
              <span class="esc-to">{{ esc.toPersonName }} ({{ esc.toRole }})</span>
            </div>
            <div class="esc-status">
              <el-tag :type="esc.status === 'Pending' ? 'warning' : 'success'" size="small">
                {{ esc.status }}
              </el-tag>
            </div>
          </div>
        </el-timeline-item>
      </el-timeline>
      <el-empty v-else description="暂无升级记录" :image-size="60" />
    </el-card>

    <!-- 拒单对话框 -->
    <el-dialog
      v-model="showRejectDialog"
      title="拒单"
      width="500px"
    >
      <el-form label-width="80px">
        <el-form-item label="拒单原因" required>
          <el-input
            v-model="rejectForm.reason"
            type="textarea"
            :rows="4"
            placeholder="请输入拒单原因"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRejectDialog = false">取消</el-button>
        <el-button type="warning" @click="submitReject" :disabled="!rejectForm.reason.trim()">
          确认拒单
        </el-button>
      </template>
    </el-dialog>

    <!-- 完成对话框 -->
    <el-dialog
      v-model="showFinishDialog"
      title="完成工单"
      width="500px"
    >
      <el-form label-width="80px">
        <el-form-item label="解决方案">
          <el-input
            v-model="finishForm.solution"
            type="textarea"
            :rows="4"
            placeholder="请输入解决方案（可选）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showFinishDialog = false">取消</el-button>
        <el-button type="success" @click="submitFinish">
          确认完成
        </el-button>
      </template>
    </el-dialog>

    <!-- 评价对话框 -->
    <el-dialog
      v-model="showRateDialog"
      title="评价工单"
      width="500px"
    >
      <el-form label-width="80px">
        <el-form-item label="评分" required>
          <el-rate
            v-model="rateForm.rating"
            :max="5"
            show-text
            :texts="['很差', '差', '一般', '好', '很好']"
          />
        </el-form-item>
        <el-form-item label="评价内容">
          <el-input
            v-model="rateForm.comment"
            type="textarea"
            :rows="3"
            placeholder="请输入评价内容（可选）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRateDialog = false">取消</el-button>
        <el-button type="warning" @click="submitRate">
          提交评价
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { 
  ArrowLeft, Edit, Delete, Plus, User, Check, WarningFilled
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';
import { useTicketStore } from '@/stores/ticket';
import { personnelStore } from '@/stores/personnel';
import { ticketTypeStore } from '@/stores/ticketType';
import type { UpdateTicketRequest } from '@/api/ticket';
import dispatchApi from '@/api/dispatch';
import { linkedService } from '@/api/linkedService';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const ticketStore = useTicketStore();

// 升级数据
const escalationData = ref<any>(null);

// 工单ID
const ticketId = computed(() => parseInt(route.params.id as string));

// 编辑模式
const editMode = ref(false);
const saving = ref(false);

// 状态流转对话框
const showDispatchDialog = ref(false);
const showRejectDialog = ref(false);
const showFinishDialog = ref(false);
const showRateDialog = ref(false);

// 派单表单
const dispatchForm = ref({
  assigneeId: undefined as number | undefined,
  remark: ''
});

// 拒单表单
const rejectForm = ref({
  reason: ''
});

// 完成表单
const finishForm = ref({
  solution: ''
});

// 评价表单
const rateForm = ref({
  rating: 5,
  comment: ''
});

// 人员列表
const personnelList = computed(() => personnelStore.personnelList);

// 工单表单
const ticketFormRef = ref<FormInstance>();
const ticketForm = ref({
  title: '',
  description: '',
  category: '',
  priority: 'Medium',
  status: 'New',
  assignedTo: undefined as number | undefined
});

// 表单验证规则
const ticketRules: FormRules = {
  title: [
    { required: true, message: '请输入工单标题', trigger: 'blur' },
    { min: 3, message: '标题至少3个字符', trigger: 'blur' }
  ]
};

// 获取状态类型
const getStatusType = (status?: string) => {
  switch (status) {
    case 'New': return 'info';
    case 'Dispatched': return 'warning';
    case 'Accepted': return 'primary';
    case 'InProgress': return 'warning';
    case 'Finished': return 'success';
    case 'Confirmed': return 'success';
    case 'Rated': return 'info';
    case 'Closed': return 'info';
    default: return 'info';
  }
};

// 获取状态文本
const getStatusText = (status?: string) => {
  switch (status) {
    case 'New': return '新建';
    case 'Dispatched': return '已派单';
    case 'Accepted': return '已接单';
    case 'InProgress': return '处理中';
    case 'Finished': return '已完成';
    case 'Confirmed': return '已确认';
    case 'Rated': return '已评价';
    case 'Closed': return '已关闭';
    default: return status || '未知';
  }
};

// 获取优先级类型
const getPriorityType = (priority?: string) => {
  switch (priority) {
    case 'High': return 'danger';
    case 'Medium': return 'warning';
    case 'Low': return 'info';
    default: return 'info';
  }
};

// 格式化日期
const formatDate = (dateString?: string) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
};

// 格式化日期时间
const formatDateTime = (dateString?: string) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 计算处理时长
const calculateProcessingTime = () => {
  const ticket = ticketStore.currentTicket;
  if (!ticket || !ticket.createdAt) return '0天';
  
  const createDate = new Date(ticket.createdAt);
  const now = new Date();
  const diffTime = Math.abs(now.getTime() - createDate.getTime());
  const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
  
  if (diffDays === 0) {
    const diffHours = Math.floor(diffTime / (1000 * 60 * 60));
    if (diffHours === 0) {
      const diffMinutes = Math.floor(diffTime / (1000 * 60));
      return `${diffMinutes}分钟`;
    }
    return `${diffHours}小时`;
  }
  
  return `${diffDays}天`;
};

// 保存工单信息
const saveTicket = async () => {
  if (!ticketFormRef.value) return;
  
  try {
    await ticketFormRef.value.validate();
    saving.value = true;
    
    const updateData: UpdateTicketRequest = {
      title: ticketForm.value.title,
      description: ticketForm.value.description,
      category: ticketForm.value.category,
      priority: ticketForm.value.priority,
      status: ticketForm.value.status,
      assignedTo: ticketForm.value.assignedTo
    };
    
    const result = await ticketStore.updateExistingTicket(ticketId.value, updateData);
    
    if (result.success) {
      ElMessage.success('工单信息已更新');
      editMode.value = false;
    } else {
      ElMessage.error(result.message || '更新失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    saving.value = false;
  }
};

// 取消编辑
const cancelEdit = () => {
  editMode.value = false;
  // 重置表单数据
  const ticket = ticketStore.currentTicket;
  if (ticket) {
    ticketForm.value = {
      title: ticket.title,
      description: ticket.description || '',
      category: ticket.category || '',
      categoryName: ticket.categoryName || '',
      priority: ticket.priority,
      priorityName: ticket.priorityName || '',
      status: ticket.status,
      statusName: ticket.statusName || '',
      contactPhone: ticket.contactPhone || '',
      address: ticket.address || '',
      assignedTo: ticket.assignedTo,
      assigneeName: ticket.assigneeName || ''
    };
  }
};

// 删除工单
const handleDelete = async () => {
  try {
    await ElMessageBox.confirm(
      '确定要删除这个工单吗？此操作不可恢复。',
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    
    const result = await ticketStore.deleteExistingTicket(ticketId.value);
    
    if (result.success) {
      ElMessage.success('工单已删除');
      router.push('/tickets');
    } else {
      ElMessage.error(result.message || '删除失败');
    }
  } catch {
    // 用户取消删除
  }
};

// 更改工单状态
const changeTicketStatus = async (status: string) => {
  try {
    const result = await ticketStore.updateExistingTicket(ticketId.value, { status });
    
    if (result.success) {
      ElMessage.success(`工单状态已更新为: ${getStatusText(status)}`);
    } else {
      ElMessage.error(result.message || '状态更新失败');
    }
  } catch (error) {
    ElMessage.error('状态更新失败');
  }
};

// 更改优先级
const changePriority = async (priority: string) => {
  try {
    const result = await ticketStore.updateExistingTicket(ticketId.value, { priority });
    
    if (result.success) {
      ElMessage.success(`工单优先级已更新为: ${priority}`);
    } else {
      ElMessage.error(result.message || '优先级更新失败');
    }
  } catch (error) {
    ElMessage.error('优先级更新失败');
  }
};

// 派单
const submitDispatch = async () => {
  if (!dispatchForm.value.assigneeId) {
    ElMessage.warning('请选择员工');
    return;
  }
  try {
    const result = await ticketStore.dispatchExistingTicket(
      ticketId.value,
      dispatchForm.value.assigneeId,
      dispatchForm.value.remark
    );
    if (result.success) {
      ElMessage.success('工单已派发');
      showDispatchDialog.value = false;
      loadTicketData();
    } else {
      ElMessage.error(result.message || '派单失败');
    }
  } catch {
    ElMessage.error('派单失败');
  }
};

// 接单
const handleAccept = async () => {
  try {
    const result = await ticketStore.acceptExistingTicket(ticketId.value);
    if (result.success) {
      ElMessage.success('已接单');
      loadTicketData();
    } else {
      ElMessage.error(result.message || '接单失败');
    }
  } catch {
    ElMessage.error('接单失败');
  }
};

// 拒单
const submitReject = async () => {
  if (!rejectForm.value.reason.trim()) {
    ElMessage.warning('请输入拒单原因');
    return;
  }
  try {
    const result = await ticketStore.rejectExistingTicket(ticketId.value, rejectForm.value.reason);
    if (result.success) {
      ElMessage.success('已拒单');
      showRejectDialog.value = false;
      loadTicketData();
    } else {
      ElMessage.error(result.message || '拒单失败');
    }
  } catch {
    ElMessage.error('拒单失败');
  }
};

// 处理中
const handleProgress = async (remark?: string) => {
  try {
    const result = await ticketStore.progressExistingTicket(ticketId.value, remark);
    if (result.success) {
      ElMessage.success('已开始处理');
      loadTicketData();
    } else {
      ElMessage.error(result.message || '操作失败');
    }
  } catch {
    ElMessage.error('操作失败');
  }
};

// 完成
const submitFinish = async () => {
  try {
    const result = await ticketStore.finishExistingTicket(ticketId.value, finishForm.value.solution);
    if (result.success) {
      ElMessage.success('工单已完成');
      showFinishDialog.value = false;
      loadTicketData();
    } else {
      ElMessage.error(result.message || '完成失败');
    }
  } catch {
    ElMessage.error('完成失败');
  }
};

// 确认完工
const handleConfirm = async () => {
  try {
    const result = await ticketStore.confirmExistingTicket(ticketId.value);
    if (result.success) {
      ElMessage.success('已确认完工');
      loadTicketData();
    } else {
      ElMessage.error(result.message || '确认失败');
    }
  } catch {
    ElMessage.error('确认失败');
  }
};

// 评价
const submitRate = async () => {
  try {
    const result = await ticketStore.rateExistingTicket(
      ticketId.value,
      rateForm.value.rating,
      rateForm.value.comment
    );
    if (result.success) {
      ElMessage.success('评价成功');
      showRateDialog.value = false;
      loadTicketData();
    } else {
      ElMessage.error(result.message || '评价失败');
    }
  } catch {
    ElMessage.error('评价失败');
  }
};

// 分配给我
const assignToMe = async () => {
  try {
    const currentUserId = authStore.user?.id || 1;
    dispatchForm.value.assigneeId = currentUserId;
    await submitDispatch();
  } catch {
    ElMessage.error('分配失败');
  }
};

// 加载工单数据
const loadTicketData = async () => {
  if (!ticketId.value) return;
  
  const ticketResult = await ticketStore.fetchTicket(ticketId.value);
  if (ticketResult.success && ticketStore.currentTicket) {
    // 初始化表单数据
    const ticket = ticketStore.currentTicket;
    ticketForm.value = {
      title: ticket.title,
      description: ticket.description || '',
      category: ticket.category || '',
      priority: ticket.priority,
      status: ticket.status,
      assignedTo: ticket.assignedTo
    };
    // 加载升级数据
    loadEscalationData();
  } else {
    ElMessage.error('工单不存在或加载失败');
    router.push('/tickets');
  }
};


// 加载升级数据
const loadEscalationData = async () => {
  try {
    const res = await dispatchApi.getEscalations(ticketId.value);
    if (res.data?.success) {
      escalationData.value = res.data.data;
    }
  } catch (e) {
    console.warn('加载升级数据失败:', e);
  }
};

// 跨模块关联记录
const linkedRecords = ref<any[]>([]);

// 加载跨模块关联记录
const loadLinkedRecords = async () => {
  const ticket = ticketStore.currentTicket;
  if (!ticket) return;
  const phone = ticket.contactPhone || ticket.reporterPhone;
  if (!phone) return;
  try {
    const res = await linkedService.getByPhone(phone);
    if (!res.data?.success || !res.data.data?.groups) return;
    linkedRecords.value = res.data.data.groups.filter((g: any) => g.module !== 'ticket');
  } catch {
    // ignore - linked records are optional
  }
};

// 获取模块图标
const getModuleIcon = (module: string) => {
  const icons: Record<string, string> = {
    personnel: 'User',
    visitor: 'User',
    delivery: 'Box',
    renovation: 'House',
  };
  return icons[module] || 'Document';
};

// 组件挂载时加载数据
onMounted(() => {
  ticketTypeStore.fetchFromApi();
  loadTicketData().then(() => loadLinkedRecords());
});

<style scoped>
.ticket-detail-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 返回按钮 */
.back-button {
  margin-bottom: 8px;
}

/* 工单头部卡片 */
.ticket-header-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.ticket-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.ticket-title h1 {
  font-size: 28px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 12px 0;
}

.ticket-meta {
  display: flex;
  align-items: center;
  gap: 12px;
}

.ticket-code {
  font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 16px;
  color: #6b7280;
  background-color: #f3f4f6;
  padding: 4px 12px;
  border-radius: 6px;
}

.ticket-actions {
  display: flex;
  gap: 12px;
}

/* 详情内容布局 */
.detail-content {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 24px;
}

@media (max-width: 1200px) {
  .detail-content {
    grid-template-columns: 1fr;
  }
}

/* 左侧列 */
.left-column {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* 右侧列 */
.right-column {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* 卡片通用样式 */
.info-card,
.activity-card,
.stats-card,
.quick-actions-card,
.info-summary-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.info-card h3,
.activity-card h3,
.stats-card h3,
.quick-actions-card h3,
.info-summary-card h3,
.escalation-card h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
  display: flex;
  align-items: center;
  gap: 8px;
}

.escalation-card {
  border-radius: 12px;
  border: 1px solid #fee2e2;
  background: #fef2f2;
}

.escalation-summary {
  display: flex;
  gap: 24px;
  margin-bottom: 12px;
}

.escalation-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.escalation-item .label {
  color: #666;
  font-size: 14px;
}

.escalation-item .value {
  font-weight: 600;
  color: #333;
}


.escalation-role {
  color: #E6A23C;
}


.escalation-timeline-item {
  line-height: 1.6;
}

.esc-level {
  font-weight: bold;
  color: #333;
}


.esc-detail {
  color: #666;
  font-size: 13px;
}

.esc-arrow {
  margin: 0 6px;
  color: #999;
}


.esc-status {
  margin-top: 4px;
}

/* 表单操作 */
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
}

/* 操作记录 */
.activity-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.activity-item {
  display: flex;
  gap: 16px;
  padding-bottom: 20px;
  border-bottom: 1px solid #f3f4f6;
}

.activity-item:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.activity-icon {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  background-color: #f3f4f6;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  color: #6b7280;
  flex-shrink: 0;
}

.activity-content {
  flex: 1;
}

.activity-title {
  font-weight: 500;
  color: #1f2937;
  margin-bottom: 4px;
}

.activity-desc {
  color: #6b7280;
  font-size: 14px;
  margin-bottom: 4px;
}

.activity-time {
  color: #9ca3af;
  font-size: 13px;
}

/* 统计卡片 */
.stats-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px;
  border-radius: 8px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  background-color: #3b82f6;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
}

.stat-icon .el-icon {
  color: white;
}

.stat-info {
  flex: 1;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  line-height: 1;
}

/* 快速操作 */
.quick-actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.action-button {
  width: 100%;
  justify-content: flex-start;
  padding: 12px 16px;
}

/* 信息摘要 */
.info-summary {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.summary-item {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid #f3f4f6;
}

.summary-item:last-child {
  border-bottom: none;
}

.summary-label {
  font-weight: 500;
  color: #4b5563;
}

.summary-value {
  color: #1f2937;
  text-align: right;
}

/* 跨模块关联 */
.linked-records-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.linked-header {
  display: flex;
  align-items: center;
  gap: 12px;
}

.linked-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.linked-badge {
  background: #3b82f6;
  color: white;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 13px;
}

.linked-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}


.linked-group {
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 12px;
}

.linked-group-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 8px;
}

.linked-items {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-left: 28px;
}


.linked-item {
  display: flex;
  gap: 8px;
  font-size: 14px;
}


.linked-label {
  color: #6b7280;
  min-width: 80px;
}


.linked-value {
  color: #1f2937;
}
</style>
