<template>
  <div class="system-settings-view">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1>系统设置</h1>
      <p>管理系统配置和参数</p>
    </div>
    
    <div class="settings-container">
      <!-- 左侧：设置导航 -->
      <div class="settings-nav">
        <el-menu
          :default-active="activeTab"
          class="settings-menu"
          @select="handleTabSelect"
        >
          <el-menu-item index="general">
            <el-icon><Setting /></el-icon>
            <span>常规设置</span>
          </el-menu-item>
          
          <el-menu-item index="notification">
            <el-icon><Bell /></el-icon>
            <span>通知设置</span>
          </el-menu-item>
          
          <el-menu-item index="security">
            <el-icon><Lock /></el-icon>
            <span>安全设置</span>
          </el-menu-item>
          
          <el-menu-item index="integration">
            <el-icon><Link /></el-icon>
            <span>集成设置</span>
          </el-menu-item>
          
          <el-menu-item index="backup">
            <el-icon><Top /></el-icon>
            <span>备份与恢复</span>
          </el-menu-item>
          
          <el-menu-item index="about">
            <el-icon><InfoFilled /></el-icon>
            <span>关于系统</span>
          </el-menu-item>
        </el-menu>
      </div>
      
      <!-- 右侧：设置内容 -->
      <div class="settings-content">
        <!-- 常规设置 -->
        <div v-if="activeTab === 'general'" class="settings-tab">
          <el-card class="settings-card" shadow="never">
            <template #header>
              <h3>常规设置</h3>
            </template>
            
            <el-form
              ref="generalFormRef"
              :model="generalForm"
              label-width="180px"
            >
              <el-form-item label="系统名称">
                <el-input v-model="generalForm.systemName" placeholder="请输入系统名称" />
              </el-form-item>
              
              <el-form-item label="系统Logo">
                <div class="logo-upload">
                  <el-upload
                    class="avatar-uploader"
                    action="#"
                    :show-file-list="false"
                    :on-change="handleLogoChange"
                    :auto-upload="false"
                  >
                    <img v-if="generalForm.logoUrl" :src="generalForm.logoUrl" class="logo-image" />
                    <el-icon v-else class="avatar-uploader-icon"><Plus /></el-icon>
                  </el-upload>
                  <div class="logo-hint">建议尺寸：200x200px，支持PNG、JPG格式</div>
                </div>
              </el-form-item>
              
              <el-form-item label="默认语言">
                <el-select v-model="generalForm.defaultLanguage" placeholder="请选择默认语言">
                  <el-option label="简体中文" value="zh-CN" />
                  <el-option label="English" value="en-US" />
                </el-select>
              </el-form-item>
              
              <el-form-item label="时区设置">
                <el-select v-model="generalForm.timezone" placeholder="请选择时区">
                  <el-option label="Asia/Shanghai (UTC+8)" value="Asia/Shanghai" />
                  <el-option label="America/New_York (UTC-5)" value="America/New_York" />
                  <el-option label="Europe/London (UTC+0)" value="Europe/London" />
                </el-select>
              </el-form-item>
              
              <el-form-item label="日期格式">
                <el-select v-model="generalForm.dateFormat" placeholder="请选择日期格式">
                  <el-option label="YYYY-MM-DD" value="YYYY-MM-DD" />
                  <el-option label="MM/DD/YYYY" value="MM/DD/YYYY" />
                  <el-option label="DD/MM/YYYY" value="DD/MM/YYYY" />
                </el-select>
              </el-form-item>
              
              <el-form-item label="时间格式">
                <el-radio-group v-model="generalForm.timeFormat">
                  <el-radio label="24h">24小时制</el-radio>
                  <el-radio label="12h">12小时制</el-radio>
                </el-radio-group>
              </el-form-item>
              
              <el-form-item label="每页显示数量">
                <el-input-number
                  v-model="generalForm.itemsPerPage"
                  :min="10"
                  :max="100"
                  :step="5"
                />
                <span class="form-hint">条记录</span>
              </el-form-item>
              
              <el-form-item>
                <el-button type="primary" @click="saveGeneralSettings" :loading="saving">
                  保存设置
                </el-button>
                <el-button @click="resetGeneralSettings">重置</el-button>
              </el-form-item>
            </el-form>
          </el-card>
        </div>
        
        <!-- 通知设置 -->
        <div v-if="activeTab === 'notification'" class="settings-tab">
          <el-card class="settings-card" shadow="never">
            <template #header>
              <h3>通知设置</h3>
            </template>
            
            <el-form
              ref="notificationFormRef"
              :model="notificationForm"
              label-width="180px"
            >
              <div class="form-section">
                <h4>邮件通知</h4>
                <el-form-item label="启用邮件通知">
                  <el-switch v-model="notificationForm.emailEnabled" />
                </el-form-item>
                
                <el-form-item label="SMTP服务器" v-if="notificationForm.emailEnabled">
                  <el-input v-model="notificationForm.smtpServer" placeholder="smtp.example.com" />
                </el-form-item>
                
                <el-form-item label="SMTP端口" v-if="notificationForm.emailEnabled">
                  <el-input-number
                    v-model="notificationForm.smtpPort"
                    :min="1"
                    :max="65535"
                  />
                </el-form-item>
                
                <el-form-item label="发件人邮箱" v-if="notificationForm.emailEnabled">
                  <el-input v-model="notificationForm.senderEmail" placeholder="noreply@example.com" />
                </el-form-item>
              </div>
              
              <div class="form-section">
                <h4>通知类型</h4>
                <el-form-item label="新工单通知">
                  <el-switch v-model="notificationForm.newTicketNotification" />
                </el-form-item>
                
                <el-form-item label="工单分配通知">
                  <el-switch v-model="notificationForm.ticketAssignmentNotification" />
                </el-form-item>
                
                <el-form-item label="工单解决通知">
                  <el-switch v-model="notificationForm.ticketResolvedNotification" />
                </el-form-item>
                
                <el-form-item label="设备维护提醒">
                  <el-switch v-model="notificationForm.deviceMaintenanceNotification" />
                </el-form-item>
              </div>
              
              <el-form-item>
                <el-button type="primary" @click="saveNotificationSettings" :loading="saving">
                  保存设置
                </el-button>
              </el-form-item>
            </el-form>
          </el-card>
        </div>
        
        <!-- 安全设置 -->
        <div v-if="activeTab === 'security'" class="settings-tab">
          <el-card class="settings-card" shadow="never">
            <template #header>
              <h3>安全设置</h3>
            </template>
            
            <el-form
              ref="securityFormRef"
              :model="securityForm"
              label-width="180px"
            >
              <div class="form-section">
                <h4>密码策略</h4>
                <el-form-item label="最小密码长度">
                  <el-input-number
                    v-model="securityForm.minPasswordLength"
                    :min="6"
                    :max="32"
                  />
                  <span class="form-hint">个字符</span>
                </el-form-item>
                
                <el-form-item label="密码复杂度要求">
                  <el-checkbox-group v-model="securityForm.passwordComplexity">
                    <el-checkbox label="uppercase">包含大写字母</el-checkbox>
                    <el-checkbox label="lowercase">包含小写字母</el-checkbox>
                    <el-checkbox label="numbers">包含数字</el-checkbox>
                    <el-checkbox label="symbols">包含特殊字符</el-checkbox>
                  </el-checkbox-group>
                </el-form-item>
                
                <el-form-item label="密码有效期">
                  <el-input-number
                    v-model="securityForm.passwordExpiryDays"
                    :min="0"
                    :max="365"
                  />
                  <span class="form-hint">天（0表示永不过期）</span>
                </el-form-item>
              </div>
              
              <div class="form-section">
                <h4>登录安全</h4>
                <el-form-item label="启用双重认证">
                  <el-switch v-model="securityForm.twoFactorEnabled" />
                </el-form-item>
                
                <el-form-item label="最大登录失败次数" v-if="securityForm.twoFactorEnabled">
                  <el-input-number
                    v-model="securityForm.maxLoginAttempts"
                    :min="1"
                    :max="10"
                  />
                  <span class="form-hint">次</span>
                </el-form-item>
                
                <el-form-item label="账户锁定时间" v-if="securityForm.twoFactorEnabled">
                  <el-input-number
                    v-model="securityForm.lockoutMinutes"
                    :min="1"
                    :max="1440"
                  />
                  <span class="form-hint">分钟</span>
                </el-form-item>
                
                <el-form-item label="会话超时时间">
                  <el-input-number
                    v-model="securityForm.sessionTimeout"
                    :min="5"
                    :max="480"
                  />
                  <span class="form-hint">分钟</span>
                </el-form-item>
              </div>
              
              <el-form-item>
                <el-button type="primary" @click="saveSecuritySettings" :loading="saving">
                  保存设置
                </el-button>
              </el-form-item>
            </el-form>
          </el-card>
        </div>
        
        <!-- 集成设置 -->
        <div v-if="activeTab === 'integration'" class="settings-tab">
          <el-card class="settings-card" shadow="never">
            <template #header>
              <h3>集成设置</h3>
            </template>
            
            <div class="integration-list">
              <div class="integration-item">
                <div class="integration-info">
                  <div class="integration-icon">
                    <el-icon><Message /></el-icon>
                  </div>
                  <div class="integration-details">
                    <div class="integration-name">企业微信</div>
                    <div class="integration-desc">将工单通知推送到企业微信</div>
                  </div>
                </div>
                <el-switch v-model="integrations.wechatWork" />
              </div>
              
              <div class="integration-item">
                <div class="integration-info">
                  <div class="integration-icon">
                    <el-icon><ChatDotRound /></el-icon>
                  </div>
                  <div class="integration-details">
                    <div class="integration-name">钉钉</div>
                    <div class="integration-desc">将系统通知推送到钉钉群</div>
                  </div>
                </div>
                <el-switch v-model="integrations.dingtalk" />
              </div>
              
              <div class="integration-item">
                <div class="integration-info">
                  <div class="integration-icon">
                    <el-icon><Promotion /></el-icon>
                  </div>
                  <div class="integration-details">
                    <div class="integration-name">Webhook</div>
                    <div class="integration-desc">通过Webhook推送事件到外部系统</div>
                  </div>
                </div>
                <el-switch v-model="integrations.webhook" />
              </div>
              
              <div class="integration-item">
                <div class="integration-info">
                  <div class="integration-icon">
                    <el-icon><DataLine /></el-icon>
                  </div>
                  <div class="integration-details">
                    <div class="integration-name">API访问</div>
                    <div class="integration-desc">启用外部系统API访问</div>
                  </div>
                </div>
                <el-switch v-model="integrations.apiAccess" />
              </div>
            </div>
            
            <div class="integration-actions">
              <el-button type="primary" @click="saveIntegrationSettings" :loading="saving">
                保存设置
              </el-button>
            </div>
          </el-card>
        </div>
        
        <!-- 备份与恢复 -->
        <div v-if="activeTab === 'backup'" class="settings-tab">
          <el-card class="settings-card" shadow="never">
            <template #header>
              <h3>备份与恢复</h3>
            </template>
            
            <div class="backup-section">
              <h4>数据备份</h4>
              <div class="backup-options">
                <el-button type="primary" @click="createBackup" :loading="backupLoading">
                  <el-icon><Download /></el-icon>
                  立即备份
                </el-button>
                
                <el-button @click="scheduleBackup">
                  <el-icon><Clock /></el-icon>
                  计划备份
                </el-button>
              </div>
              
              <div class="backup-history">
                <h5>备份历史</h5>
                <el-table :data="backupHistory" style="width: 100%">
                  <el-table-column prop="date" label="备份时间" width="180" />
                  <el-table-column prop="size" label="文件大小" width="120" />
                  <el-table-column prop="type" label="备份类型" width="120" />
                  <el-table-column label="操作" width="200">
                    <template #default="scope">
                      <el-button size="small" @click="restoreBackup(scope.row)">
                        恢复
                      </el-button>
                      <el-button size="small" type="danger" @click="deleteBackup(scope.row)">
                        删除
                      </el-button>
                    </template>
                  </el-table-column>
                </el-table>
              </div>
            </div>
            
            <div class="restore-section">
              <h4>数据恢复</h4>
              <div class="restore-options">
                <el-upload
                  class="restore-upload"
                  action="#"
                  :show-file-list="false"
                  :on-change="handleRestoreFile"
                  :auto-upload="false"
                >
                  <el-button type="warning">
                    <el-icon><Upload /></el-icon>
                    选择备份文件
                  </el-button>
                </el-upload>
                
                <div class="restore-hint">
                  支持JSON格式的备份文件，恢复前请确认已备份当前数据
                </div>
              </div>
            </div>
          </el-card>
        </div>
        
        <!-- 关于系统 -->
        <div v-if="activeTab === 'about'" class="settings-tab">
          <el-card class="settings-card" shadow="never">
            <template #header>
              <h3>关于系统</h3>
            </template>
            
            <div class="about-content">
              <div class="about-header">
                <div class="system-logo">
                  <div class="logo-placeholder">
                    <span>WO</span>
                  </div>
                </div>
                <div class="system-info">
                  <h2>WO物业管理软件</h2>
                  <p class="system-version">版本 2.0.0</p>
                  <p class="system-description">
                    现代化的物业管理解决方案，提供完整的工单管理、设备管理和用户管理功能。
                  </p>
                </div>
              </div>
              
              <div class="about-details">
                <div class="detail-item">
                  <div class="detail-label">技术架构</div>
                  <div class="detail-value">
                    .NET 10 + Vue 3 + TypeScript + SQLite
                  </div>
                </div>
                
                <div class="detail-item">
                  <div class="detail-label">开发团队</div>
                  <div class="detail-value">李先生技术团队</div>
                </div>
                
                <div class="detail-item">
                  <div class="detail-label">联系方式</div>
                  <div class="detail-value">support@wo-property.com</div>
                </div>
                
                <div class="detail-item">
                  <div class="detail-label">系统状态</div>
                  <div class="detail-value">
                    <el-tag type="success">运行正常</el-tag>
                  </div>
                </div>
                
                <div class="detail-item">
                  <div class="detail-label">最后更新</div>
                  <div class="detail-value">2026-04-20</div>
                </div>
              </div>
              
              <div class="about-actions">
                <el-button type="primary" @click="checkForUpdates">
                  <el-icon><Refresh /></el-icon>
                  检查更新
                </el-button>
                
                <el-button @click="viewLicense">
                  <el-icon><Document /></el-icon>
                  查看许可证
                </el-button>
                
                <el-button @click="viewDocumentation">
                  <el-icon><Help /></el-icon>
                  查看文档
                </el-button>
              </div>
            </div>
          </el-card>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref} from 'vue';
