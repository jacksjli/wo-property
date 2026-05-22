<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, Search, Check, Warning, Clock, Document } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getInspectionFields = () => getActiveFields('inspection')

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

// 状态配置
const statusLabels: Record<string, string> = {
  pending: '待巡检',
  in_progress: '巡检中',
  completed: '已完成',
  issue_found: '发现异常'
}

const resultLabels: Record<string, string> = {
  pass: '通过',
  fail: '不通过'
}

// 数据状态
const loading = ref(false)
const tableData = ref<any[]>([])
const pagination = ref({ page: 1, pageSize: 20, total: 0, totalPages: 0 })
const searchKeyword = ref('')

// 统计
const stats = ref({ total: 0, pending: 0, inProgress: 0, completed: 0, issues: 0 })

// 标签页
const activeTab = ref('records')

// 对话框状态
const dialogVisible = ref(false)
const dialogTitle = ref('新增巡检记录')
const editingId = ref<number | null>(null)
const submitting = ref(false)

// 表单
const formRef = ref()
const form = ref({
  inspectionTitle: '',
  buildingId: null as number | null,
  inspectionArea: '',
  inspectorName: '',
  inspectionDate: '',
  inspectionTime: '',
  nextInspectionDate: '',
  remarks: ''
})

// 搜索
const handleSearch = () => {
  pagination.value.page = 1
  loadData()
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/inspection-records', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.pageSize,
        keyword: searchKeyword.value || undefined
      }
    })
    if (res.success) {
      tableData.value = res.data || []
      pagination.value = res.pagination || pagination.value
      // 更新统计
      stats.value = {
        total: res.pagination?.totalCount || 0,
        pending: tableData.value.filter(r => r.status === 'pending').length,
        inProgress: tableData.value.filter(r => r.status === 'in_progress').length,
        completed: tableData.value.filter(r => r.status === 'completed').length,
        issues: tableData.value.filter(r => r.result === 'fail').length
      }
    }
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败')
  } finally {
    loading.value = false
  }
}

// 刷新
const handleRefresh = () => {
  loadData()
  ElMessage.success('已刷新')
}

// 状态颜色
const getStatusType = (status: string) => {
  const map: Record<string, string> = {
    pending: 'info',
    in_progress: 'warning',
    completed: 'success',
    issue_found: 'danger'
  }
  return map[status] || 'info'
}

// 打开新增对话框
const openCreateDialog = () => {
  dialogTitle.value = '新增巡检记录'
  editingId.value = null
  form.value = {
    inspectionTitle: '',
    buildingId: null,
    inspectionArea: '',
    inspectorName: '',
    inspectionDate: '',
    inspectionTime: '',
    nextInspectionDate: '',
    remarks: ''
  }
  dialogVisible.value = true
}

