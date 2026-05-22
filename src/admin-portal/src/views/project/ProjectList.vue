<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Delete, Setting, Check, InfoFilled, ArrowLeft, ArrowRight, View, Monitor, Tickets, Box, Document, Money, Location, Warning, Key, User, Bell, DataAnalysis, UserFilled, House, CreditCard, Odometer, Tools, Folder } from '@element-plus/icons-vue'
import { useRouter, useRoute } from 'vue-router'
import { currentProject, projects, allModules, getProjectModules, enterProject, exitProject as exitProjectStore, toggleProjectStatus, saveProjectsConfig } from '@/stores/project'

const router = useRouter()
const route = useRoute()

// 模拟当前用户角色
const currentUserRole = ref('Administrator')

// 选中的项目（编辑用）
const selectedProject = ref<any>(null)

// 获取项目启用的模块
const getProjectModules = (project: any) => {
  return allModules.filter(m => project.modules.includes(m.name))
}

// 切换项目状态
const handleToggleStatus = async (project: any) => {
  if (!isAdmin()) {
    ElMessage.warning('只有系统管理员才能操作')
    return
  }
  
  // 如果是要启用（当前已经是Inactive）
  if (project.status === 'Active') {
    // 项目已启用
    return
  }
  
  // 如果是要停用（当前是Active），弹出确认
  try {
    await ElMessageBox.confirm(
      `确定要停用项目「${project.name}」吗？停用后该项目的所有模块将无法访问。`,
      '停用确认',
      {
        confirmButtonText: '确定停用',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    // 用户确认停用 - project.status 已经是 'Inactive'（由 v-model 设置）
    ElMessage.success('项目已停用')
  } catch (error: any) {
    // 用户取消 - 恢复状态
    if (error !== 'cancel') {
      project.status = 'Active'
    } else {
      project.status = 'Active'
    }
  }
}

// 进入项目视图
const enterProjectView = (project: any) => {
  if (project.status !== 'Active') {
    ElMessage.warning('请先启用该项目')
    return
  }
  enterProject(project)
  const firstModule = getProjectModules(project)[0]
  if (firstModule) {
    router.push(firstModule.path)
  } else {
    router.push('/project')
  }
  ElMessage.success(`已进入"${project.name}"`)
}

// 退出项目视图
const exitProjectView = () => {
  exitProjectStore()
  ElMessage.info('已退出项目')
}

// 导航到模块
const navigateToModule = (mod: any) => {
  router.push(mod.path)
}

// 对话框状态
const dialogVisible = ref(false)
const dialogTitle = ref('新增项目')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

// 表单数据
const form = ref({
  name: '',
  code: '',
  description: '',
  status: 'Active'
})

// 表单验证
const rules: FormRules = {
  name: [{ required: true, message: '请输入项目名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入项目代码', trigger: 'blur' }]
}

// 状态选项
const statusOptions = [
  { value: 'Active', label: '启用' },
  { value: 'Inactive', label: '停用' }
]

// 获取状态标签
const getStatusType = (status: string) => status === 'Active' ? 'success' : 'info'
const getStatusText = (status: string) => status === 'Active' ? '启用' : '停用'

// 检查是否为管理员
const isAdmin = () => currentUserRole.value === 'Administrator'

// 选择项目
const selectProject = (project: any) => {
  selectedProject.value = project
}

// 切换模块选择
const toggleModule = (moduleName: string) => {
  if (!isAdmin()) {
    ElMessage.warning('只有系统管理员才能配置模块')
    return
  }
  
  const index = selectedProject.value.modules.indexOf(moduleName)
  if (index === -1) {
    selectedProject.value.modules.push(moduleName)
  } else {
    selectedProject.value.modules.splice(index, 1)
  }
  
  const projectIndex = projects.value.findIndex(p => p.id === selectedProject.value.id)
  if (projectIndex !== -1) {
    projects.value[projectIndex] = { ...selectedProject.value }
  }
  
  // 自动保存
  saveProjectsConfig()
  ElMessage.success('模块配置已保存')
}

// 检查模块是否已选中
const isModuleSelected = (moduleName: string) => selectedProject.value?.modules.includes(moduleName)

// 打开新增对话框
const handleAdd = () => {
  if (!isAdmin()) {
    ElMessage.warning('只有系统管理员才能执行此操作')
    return
  }
  dialogTitle.value = '新增项目'
  editingId.value = null
  form.value = { name: '', code: '', description: '', address: '', contactPhone: '', status: 'Active' }
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = () => {
  if (!isAdmin()) {
    ElMessage.warning('只有系统管理员才能执行此操作')
    return
  }
  if (!selectedProject.value) {
    ElMessage.warning('请先选择一个项目')
    return
  }
  dialogTitle.value = '编辑项目'
  editingId.value = selectedProject.value.id
  form.value = { ...selectedProject.value }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    submitting.value = true
    
    const token = localStorage.getItem('token')
    
    if (editingId.value) {
      // 编辑模式 - 调用后端 API
      const response = await fetch(`http://localhost:5000/api/projects/${editingId.value}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(form.value)
      })
      const data = await response.json()
      if (data.success) {
        // 更新本地数据
        const index = projects.value.findIndex(p => p.id === editingId.value)
        if (index !== -1) {
          projects.value[index] = { 
            ...projects.value[index], 
            ...form.value,
            updatedAt: new Date().toLocaleDateString('zh-CN')
          }
          if (selectedProject.value?.id === editingId.value) {
            selectedProject.value = projects.value[index]
          }
        }
        ElMessage.success('项目更新成功')
      } else {
        ElMessage.error(data.message || '更新失败')
      }
    } else {
      // 新增模式 - 调用后端 API
      const response = await fetch('http://localhost:5000/api/projects', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(form.value)
      })
      const data = await response.json()
      if (data.success) {
        // 刷新项目列表
        await loadProjects()
        ElMessage.success(data.message || '项目创建成功，数据库正在初始化中')
      } else {
        ElMessage.error(data.message || '创建失败')
      }
    }
    
    dialogVisible.value = false
  } catch (error: any) {
    ElMessage.error('操作失败: ' + (error.message || '网络错误'))
  } finally {
    submitting.value = false
  }
}

// 加载项目列表从后端
const loadProjects = async () => {
  try {
    const token = localStorage.getItem('token')
    const response = await fetch('http://localhost:5000/api/projects', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
    const data = await response.json()
    if (data.success && data.data) {
      // 转换后端数据为前端格式
      // 先获取所有项目的模块配置
      const projectsWithModules = await Promise.all(data.data.map(async (p: any) => {
        let modules: string[] = []
        try {
          const modRes = await fetch(`http://localhost:5000/api/projects/${p.code}/modules`, {
            headers: { 'Authorization': `Bearer ${token}` }
          })
          const modData = await modRes.json()
          if (modData.success && modData.data) {
            modules = modData.data
          }
        } catch (e) {
          console.error(`获取项目 ${p.code} 模块失败`, e)
        }
        return {
          id: p.id,
          code: p.code,
          name: p.name,
          description: p.description || '',
          status: p.status === 'active' ? 'Active' : 'Inactive',
          databaseName: p.databaseName,
          modules,
          createdAt: p.createdAt,
          updatedAt: p.updatedAt
        }
      }))
      projects.value = projectsWithModules
    }
  } catch (error) {
    console.error('加载项目列表失败:', error)
  }
}