import { ElMessage, type UploadFile } from 'element-plus';
import {
  Setting, Bell, Lock, Link, Top, InfoFilled,
  Plus, Message, ChatDotRound, Promotion, DataLine,
  Download, Clock, Upload, Refresh, Document, Help
} from '@element-plus/icons-vue';

// 当前激活的标签页
const activeTab = ref('general');

// 保存状态
const saving = ref(false);
const backupLoading = ref(false);

// 常规设置表单
const generalForm = ref({
  systemName: 'WO物业管理软件',
  logoUrl: '',
  defaultLanguage: 'zh-CN',
  timezone: 'Asia/Shanghai',
  dateFormat: 'YYYY-MM-DD',
  timeFormat: '24h',
  itemsPerPage: 20
});

// 通知设置表单
const notificationForm = ref({
  emailEnabled: false,
  smtpServer: '',
  smtpPort: 587,
  senderEmail: '',
  newTicketNotification: true,
  ticketAssignmentNotification: true,
  ticketResolvedNotification: true,
  deviceMaintenanceNotification: true
});

// 安全设置表单
const securityForm = ref({
  minPasswordLength: 8,
  passwordComplexity: ['uppercase', 'lowercase', 'numbers'],
  passwordExpiryDays: 90,
  twoFactorEnabled: false,
  maxLoginAttempts: 5,
  lockoutMinutes: 30,
  sessionTimeout: 30
});

