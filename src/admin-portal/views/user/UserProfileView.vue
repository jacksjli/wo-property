<template>
  <div class="user-profile-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1>个人资料</h1>
      <p>管理您的账户信息和设置</p>
    </div>
    
    <div class="profile-container">
      <!-- 左侧：个人信息 -->
      <el-card class="profile-card" shadow="never">
        <template #header>
          <div class="card-header">
            <h3>个人信息</h3>
            <el-button 
              type="text" 
              @click="editMode = !editMode"
              v-if="!editMode"
            >
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
          </div>
        </template>
        
        <div class="profile-content">
          <!-- 头像区域 -->
          <div class="avatar-section">
            <el-avatar :size="120" :src="userAvatar" class="profile-avatar">
              {{ authStore.userName.charAt(0).toUpperCase() }}
            </el-avatar>
            <div class="avatar-actions" v-if="editMode">
              <el-button size="small" @click="changeAvatar">
                <el-icon><Camera /></el-icon>
                更换头像
              </el-button>
            </div>
          </div>
          
          <!-- 用户信息 -->
          <div class="info-section">
            <el-form
              ref="profileFormRef"
              :model="profileForm"
              :rules="profileRules"
              label-width="100px"
              :disabled="!editMode"
            >
              <el-form-item label="用户名" prop="username">
                <el-input v-model="profileForm.username" />
              </el-form-item>
              
              <el-form-item label="姓名" prop="fullName">
                <el-input v-model="profileForm.fullName" />
              </el-form-item>
              
              <el-form-item label="邮箱" prop="email">
                <el-input v-model="profileForm.email" type="email" />
              </el-form-item>
              
              <el-form-item label="手机号" prop="phone">
                <el-input v-model="profileForm.phone" />
              </el-form-item>
              
              <el-form-item label="角色">
                <el-input :value="authStore.userRole" disabled />
              </el-form-item>
              
              <el-form-item label="账户状态">
                <el-input :value="authStore.user?.status || 'Active'" disabled />
              </el-form-item>
              
              <el-form-item label="注册时间">
                <el-input :value="formatDate(authStore.user?.createdAt)" disabled />
              </el-form-item>
              
              <div v-if="editMode" class="form-actions">
                <el-button @click="cancelEdit">取消</el-button>
                <el-button type="primary" @click="saveProfile" :loading="saving">
                  保存更改
                </el-button>
              </div>
            </el-form>
          </div>
        </div>
      </el-card>
      
      <!-- 右侧：安全设置和统计 -->
      <div class="right-column">
        <!-- 安全设置 -->
        <el-card class="security-card" shadow="never">
          <template #header>
            <h3>安全设置</h3>
          </template>
          
          <div class="security-content">
            <div class="security-item">
              <div class="security-info">
                <div class="security-title">修改密码</div>
                <div class="security-desc">定期更新密码以提高账户安全性</div>
              </div>
              <el-button type="text" @click="showChangePassword = true">
                修改
              </el-button>
            </div>
            
            <div class="security-item">
              <div class="security-info">
                <div class="security-title">登录记录</div>
                <div class="security-desc">查看最近的登录活动</div>
              </div>
              <el-button type="text" @click="viewLoginHistory">
                查看
              </el-button>
            </div>
            
            <div class="security-item">
              <div class="security-info">
                <div class="security-title">双重认证</div>
                <div class="security-desc">为账户添加额外的安全层</div>
              </div>
              <el-switch v-model="twoFactorEnabled" />
            </div>
          </div>
        </el-card>
        
        <!-- 用户统计 -->
        <el-card class="stats-card" shadow="never">
          <template #header>
            <h3>活动统计</h3>
          </template>
          
          <div class="stats-content">
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Ticket /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ userStats.createdTickets }}</div>
                <div class="stat-label">创建的工单</div>
              </div>
            </div>
            
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Check /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ userStats.resolvedTickets }}</div>
                <div class="stat-label">解决的工单</div>
              </div>
            </div>
            
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Clock /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ userStats.activeTickets }}</div>
                <div class="stat-label">进行中的工单</div>
              </div>
            </div>
            
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Calendar /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ userStats.daysActive }}</div>
                <div class="stat-label">活跃天数</div>
              </div>
            </div>
          </div>
        </el-card>
      </div>
    </div>
    
    <!-- 修改密码对话框 -->
    <el-dialog
      v-model="showChangePassword"
      title="修改密码"
      width="400px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="passwordFormRef"
        :model="passwordForm"
        :rules="passwordRules"
        label-width="100px"
      >
        <el-form-item label="当前密码" prop="currentPassword">
          <el-input
            v-model="passwordForm.currentPassword"
            type="password"
            show-password
          />
        </el-form-item>
        
        <el-form-item label="新密码" prop="newPassword">
          <el-input
            v-model="passwordForm.newPassword"
            type="password"
            show-password
          />
        </el-form-item>
        
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input
            v-model="passwordForm.confirmPassword"
            type="password"
            show-password
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="showChangePassword = false">取消</el-button>
          <el-button type="primary" @click="changePassword" :loading="changingPassword">
            确认修改
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { Edit, Camera, Ticket, Check, Clock, Calendar } from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores/auth';
import { useTicketStore } from '@/stores/ticket';

