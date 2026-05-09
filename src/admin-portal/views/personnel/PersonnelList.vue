<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Phone, User, Link, ArrowUp, ArrowDown, Close } from '@element-plus/icons-vue'
import {
  getAllStaff,
  getActiveStaff,
  addStaff,
  updateStaff,
  deleteStaff,
  toggleStaffStatus,
  addLeader,
  removeLeader,
  reorderLeaders,
  type StaffMember
} from '@/stores/staff'

// 列表数据
const staffList = ref<StaffMember[]>(getAllStaff())

// 对话框
const dialogVisible = ref(false)
const leaderDialogVisible = ref(false)
const dialogTitle = ref('新增人员')
const editingId = ref<number | null>(null)

// 表单数据
const form = ref({
  name: '',
  phone: '',
  specialty: [] as string[],
  role: 'primary' as 'primary' | 'backup' | 'supervisor'
})

// 当前编辑的人员
const currentStaff = ref<StaffMember | null>(null)
const selectedLeaderId = ref<number | null>(null)

// 角色选项
const roleOptions = [
  { value: 'primary', label: '主要负责人' },
  { value: 'backup', label: '备份负责人' },
  { value: 'supervisor', label: '上一级负责人' }
]

// 专长选项
const specialtyOptions = [
  '电梯', '水电', '消防', '设备维修', '日常维修', '门禁', '监控', '清洁', '绿化', '其他'
]

// 角色标签
const getRoleLabel = (role: string) => roleOptions.find(r => r.value === role)?.label || role
const getRoleType = (role: string) => ({ primary: 'success', backup: 'warning', supervisor: 'danger' }[role] || 'info')

// 统计
const stats = computed(() => {
  const all = staffList.value
  return {
    total: all.length,
    active: all.filter(s => s.status === 'Active').length,
    primary: all.filter(s => s.role === 'primary').length,
    backup: all.filter(s => s.role === 'backup').length,
    supervisor: all.filter(s => s.role === 'supervisor').length
  }
})

// 新增
const handleAdd = () => {
  dialogTitle.value = '新增人员'
  editingId.value = null
  form.value = { name: '', phone: '', specialty: [], role: 'primary' }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: StaffMember) => {
  dialogTitle.value = '编辑人员'
  editingId.value = row.id
  form.value = {
    name: row.name,
    phone: row.phone,
    specialty: [...row.specialty],
    role: row.role
  }
  dialogVisible.value = true
}

// 提交
const handleSubmit = () => {
  if (!form.value.name.trim()) {
    ElMessage.warning('请输入姓名')
    return
  }
  if (!form.value.phone.trim()) {
    ElMessage.warning('请输入电话')
    return
  }
  
  if (editingId.value) {
    updateStaff(editingId.value, { ...form.value })
    ElMessage.success('更新成功')
  } else {
    addStaff({ ...form.value, status: 'Active', leaders: [] })
    ElMessage.success('添加成功')
  }
  
  staffList.value = getAllStaff()
  dialogVisible.value = false
}