// 集成设置
const integrations = ref({
  wechatWork: false,
  dingtalk: false,
  webhook: false,
  apiAccess: true
});

// 备份历史数据
const backupHistory = ref([
  { date: '2026-04-20 15:30:00', size: '2.5 MB', type: '完整备份' },
  { date: '2026-04-19 03:00:00', size: '2.4 MB', type: '自动备份' },
  { date: '2026-04-18 15:30:00', size: '2.3 MB', type: '完整备份' },
  { date: '2026-04-17 03:00:00', size: '2.2 MB', type: '自动备份' }
]);

// 处理标签页选择
const handleTabSelect = (index: string) => {
  activeTab.value = index;
};

// 处理Logo上传
const handleLogoChange = (file: UploadFile) => {
  if (file.raw) {
    const reader = new FileReader();
    reader.onload = (e) => {
      generalForm.value.logoUrl = e.target?.result as string;
    };
    reader.readAsDataURL(file.raw);
  }
};

// 保存常规设置
const saveGeneralSettings = async () => {
  saving.value = true;
  try {
    // 模拟API调用
    await new Promise(resolve => setTimeout(resolve, 1000));
    ElMessage.success('常规设置已保存');
  } catch (error) {
    ElMessage.error('保存失败');
  } finally {
    saving.value = false;
  }
};

