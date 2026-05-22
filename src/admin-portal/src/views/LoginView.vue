<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { auth } from '../api/auth'
import ProjectSelectorDialog from '../components/ProjectSelectorDialog.vue'

const router = useRouter()

const loginForm = ref({
  username: 'admin',
  password: 'Admin@123'
})

const loading = ref(false)
const showProjectSelector = ref(false)
const projectList = ref<Array<{ code: string; name: string; displayName?: string }>>([])
const loginToken = ref('')

const handleLogin = async () => {
  if (!loginForm.value.username || !loginForm.value.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }

  loading.value = true
  try {
    // Login via CenterService (5016) which returns project list
    const response = await auth.login(
      loginForm.value.username,
      loginForm.value.password
    )

    if (response.success && response.token) {
      // Store auth info (use 'token' key for router guard compatibility)
      localStorage.setItem('token', response.token)
      localStorage.setItem('user', JSON.stringify(response.user || response))
      loginToken.value = response.token
      
      // Extract project list from response
      // CenterService returns: { token, projects: [{code, name, ...}], user }
      const projects = response.projects || []
      
      if (projects.length === 0) {
        ElMessage.error('该账号没有可访问的项目')
        return
      }
      
      console.log('[Login] Projects:', JSON.stringify(projects))
      console.log('[Login] showProjectSelector will be set to:', projects.length > 1)
      console.log('[Login] projectList will be:', JSON.stringify(projects))
      
      if (projects.length === 1) {
        // Only one project - auto select
        selectProject(projects[0])
      } else {
        // Multiple projects - go directly to project selection page
        localStorage.setItem('pendingProjects', JSON.stringify(projects))
        router.push('/project')
      }
    } else {
      ElMessage.error(response.message || '登录失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '登录失败，请检查用户名和密码')
  } finally {
    loading.value = false
  }
}

const selectProject = (project: { code: string; name: string }) => {
  console.log('[selectProject] called with:', project)
  localStorage.setItem('currentProject', project.code)
  localStorage.setItem('currentProjectName', project.name)
  
  showProjectSelector.value = false
  ElMessage.success(`已进入项目：${project.name}`)
  console.log('[selectProject] calling router.push("/")')
  router.push('/project').then(() => {
    console.log('[selectProject] navigation complete')
  }).catch(err => {
    console.error('[selectProject] navigation error:', err)
  })
}

const handleProjectSelected = (project: { code: string; name: string }) => {
  selectProject(project)
}
</script>

<template>
  <div class="login-container">
    <div class="login-box">
      <div class="login-header">
        <el-icon size="48" color="#409eff"><House /></el-icon>
        <h1>WO物业管理</h1>
        <p>单租户多项目版</p>
      </div>

      <el-form :model="loginForm" class="login-form">
        <el-form-item>
          <el-input
            v-model="loginForm.username"
            placeholder="用户名"
            size="large"
            prefix-icon="User"
          />
        </el-form-item>
        <el-form-item>
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="密码"
            size="large"
            prefix-icon="Lock"
            @keyup.enter="handleLogin"
          />
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            size="large"
            :loading="loading"
            @click="handleLogin"
            class="login-btn"
          >
            登 录
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-footer">
        <p>测试账号: admin/Admin@123</p>
      </div>
    </div>

    <!-- Project Selector Dialog -->
    <ProjectSelectorDialog
      v-model:visible="showProjectSelector"
      :projects="projectList"
      @select="handleProjectSelected"
    />
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-box {
  width: 400px;
  padding: 40px;
  background: #fff;
  border-radius: 10px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
}

.login-header {
  text-align: center;
  margin-bottom: 30px;
}

.login-header h1 {
  margin: 15px 0 5px;
  font-size: 28px;
  color: #303133;
}

.login-header p {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

.login-form {
  margin-top: 20px;
}

.login-btn {
  width: 100%;
}

.login-footer {
  margin-top: 20px;
  text-align: center;
}

.login-footer p {
  color: #c0c4cc;
  font-size: 12px;
}
</style>