// 删除
const handleDelete = async (row: StaffMember) => {
  try {
    await ElMessageBox.confirm(`确定删除 "${row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    deleteStaff(row.id)
    staffList.value = getAllStaff()
    ElMessage.success('删除成功')
  } catch {
    // 取消
  }
}

// 切换状态
const handleToggle = (row: StaffMember) => {
  toggleStaffStatus(row.id)
  staffList.value = getAllStaff()
  ElMessage.success(`已将 "${row.name}" 设为 ${row.status === 'Active' ? '在职' : '离职'}`)
}

// 打开领导配置
const openLeaderDialog = (row: StaffMember) => {
  currentStaff.value = row
  selectedLeaderId.value = null
  staffList.value = getAllStaff()
  leaderDialogVisible.value = true
}

// 可添加的领导
const getAvailableLeaders = computed(() => {
  if (!currentStaff.value) return []
  const excludeIds = [...currentStaff.value.leaders.map(l => l.staffId), currentStaff.value.id]
  return getActiveStaff().filter(s => !excludeIds.includes(s.id))
})

// 添加领导
const handleAddLeader = () => {
  if (!currentStaff.value || !selectedLeaderId.value) return
  const result = addLeader(currentStaff.value.id, selectedLeaderId.value)
  if (result) {
    staffList.value = getAllStaff()
    currentStaff.value = staffList.value.find(s => s.id === currentStaff.value!.id) || null
    selectedLeaderId.value = null
    ElMessage.success('已添加')
  } else {
    ElMessage.warning('该领导已存在')
  }
}

// 移除领导
const handleRemoveLeader = (leaderId: number) => {
  if (!currentStaff.value) return
  removeLeader(currentStaff.value.id, leaderId)
  staffList.value = getAllStaff()
  currentStaff.value = staffList.value.find(s => s.id === currentStaff.value!.id) || null
  ElMessage.success('已移除')
}

// 上移
const handleMoveUp = (index: number) => {
  if (!currentStaff.value || index <= 0) return
  reorderLeaders(currentStaff.value.id, index, index - 1)
  staffList.value = getAllStaff()
  currentStaff.value = staffList.value.find(s => s.id === currentStaff.value!.id) || null
}

// 下移
const handleMoveDown = (index: number) => {
  if (!currentStaff.value || index >= currentStaff.value.leaders.length - 1) return
  reorderLeaders(currentStaff.value.id, index, index + 1)
  staffList.value = getAllStaff()
  currentStaff.value = staffList.value.find(s => s.id === currentStaff.value!.id) || null
}
</script>

<template>
  <div class="personnel-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>人员管理</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon> 新增人员
          </el-button>
        </div>
      </template>

      <!-- 统计 -->
      <div class="stats-row">
        <el-statistic title="总人数" :value="stats.total" />
        <el-statistic title="在职" :value="stats.active" />
        <el-statistic title="主责" :value="stats.primary" />
        <el-statistic title="备份" :value="stats.backup" />
        <el-statistic title="上级" :value="stats.supervisor" />
      </div>

      <!-- 列表 -->
      <el-table :data="staffList" stripe>
        <el-table-column prop="name" label="姓名" width="120">
          <template #default="{ row }">
            <span class="name-cell"><el-icon><User /></el-icon> {{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="电话" width="130">
          <template #default="{ row }">
            <span><el-icon><Phone /></el-icon> {{ row.phone }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="specialty" label="专长" min-width="180">
          <template #default="{ row }">
            <el-tag v-for="s in row.specialty" :key="s" size="small" style="margin-right: 4px;">{{ s }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="role" label="角色" width="110" align="center">
          <template #default="{ row }">
            <el-tag :type="getRoleType(row.role)" size="small">{{ getRoleLabel(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="leaders" label="领导链" min-width="200">
          <template #default="{ row }">
            <template v-if="row.leaders.length > 0">
              <el-tag
                v-for="(leader, idx) in row.leaders"
                :key="leader.id"
                size="small"
                :type="idx === 0 ? 'warning' : 'info'"
                style="margin-right: 4px;"
              >
                {{ leader.staffName }}
              </el-tag>
            </template>
            <span v-else style="color: #c0c4cc;">无</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Active' ? 'success' : 'info'" size="small">
              {{ row.status === 'Active' ? '在职' : '离职' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="success" size="small" @click="openLeaderDialog(row)">
              <el-icon><Link /></el-icon> 领导
            </el-button>
            <el-button link type="warning" size="small" @click="handleToggle(row)">
              {{ row.status === 'Active' ? '离职' : '在职' }}
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
      <el-form label-width="80px">
        <el-form-item label="姓名">
          <el-input v-model="form.name" placeholder="请输入姓名" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="form.phone" placeholder="请输入联系电话" />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="form.role" style="width: 100%">
            <el-option v-for="opt in roleOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="专长">
          <el-select v-model="form.specialty" multiple style="width: 100%">
            <el-option v-for="s in specialtyOptions" :key="s" :label="s" :value="s" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 领导链配置对话框 -->
    <el-dialog v-model="leaderDialogVisible" :title="`领导链 - ${currentStaff?.name}`" width="550px">
      <div v-if="currentStaff">
        <h4 style="margin-bottom: 15px;">当前领导链</h4>
        
        <div v-if="currentStaff.leaders.length === 0" class="no-leader">
          暂无领导配置
        </div>
        
        <div v-else class="leader-list">
          <div v-for="(leader, idx) in currentStaff.leaders" :key="leader.id" class="leader-item">
            <span class="level">第{{ idx + 1 }}级</span>
            <el-tag type="warning">{{ leader.staffName }}</el-tag>
            <div class="actions">
              <el-button link :disabled="idx === 0" @click="handleMoveUp(idx)">
                <el-icon><ArrowUp /></el-icon>上移
              </el-button>
              <el-button link :disabled="idx === currentStaff.leaders.length - 1" @click="handleMoveDown(idx)">
                <el-icon><ArrowDown /></el-icon>下移
              </el-button>
              <el-button link type="danger" @click="handleRemoveLeader(leader.id)">
                <el-icon><Close /></el-icon>移除
              </el-button>
            </div>
          </div>
        </div>

        <el-divider />

        <h4 style="margin-bottom: 15px;">添加领导</h4>
        <div class="add-leader">
          <el-select v-model="selectedLeaderId" placeholder="选择领导" style="width: 300px;" filterable clearable>
            <el-option
              v-for="s in getAvailableLeaders"
              :key="s.id"
              :label="`${s.name} (${getRoleLabel(s.role)})`"
              :value="s.id"
            />
          </el-select>
          <el-button type="primary" :disabled="!selectedLeaderId" @click="handleAddLeader">添加</el-button>
        </div>
        <div v-if="getAvailableLeaders.length === 0" class="no-more">
          没有更多可添加的领导
        </div>
      </div>
      <template #footer>
        <el-button type="primary" @click="leaderDialogVisible = false">完成</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.personnel-page {
  width: 100%;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.stats-row {
  display: flex;
  gap: 40px;
  padding: 20px 0;
  border-bottom: 1px solid #ebeef5;
  margin-bottom: 20px;
}
.name-cell {
  display: flex;
  align-items: center;
  gap: 5px;
}
.leader-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.leader-item {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 10px 15px;
  background: #f5f7fa;
  border-radius: 8px;
}
.level {
  color: #909399;
  font-size: 13px;
  min-width: 45px;
}
.actions {
  margin-left: auto;
  display: flex;
  gap: 5px;
}
.no-leader {
  color: #909399;
  padding: 30px;
  text-align: center;
  background: #f5f7fa;
  border-radius: 8px;
}
.add-leader {
  display: flex;
  gap: 10px;
}
.no-more {
  margin-top: 10px;
  color: #909399;
  font-size: 13px;
}
</style>
