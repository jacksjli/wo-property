<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { complaintApi } from '../../api/http'
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
const complaints = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 10, total: 0 })

const dialogVisible = ref(false)
const dialogTitle = ref('新增投诉')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

const form = ref({
  title: '',
  type: '',
  complainant: '',
  phone: '',
  content: '',
  status: 'New'
})

const rules: FormRules = {
  title: [{ required: true, message: '请输入投诉标题', trigger: 'blur' }],
  complainant: [{ required: true, message: '请输入投诉人', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入联系电话', trigger: 'blur' }],
  content: [{ required: true, message: '请输入投诉内容', trigger: 'blur' }]
}

const loadData = async () => {
  loading.value = true
  try {
    const response = await complaintApi.get('/api/complaints', {
      params: { page: pagination.value.page, pageSize: pagination.value.pageSize }
    })
    if (response.success) {
      complaints.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    complaints.value = getMockData()
    pagination.value.total = 5
  }
  loading.value = false
}

const getMockData = () => [
  { id: 1, complaintNumber: 'TS-2026-001', title: '噪音扰民投诉', type: '噪音', complainant: '业主张先生', phone: '138****1234', content: '楼上住户深夜装修', status: 'Processing', createdAt: '2026-04-20' },
  { id: 2, complaintNumber: 'TS-2026-002', title: '停车位被占用', type: '停车', complainant: '业主李女士', phone: '139****5678', content: '私人车位被其他车辆占用', status: 'Resolved', createdAt: '2026-04-19' },
  { id: 3, complaintNumber: 'TS-2026-003', title: '公共设施损坏', type: '设施', complainant: '业主王先生', phone: '137****9012', content: '小区健身器材损坏', status: 'New', createdAt: '2026-04-21' },
]

const getStatusType = (status: string) => status === 'Resolved' ? 'success' : status === 'Processing' ? 'warning' : 'info'
const getStatusText = (status: string) => status === 'Resolved' ? '已处理' : status === 'Processing' ? '处理中' : '待处理'

const openCreateDialog = () => {
  dialogTitle.value = '新增投诉'
  editingId.value = null
  form.value = { title: '', type: '', complainant: '', phone: '', content: '', status: 'New' }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  dialogTitle.value = '处理投诉'
  editingId.value = row.id
  form.value = { ...row }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    submitting.value = true
    if (editingId.value) {
      await complaintApi.put(`/api/complaints/${editingId.value}`, form.value)
      ElMessage.success('投诉处理成功')
    } else {
      await complaintApi.post('/api/complaints', form.value)
      ElMessage.success('投诉提交成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) ElMessage.error(error.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除投诉"${row.title}"吗？`, '删除确认', { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' })
    await complaintApi.delete(`/api/complaints/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="complaint-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>投诉管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openCreateDialog"><el-icon><Plus /></el-icon> 新增投诉</el-button>
          </div>
        </div>
      </template>
      <el-table :data="complaints" v-loading="loading" stripe>
        <el-table-column prop="complaintNumber" label="投诉编号" width="130" />
        <el-table-column prop="title" label="投诉标题" min-width="150" />
        <el-table-column prop="type" label="类型" width="80" />
        <el-table-column prop="complainant" label="投诉人" width="100" />
        <el-table-column prop="phone" label="联系电话" width="120" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="createdAt" label="提交时间" width="110" />
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEdit(row)">处理</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="loadData" /></div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="550px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="投诉标题" prop="title"><el-input v-model="form.title" placeholder="请输入投诉标题" /></el-form-item>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="投诉类型"><el-select v-model="form.type" style="width:100%"><el-option value="噪音" label="噪音" /><el-option value="停车" label="停车" /><el-option value="设施" label="设施" /><el-option value="卫生" label="卫生" /><el-option value="其他" label="其他" /></el-select></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="状态"><el-select v-model="form.status" style="width:100%"><el-option value="New" label="待处理" /><el-option value="Processing" label="处理中" /><el-option value="Resolved" label="已处理" /></el-select></el-form-item></el-col>
        </el-row>
        <el-form-item label="投诉人" prop="complainant"><el-input v-model="form.complainant" placeholder="请输入投诉人姓名" /></el-form-item>
        <el-form-item label="联系电话" prop="phone"><el-input v-model="form.phone" placeholder="请输入联系电话" /></el-form-item>
        <el-form-item label="投诉内容" prop="content"><el-input v-model="form.content" type="textarea" :rows="4" placeholder="请详细描述投诉内容" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="complaint" 
      module-name="投诉管理" 
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
.complaint-list { width: 100%; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.pagination { margin-top: 20px; display: flex; justify-content: flex-end; }
</style>
