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
                <el-option label="设备维修" value="设备维修" />
                <el-option label="网络问题" value="网络问题" />
                <el-option label="电气问题" value="电气问题" />
                <el-option label="给排水" value="给排水" />
                <el-option label="其他" value="其他" />
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
                <div class="stat-label">创建人</div>
                <div class="stat-value">用户 {{ ticketStore.currentTicket?.createdBy }}</div>
              </div>
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
            <el-button 
              type="primary" 
              @click="changeTicketStatus('InProgress')"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'New' && (authStore.isAdmin || authStore.isTechnician)"
            >
              <el-icon><PlayCircle /></el-icon>
              开始处理
            </el-button>
            
            <el-button 
              type="success" 
              @click="changeTicketStatus('Resolved')"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'InProgress' && (authStore.isAdmin || authStore.isTechnician)"
            >
              <el-icon><Check /></el-icon>
              标记解决
            </el-button>
            
            <el-button 
              @click="changeTicketStatus('Closed')"
              class="action-button"
              v-if="ticketStore.currentTicket?.status === 'Resolved' && (authStore.isAdmin || authStore.isTechnician)"
            >
              <el-icon><CircleClose /></el-icon>
              关闭工单
            </el-button>
            
            <el-button 
              type="warning" 
              @click="changeTicketStatus('New')"
              class="action-button"
              v-if="ticketStore.currentTicket?.status !== 'New' && ticketStore.currentTicket?.status !== 'Closed' && (authStore.isAdmin || authStore.isTechnician)"
            >
              <el-icon><Refresh /></el-icon>
              重新打开
            </el-button>
            
            <el-button 
              @click="assignToMe"
              class="action-button"
              v-if="!ticketStore.currentTicket?.assignedTo && (authStore.isAdmin || authStore.isTechnician)"
            >
              <el-icon><User /></el-icon>
              分配给我
            </el-button>
            
            <el-button 
              type="danger" 
              @click="changePriority('High')"
              class="action-button"
              v-if="ticketStore.currentTicket?.priority !== 'High' && (authStore.isAdmin || authStore.isTechnician)"
            >
              <el-icon><Warning /></el-icon>
              设为高优先级
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
                {{ ticketStore.currentTicket?.category || '未分类' }}
              </span>
            </div>
            
            <div class="summary-item">
              <span class="summary-label">创建人ID</span>
              <span class="summary-value">
                {{ ticketStore.currentTicket?.createdBy }}
              </span>
            </div>
            
            <div class="summary-item">
              <span class="summary-label">负责人ID</span>
              <span class="summary-value">
                {{ ticketStore.currentTicket?.assignedTo || '未分配' }}
              </span>
            </div>
          </div>
        </el-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { 
  ArrowLeft, Edit, Delete, Plus, User, Check, 
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';
import { useTicketStore } from '@/stores/ticket';
import type { UpdateTicketRequest } from '@/api/ticket';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const ticketStore = useTicketStore();

// 工单ID
const ticketId = computed(() => parseInt(route.params.id as string));

// 编辑模式
const editMode = ref(false);
const saving = ref(false);

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
    case 'InProgress': return 'warning';
    case 'Resolved': return 'success';
    case 'Closed': return '';
    default: return 'info';
  }
};

// 获取状态文本
const getStatusText = (status?: string) => {
  switch (status) {
    case 'New': return '新建';
    case 'InProgress': return '进行中';
    case 'Resolved': return '已解决';
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
      priority: ticket.priority,
      status: ticket.status,
      assignedTo: ticket.assignedTo
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

// 分配给我
const assignToMe = async () => {
  try {
    // 这里应该使用当前用户的ID，暂时使用固定值
    const currentUserId = authStore.user?.id || 1;
    const result = await ticketStore.assignExistingTicket(ticketId.value, currentUserId);
    
    if (result.success) {
      ElMessage.success('工单已分配给您');
    } else {
      ElMessage.error(result.message || '分配失败');
    }
  } catch (error) {
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
  } else {
    ElMessage.error('工单不存在或加载失败');
    router.push('/tickets');
  }
};

// 组件挂载时加载数据
onMounted(() => {
  loadTicketData();
});

// 监听路由变化
watch(() => route.params.id, () => {
  if (route.name === 'ticket-detail') {
    loadTicketData();
  }
});
</script>

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
.info-summary-card h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
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
</style>