// 重置常规设置
const resetGeneralSettings = () => {
  generalForm.value = {
    systemName: 'WO物业管理软件',
    logoUrl: '',
    defaultLanguage: 'zh-CN',
    timezone: 'Asia/Shanghai',
    dateFormat: 'YYYY-MM-DD',
    timeFormat: '24h',
    itemsPerPage: 20
  };
  ElMessage.info('常规设置已重置为默认值');
};

// 保存通知设置
const saveNotificationSettings = async () => {
  saving.value = true;
  try {
    await new Promise(resolve => setTimeout(resolve, 1000));
    ElMessage.success('通知设置已保存');
  } catch (error) {
    ElMessage.error('保存失败');
  } finally {
    saving.value = false;
  }
};

// 保存安全设置
const saveSecuritySettings = async () => {
  saving.value = true;
  try {
    await new Promise(resolve => setTimeout(resolve, 1000));
    ElMessage.success('安全设置已保存');
  } catch (error) {
    ElMessage.error('保存失败');
  } finally {
    saving.value = false;
  }
};

// 保存集成设置
const saveIntegrationSettings = async () => {
  saving.value = true;
  try {
    await new Promise(resolve => setTimeout(resolve, 1000));
    ElMessage.success('集成设置已保存');
  } catch (error) {
    ElMessage.error('保存失败');
  } finally {
    saving.value = false;
  }
};

