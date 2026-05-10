<template>
  <div class="ticket-list-view">
    <!-- 页面标题和操作 -->
    <div class="page-header">
      <div class="header-left">
        <h1>工单管理</h1>
        <p>管理所有维修请求和工单</p>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="showCreateDialog = true">
          <el-icon><Plus /></el-icon>
          新建工单
        </el-button>
        <el-button @click="refreshTickets">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </div>
    </div>
    
    <!-- 筛选工具栏 -->
    <el-card class="filter-card" shadow="never">
      <div class="filter-toolbar">
        <el-input
          v-model="searchQuery"
          placeholder="搜索工单标题或描述..."
          class="search-input"
          :prefix-icon="Search"
          clearable
          @input="handleSearch"
        />
        
        <div class="filter-controls">
          <el-select
            v-model="filterStatus"
            placeholder="状态筛选"
            clearable
            @change="handleFilterChange"
          >
            <el-option label="新建" value="New" />
            <el-option label="已派单" value="Dispatched" />
            <el-option label="已接单" value="Accepted" />
            <el-option label="处理中" value="InProgress" />
            <el-option label="已完成" value="Finished" />
            <el-option label="已确认" value="Confirmed" />
            <el-option label="已评价" value="Rated" />
            <el-option label="已关闭" value="Closed" />
          </el-select>
          
          <el-select
            v-model="filterPriority"
            placeholder="优先级筛选"
            clearable
            @change="handleFilterChange"
          >
            <el-option label="高" value="High" />
            <el-option label="中" value="Medium" />
            <el-option label="低" value="Low" />
          </el-select>
          
          <el-select
            v-model="filterCategory"
            placeholder="分类筛选"
            clearable
            @change="handleFilterChange"
          >
            <el-option
              v-for="type in ticketTypeStore.ticketTypes"
              :key="type.id"
              :label="type.name"
              :value="type.name"
            />
          </el-select>
        </div>
      </div>
    </el-card>
    
    <!-- 工单表格 -->
    <el-card class="table-card" shadow="never">
      <el-table
        :data="filteredTickets"
        v-loading="ticketStore.loading"
        style="width: 100%"
        @row-click="handleRowClick"
      >
        <el-table-column prop="ticketCode" :label="ticketLabels.ticketNo || '工单号'" width="140">
          <template #default="{ row }">
            <div class="ticket-code-cell">
              <span class="code">{{ row.ticketCode }}</span>
            </div>
          </template>
        </el-table-column>
        
        <el-table-column prop="title" :label="ticketLabels.title || '标题'" min-width="200">
          <template #default="{ row }">
            <div class="title-cell">
              <div class="title">{{ row.title }}</div>
              <div v-if="row.description" class="description">
                {{ row.description.substring(0, 50) }}{{ row.description.length > 50 ? '...' : '' }}
              </div>
            </div>
          </template>
        </el-table-column>
        
        <el-table-column prop="statusName" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ row.statusName || getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column prop="priorityName" label="优先级" width="90">
          <template #default="{ row }">
            <el-tag :type="getPriorityType(row.priority)" size="small">
              {{ row.priorityName || row.priority }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column prop="categoryName" label="分类" width="100">
          <template #default="{ row }">
            <span>{{ row.categoryName || row.category || '未分类' }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="creatorName" label="创建人" width="100">
          <template #default="{ row }">
            <span>{{ row.creatorName || `用户${row.createdBy}` }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="assigneeName" label="负责人" width="100">
          <template #default="{ row }">
            <span>{{ row.assigneeName || (row.assignedTo ? `用户${row.assignedTo}` : '未分配') }}</span>
          </template>
        </el-table-column>
        
        <el-table-column prop="createdAt" :label="ticketLabels.createdAt || '创建时间'" width="140">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <div class="action-buttons">
              <el-button
                type="text"
                size="small"
                @click.stop="handleView(row)"
              >
                查看
              </el-button>
              <el-button
                v-if="authStore.isAdmin || authStore.isTechnician"
                type="text"
                size="small"
                @click.stop="handleEdit(row)"
              >
                编辑
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
      
      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="ticketStore.pagination.totalCount"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
      
      <!-- 空状态 -->
      <div v-if="ticketStore.tickets.length === 0 && !ticketStore.loading" class="empty-state">
        <el-empty description="暂无工单数据">
          <el-button type="primary" @click="showCreateDialog = true">
            创建第一个工单
          </el-button>
        </el-empty>
      </div>
    </el-card>
    
    <!-- 创建工单对话框 -->
    <el-dialog
      v-model="showCreateDialog"
      title="新建工单"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="createFormRef"
        :model="createForm"
        :rules="createRules"
        label-width="80px"
      >
        <el-form-item :label="ticketLabels.title || '标题'" prop="title">
          <el-input
            v-model="createForm.title"
            placeholder="请输入工单标题"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
        
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="createForm.description"
            type="textarea"
            :rows="4"
            placeholder="请输入工单详细描述"
            maxlength="1000"
            show-word-limit
          />
        </el-form-item>
        
        <el-form-item label="分类">
          <el-select v-model="createForm.category" placeholder="请选择分类">
            <el-option
              v-for="type in ticketTypeStore.ticketTypes"
              :key="type.id"
              :label="type.name"
              :value="type.name"
            />
          </el-select>
        </el-form-item>
        
        <el-form-item label="优先级">
          <el-select v-model="createForm.priority" placeholder="请选择优先级">
            <el-option label="高" value="High" />
            <el-option label="中" value="Medium" />
            <el-option label="低" value="Low" />
          </el-select>
        </el-form-item>

        <el-form-item label="联系电话">
          <el-input
            v-model="createForm.contactPhone"
            placeholder="请输入联系电话"
          />
        </el-form-item>

        <el-form-item label="地址">
          <el-input
            v-model="createForm.address"
            placeholder="请输入工单地址"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showCreateDialog = false">取消</el-button>
          <el-button type="primary" @click="handleCreate" :loading="creating">
            创建
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { Plus, Refresh, Search } from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';
import { useTicketStore } from '@/stores/ticket';
import { ticketTypeStore } from '@/stores/ticketType';
import type { Ticket, TicketOptions } from '@/stores/ticket';
import { useFieldConfig } from '@/composables/useFieldConfig';

const router = useRouter();
const authStore = useAuthStore();
const ticketStore = useTicketStore();
const { fetchFieldConfig, getLabel } = useFieldConfig();

// 字段标签（从 API 获取，alias 优先）
const ticketLabels = ref<Record<string, any>>({});

// 获取某字段是否可编辑
const isFieldEditable = (fieldKey: string): boolean => {
  return ticketLabels.value[fieldKey]?.isEditable ?? true;
};

// 搜索和筛选
const searchQuery = ref('');
const filterStatus = ref('');
const filterPriority = ref('');
const filterCategory = ref('');

// 分页
const currentPage = ref(1);
const pageSize = ref(20);

// 创建工单对话框
const showCreateDialog = ref(false);
const createFormRef = ref<FormInstance>();
const creating = ref(false);
const createForm = ref({
  title: '',
  description: '',
  category: '',
  priority: 'Medium',
  contactPhone: '',
  address: ''
});

// 表单验证规则
const createRules: FormRules = {
  title: [
    { required: true, message: '请输入工单标题', trigger: 'blur' },
    { min: 3, message: '标题至少3个字符', trigger: 'blur' }
  ]
};

// 过滤后的工单
const filteredTickets = computed(() => {
  let tickets = ticketStore.tickets;
  
  // 搜索过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    tickets = tickets.filter(ticket => 
      ticket.title.toLowerCase().includes(query) ||
      (ticket.description && ticket.description.toLowerCase().includes(query))
    );
  }
  
  // 状态过滤
  if (filterStatus.value) {
    tickets = tickets.filter(ticket => ticket.status === filterStatus.value);
  }
  
  // 优先级过滤
  if (filterPriority.value) {
    tickets = tickets.filter(ticket => ticket.priority === filterPriority.value);
  }
  
  // 分类过滤
  if (filterCategory.value) {
    tickets = tickets.filter(ticket => ticket.category === filterCategory.value);
  }
  
  return tickets;
});

// 获取状态类型
const getStatusType = (status: string) => {
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
const getStatusText = (status: string) => {
  switch (status) {
    case 'New': return '新建';
    case 'Dispatched': return '已派单';
    case 'Accepted': return '已接单';
    case 'InProgress': return '处理中';
    case 'Finished': return '已完成';
    case 'Confirmed': return '已确认';
    case 'Rated': return '已评价';
    case 'Closed': return '已关闭';
    default: return status;
  }
};

// 获取优先级类型
const getPriorityType = (priority: string) => {
  switch (priority) {
    case 'High': return 'danger';
    case 'Medium': return 'warning';
    case 'Low': return 'info';
    default: return 'info';
  }
};

// 格式化日期
const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('zh-CN', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 处理搜索
const handleSearch = () => {
  currentPage.value = 1;
};

// 处理筛选变化
const handleFilterChange = () => {
  currentPage.value = 1;
};

// 处理行点击
const handleRowClick = (row: any) => {
  router.push(`/tickets/${row.id}`);
};

// 查看工单
const handleView = (row: any) => {
  router.push(`/tickets/${row.id}`);
};

// 编辑工单
const handleEdit = (row: any) => {
  router.push(`/tickets/${row.id}?edit=true`);
};

// 刷新工单
const refreshTickets = async () => {
  await ticketStore.fetchTickets({
    page: currentPage.value,
    pageSize: pageSize.value
  });
  ElMessage.success('工单列表已刷新');
};

// 处理分页大小变化
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  refreshTickets();
};

// 处理当前页变化
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
  refreshTickets();
};

// 处理创建工单
const handleCreate = async () => {
  if (!createFormRef.value) return;

  try {
    await createFormRef.value.validate();
    creating.value = true;

    const result = await ticketStore.createNewTicket(createForm.value);

    if (result.success) {
      ElMessage.success('工单创建成功');
      showCreateDialog.value = false;
      createFormRef.value.resetFields();
      refreshTickets();
    } else {
      ElMessage.error(result.message || '创建失败');
    }
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    creating.value = false;
  }
};

// 组件挂载时加载数据
onMounted(async () => {
  ticketTypeStore.fetchFromApi();
  if (ticketStore.tickets.length === 0) {
    await refreshTickets();
  }
  // 加载字段配置（alias 优先的 label）
  const config = await fetchFieldConfig('ticket');
  if (config) ticketLabels.value = config;
});
</script>

<style scoped>
.ticket-list-view {
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

/* 筛选工具栏 */
.filter-card {
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.filter-toolbar {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.search-input {
  width: 100%;
  max-width: 400px;
}

.filter-controls {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

/* 表格卡片 */
.table-card {
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

/* 表格单元格样式 */
.ticket-code-cell {
  font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 13px;
  font-weight: 500;
  color: #374151;
}

.title-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.title {
  font-weight: 500;
  color: #1f2937;
}

.description {
  font-size: 12px;
  color: #6b7280;
  line-height: 1.4;
}

/* 操作按钮 */
.action-buttons {
  display: flex;
  gap: 8px;
}

/* 分页 */
.pagination-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
}

/* 空状态 */
.empty-state {
  padding: 60px 0;
}

/* 对话框 */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
