<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields } from '@/stores/fieldConfig'
import { masterApi } from '@/api/http'

const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

const getKeyFields = () => getActiveFields('key')

const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

const refreshKey = ref(0)
const refreshFields = () => { refreshKey.value++ }

const loading = ref(false)
const recordList = ref<any[]>([])
const total = ref(0)

const filterKeyword = ref('')
const filterType = ref('')
const filterStatus = ref('')
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const dialogTitle = ref('新增钥匙')
const editingId = ref<number | null>(null)
const viewingRecord = ref<any>(null)

const form = ref({
  keyNo: '',
  name: '',
  type: 'door_key',
  location: '',
  building: '',
  floor: '',
  doorNo: '',
  quantity: 1,
  status: 'available',
  holder: '',
  holderPhone: '',
  remark: ''
})

const typeOptions = [
  { value: 'door_key', label: '入户门钥匙' },
  { value: 'room_key', label: '房门钥匙' },
  { value: 'card', label: '门禁卡' },
  { value: 'password', label: '密码' },
  { value: 'remote', label: '遥控器' },
  { value: 'other', label: '其他' }
]

const statusOptions = [
  { value: 'available', label: '可用' },
  { value: 'borrowed', label: '已借出' },
  { value: 'lost', label: '已遗失' },
  { value: 'damaged', label: '已损坏' },
  { value: 'disabled', label: '已停用' }
]

const stats = computed(() => ({
  total: recordList.value.length,
  available: recordList.value.filter((k: any) => k.Status === 'available').length,
  borrowed: recordList.value.filter((k: any) => k.Status === 'borrowed').length,
  lost: recordList.value.filter((k: any) => k.Status === 'lost').length,
  damaged: recordList.value.filter((k: any) => k.Status === 'damaged').length
}))

const filteredList = computed(() => {
  let result = recordList.value
  if (filterKeyword.value) {
    const kw = filterKeyword.value.toLowerCase()
    result = result.filter((r: any) =>
      (r.KeyNo?.toLowerCase().includes(kw)) ||
      (r.Name?.toLowerCase().includes(kw))
    )
  }
  if (filterType.value) result = result.filter((r: any) => r.Type === filterType.value)
  if (filterStatus.value) result = result.filter((r: any) => r.Status === filterStatus.value)
  return result.sort((a: any, b: any) => b.BorrowCount - a.BorrowCount)
})

const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 200 }
    if (filterKeyword.value) params.keyword = filterKeyword.value
    if (filterType.value) params.type = filterType.value
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await masterApi.get('/keys', { params })
    recordList.value = res.data || []
    total.value = res.total || 0
  } catch (e: any) {
    console.error('Load data error:', e)
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => loadData())

const handleAdd = () => {
  editingId.value = null
  dialogTitle.value = '新增钥匙'
  form.value = {
    keyNo: 'KEY-' + String(Date.now()).slice(-5),
    name: '',
    type: 'door_key',
    location: '前台钥匙柜',
    building: '',
    floor: '',
    doorNo: '',
    quantity: 1,
    status: 'available',
    holder: '',
    holderPhone: '',
    remark: ''
  }
  dialogVisible.value = true
}

const handleEdit = (row: any) => {
  editingId.value = row.Id
  dialogTitle.value = '编辑钥匙'
  form.value = {
    keyNo: row.KeyNo || '',
    name: row.Name || '',
    type: row.Type || 'door_key',
    location: row.Location || '',
    building: row.Building || '',
    floor: row.Floor || '',
    doorNo: row.DoorNo || '',
    quantity: row.Quantity || 1,
    status: row.Status || 'available',
    holder: row.Holder || '',
    holderPhone: row.HolderPhone || '',
    remark: row.Remark || ''
  }
  dialogVisible.value = true
}

const handleView = (row: any) => {
  viewingRecord.value = row
  detailDialogVisible.value = true
}

