<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { masterApi, accessControlApi } from '../../api/http'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Lock, User, RefreshRight } from '@element-plus/icons-vue'

// Types
interface Role {
  id: number
  name: string
  code: string
  level: number
  description: string
}

interface Permission {
  moduleKey: string
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
}

interface UserItem {
  id: number
  username: string
  fullName: string
  email: string
  phone: string
  role: string
  status: string
  createdAt: string
}

// All module keys from project store
const moduleKeys = [
  'ticket', 'device', 'material', 'contract', 'finance', 'inspection',
  'key', 'visitor', 'notification', 'statistics', 'resident', 'parking',
  'payment', 'personnel', 'dispatch', 'timeout', 'ticketType', 'projectTracking',
  'accessControl', 'announcement', 'cleaning', 'community', 'delivery',
  'express', 'renovation', 'projectConfig', 'fieldDefinition', 'department',
  'region', 'area', 'building', 'room', 'jobType', 'supplier', 'deviceType'
]

const moduleLabels: Record<string, string> = {
  ticket: '工单管理', device: '设备管理', material: '物料管理', contract: '合同管理',
  finance: '财务管理', inspection: '巡检管理', key: '钥匙管理', visitor: '访客管理',
  notification: '通知管理', statistics: '统计分析', resident: '住户管理', parking: '车位管理',
  payment: '缴费管理', personnel: '人员管理', dispatch: '派单规则', timeout: '超时设置',
  ticketType: '工单类型', projectTracking: '项目跟踪', accessControl: '权限控制',
  announcement: '公告管理', cleaning: '清洁管理', community: '社区管理', delivery: '配送管理',
  express: '快递管理', renovation: '装修管理', projectConfig: '项目配置',
  fieldDefinition: '字段管理', department: '部门管理', region: '大区省市区', area: '区域管理',
  building: '楼栋管理', room: '房号管理', jobType: '工种管理', supplier: '供应商管理',
  deviceType: '设备类型'
}

// State
const loading = ref(false)
const roles = ref<Role[]>([])
const selectedRole = ref<Role | null>(null)
const permissions = ref<Permission[]>([])
const permissionChanged = ref(false)

// User tab
const userLoading = ref(false)
const users = ref<UserItem[]>([])
const userPagination = ref({ page: 1, pageSize: 20, total: 0 })
const userSearch = ref('')
const userRoleFilter = ref('')

// Dialogs
const userDialogVisible = ref(false)
const editingUser = ref<UserItem | null>(null)
const newRole = ref('')

// Load roles
const loadRoles = async () => {
  loading.value = true
  try {
    const res = await masterApi.get('/roles')
    if (res.success) {
      roles.value = res.data
    }
  } catch (e) {
    console.error('加载角色失败', e)
  }
  loading.value = false
}

// Load permissions for selected role
const loadPermissions = async (roleCode: string) => {
  try {
    const res = await masterApi.get(`/roles/${roleCode}/permissions`)
    if (res.success) {
      permissions.value = res.data
    }
  } catch (e) {
    console.error('加载权限失败', e)
  }
}

// Select role
const handleRoleSelect = (role: Role) => {
  if (permissionChanged.value) {
    ElMessageBox.confirm('有未保存的权限修改，是否放弃？', '提示', {
      confirmButtonText: '放弃',
      cancelButtonText: '取消',
      type: 'warning'
    }).then(() => {
      selectedRole.value = role
      loadPermissions(role.code)
      permissionChanged.value = false
    }).catch(() => {})
  } else {
    selectedRole.value = role
    loadPermissions(role.code)
  }
}

// Get permission for module
const getPerm = (moduleKey: string): Permission | undefined => {
  return permissions.value.find(p => p.moduleKey === moduleKey)
}

// Check if module has any permission
const hasModulePerm = (moduleKey: string): boolean => {
  const perm = getPerm(moduleKey)
  return perm ? perm.canView || perm.canCreate || perm.canEdit || perm.canDelete : false
}

// Toggle permission
const togglePerm = (moduleKey: string, field: 'canView' | 'canCreate' | 'canEdit' | 'canDelete') => {
  const idx = permissions.value.findIndex(p => p.moduleKey === moduleKey)
  if (idx >= 0) {
    (permissions.value[idx] as any)[field] = !(permissions.value[idx] as any)[field]
  } else {
    const perm: Permission = {
      moduleKey,
      canView: field === 'canView',
      canCreate: field === 'canCreate',
      canEdit: field === 'canEdit',
      canDelete: field === 'canDelete'
    }
    permissions.value.push(perm)
  }
  permissionChanged.value = true
}

