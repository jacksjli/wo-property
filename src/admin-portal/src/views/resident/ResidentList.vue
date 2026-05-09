<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, House, Phone, User, Setting, Refresh } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

interface Resident {
  id: number
  name: string
  phone: string
  idCardNumber?: string
  buildingId?: number
  buildingName?: string
  roomId?: number
  roomNumber?: string
  residentType?: string
  checkInDate?: string
  status: string
}

const residentList = ref<Resident[]>([])
const loading = ref(false)

const dialogVisible = ref(false)
const dialogTitle = ref('新增住户')
const editingId = ref<number | null>(null)

const form = ref({
  name: '',
  phone: '',
  idCardNumber: '',
  buildingId: null as number | null,
  roomId: null as number | null,
  residentType: 'owner',
  checkInDate: '',
  remark: ''
})

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getResidentFields = () => getActiveFields('resident')

// 打开字段配置
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

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/residents', { params: { page: 1, pageSize: 200 } })
    if (res.success) {
      residentList.value = res.data || []
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载住户数据失败')
  } finally {
    loading.value = false
  }
}

// 计算单元显示
const getUnitDisplay = (row: Resident) => {
  if (row.buildingName && row.roomNumber) {
    return `${row.buildingName}${row.roomNumber}`
  }
  return row.roomNumber || '-'
}

// 获取住户类型标签
const getResidentTypeLabel = (type?: string) => {
  const labels: Record<string, string> = {
    owner: '业主',
    tenant: '租户',
    family: '家属',
    other: '其他'
  }
  return labels[type || ''] || '业主'
}

const handleAdd = () => {
  dialogTitle.value = '新增住户'
  editingId.value = null
  form.value = {
    name: '',
    phone: '',
    idCardNumber: '',
    buildingId: null,
    roomId: null,
    residentType: 'owner',
    checkInDate: '',
    remark: ''
  }
  dialogVisible.value = true
}

const handleEdit = (row: Resident) => {
  dialogTitle.value = '编辑住户'
  editingId.value = row.id
  form.value = {
    name: row.name,
    phone: row.phone,
    idCardNumber: row.idCardNumber || '',
    buildingId: row.buildingId || null,
    roomId: row.roomId || null,
    residentType: row.residentType || 'owner',
    checkInDate: row.checkInDate ? row.checkInDate.split('T')[0] : '',
    remark: ''
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.value.name.trim()) {
    ElMessage.warning('请输入姓名')
    return
  }
  if (!form.value.phone.trim()) {
    ElMessage.warning('请输入电话')
    return
  }

  try {
    if (editingId.value) {
      await masterApi.put(`/residents/${editingId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/residents', form.value)
      ElMessage.success('添加成功')
    }
    dialogVisible.value = false
    await loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

const handleDelete = async (row: Resident) => {
  try {
    await ElMessageBox.confirm(`确定删除住户 "${row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await masterApi.delete(`/residents/${row.id}`)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

const handleToggle = async (row: Resident) => {
  const newStatus = row.status === 'active' ? 'inactive' : 'active'
  try {
    await masterApi.put(`/residents/${row.id}`, { status: newStatus })
    ElMessage.success(`已将 "${row.name}" 设为${newStatus === 'active' ? '入住' : '搬出'}`)
    await loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

const handleRefresh = () => {
  loadData()
  ElMessage.success('已刷新')
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="resident-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>住户管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增住户
            </el-button>
          </div>
        </div>
      </template>

      <el-table :data="residentList" stripe v-loading="loading">
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
        <el-table-column prop="idCardNumber" label="身份证" width="170">
          <template #default="{ row }">
            {{ row.idCardNumber || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="单元" width="120" align="center">
          <template #default="{ row }">
            <el-tag type="info" size="small">{{ getUnitDisplay(row) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="residentType" label="类型" width="80" align="center">
          <template #default="{ row }">
            <el-tag size="small">{{ getResidentTypeLabel(row.residentType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="checkInDate" label="入住日期" width="110" align="center">
          <template #default="{ row }">
            {{ row.checkInDate ? row.checkInDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'active' ? 'success' : 'info'" size="small">
              {{ row.status === 'active' ? '入住' : '搬出' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="warning" size="small" @click="handleToggle(row)">
              {{ row.status === 'active' ? '搬出' : '入住' }}
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
      <el-form label-width="90px">
        <el-form-item label="姓名" required>
          <el-input v-model="form.name" placeholder="请输入姓名" />
        </el-form-item>
        <el-form-item label="电话" required>
          <el-input v-model="form.phone" placeholder="请输入联系电话" />
        </el-form-item>
        <el-form-item label="身份证号">
          <el-input v-model="form.idCardNumber" placeholder="请输入身份证号" />
        </el-form-item>
        <el-form-item label="住户类型">
          <el-select v-model="form.residentType" style="width: 100%">
            <el-option label="业主" value="owner" />
            <el-option label="租户" value="tenant" />
            <el-option label="家属" value="family" />
            <el-option label="其他" value="other" />
          </el-select>
        </el-form-item>
        <el-form-item label="入住日期">
          <el-date-picker v-model="form.checkInDate" type="date" style="width: 100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
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
.resident-page {
  width: 100%;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.header-actions {
  display: flex;
  gap: 10px;
}
.name-cell {
  display: flex;
  align-items: center;
  gap: 5px;
}
</style>