const authStore = useAuthStore();
const ticketStore = useTicketStore();

// 编辑模式
const editMode = ref(false);
const saving = ref(false);

// 用户头像
const userAvatar = computed(() => {
  return `https://api.dicebear.com/7.x/avataaars/svg?seed=${authStore.userName}`;
});

// 个人信息表单
const profileFormRef = ref<FormInstance>();
const profileForm = ref({
  username: authStore.user?.username || '',
  fullName: authStore.user?.fullName || '',
  email: authStore.user?.email || '',
  phone: authStore.user?.phone || ''
});

// 表单验证规则
const profileRules: FormRules = {
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
  ]
};

// 安全设置
const showChangePassword = ref(false);
const twoFactorEnabled = ref(false);
const changingPassword = ref(false);

// 修改密码表单
const passwordFormRef = ref<FormInstance>();
const passwordForm = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
});

// 密码验证规则
const passwordRules: FormRules = {
  currentPassword: [
    { required: true, message: '请输入当前密码', trigger: 'blur' }
  ],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少6个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (value !== passwordForm.value.newPassword) {
          callback(new Error('两次输入的密码不一致'));
        } else {
          callback();
        }
      },
      trigger: 'blur'
    }
  ]
};

// 用户统计
const userStats = ref({
  createdTickets: 0,
  resolvedTickets: 0,
  activeTickets: 0,
  daysActive: 0
});

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

// 更换头像
const changeAvatar = () => {
  ElMessage.info('头像更换功能开发中...');
};

// 保存个人资料
const saveProfile = async () => {
  if (!profileFormRef.value) return;
  
  try {
    await profileFormRef.value.validate();
    saving.value = true;
    
    // 模拟保存操作
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    ElMessage.success('个人资料已更新');
    editMode.value = false;
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
  if (authStore.user) {
    profileForm.value = {
      username: authStore.user.username,
      fullName: authStore.user.fullName,
      email: authStore.user.email || '',
      phone: authStore.user.phone || ''
    };
  }
};

// 修改密码
const changePassword = async () => {
  if (!passwordFormRef.value) return;
  
  try {
    await passwordFormRef.value.validate();
    changingPassword.value = true;
    
    // 模拟密码修改操作
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    ElMessage.success('密码修改成功');
    showChangePassword.value = false;
    passwordFormRef.value.resetFields();
  } catch (error) {
    console.error('表单验证失败:', error);
  } finally {
    changingPassword.value = false;
  }
};

// 查看登录历史
const viewLoginHistory = () => {
  ElMessage.info('登录记录功能开发中...');
};

// 计算用户统计
const calculateUserStats = () => {
  if (!authStore.user) return;
  
  const userId = authStore.user.id;
  const tickets = ticketStore.tickets;
  
  userStats.value = {
    createdTickets: tickets.filter(t => t.createdBy === userId).length,
    resolvedTickets: tickets.filter(t => t.createdBy === userId && t.status === 'Resolved').length,
    activeTickets: tickets.filter(t => t.createdBy === userId && (t.status === 'New' || t.status === 'InProgress')).length,
    daysActive: Math.floor((new Date().getTime() - new Date(authStore.user.createdAt).getTime()) / (1000 * 60 * 60 * 24))
  };
};

// 组件挂载时初始化
onMounted(() => {
  if (authStore.user) {
    profileForm.value = {
      username: authStore.user.username,
      fullName: authStore.user.fullName,
      email: authStore.user.email || '',
      phone: authStore.user.phone || ''
    };
    
    calculateUserStats();
  }
});
</script>

<style scoped>
.user-profile-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 页面标题 */
.page-header {
  margin-bottom: 8px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 4px 0;
}

.page-header p {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

/* 个人资料容器 */
.profile-container {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 24px;
}

@media (max-width: 1200px) {
  .profile-container {
    grid-template-columns: 1fr;
  }
}

/* 个人信息卡片 */
.profile-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.profile-content {
  display: flex;
  flex-direction: column;
  gap: 32px;
}

/* 头像区域 */
.avatar-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.profile-avatar {
  border: 4px solid #f3f4f6;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.avatar-actions {
  display: flex;
  gap: 8px;
}

/* 信息区域 */
.info-section {
  max-width: 600px;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
}

/* 右侧列 */
.right-column {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* 安全设置卡片 */
.security-card,
.stats-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.security-card h3,
.stats-card h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.security-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.security-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f3f4f6;
}

.security-item:last-child {
  border-bottom: none;
}

.security-info {
  flex: 1;
}

.security-title {
  font-size: 16px;
  font-weight: 500;
  color: #1f2937;
  margin-bottom: 4px;
}

.security-desc {
  font-size: 14px;
  color: #6b7280;
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

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #1f2937;
  line-height: 1;
}

.stat-label {
  font-size: 14px;
  color: #6b7280;
  margin-top: 4px;
}

/* 对话框 */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