// 创建备份
const createBackup = async () => {
  backupLoading.value = true;
  try {
    await new Promise(resolve => setTimeout(resolve, 2000));
    backupHistory.value.unshift({
      date: new Date().toLocaleString('zh-CN'),
      size: '2.6 MB',
      type: '手动备份'
    });
    ElMessage.success('数据备份已创建');
  } catch (error) {
    ElMessage.error('备份失败');
  } finally {
    backupLoading.value = false;
  }
};

// 计划备份
const scheduleBackup = () => {
  ElMessage.info('计划备份功能开发中...');
};

// 恢复备份
const restoreBackup = (backup: any) => {
  ElMessageBox.confirm(
    `确定要恢复备份 ${backup.date} 吗？当前数据将被覆盖。`,
    '恢复确认',
    {
      confirmButtonText: '确定恢复',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(() => {
    ElMessage.success('数据恢复已开始，请稍候...');
  }).catch(() => {
    // 用户取消
  });
};

// 删除备份
const deleteBackup = (backup: any) => {
  ElMessageBox.confirm(
    `确定要删除备份 ${backup.date} 吗？`,
    '删除确认',
    {
      confirmButtonText: '确定删除',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(() => {
    const index = backupHistory.value.findIndex(b => b.date === backup.date);
    if (index > -1) {
      backupHistory.value.splice(index, 1);
      ElMessage.success('备份已删除');
    }
  }).catch(() => {
    // 用户取消
  });
};

// 处理恢复文件上传
const handleRestoreFile = (file: UploadFile) => {
  if (file.raw) {
    ElMessageBox.confirm(
      '确定要使用此文件恢复数据吗？当前数据将被覆盖。',
      '恢复确认',
      {
        confirmButtonText: '确定恢复',
        cancelButtonText: '取消',
        type: 'warning'
      }
    ).then(() => {
      ElMessage.success('数据恢复已开始，请稍候...');
    }).catch(() => {
      // 用户取消
    });
  }
};

// 检查更新
const checkForUpdates = () => {
  ElMessage.info('当前已是最新版本');
};

// 查看许可证
const viewLicense = () => {
  ElMessage.info('许可证查看功能开发中...');
};

// 查看文档
const viewDocumentation = () => {
  window.open('https://docs.wo-property.com', '_blank');
};
</script>

<style scoped>
.system-settings-view {
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

/* 设置容器 */
.settings-container {
  display: grid;
  grid-template-columns: 240px 1fr;
  gap: 24px;
}

@media (max-width: 768px) {
  .settings-container {
    grid-template-columns: 1fr;
  }
}

/* 设置导航 */
.settings-nav {
  border-right: 1px solid #e5e7eb;
}

.settings-menu {
  border-right: none;
}

/* 设置内容 */
.settings-content {
  min-height: 600px;
}

.settings-card {
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.settings-card h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

/* 表单样式 */
.form-section {
  margin-bottom: 32px;
  padding-bottom: 24px;
  border-bottom: 1px solid #e5e7eb;
}

.form-section:last-child {
  margin-bottom: 0;
  padding-bottom: 0;
  border-bottom: none;
}

.form-section h4 {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 16px 0;
}

.form-hint {
  margin-left: 8px;
  color: #6b7280;
  font-size: 14px;
}

/* Logo上传 */
.logo-upload {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.avatar-uploader {
  width: 120px;
  height: 120px;
  border: 2px dashed #d1d5db;
  border-radius: 8px;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  transition: border-color 0.3s;
}

.avatar-uploader:hover {
  border-color: #3b82f6;
}

.avatar-uploader-icon {
  font-size: 28px;
  color: #9ca3af;
  width: 120px;
  height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.logo-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.logo-hint {
  font-size: 12px;
  color: #6b7280;
}

/* 集成列表 */
.integration-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-bottom: 24px;
}

.integration-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  border-radius: 8px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
}

.integration-info {
  display: flex;
  align-items: center;
  gap: 16px;
}

.integration-icon {
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

.integration-details {
  flex: 1;
}

.integration-name {
  font-weight: 500;
  color: #1f2937;
  margin-bottom: 4px;
}

.integration-desc {
  font-size: 14px;
  color: #6b7280;
}

.integration-actions {
  display: flex;
  justify-content: flex-end;
}

/* 备份与恢复 */
.backup-section,
.restore-section {
  margin-bottom: 32px;
}

.backup-section h4,
.restore-section h4 {
  font-size: 16px;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 16px 0;
}

.backup-options {
  display: flex;
  gap: 12px;
  margin-bottom: 24px;
}

.backup-history h5 {
  font-size: 14px;
  font-weight: 600;
  color: #4b5563;
  margin: 0 0 12px 0;
}

.restore-options {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.restore-hint {
  font-size: 12px;
  color: #6b7280;
}

/* 关于系统 */
.about-content {
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.about-header {
  display: flex;
  align-items: center;
  gap: 24px;
}

.system-logo {
  flex-shrink: 0;
}

.logo-placeholder {
  width: 80px;
  height: 80px;
  border-radius: 16px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 24px;
  font-weight: bold;
}

.system-info {
  flex: 1;
}

.system-info h2 {
  font-size: 24px;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 8px 0;
}

.system-version {
  font-size: 14px;
  color: #6b7280;
  margin: 0 0 12px 0;
}

.system-description {
  font-size: 16px;
  color: #4b5563;
  line-height: 1.5;
  margin: 0;
}

.about-details {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.detail-item {
  display: flex;
  justify-content: space-between;
  padding: 12px 0;
  border-bottom: 1px solid #f3f4f6;
}

.detail-item:last-child {
  border-bottom: none;
}

.detail-label {
  font-weight: 500;
  color: #4b5563;
}

.detail-value {
  color: #1f2937;
  text-align: right;
}

.about-actions {
  display: flex;
  gap: 12px;
}
</style>