// 组件挂载时自动加载项目
onMounted(() => {
  loadProjects()
})

// 删除项目
const handleDelete = async () => {
  if (!isAdmin()) {
    ElMessage.warning('只有系统管理员才能执行此操作')
    return
  }
  if (!selectedProject.value) {
    ElMessage.warning('请先选择一个项目')
    return
  }
  
  try {
    await ElMessageBox.confirm(
      `确定要删除项目"${selectedProject.value.name}"吗？此操作将同时删除项目的所有数据，不可恢复！`,
      '危险操作确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'error' }
    )
    
    const token = localStorage.getItem('token')
    const code = selectedProject.value.code
    
    const response = await fetch(`http://localhost:5000/api/projects/${code}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
    
    const data = await response.json()
    if (data.success) {
      // 从列表中移除
      const index = projects.value.findIndex(p => p.id === selectedProject.value.id)
      if (index !== -1) {
        projects.value.splice(index, 1)
      }
      selectedProject.value = projects.value[0] || null
      ElMessage.success(data.message || '删除成功')
    } else {
      ElMessage.error(data.message || '删除失败')
    }
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}
</script>

<template>
  <div class="project-page">
    
    <!-- 项目视图模式 -->
    <template v-if="currentProjectMode">
      <div class="project-view-container">
        <!-- 顶部导航栏 -->
        <div class="project-navbar">
          <div class="navbar-left">
            <el-button text @click="exitProjectView">
              <el-icon><ArrowLeft /></el-icon>
              返回项目列表
            </el-button>
            <el-divider direction="vertical" />
            <span class="project-title">{{ currentProjectMode.name }}</span>
            <el-tag type="success" size="small">项目视图中</el-tag>
          </div>
          <div class="navbar-right">
            <span class="module-count">共 {{ getProjectModules(currentProjectMode).length }} 个模块</span>
            <el-button type="primary" plain @click="handleEdit" v-if="isAdmin()">
              <el-icon><Setting /></el-icon>
              配置项目
            </el-button>
          </div>
        </div>
        
        <!-- 项目模块快捷入口 -->
        <div class="project-modules-view">
          <div class="modules-header">
            <h2>项目模块</h2>
            <p>点击下方模块卡片直接进入管理页面</p>
          </div>
          
          <div class="module-cards-grid">
            <div 
              v-for="mod in getProjectModules(currentProjectMode)" 
              :key="mod.key"
              class="module-card"
              @click="navigateToModule(mod)"
            >
              <div class="card-icon">
                <el-icon><component :is="mod.icon" /></el-icon>
              </div>
              <div class="card-info">
                <span class="card-name">{{ mod.name }}</span>
                <span class="card-desc">点击进入管理</span>
              </div>
              <div class="card-arrow">
                <el-icon><ArrowRight /></el-icon>
              </div>
            </div>
          </div>
          
          <!-- 项目统计概览 -->
          <div class="project-stats">
            <el-card shadow="hover" class="stat-card">
              <div class="stat-content">
                <el-icon class="stat-icon"><Odometer /></el-icon>
                <div class="stat-info">
                  <span class="stat-value">{{ getProjectModules(currentProjectMode).length }}</span>
                  <span class="stat-label">启用模块</span>
                </div>
              </div>
            </el-card>
            <el-card shadow="hover" class="stat-card">
              <div class="stat-content">
                <el-icon class="stat-icon"><Tools /></el-icon>
                <div class="stat-info">
                  <span class="stat-value">{{ currentProjectMode.modules.length }}</span>
                  <span class="stat-label">配置模块</span>
                </div>
              </div>
            </el-card>
            <el-card shadow="hover" class="stat-card">
              <div class="stat-content">
                <el-icon class="stat-icon"><Check /></el-icon>
                <div class="stat-info">
                  <span class="stat-value">100%</span>
                  <span class="stat-label">运行状态</span>
                </div>
              </div>
            </el-card>
          </div>
        </div>
      </div>
    </template>

    <!-- 项目列表模式 -->
    <template v-else>
      <!-- 左侧项目列表 -->
      <div class="project-list-panel">
        <div class="panel-header">
          <span class="panel-title">项目列表</span>
          <el-button type="primary" size="small" @click="handleAdd" :disabled="!isAdmin()">
            <el-icon><Plus /></el-icon> 新增
          </el-button>
        </div>
        
        <div class="project-list">
          <div 
            v-for="project in projects" 
            :key="project.id"
            class="project-item"
            :class="{ 'is-active': selectedProject?.id === project.id }"
            @click="selectProject(project)"
          >
            <div class="project-info">
              <div class="project-name">
                <span>{{ project.name }}</span>
                <el-switch
                  v-model="project.status"
                  active-value="Active"
                  inactive-value="Inactive"
                  size="small"
                  :disabled="!isAdmin()"
                  @change="handleToggleStatus(project)"
                  @click.stop
                />
              </div>
              <div class="project-code">{{ project.code }}</div>
              <div class="project-modules">
                <span class="module-count">{{ project.modules.length }} 个模块</span>
              </div>
            </div>
            <el-button 
              type="primary" 
              size="small" 
              plain
              :disabled="project.status !== 'Active'"
              @click.stop="enterProjectView(project)"
            >
              <el-icon><View /></el-icon>
              进入
            </el-button>
          </div>
        </div>
        
        <div class="panel-footer">
          <el-tag type="warning" v-if="!isAdmin()">普通用户</el-tag>
          <el-tag type="danger" v-else>系统管理员</el-tag>
        </div>
      </div>

      <!-- 右侧模块配置 -->
      <div class="module-config-panel">
        <template v-if="selectedProject">
          <div class="panel-header">
            <div class="header-left">
              <span class="panel-title">模块配置</span>
              <span class="project-name-tag">{{ selectedProject.name }}</span>
            </div>
            <div class="header-actions">
              <el-button size="small" @click="handleEdit" :disabled="!isAdmin()">
                <el-icon><Setting /></el-icon> 编辑项目
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete" :disabled="!isAdmin()">
                <el-icon><Delete /></el-icon> 删除
              </el-button>
            </div>
          </div>
          
          <div class="project-detail">
            <div class="detail-item">
              <span class="label">项目代码：</span>
              <span class="value code">{{ selectedProject.code }}</span>
            </div>
            <div class="detail-item">
              <span class="label">项目描述：</span>
              <span class="value">{{ selectedProject.description }}</span>
            </div>
            <div class="detail-item">
              <span class="label">启用状态：</span>
              <el-tag :type="getStatusType(selectedProject.status)" size="small">{{ getStatusText(selectedProject.status) }}</el-tag>
            </div>
          </div>
          
          <el-divider content-position="left">
            <span class="module-title">可用模块（点击添加/移除）</span>
          </el-divider>
          
          <div class="module-grid">
            <div 
              v-for="mod in allModules" 
              :key="mod.key" 
              class="module-item"
              :class="{ 'is-selected': isModuleSelected(mod.name) }"
              @click="toggleModule(mod.name)"
            >
              <el-icon class="module-icon"><component :is="mod.icon" /></el-icon>
              <span class="module-name">{{ mod.name }}</span>
              <el-icon v-if="isModuleSelected(mod.name)" class="check-icon"><Check /></el-icon>
            </div>
          </div>
          
          <div class="module-summary">
            <span>已配置 {{ selectedProject.modules.length }} / {{ allModules.length }} 个模块</span>
            <span class="tip" v-if="isAdmin()">（管理员可修改配置）</span>
            <span class="tip" v-else>（普通用户无权修改）</span>
          </div>
        </template>
        
        <template v-else>
          <div class="empty-state">
            <el-icon class="empty-icon"><Setting /></el-icon>
            <p>请从左侧选择一个项目</p>
            <p>然后在该页面配置该项目启用的模块</p>
          </div>
        </template>
      </div>
    </template>

    <!-- 新增/编辑项目对话框 -->
    <el-dialog 
      v-model="dialogVisible" 
      :title="dialogTitle" 
      width="550px" 
      :close-on-click-modal="false"
    >
      <el-form 
        ref="formRef" 
        :model="form" 
        :rules="rules" 
        label-width="100px"
      >
        <el-form-item label="项目名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入项目名称" />
        </el-form-item>
        <el-form-item label="项目代码" prop="code">
          <el-input v-model="form.code" placeholder="如：WO-PMS" />
        </el-form-item>
        <el-form-item label="项目描述">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入项目描述" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" style="width: 100%">
            <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.project-page {
  width: 100%;
  height: calc(100vh - 140px);
}

/* 项目视图模式 */
.project-view-container {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.project-navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background: #fff;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.navbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.project-title {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.module-count {
  color: #606266;
  font-size: 14px;
}

.project-modules-view {
  flex: 1;
  background: #fff;
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  overflow-y: auto;
}

.modules-header {
  margin-bottom: 24px;
}

.modules-header h2 {
  margin: 0 0 8px 0;
  font-size: 20px;
  color: #303133;
}

.modules-header p {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

.module-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.module-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  background: #f5f7fa;
  border: 2px solid transparent;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s;
}

.module-card:hover {
  border-color: #409eff;
  background: #ecf5ff;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(64, 158, 255, 0.2);
}

.card-icon {
  width: 56px;
  height: 56px;
  background: #fff;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  color: #409eff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.card-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.card-name {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.card-desc {
  font-size: 12px;
  color: #909399;
}

.card-arrow {
  font-size: 20px;
  color: #c0c4cc;
  transition: all 0.3s;
}

.module-card:hover .card-arrow {
  color: #409eff;
  transform: translateX(4px);
}

.project-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid #eee;
}

.stat-card :deep(.el-card__body) {
  padding: 20px;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  font-size: 36px;
  color: #409eff;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
}

/* 项目列表模式 */
.project-list-panel {
  width: 320px;
  background: #fff;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.panel-header {
  padding: 16px;
  border-bottom: 1px solid #eee;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.panel-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-actions {
  display: flex;
  gap: 8px;
}

.project-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.project-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  border: 2px solid transparent;
  margin-bottom: 8px;
}

.project-item:hover {
  background: #f5f7fa;
}

.project-item.is-active {
  background: #ecf5ff;
  border-color: #409eff;
}

.project-info {
  flex: 1;
}

.project-name {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 4px;
}

.project-name span {
  flex: 1;
}

.project-code {
  font-size: 12px;
  color: #909399;
  font-family: Monaco, Menlo, monospace;
  margin-bottom: 6px;
}

.project-modules {
  font-size: 12px;
  color: #606266;
}

.module-count {
  background: #f4f4f5;
  padding: 2px 8px;
  border-radius: 3px;
}

.panel-footer {
  padding: 12px 16px;
  border-top: 1px solid #eee;
  text-align: center;
}

/* 右侧模块配置 */
.module-config-panel {
  flex: 1;
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  overflow-y: auto;
}

.project-detail {
  display: flex;
  gap: 24px;
  padding: 16px;
  background: #f5f7fa;
  border-radius: 6px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.detail-item .label {
  color: #909399;
  font-size: 14px;
}

.detail-item .value {
  color: #303133;
  font-size: 14px;
}

.detail-item .code {
  font-family: Monaco, Menlo, monospace;
  color: #409eff;
}

.project-name-tag {
  font-size: 14px;
  color: #606266;
  font-weight: normal;
}

.module-title {
  font-size: 14px;
  font-weight: normal;
  color: #606266;
}

.module-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-bottom: 20px;
}

.module-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px;
  border: 2px solid #dcdfe6;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  user-select: none;
}

.module-item:hover {
  border-color: #409eff;
  background: #f5f7fa;
}

.module-item.is-selected {
  border-color: #409eff;
  background: #ecf5ff;
  color: #409eff;
}

.module-icon {
  font-size: 20px;
}

.module-name {
  flex: 1;
  font-size: 14px;
}

.check-icon {
  font-weight: bold;
  font-size: 16px;
}

.module-summary {
  text-align: center;
  padding: 16px;
  background: #f4f4f5;
  border-radius: 6px;
  color: #606266;
  font-size: 14px;
}

.module-summary .tip {
  margin-left: 8px;
  color: #909399;
  font-size: 12px;
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #909399;
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 20px;
  opacity: 0.5;
}

.empty-state p {
  margin: 8px 0;
  font-size: 16px;
}
</style>
