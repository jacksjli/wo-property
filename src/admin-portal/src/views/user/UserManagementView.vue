<template>
  <div class="user-management-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h1>用户管理</h1>
        <p>管理系统用户和权限</p>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="showCreateDialog = true">
          <el-icon><Plus /></el-icon>
          添加用户
        </el-button>
        <el-button @click="exportUsers">
          <el-icon><Download /></el-icon>
          导出用户
        </el-button>
      </div>
    </div>
    
    <!-- 筛选工具栏 -->
    <el-card class="filter-card" shadow="never">
      <div class="filter-toolbar">
        <el-input
          v-model="searchQuery"
          placeholder="搜索用户名、姓名、邮箱..."
          class="search-input"
          clearable
          @input="handleSearch"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        
        <div class="filter-actions">
          <el-select
            v-model="filterRole"
            placeholder="所有角色"
            clearable
            @change="handleFilter"
          >
            <el-option label="管理员" value="Admin" />
            <el-option label="技术人员" value="Technician" />
            <el-option label="普通用户" value="User" />
          </el-select>
          
          <el-select
            v-model="filterStatus"
            placeholder="所有状态"
            clearable
            @change="handleFilter"
          >
            <el-option label="活跃" value="Active" />
            <el-option label="停用" value="Inactive" />
            <el-option label="锁定" value="Locked" />
          </el-select>
          
          <el-button @click="resetFilters">
            <el-icon><Refresh /></el-icon>
            重置筛选
          </el-button>
        </div>
      </div>
    </el-card>
    
    <!-- 用户统计卡片 -->
    <div class="stats-cards">
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon total-users">
            <el-icon><User /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ userStats.total }}</div>
            <div class="stat-label">总用户数</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon active-users">
            <el-icon><UserFilled /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ userStats.active }}</div>
            <div class="stat-label">活跃用户</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon admin-users">
            <el-icon><Star /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ userStats.admins }}</div>
            <div class="stat-label">管理员</div>
          </div>
        </div>
      </el-card>
      
      <el-card class="stat-card" shadow="never">
        <div class="stat-content">
          <div class="stat-icon new-users">
            <el-icon><TrendCharts /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ userStats.newThisMonth }}</div>
            <div class="stat-label">本月新增</div>
          </div>
        </div>
      </el-card>
    </div>
    
    <!-- 用户表格 -->
    <el-card class="users-table-card" shadow="never">
      <template #header>
        <div class="table-header">
          <h3>用户列表</h3>
          <div class="table-actions">
            <el-button type="text" @click="refreshUsers">
              <el-icon><Refresh /></el-icon>
              刷新
            </el-button>
          </div>
        </div>
      </template>
      
      <el-table
        :data="filteredUsers"
        v-loading="loading"
        style="width: 100%"
        @sort-change="handleSort"
      >
        <el-table-column prop="id" label="ID" width="80" sortable />
        
        <el-table-column prop="username" label="用户名" width="150" sortable>
          <template #default="scope">
            <div class="user-cell">
              <el-avatar :size="32" :src="getUserAvatar(scope.row.username)" class="user-avatar">
                {{ scope.row.username.charAt(0).toUpperCase() }}
              </el-avatar>
              <div class="user-info">
                <div class="username">{{ scope.row.username }}</div>
                <div class="full-name" v-if="scope.row.fullName">{{ scope.row.fullName }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        
        <el-table-column prop="email" label="邮箱" width="200" sortable />
        
        <el-table-column prop="role" label="角色" width="120" sortable>
          <template #default="scope">
            <el-tag :type="getRoleType(scope.row.role)" size="small">
              {{ getRoleText(scope.row.role) }}
            </el-tag>
          </template>
        </el-table-column>
        
        <el-table-column prop="status" label="状态" width="100" sortable>
          <template #default="scope">
            <el-tag :type="getStatusType(scope.row.status)" size="small">
              {{ getStatusText(scope.row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        
        <el-table-column prop="createdAt" label="注册时间" width="180" sortable>
          <template #default="scope">
            {{ formatDate(scope.row.createdAt) }}
          </template>
        </el-table-column>
        
        <el-table-column prop="lastLogin" label="最后登录" width="180" sortable>
          <template #default="scope">
            {{ scope.row.lastLogin ? formatDate(scope.row.lastLogin) : '从未登录' }}
          </template>
        </el-table-column>
        
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="scope">
            <div class="action-buttons">
              <el-button
                type="text"
                size="small"
                @click="editUser(scope.row)"
                v-if="authStore.isAdmin"
              >
                编辑
              </el-button>
              
              <el-button
                type="text"
                size="small"
                @click="resetPassword(scope.row)"
                v-if="authStore.isAdmin"
              >
                重置密码
              </el-button>
              
              <el-button
                type="text"
                size="small"
                :type="scope.row.status === 'Active' ? 'danger' : 'success'"
                @click="toggleUserStatus(scope.row)"
                v-if="authStore.isAdmin"
              >
                {{ scope.row.status === 'Active' ? '停用' : '启用' }}
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
      
      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="totalUsers"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
    
    <!-- 创建用户对话框 -->
    <el-dialog
      v-model="showCreateDialog"
      title="添加用户"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="createFormRef"
        :model="createForm"
        :rules="createRules"
        label-width="100px"
      >
        <el-form-item label="用户名" prop="username">
          <el-input v-model="createForm.username" placeholder="请输入用户名" />
        </el-form-item>
        
        <el-form-item label="姓名" prop="fullName">
          <el-input v-model="createForm.fullName" placeholder="请输入姓名" />
        </el-form-item>
        
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="createForm.email" type="email" placeholder="请输入邮箱" />
        </el-form-item>
        
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="createForm.phone" placeholder="请输入手机号" />
        </el-form-item>
        
        <el-form-item label="角色" prop="role">
          <el-select v-model="createForm.role" placeholder="请选择角色">
            <el-option label="管理员" value="Admin" />
            <el-option label="技术人员" value="Technician" />
            <el-option label="普通用户" value="User" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="初始密码" prop="password">
          <el-input
            v-model="createForm.password"
            type="password"
            show-password
            placeholder="请输入初始密码"
          />
        </el-form-item>
        
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input
            v-model="createForm.confirmPassword"
            type="password"
            show-password
            placeholder="请确认密码"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showCreateDialog = false">取消</el-button>
          <el-button type="primary" @click="createUser" :loading="creating">
            创建用户
          </el-button>
        </span>
      </template>
    </el-dialog>
    
    <!-- 编辑用户对话框 -->
    <el-dialog
      v-model="showEditDialog"
      title="编辑用户"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="editFormRef"
        :model="editForm"
        :rules="editRules"
        label-width="100px"
      >
        <el-form-item label="用户名">
          <el-input v-model="editForm.username" disabled />
        </el-form-item>
        
        <el-form-item label="姓名" prop="fullName">
          <el-input v-model="editForm.fullName" placeholder="请输入姓名" />
        </el-form-item>
        
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="editForm.email" type="email" placeholder="请输入邮箱" />
        </el-form-item>
        
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="editForm.phone" placeholder="请输入手机号" />
        </el-form-item>
        
        <el-form-item label="角色" prop="role">
          <el-select v-model="editForm.role" placeholder="请选择角色">
            <el-option label="管理员" value="Admin" />
            <el-option label="技术人员" value="Technician" />
            <el-option label="普通用户" value="User" />
          </el-select>
        </el-form-item>
        
        <el-form-item label="状态" prop="status">
          <el-select v-model="editForm.status" placeholder="请选择状态">
            <el-option label="活跃" value="Active" />
            <el-option label="停用" value="Inactive" />
            <el-option label="锁定" value="Locked" />
          </el-select>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showEditDialog = false">取消</el-button>
          <el-button type="primary" @click="updateUser" :loading="updating">
            保存更改
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { Plus, Download, Search, Refresh, User, UserFilled, Star, TrendCharts } from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';

const authStore = useAuthStore();

// 用户数据
const users = ref<any[]>([]);
const loading = ref(false);

// 筛选和搜索
const searchQuery = ref('');
const filterRole = ref('');
const filterStatus = ref('');

// 分页
const currentPage = ref(1);
const pageSize = ref(20);
const totalUsers = ref(0);

// 用户统计
const userStats = ref({
  total: 0,
  active: 0,
  admins: 0,
  newThisMonth: 0
});

// 对话框状态
const showCreateDialog = ref(false);
const showEditDialog = ref(false);
const creating = ref(false);
const updating = ref(false);

// 创建用户表单
const createFormRef = ref<FormInstance>();
const createForm = ref({
  username: '',
  fullName: '',
  email: '',
  phone: '',
  role: 'User',
  password: '',
  confirmPassword: ''
});

// 编辑用户表单
const editFormRef = ref<FormInstance>();
const editForm = ref({
  id: 0,
  username: '',
  fullName: '',
  email: '',
  phone: '',
  role: 'User',
  status: 'Active'
});

// 表单验证规则
const createRules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, message: '用户名至少3个字符', trigger: 'blur' }
  ],
  fullName: [
    { required: true, message: '请输入姓名', trigger: 'blur' },
    { min: 2, message: '姓名至少2个字符', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ],
  role: [
    { required: true, message: '请选择角色', trigger: 'change' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少6个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (value !== createForm.value.password) {
          callback(new Error('两次输入的密码不一致'));
        } else {
          callback();
        }
      },
      trigger: 'blur'
    }
  ]
};

const editRules: FormRules = {
  fullName: [
    { required: true, message: '请输入姓名', trigger: 'blur' },
    { min: 2, message: '姓名至少2个字符', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ],
  role: [
    { required: true, message: '请选择角色', trigger: 'change' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
};

// 获取用户头像
const getUserAvatar = (username: string) => {
  return `https://api.dicebear.com/7.x/avataaars/svg?seed=${username}`;
};

// 获取角色类型
const getRoleType = (role: string) => {
  switch (role) {
    case 'Admin': return 'danger';
    case 'Technician': return 'warning';
    case 'User': return 'success';
    default: return 'info';
  }
};

// 获取角色文本
const getRoleText = (role: string) => {
  switch (role) {
    case 'Admin': return '管理员';
    case 'Technician': return '技术人员';
    case 'User': return '普通用户';
    default: return role;
  }
};

// 获取状态类型
const getStatusType = (status: string) => {
  switch (status) {
    case 'Active': return 'success';
    case 'Inactive': return 'info';
    case 'Locked': return 'danger';
    default: return 'info';
  }
};

// 获取状态文本
const getStatusText = (status: string) => {
  switch (status) {
    case 'Active': return '活跃';
    case 'Inactive': return '停用';
    case 'Locked': return '锁定';
    default: return status;
  }
};

// 格式化日期
const formatDate = (dateString: string) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 过滤后的用户
const filteredUsers = computed(() => {
  let filtered = [...users.value];
  
  // 搜索过滤
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    filtered = filtered.filter(user =>
      user.username.toLowerCase().includes(query) ||
      (user.fullName && user.fullName.toLowerCase().includes(query)) ||
      (user.email && user.email.toLowerCase().includes(query))
    );
  }
  
  // 角色过滤
  if (filterRole.value) {
    filtered = filtered.filter(user => user.role === filterRole.value);
  }
  
  // 状态过滤
  if (filterStatus.value) {
    filtered = filtered.filter(user => user.status === filterStatus.value);
  }
  
  // 更新总数
  totalUsers.value = filtered.length;
  
  // 分页
  const start = (currentPage.value - 1) * pageSize.value;
  const end = start + pageSize.value;
  return filtered.slice(start, end);
});

// 计算用户统计
const calculateUserStats = () => {
  const now = new Date();
  const currentMonth = now.getMonth();
  const currentYear = now.getFullYear();
  
  userStats.value = {
    total: users.value.length,
    active: users.value.filter(u => u.status === 'Active').length,
    admins: users.value.filter(u => u.role === 'Admin').length,
    newThisMonth: users.value.filter(u => {
      const created = new Date(u.createdAt);
      return created.getMonth() === currentMonth && created.getFullYear() === currentYear;
    }).length
  };
};

// 处理搜索
const handleSearch = () => {
  currentPage.value = 1;
};

// 处理筛选
const handleFilter = () => {
  currentPage.value = 1;
};

// 重置筛选
const resetFilters = () => {
  searchQuery.value = '';
  filterRole.value = '';
  filterStatus.value = '';
  currentPage.value = 1;
};

// 处理排序
const handleSort = (sort: any) => {
  console.log('排序:', sort);
  // 这里可以实现实际的排序逻辑
};

// 处理分页大小变化
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  currentPage.value = 1;
};

// 处理当前页变化
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
};

// 刷新用户列表
const refreshUsers = async () => {
  loading.value = true;
  try {
    // 模拟加载数据
    await new Promise(resolve => setTimeout(resolve, 1000));
    loadSampleUsers();
    ElMessage.success('用户列表已刷新');
  } catch (error) {
    ElMessage.error('刷新失败');
  } finally {
    loading.value = false;
  }
};

// 导出用户
const exportUsers = () => {
  ElMessage.info('导出功能开发中...');
};

// 创建用户
const createUser = async () => {
  if (!createFormRef.value) return;
  
  try {
    await createFormRef.value.validate();
    creating.value = true;
    
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // 添加新用户到列表
    const newUser = {
      id: users.value.length + 1,
      username: createForm.value.username,
      fullName: createForm.value.fullName,
      email: createForm.value.email,
      phone: createForm.value.phone,
      role: createForm.value.role,
      status: 'Active',
      createdAt: new Date().toISOString(),
      lastLogin: null
    };
    
    users.value.unshift(newUser);
    calculateUserStats();
    
    ElMessage.success('用户创建成功');
    showCreateDialog.value = false;
    createFormRef.value.resetFields();
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    creating.value = false;
  }
};

// 编辑用户
const editUser = (user: any) => {
  editForm.value = {
    id: user.id,
    username: user.username,
    fullName: user.fullName || '',
    email: user.email || '',
    phone: user.phone || '',
    role: user.role,
    status: user.status
  };
  showEditDialog.value = true;
};

// 更新用户
const updateUser = async () => {
  if (!editFormRef.value) return;
  
  try {
    await editFormRef.value.validate();
    updating.value = true;
    
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // 更新用户信息
    const index = users.value.findIndex(u => u.id === editForm.value.id);
    if (index > -1) {
      users.value[index] = {
        ...users.value[index],
        fullName: editForm.value.fullName,
        email: editForm.value.email,
        phone: editForm.value.phone,
        role: editForm.value.role,
        status: editForm.value.status
      };
    }
    
    ElMessage.success('用户信息已更新');
    showEditDialog.value = false;
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    updating.value = false;
  }
};

// 重置密码
const resetPassword = (user: any) => {
  ElMessageBox.prompt('请输入新密码', '重置密码', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    inputType: 'password',
    inputPlaceholder: '请输入新密码',
    inputValidator: (value) => {
      if (!value) {
        return '密码不能为空';
      }
      if (value.length < 6) {
        return '密码至少6个字符';
      }
      return true;
    }
  }).then(({ value }) => {
    ElMessage.success(`用户 ${user.username} 的密码已重置`);
  }).catch(() => {
    // 用户取消
  });
};