// 打开编辑对话框
const openEditDialog = (row: any) => {
  dialogTitle.value = '编辑巡检记录'
  editingId.value = row.id
  form.value = {
    inspectionTitle: row.inspectionTitle || '',
    buildingId: row.buildingId || null,
    inspectionArea: row.inspectionArea || '',
    inspectorName: row.inspectorName || '',
    inspectionDate: row.inspectionDate?.split('T')[0] || '',
    inspectionTime: row.inspectionTime || '',
    nextInspectionDate: row.nextInspectionDate?.split('T')[0] || '',
    remarks: row.remarks || ''
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!form.value.inspectionTitle.trim()) {
    ElMessage.warning('请输入巡检标题')
    return
  }
  if (!form.value.inspectionDate) {
    ElMessage.warning('请选择巡检日期')
    return
  }

  submitting.value = true
  try {
    const payload = {
      InspectionTitle: form.value.inspectionTitle,
      BuildingId: form.value.buildingId,
      InspectionArea: form.value.inspectionArea,
      InspectorName: form.value.inspectorName,
      InspectionDate: form.value.inspectionDate || null,
      InspectionTime: form.value.inspectionTime || null,
      NextInspectionDate: form.value.nextInspectionDate || null,
      Remarks: form.value.remarks || null,
    }
    if (editingId.value) {
      await masterApi.put(`/inspection-records/${editingId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/inspection-records', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除巡检记录 "${row.inspectionTitle}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await masterApi.delete(`/inspection-records/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

// 分页
const handlePageChange = (page: number) => {
  pagination.value.page = page
  loadData()
}

// 初始化加载
onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="inspection-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>巡检管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="巡检管理说明"
        description="查看巡检记录、编辑巡检信息、管理巡检状态。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value">{{ stats.total }}</div>
            <div class="stat-label">巡检总数</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value warning">{{ stats.pending }}</div>
            <div class="stat-label">待巡检</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content">
            <div class="stat-value">{{ stats.completed }}</div>
            <div class="stat-label">已完成</div>
          </div>
        </el-card>
        <el-card shadow="hover" class="stat-card">
          <div class="stat-value danger">{{ stats.issues }}</div>
          <div class="stat-label">发现异常</div>
        </el-card>
      </div>

      <!-- 搜索和操作栏 -->
      <div class="toolbar">
        <el-input
          v-model="searchKeyword"
          placeholder="搜索巡检标题/区域/巡检员"
          style="width: 300px;"
          clearable
          @keyup.enter="handleSearch"
        >
          <template #append>
            <el-button icon="Search" @click="handleSearch" />
          </template>
        </el-input>
        <el-button type="primary" @click="openCreateDialog">
          <el-icon><Plus /></el-icon> 新增巡检记录
        </el-button>
      </div>

      <!-- 表格 -->
      <el-table :data="tableData" stripe v-loading="loading">
        <el-table-column prop="inspectionTitle" label="巡检标题" min-width="150" />
        <el-table-column prop="buildingName" label="楼栋" width="100" align="center" />
        <el-table-column prop="inspectionArea" label="巡检区域" min-width="120" />
        <el-table-column prop="inspectorName" label="巡检员" width="100" align="center" />
        <el-table-column prop="inspectionDate" label="巡检日期" width="120" align="center">
          <template #default="{ row }">
            {{ row.inspectionDate?.split('T')[0] || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ statusLabels[row.status] || row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="result" label="结果" width="80" align="center">
          <template #default="{ row }">
            <span :style="{ color: row.result === 'fail' ? '#F56C6C' : '#67C23A' }">
              {{ resultLabels[row.result] || '-' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="findings" label="发现" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openEditDialog(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.page"
          :page-size="pagination.pageSize"
          :total="pagination.totalCount"
          layout="total, prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
      <el-form ref="formRef" :model="form" label-width="100px">
        <el-form-item label="巡检标题" required>
          <el-input v-model="form.inspectionTitle" placeholder="请输入巡检标题" />
        </el-form-item>
        <el-form-item label="巡检区域">
          <el-input v-model="form.inspectionArea" placeholder="如：1单元全区域" />
        </el-form-item>
        <el-form-item label="巡检员">
          <el-input v-model="form.inspectorName" placeholder="请输入巡检员姓名" />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="巡检日期" required>
              <el-date-picker v-model="form.inspectionDate" type="date" style="width: 100%" value-format="YYYY-MM-DD" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="巡检时间">
              <el-input v-model="form.inspectionTime" placeholder="如：09:00" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="下次巡检日期">
          <el-date-picker v-model="form.nextInspectionDate" type="date" style="width: 100%" value-format="YYYY-MM-DD" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remarks" type="textarea" placeholder="备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
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
.inspection-page {
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
.stats-grid {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
}
.stat-card {
  flex: 1;
  cursor: pointer;
  transition: all 0.3s;
}
.stat-card:hover {
  transform: translateY(-2px);
}
.stat-content {
  text-align: center;
}
.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: #409EFF;
}
.stat-value.warning {
  color: #E6A23C;
}
.stat-value.danger {
  color: #F56C6C;
}
.stat-label {
  font-size: 13px;
  color: #909399;
  margin-top: 5px;
}
.toolbar {
  display: flex;
  gap: 10px;
  margin-bottom: 15px;
}
.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>