// Save permissions
const savePermissions = async () => {
  if (!selectedRole.value) return
  try {
    const res = await masterApi.put(`/roles/${selectedRole.value.code}/permissions`, {
      permissions: permissions.value.map(p => ({
        ModuleKey: p.moduleKey,
        CanView: p.canView,
        CanCreate: p.canCreate,
        CanEdit: p.canEdit,
        CanDelete: p.canDelete
      }))
    })
    if (res.success) {
      ElMessage.success('权限保存成功')
      permissionChanged.value = false
    }
  } catch (e) {
    ElMessage.error('权限保存失败')
  }
}

// Cancel changes
const cancelChanges = () => {
  if (selectedRole.value) {
    loadPermissions(selectedRole.value.code)
    permissionChanged.value = false
  }
}

// Load users
const loadUsers = async () => {
  userLoading.value = true
  try {
    const res = await masterApi.get('/users', {
      params: {
        page: userPagination.value.page,
        pageSize: userPagination.value.pageSize,
        keyword: userSearch.value,
        role: userRoleFilter.value
      }
    })
    if (res.success) {
      users.value = res.data
      userPagination.value.total = res.pagination.totalCount
    }
  } catch (e) {
    console.error('加载用户失败', e)
  }
  userLoading.value = false
}

// Open assign role dialog
const openAssignRole = (user: UserItem) => {
  editingUser.value = user
  newRole.value = user.role
  userDialogVisible.value = true
}

// Confirm role assignment
const confirmAssignRole = async () => {
  if (!editingUser.value) return
  try {
    const res = await masterApi.put(`/users/${editingUser.value.id}/role`, {
      role: newRole.value
    })
    if (res.success) {
      ElMessage.success('角色分配成功')
      userDialogVisible.value = false
      loadUsers()
    }
  } catch (e) {
    ElMessage.error('角色分配失败')
  }
}

// Watch for permission changes
watch(permissions, () => {}, { deep: true })

onMounted(() => {
  loadRoles()
  loadUsers()
})
</script>

