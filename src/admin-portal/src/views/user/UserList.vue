<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { authApi } from '../../api/http'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Setting } from '@element-plus/icons-vue'
import { getActiveFields } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getUserFields = () => getActiveFields('resident')

// 打开字段配置（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 刷新字段
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

const loading = ref(false)
const users = ref<any[]>([])
const pagination = ref({
  page: 1,
  pageSize: 10,
  total: 0
})
const searchQuery = ref('')

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('新增用户')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

const form = ref({
  username: '',
  password: '',
  name: '',
  email: '',
  phone: '',
  role: 'User'
})

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, message: '用户名至少3个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少6个字符', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '请输入姓名', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  role: [
    { required: true, message: '请选择角色', trigger: 'change' }
  ]
}

const roleOptions = [
  { value: 'Administrator', label: '管理员' },
  { value: 'Technician', label: '技术员' },
  { value: 'User', label: '普通用户' }
]

const loadData = async () => {
  loading.value = true
  try {
    const response = await authApi.get('/api/users', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.pageSize,
        keyword: searchQuery.value
      }
    })
    if (response.success) {
      users.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    console.error('加载数据失败:', error)
  }
  loading.value = false
}

const handleSearch = () => {
  pagination.value.page = 1
  loadData()
}

const handlePageChange = (page: number) => {
  pagination.value.page = page
  loadData()
}

const getRoleType = (role: string) => {
  const map: Record<string, string> = { 'Administrator': 'danger', 'Technician': 'warning', 'User': 'primary' }
  return map[role] || 'info'
}

const getRoleText = (role: string) => {
  const map: Record<string, string> = { 'Administrator': '管理员', 'Technician': '技术员', 'User': '用户' }
  return map[role] || role
}

const openCreateDialog = () => {
  dialogTitle.value = '新增用户'
  editingId.value = null
  form.value = {
    username: '',
    password: '',
    name: '',
    email: '',
    phone: '',
    role: 'User'
  }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  dialogTitle.value = '编辑用户'
  editingId.value = row.id
  form.value = {
    username: row.username || row.username,
    password: '',
    name: row.name || row.name,
    email: row.email || row.email,
    phone: row.phone || row.phone || '',
    role: row.role || row.role
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    submitting.value = true
    
    if (editingId.value) {
      // 更新（不更新密码）
      const updateData = { ...form.value }
      if (!updateData.password) {
        delete updateData.password
      }
      await authApi.put(`/api/users/${editingId.value}`, updateData)
      ElMessage.success('用户更新成功')
    } else {
      // 创建
      await authApi.post('/api/users/register', form.value)
      ElMessage.success('用户创建成功')
    }
    
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) {
      ElMessage.error(error.message || '操作失败')
    }
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除用户"${row.username}"吗？`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    await authApi.delete(`/api/users/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
    }
  }
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="user-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>用户管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openCreateDialog">
              <el-icon><Plus /></el-icon> 新增用户
            </el-button>
          </div>
        </div>
      </template>
      
      <!-- 搜索 -->
      <div class="search-bar">
        <el-input 
          v-model="searchQuery"
          placeholder="搜索用户名/姓名/邮箱"
          style="width: 300px;"
          @keyup.enter="handleSearch"
        >
          <template #append>
            <el-button @click="handleSearch" icon="Search" />
          </template>
        </el-input>
        <el-button @click="loadData" style="margin-left: 8px;">刷新</el-button>
      </div>
      
      <el-table :data="users" v-loading="loading" stripe>
        <el-table-column prop="username" label="用户名" width="150" />
        <el-table-column prop="name" label="姓名" width="120" />
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column prop="phone" label="电话" width="130" />
        <el-table-column prop="role" label="角色" width="100">
          <template #default="{ row }">
            <el-tag :type="getRoleType(row.role)" size="small">
              {{ getRoleText(row.role) }}
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
        <el-table-column prop="createdAt" label="创建时间" width="120">
          <template #default="{ row }">
            {{ row.createdAt ? new Date(row.createdAt).toLocaleDateString('zh-CN') : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      
      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="pagination.page"
          :page-size="pagination.pageSize"
          :total="pagination.total"
          layout="total, prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>
    
    <!-- 创建/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="80px"
      >
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" :disabled="!!editingId" />
        </el-form-item>
        
        <el-form-item label="密码" :prop="editingId ? 'password' : 'password'">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
          <span v-if="editingId" style="color: #999; font-size: 12px;">留空则不修改密码</span>
        </el-form-item>
        
        <el-form-item label="姓名" prop="name">
          <el-input v-model="form.name" placeholder="请输入姓名" />
        </el-form-item>
        
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" placeholder="请输入邮箱" />
        </el-form-item>
        
        <el-form-item label="电话" prop="phone">
          <el-input v-model="form.phone" placeholder="请输入电话号码" />
        </el-form-item>
        
        <el-form-item label="角色" prop="role">
          <el-select v-model="form.role" placeholder="请选择角色" style="width: 100%">
            <el-option
              v-for="opt in roleOptions"
              :key="opt.value"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="resident" 
      module-name="住户管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.user-list {
  width: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.search-bar {
  margin-bottom: 20px;
  display: flex;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
