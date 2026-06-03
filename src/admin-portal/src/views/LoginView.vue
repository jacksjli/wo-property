<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { auth } from '../api/auth'
import { setProjects } from '../stores/project'

const router = useRouter()

const loginForm = ref({
  username: 'admin',
  password: 'Admin@123'
})

const loading = ref(false)

const handleLogin = async () => {
  if (!loginForm.value.username || !loginForm.value.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }

  loading.value = true
  try {
    // Login via AuthService (5106) which returns project list
    const response = await auth.login(
      loginForm.value.username,
      loginForm.value.password
    )

    if (response.success && response.token) {
      // Store auth info
      localStorage.setItem('token', response.token)
      localStorage.setItem('user', JSON.stringify(response.user || response))
      localStorage.setItem('tenantCode', response.tenantCode || 'wo_property')

      // Extract real project list from AuthService response
      const projects = response.projects || []

      if (projects.length === 0) {
        ElMessage.error('该账号没有可访问的项目')
        return
      }

      // 给每个项目添加默认模块
      const allModuleNames = ['工单管理', '设备管理', '物料管理', '合同管理', '财务管理', '巡检管理', '钥匙管理', '访客管理', '消息管理', '统计分析', '住户管理', '车位管理', '缴费管理', '人员管理', '派单规则', '超时设置', '工单类型', '项目跟踪', '权限控制', '清洁管理', '社区管理', '配送管理', '快递管理', '装修管理', '项目配置', '字段管理', '部门管理', '大区省市区', '区域管理', '楼栋管理', '房号管理', '工种管理', '供应商管理', '设备类型', '公告管理', '社区活动', '设备报表', '工单报表', '物料报表', '满意度调查', '采购订单', '库存事务', '枚举定义', '综合报表']

      const projectsWithModules = projects.map((p: any) => ({
        ...p,
        status: 'Active',
        modules: allModuleNames
      }))

      // 保存到 store
      setProjects(projectsWithModules)

      if (projectsWithModules.length === 1) {
        // Only one project - auto select
        localStorage.setItem('currentProject', JSON.stringify(projectsWithModules[0]))
        ElMessage.success(`已进入项目：${projectsWithModules[0].name}`)
        router.push('/')
      } else {
        // Multiple projects - go to project selection page
        localStorage.setItem('pendingProjects', JSON.stringify(projectsWithModules))
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
</script>

<template>
  <div class="login-container">
    <div class="login-box">
      <h1 class="title">WO 物业管理</h1>
      <el-form :model="loginForm" class="login-form">
        <el-form-item>
          <el-input
            v-model="loginForm.username"
            placeholder="用户名"
            prefix-icon="User"
          />
        </el-form-item>
        <el-form-item>
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="密码"
            prefix-icon="Lock"
            @keyup.enter="handleLogin"
          />
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            class="login-button"
            :loading="loading"
            @click="handleLogin"
          >
            登录
          </el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-box {
  background: white;
  padding: 40px;
  border-radius: 10px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
  width: 400px;
}

.title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 28px;
}

.login-form {
  margin-top: 20px;
}

.login-button {
  width: 100%;
  height: 40px;
  font-size: 16px;
}
</style>