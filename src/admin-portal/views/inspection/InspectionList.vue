<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { inspectionApi } from '../../api/http'
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
const inspections = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 10, total: 0 })

const dialogVisible = ref(false)
const dialogTitle = ref('新增巡检计划')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

const form = ref({
  planName: '',
  route: '',
  inspector: '',
  scheduledDate: '',
  status: 'Scheduled',
  description: ''
})

const rules: FormRules = {
  planName: [{ required: true, message: '请输入计划名称', trigger: 'blur' }],
  route: [{ required: true, message: '请输入巡检路线', trigger: 'blur' }],
  inspector: [{ required: true, message: '请输入巡检人员', trigger: 'blur' }],
  scheduledDate: [{ required: true, message: '请选择计划日期', trigger: 'change' }]
}

const loadData = async () => {
  loading.value = true
  try {
    const response = await inspectionApi.get('/api/inspections', {
      params: { page: pagination.value.page, pageSize: pagination.value.pageSize }
    })
    if (response.success) {
      inspections.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    inspections.value = getMockData()
    pagination.value.total = 5
  }
  loading.value = false
}

const getMockData = () => [
  { id: 1, planName: '月度巡检计划', route: 'A栋-B栋-C栋', inspector: '张三', scheduledDate: '2026-04-25', status: 'Scheduled' },
  { id: 2, planName: '消防设备周检', route: '地下车库', inspector: '李四', scheduledDate: '2026-04-22', status: 'InProgress' },
  { id: 3, planName: '电梯月度维保', route: '全部电梯', inspector: '王五', scheduledDate: '2026-04-20', status: 'Completed' },
]

const getStatusType = (status: string) => status === 'Completed' ? 'success' : status === 'InProgress' ? 'warning' : 'info'
const getStatusText = (status: string) => status === 'Completed' ? '已完成' : status === 'InProgress' ? '进行中' : '待执行'

const openCreateDialog = () => {
  dialogTitle.value = '新增巡检计划'
  editingId.value = null
  form.value = { planName: '', route: '', inspector: '', scheduledDate: '', status: 'Scheduled', description: '' }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  dialogTitle.value = '编辑巡检计划'
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
      await inspectionApi.put(`/api/inspections/${editingId.value}`, form.value)
      ElMessage.success('巡检计划更新成功')
    } else {
      await inspectionApi.post('/api/inspections', form.value)
      ElMessage.success('巡检计划创建成功')
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
    await ElMessageBox.confirm(`确定要删除巡检计划"${row.planName}"吗？`, '删除确认', { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' })
    await inspectionApi.delete(`/api/inspections/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

const handleStart = async (row: any) => {
  try {
    await inspectionApi.put(`/api/inspections/${row.id}/status`, { status: 'InProgress' })
    ElMessage.success('巡检已开始')
    loadData()
  } catch (error: any) {
    ElMessage.error('操作失败')
  }
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="inspection-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>巡检管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openCreateDialog"><el-icon><Plus /></el-icon> 新增巡检</el-button>
          </div>
        </div>
      </template>
      <el-table :data="inspections" v-loading="loading" stripe>
        <el-table-column prop="planName" label="计划名称" min-width="180" />
        <el-table-column prop="route" label="巡检路线" min-width="150" />
        <el-table-column prop="inspector" label="巡检人员" width="100" />
        <el-table-column prop="scheduledDate" label="计划日期" width="120" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="success" v-if="row.status === 'Scheduled'" @click="handleStart(row)">开始</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="loadData" /></div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="550px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="计划名称" prop="planName"><el-input v-model="form.planName" placeholder="如：月度巡检计划" /></el-form-item>
        <el-form-item label="巡检路线" prop="route"><el-input v-model="form.route" placeholder="如：A栋-B栋-C栋" /></el-form-item>
        <el-form-item label="巡检人员" prop="inspector"><el-input v-model="form.inspector" placeholder="请输入巡检人员姓名" /></el-form-item>
        <el-form-item label="计划日期" prop="scheduledDate"><el-date-picker v-model="form.scheduledDate" type="date" style="width:100%" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入备注信息" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="inspection" 
      module-name="巡检管理" 
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
.inspection-list { width: 100%; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.pagination { margin-top: 20px; display: flex; justify-content: flex-end; }
</style>