const handleSubmit = async () => {
  if (!form.value.keyNo.trim()) { ElMessage.warning('请输入钥匙编号'); return }
  if (!form.value.name.trim()) { ElMessage.warning('请输入钥匙名称'); return }

  try {
    if (editingId.value) {
      await masterApi.put(`/keys/${editingId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/keys', form.value)
      ElMessage.success('添加成功')
    }
    await loadData()
    dialogVisible.value = false
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除钥匙 "${row.Name}" 吗？`, '提示', { type: 'warning' })
    await masterApi.delete(`/keys/${row.Id}`)
    ElMessage.success('删除成功')
    await loadData()
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
  }
}

const handleRefresh = async () => {
  refreshFields()
  await loadData()
}

const handleReset = () => {
  filterKeyword.value = ''
  filterType.value = ''
  filterStatus.value = ''
}

const getStatusType = (status: string) => {
  const map: Record<string, string> = { available: 'success', borrowed: 'warning', lost: 'danger', damaged: 'info', disabled: 'info' }
  return map[status] || 'info'
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = { available: '可用', borrowed: '已借出', lost: '已遗失', damaged: '已损坏', disabled: '已停用' }
  return map[status] || status
}

const getTypeLabel = (t: string) => {
  const map: Record<string, string> = { door_key: '入户门钥匙', room_key: '房门钥匙', card: '门禁卡', password: '密码', remote: '遥控器', other: '其他' }
  return map[t] || t
}
</script>

<template>
  <div class="key-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>钥匙管理</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon> 配置字段</el-button>
            <el-button @click="handleRefresh"><el-icon><Refresh /></el-icon> 刷新</el-button>
            <el-button type="primary" @click="handleAdd"><el-icon><Plus /></el-icon> 新增钥匙</el-button>
          </div>
        </div>
      </template>

      <el-alert title="钥匙管理说明" description="管理所有钥匙和门禁卡，支持借用/归还记录。" type="info" :closable="false" style="margin-bottom: 20px;" />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-content"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">钥匙总数</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card available" @click="filterStatus = filterStatus === 'available' ? '' : 'available'">
          <div class="stat-content"><div class="stat-value">{{ stats.available }}</div><div class="stat-label">可用</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card borrowed" @click="filterStatus = filterStatus === 'borrowed' ? '' : 'borrowed'">
          <div class="stat-content"><div class="stat-value">{{ stats.borrowed }}</div><div class="stat-label">已借出</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card lost" @click="filterStatus = filterStatus === 'lost' ? '' : 'lost'">
          <div class="stat-content"><div class="stat-value">{{ stats.lost }}</div><div class="stat-label">已遗失</div></div>
        </el-card>
        <el-card shadow="hover" class="stat-card damaged" @click="filterStatus = filterStatus === 'damaged' ? '' : 'damaged'">
          <div class="stat-content"><div class="stat-value">{{ stats.damaged }}</div><div class="stat-label">已损坏</div></div>
        </el-card>
      </div>

      <!-- 筛选工具栏 -->
      <div class="filter-toolbar">
        <el-input v-model="filterKeyword" placeholder="关键词搜索" clearable style="width: 200px;" />
        <el-select v-model="filterType" placeholder="类型" clearable style="width: 130px;">
          <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 120px;">
          <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-button @click="handleReset">重置</el-button>
      </div>

      <!-- 数据列表 -->
      <el-table :data="filteredList" stripe v-loading="loading" @row-click="handleView">
        <el-table-column prop="KeyNo" label="钥匙编号" width="100" />
        <el-table-column prop="Name" label="钥匙名称" min-width="150" />
        <el-table-column prop="Type" label="类型" width="100" align="center">
          <template #default="{ row }"><el-tag size="small">{{ getTypeLabel(row.Type) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="Location" label="存放位置" width="120" />
        <el-table-column prop="Building" label="楼栋" width="80" align="center" />
        <el-table-column prop="DoorNo" label="门牌号" width="80" align="center" />
        <el-table-column prop="Quantity" label="数量" width="60" align="center" />
        <el-table-column prop="Status" label="状态" width="90" align="center">
          <template #default="{ row }"><el-tag :type="getStatusType(row.Status)" size="small">{{ getStatusLabel(row.Status) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="Holder" label="持有人" width="100" align="center">
          <template #default="{ row }">{{ row.Holder || '-' }}</template>
        </el-table-column>
        <el-table-column prop="BorrowCount" label="借用次数" width="80" align="center" />
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="钥匙编号" required><el-input v-model="form.keyNo" placeholder="如：KEY-001" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="钥匙名称" required><el-input v-model="form.name" placeholder="如：A栋101室钥匙" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="钥匙类型">
              <el-select v-model="form.type" style="width: 100%">
                <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="存放位置">
              <el-input v-model="form.location" placeholder="如：前台钥匙柜" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="楼栋"><el-input v-model="form.building" placeholder="如：A栋" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="楼层"><el-input v-model="form.floor" placeholder="如：1楼" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="门牌号"><el-input v-model="form.doorNo" placeholder="如：101" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数量"><el-input-number v-model="form.quantity" :min="1" style="width: 100%" /></el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态">
              <el-select v-model="form.status" style="width: 100%">
                <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="钥匙详情" width="700px">
      <div v-if="viewingRecord" class="key-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="钥匙编号">{{ viewingRecord.KeyNo }}</el-descriptions-item>
          <el-descriptions-item label="钥匙名称">{{ viewingRecord.Name }}</el-descriptions-item>
          <el-descriptions-item label="类型">{{ getTypeLabel(viewingRecord.Type) }}</el-descriptions-item>
          <el-descriptions-item label="数量">{{ viewingRecord.Quantity }}</el-descriptions-item>
          <el-descriptions-item label="存放位置">{{ viewingRecord.Location }}</el-descriptions-item>
          <el-descriptions-item label="位置">{{ viewingRecord.Building }} {{ viewingRecord.Floor }} {{ viewingRecord.DoorNo }}</el-descriptions-item>
          <el-descriptions-item label="状态"><el-tag :type="getStatusType(viewingRecord.Status)">{{ getStatusLabel(viewingRecord.Status) }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="借用次数">{{ viewingRecord.BorrowCount }} 次</el-descriptions-item>
          <el-descriptions-item label="持有人">{{ viewingRecord.Holder || '-' }}</el-descriptions-item>
          <el-descriptions-item label="持有人电话">{{ viewingRecord.HolderPhone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ viewingRecord.Remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="key" module-name="钥匙管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.key-page { width: 100%; }
.header { display: flex; justify-content: space-between; align-items: center; }
.header-actions { display: flex; gap: 10px; }
.stats-grid { display: flex; gap: 15px; margin-bottom: 20px; }
.stat-card { flex: 1; cursor: pointer; transition: all 0.3s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { text-align: center; }
.stat-value { font-size: 24px; font-weight: bold; color: #409EFF; }
.stat-card.available .stat-value { color: #67C23A; }
.stat-card.borrowed .stat-value { color: #E6A23C; }
.stat-card.lost .stat-value { color: #F56C6C; }
.stat-card.damaged .stat-value { color: #909399; }
.stat-label { font-size: 13px; color: #909399; margin-top: 5px; }
.filter-toolbar { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; align-items: center; }
.key-detail { padding: 10px; }
</style>