<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { visitorApi } from '../../api/http'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Setting } from '@element-plus/icons-vue'
import { getActiveFields } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

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
const visitors = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 10, total: 0 })

const dialogVisible = ref(false)
const dialogTitle = ref('登记访客')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

const form = ref({
  visitorName: '',
  phone: '',
  visitUnit: '',
  visitPurpose: '',
  visitTime: '',
  status: 'InVisit'
})

const rules: FormRules = {
  visitorName: [{ required: true, message: '请输入访客姓名', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入联系电话', trigger: 'blur' }],
  visitUnit: [{ required: true, message: '请输入访问单元', trigger: 'blur' }]
}

const loadData = async () => {
  loading.value = true
  try {
    const response = await visitorApi.get('/api/visitors', {
      params: { page: pagination.value.page, pageSize: pagination.value.pageSize }
    })
    if (response.success) {
      visitors.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    visitors.value = getMockData()
    pagination.value.total = 5
  }
  loading.value = false
}

const getMockData = () => [
  { id: 1, visitorName: '陈先生', phone: '139****1234', visitUnit: '101室', visitPurpose: '访友', visitTime: '2026-04-21 10:30', checkInTime: '10:35', status: 'InVisit' },
  { id: 2, visitorName: '刘女士', phone: '138****5678', visitUnit: '203室', visitPurpose: '快递', visitTime: '2026-04-21 09:00', checkInTime: '09:02', checkOutTime: '09:15', status: 'Left' },
  { id: 3, visitorName: '周先生', phone: '137****9012', visitUnit: '302室', visitPurpose: '送货', visitTime: '2026-04-20 14:00', checkInTime: '14:05', checkOutTime: '15:30', status: 'Left' },
]

const getStatusType = (status: string) => status === 'InVisit' ? 'warning' : 'success'
const getStatusText = (status: string) => status === 'InVisit' ? '访问中' : '已离开'

const openCreateDialog = () => {
  dialogTitle.value = '登记访客'
  editingId.value = null
  form.value = { visitorName: '', phone: '', visitUnit: '', visitPurpose: '', visitTime: new Date().toLocaleString('zh-CN'), status: 'InVisit' }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    submitting.value = true
    form.value.checkInTime = new Date().toLocaleTimeString('zh-CN')
    await visitorApi.post('/api/visitors', form.value)
    ElMessage.success('访客登记成功')
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) ElMessage.error(error.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

const handleLeave = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定访客"${row.visitorName}"已离开吗？`, '确认离开', { confirmButtonText: '确定', cancelButtonText: '取消', type: 'info' })
    await visitorApi.put(`/api/visitors/${row.id}/leave`, { checkOutTime: new Date().toLocaleTimeString('zh-CN'), status: 'Left' })
    ElMessage.success('访客已离开')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '操作失败')
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除访客记录吗？`, '删除确认', { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' })
    await visitorApi.delete(`/api/visitors/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="visitor-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>访客管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openCreateDialog"><el-icon><Plus /></el-icon> 登记访客</el-button>
          </div>
        </div>
      </template>
      <el-table :data="visitors" v-loading="loading" stripe>
        <el-table-column prop="visitorName" label="访客姓名" width="100" />
        <el-table-column prop="phone" label="联系电话" width="120" />
        <el-table-column prop="visitUnit" label="访问单元" width="100" />
        <el-table-column prop="visitPurpose" label="访问目的" width="100" />
        <el-table-column prop="visitTime" label="预约时间" width="150" />
        <el-table-column prop="checkInTime" label="签到时间" width="100"><template #default="{ row }">{{ row.checkInTime || '-' }}</template></el-table-column>
        <el-table-column prop="checkOutTime" label="离开时间" width="100"><template #default="{ row }">{{ row.checkOutTime || '-' }}</template></el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="success" v-if="row.status === 'InVisit'" @click="handleLeave(row)">离开</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="loadData" /></div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="访客姓名" prop="visitorName"><el-input v-model="form.visitorName" placeholder="请输入访客姓名" /></el-form-item>
        <el-form-item label="联系电话" prop="phone"><el-input v-model="form.phone" placeholder="请输入联系电话" /></el-form-item>
        <el-form-item label="访问单元" prop="visitUnit"><el-input v-model="form.visitUnit" placeholder="如：101室" /></el-form-item>
        <el-form-item label="访问目的"><el-input v-model="form.visitPurpose" placeholder="如：访友、快递、送货" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">登记</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="visitor" 
      module-name="访客管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.header-actions {
  display: flex;
  gap: 10px;
}

<style scoped>
.visitor-list { width: 100%; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.pagination { margin-top: 20px; display: flex; justify-content: flex-end; }
</style>