// 切换用户状态
const toggleUserStatus = (user: any) => {
  const newStatus = user.status === 'Active' ? 'Inactive' : 'Active';
  const action = newStatus === 'Active' ? '启用' : '停用';
  
  ElMessageBox.confirm(
    `确定要${action}用户 ${user.username} 吗？`,
    `${action}确认`,
    {
      confirmButtonText: `确定${action}`,
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(() => {
    const index = users.value.findIndex(u => u.id === user.id);
    if (index > -1) {
      users.value[index].status = newStatus;
      calculateUserStats();
      ElMessage.success(`用户已${action}`);
    }
  }).catch(() => {
    // 用户取消
  });
};

// 加载示例用户数据
const loadSampleUsers = () => {
  users.value = [
    {
      id: 1,
      username: 'admin',
      fullName: '系统管理员',
      email: 'admin@wo-property.com',
      phone: '13800138000',
      role: 'Admin',
      status: 'Active',
      createdAt: '2026-01-15T08:30:00Z',
      lastLogin: '2026-04-20T14:25:00Z'
    },
    {
      id: 2,
      username: 'tech',
      fullName: '技术员张三',
      email: 'tech@wo-property.com',
      phone: '13900139000',
      role: 'Technician',
      status: 'Active',
      createdAt: '2026-02-10T10:15:00Z',
      lastLogin: '2026-04-20T13:45:00Z'
    },
    {
      id: 3,
      username: 'user',
      fullName: '普通用户李四',
      email: 'user@wo-property.com',
      phone: '13700137000',
      role: 'User',
      status: 'Active',
      createdAt: '2026-03-05T14:20:00Z',
      lastLogin: '2026-04-19T16:30:00Z'
    },
    {
      id: 4,
      username: 'manager',
      fullName: '物业经理王五',
      email: 'manager@wo-property.com',
      phone: '13600136000',
      role: 'Admin',
      status: 'Active',
      createdAt: '2026-03-20T09:45:00Z',
      lastLogin: '2026-04-20T11:20:00Z'
    },
    {
      id: 5,
      username: 'repair',
      fullName: '维修工赵六',
      email: 'repair@wo-property.com',
      phone: '13500135000',
      role: 'Technician',
      status: 'Inactive',
      createdAt: '2026-04-01T16:10:00Z',
      lastLogin: '2026-04-15T10:30:00Z'
    },
    {
      id: 6,
      username: 'tenant1',
      fullName: '租户钱七',
      email: 'tenant1@example.com',
      phone: '13400134000',
      role: 'User',
      status: 'Active',
      createdAt: '2026-04-10T11:25:00Z',
      lastLogin: '2026-04-18T15:40:00Z'
    },
    {
      id: 7,
      username: 'tenant2',
      fullName: '租户孙八',
      email: 'tenant2@example.com',
      phone: '13300133000',
      role: 'User',
      status: 'Locked',
      createdAt: '2026-04-12T13:50:00Z',
      lastLogin: '2026-04-16T09:15:00Z'
    },
    {
      id: 8,
      username: 'auditor',
      fullName: '审计员周九',
      email: 'auditor@wo-property.com',
      phone: '13200132000',
      role: 'Admin',
      status: 'Active',
      createdAt: '2026-04-15T15:35:00Z',
      lastLogin: '2026-04-20T10:05:00Z'
    }
  ];
  
  calculateUserStats();
  totalUsers.value = users.value.length;
};

// 组件挂载时加载数据
onMounted(() => {
  loadSampleUsers();
});
</script>

<style scoped>
.user-management-view {
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

/* 筛选工具栏 */
.filter-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.filter-toolbar {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

@media (min-width: 768px) {
  .filter-toolbar {
    flex-direction: row;
    align-items: center;
  }
}

.search-input {
  flex: 1;
}

.filter-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

/* 统计卡片 */
.stats-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
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
  width: 56px;
  height: 56px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.stat-icon .el-icon {
  color: white;
}

.total-users {
  background-color: #3b82f6;
}

.active-users {
  background-color: #10b981;
}

.admin-users {
  background-color: #f59e0b;
}

.new-users {
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

/* 用户表格卡片 */
.users-table-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.table-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

/* 用户单元格 */
.user-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-avatar {
  flex-shrink: 0;
}

.user-info {
  flex: 1;
}

.username {
  font-weight: 500;
  color: #1f2937;
}

.full-name {
  font-size: 12px;
  color: #6b7280;
}

/* 操作按钮 */
.action-buttons {
  display: flex;
  gap: 8px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
}

/* 对话框 */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