<template>
  <div class="access-control">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>权限控制</span>
        </div>
      </template>

      <el-tabs>
        <!-- 角色权限配置 Tab -->
        <el-tab-pane label="角色权限" name="permissions">
          <div class="role-layout">
            <!-- 左侧角色列表 -->
            <div class="role-list-panel">
              <div class="panel-title">角色列表</div>
              <el-scrollbar height="calc(100vh - 300px)">
                <div
                  v-for="role in roles"
                  :key="role.code"
                  class="role-item"
                  :class="{ active: selectedRole?.code === role.code }"
                  @click="handleRoleSelect(role)"
                >
                  <el-icon><Lock /></el-icon>
                  <div class="role-info">
                    <div class="role-name">{{ role.name }}</div>
                    <div class="role-code">{{ role.code }}</div>
                  </div>
                </div>
              </el-scrollbar>
            </div>

            <!-- 右侧权限配置 -->
            <div class="permission-panel">
              <template v-if="selectedRole">
                <div class="panel-header">
                  <span class="panel-title">
                    {{ selectedRole.name }} - 菜单权限配置
                  </span>
                  <div class="panel-actions">
                    <el-button
                      v-if="permissionChanged"
                      type="warning"
                      size="small"
                      @click="cancelChanges"
                    >
                      取消修改
                    </el-button>
                    <el-button
                      type="primary"
                      size="small"
                      :disabled="!permissionChanged"
                      @click="savePermissions"
                    >
                      保存权限
                    </el-button>
                  </div>
                </div>

                <el-scrollbar height="calc(100vh - 360px)">
                  <el-table :data="moduleKeys" border size="small" class="perm-table">
                    <el-table-column prop="moduleKey" label="模块" width="140">
                      <template #default="{ row }">
                        <span>{{ moduleLabels[row] || row }}</span>
                      </template>
                    </el-table-column>
                    <el-table-column label="查看" width="80" align="center">
                      <template #default="{ row }">
                        <el-checkbox
                          :model-value="getPerm(row)?.canView ?? false"
                          @change="togglePerm(row, 'canView')"
                        />
                      </template>
                    </el-table-column>
                    <el-table-column label="创建" width="80" align="center">
                      <template #default="{ row }">
                        <el-checkbox
                          :model-value="getPerm(row)?.canCreate ?? false"
                          @change="togglePerm(row, 'canCreate')"
                          :disabled="!getPerm(row)?.canView && !(getPerm(row)?.canCreate)"
                        />
                      </template>
                    </el-table-column>
                    <el-table-column label="编辑" width="80" align="center">
                      <template #default="{ row }">
                        <el-checkbox
                          :model-value="getPerm(row)?.canEdit ?? false"
                          @change="togglePerm(row, 'canEdit')"
                          :disabled="!getPerm(row)?.canView"
                        />
                      </template>
                    </el-table-column>
                    <el-table-column label="删除" width="80" align="center">
                      <template #default="{ row }">
                        <el-checkbox
                          :model-value="getPerm(row)?.canDelete ?? false"
                          @change="togglePerm(row, 'canDelete')"
                          :disabled="!getPerm(row)?.canView"
                        />
                      </template>
                    </el-table-column>
                  </el-table>
                </el-scrollbar>
              </template>

              <el-empty v-else description="请从左侧选择一个角色" />
            </div>
          </div>
        </el-tab-pane>

        <!-- 用户角色分配 Tab -->
        <el-tab-pane label="用户角色分配" name="users">
          <div class="user-panel">
            <div class="search-bar">
              <el-input
                v-model="userSearch"
                placeholder="搜索用户名/姓名"
                style="width: 240px"
                @keyup.enter="loadUsers"
              >
                <template #append>
                  <el-button @click="loadUsers" icon="Search" />
                </template>
              </el-input>
              <el-select
                v-model="userRoleFilter"
                placeholder="按角色筛选"
                clearable
                style="width: 150px; margin-left: 10px"
                @change="loadUsers"
              >
                <el-option
                  v-for="role in roles"
                  :key="role.code"
                  :label="role.name"
                  :value="role.code"
                />
              </el-select>
              <el-button @click="loadUsers" style="margin-left: 8px" icon="RefreshRight">
                刷新
              </el-button>
            </div>

            <el-table :data="users" v-loading="userLoading" stripe class="user-table">
              <el-table-column prop="username" label="用户名" width="150" />
              <el-table-column prop="fullName" label="姓名" width="120" />
              <el-table-column prop="email" label="邮箱" min-width="180" />
              <el-table-column prop="phone" label="电话" width="130" />
              <el-table-column label="当前角色" width="140">
                <template #default="{ row }">
                  <el-tag size="small">
                    {{ roles.find(r => r.code === row.role)?.name || row.role }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="status" label="状态" width="80">
                <template #default="{ row }">
                  <el-tag :type="row.status === 'Active' ? 'success' : 'info'" size="small">
                    {{ row.status === 'Active' ? '启用' : '禁用' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="100" fixed="right">
                <template #default="{ row }">
                  <el-button link type="primary" @click="openAssignRole(row)">
                    分配角色
                  </el-button>
                </template>
              </el-table-column>
            </el-table>

            <el-pagination
              v-model:current-page="userPagination.page"
              :page-size="userPagination.pageSize"
              :total="userPagination.total"
              layout="total, prev, pager, next"
              @current-change="loadUsers"
              style="margin-top: 15px; justify-content: flex-end"
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 分配角色对话框 -->
    <el-dialog v-model="userDialogVisible" title="分配角色" width="400px">
      <div v-if="editingUser">
        <p style="margin-bottom: 20px">
          为用户 <strong>{{ editingUser.fullName }}</strong> ({{ editingUser.username }}) 分配角色：
        </p>
        <el-select v-model="newRole" style="width: 100%">
          <el-option
            v-for="role in roles"
            :key="role.code"
            :label="role.name"
            :value="role.code"
          />
        </el-select>
      </div>
      <template #footer>
        <el-button @click="userDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmAssignRole">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.access-control {
  width: 100%;
}

.card-header {
  font-size: 16px;
  font-weight: 600;
}

.role-layout {
  display: flex;
  gap: 20px;
  min-height: 500px;
}

.role-list-panel {
  width: 240px;
  flex-shrink: 0;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  padding: 12px;
}

.panel-title {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 12px;
  color: #303133;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.panel-actions {
  display: flex;
  gap: 8px;
}

.role-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  margin-bottom: 4px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.role-item:hover {
  background: #f5f7fa;
}

.role-item.active {
  background: #ecf5ff;
  color: #409eff;
}

.role-info {
  flex: 1;
}

.role-name {
  font-size: 14px;
  font-weight: 500;
}

.role-code {
  font-size: 12px;
  color: #909399;
  margin-top: 2px;
}

.permission-panel {
  flex: 1;
  padding: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
}

.perm-table {
  width: 100%;
}

.user-panel {
  padding: 12px;
}

.search-bar {
  display: flex;
  margin-bottom: 16px;
}

.user-table {
  width: 100%;
}
</style>
