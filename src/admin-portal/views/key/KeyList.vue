<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { keyApi } from '../../api/http'
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
const keyRecords = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 10, total: 0 })

const dialogVisible = ref(false)
const dialogTitle = ref('借用钥匙')
const formRef = ref<FormInstance>()
const submitting = ref(false)

const form = ref({
  keyId: null as number | null,
  keyNumber: '',
  keyName: '',
  location: '',
  borrower: '',
  borrowTime: '',
  returnTime: '',
  status: 'Borrowed',
  notes: ''
})

const rules: FormRules = {
  keyNumber: [{ required: true, message: '请输入钥匙编号', trigger: 'blur' }],
  keyName: [{ required: true, message: '请输入钥匙名称', trigger: 'blur' }],
  borrower: [{ required: true, message: '请输入借用人', trigger: 'blur' }]
}

const loadData = async () => {
  loading.value = true
  try {
    const response = await keyApi.get('/api/key-records', {
      params: { page: pagination.value.page, pageSize: pagination.value.pageSize }
    })
    if (response.success) {
      keyRecords.value = response.data || []
      pagination.value.total = response.total || 0
    }
  } catch (error) {
    keyRecords.value = getMockData()
    pagination.value.total = 5
  }
  loading.value = false
}

const getMockData = () => [
  { id: 1, keyNumber: 'KEY-001', keyName: 'A栋配电房钥匙', location: 'A栋地下室', borrower: '张师傅', borrowTime: '2026-04-20 09:30', returnTime: null, status: 'Borrowed' },
  { id: 2, keyNumber: 'KEY-002', keyName: '监控室钥匙', location: '监控室门口', borrower: '李保安', borrowTime: '2026-04-20 08:00', returnTime: '2026-04-20 18:00', status: 'Returned' },
  { id: 3, keyNumber: 'KEY-003', keyName: '电梯机房钥匙', location: '电梯机房', borrower: '王师傅', borrowTime: '2026-04-19 14:00', returnTime: null, status: 'Borrowed' },
]

const getStatusType = (status: string) => status === 'Borrowed' ? 'warning' : 'success'
const getStatusText = (status: string) => status === 'Borrowed' ? '借用中' : '已归还'

const openBorrowDialog = () => {
  dialogTitle.value = '借用钥匙'
  form.value = { keyId: null, keyNumber: '', keyName: '', location: '', borrower: '', borrowTime: '', returnTime: '', status: 'Borrowed', notes: '' }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
    submitting.value = true
    form.value.borrowTime = new Date().toLocaleString('zh-CN')
    await keyApi.post('/api/key-records', form.value)
    ElMessage.success('钥匙借用成功')
    dialogVisible.value = false
    loadData()
  } catch (error: any) {
    if (error !== false) ElMessage.error(error.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

const handleReturn = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要归还钥匙"${row.keyName}"吗？`, '归还确认', { confirmButtonText: '确定', cancelButtonText: '取消', type: 'info' })
    await keyApi.put(`/api/key-records/${row.id}/return`, { returnTime: new Date().toLocaleString('zh-CN'), status: 'Returned' })
    ElMessage.success('钥匙已归还')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '归还失败')
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除钥匙借用记录吗？`, '删除确认', { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' })
    await keyApi.delete(`/api/key-records/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error(error.message || '删除失败')
  }
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="key-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>钥匙管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="openBorrowDialog"><el-icon><Plus /></el-icon> 借用钥匙</el-button>
          </div>
        </div>
      </template>
      <el-table :data="keyRecords" v-loading="loading" stripe>
        <el-table-column prop="keyNumber" label="钥匙编号" width="110" />
        <el-table-column prop="keyName" label="钥匙名称" min-width="150" />
        <el-table-column prop="location" label="存放位置" width="120" />
        <el-table-column prop="borrower" label="借用人" width="100" />
        <el-table-column prop="borrowTime" label="借用时间" width="150" />
        <el-table-column prop="returnTime" label="归还时间" width="150">
          <template #default="{ row }">{{ row.returnTime || '-' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleReturn(row)" v-if="row.status === 'Borrowed'">归还</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination"><el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="loadData" /></div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="钥匙编号" prop="keyNumber"><el-input v-model="form.keyNumber" placeholder="如：KEY-001" /></el-form-item>
        <el-form-item label="钥匙名称" prop="keyName"><el-input v-model="form.keyName" placeholder="如：A栋配电房钥匙" /></el-form-item>
        <el-form-item label="存放位置" prop="location"><el-input v-model="form.location" placeholder="如：A栋地下室" /></el-form-item>
        <el-form-item label="借用人" prop="borrower"><el-input v-model="form.borrower" placeholder="请输入借用人姓名" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.notes" type="textarea" :rows="2" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定借用</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="key" 
      module-name="钥匙管理" 
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
.key-list { width: 100%; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.pagination { margin-top: 20px; display: flex; justify-content: flex-end; }
</style>
