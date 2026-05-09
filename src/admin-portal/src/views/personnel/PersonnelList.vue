<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, User, UserFilled, Link, Phone, Postcard, Calendar } from '@element-plus/icons-vue'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { masterApi } from '@/api/http'

// 类型定义
type PersonnelRole = 'operator' | 'supervisor' | 'manager' | 'department_head' | 'company_head'
type PersonnelStatus = 'active' | 'inactive' | 'on_leave' | 'probation' | 'resigned'
type Gender = 'male' | 'female' | 'other'
type Education = 'high_school' | 'college' | 'bachelor' | 'master' | 'doctor'
type EmploymentType = 'full_time' | 'part_time' | 'contract' | 'intern'

interface EmergencyContact { name: string; relationship: string; phone: string }
interface BackupStaff { staffId: number; staffName: string }
interface Department { id: number; name: string; managerId?: number }
interface Personnel {
  id: number; employeeNo: string; name: string; avatar?: string; gender: Gender; birthday?: string;
  idCard: string; phone: string; emergencyContact?: EmergencyContact; email?: string; address?: string;
  education: Education; graduateSchool?: string; major?: string; role: PersonnelRole;
  departmentId?: number; departmentName?: string; position: string; employmentType: EmploymentType;
  hireDate: string; contractStart?: string; contractEnd?: string; salary?: number; bankAccount?: string;
  socialSecurityNo?: string; status: PersonnelStatus; specialties: string[]; backups: BackupStaff[];
  attendanceCount: number; overtimeHours: number; leaveDays: number; performanceScore?: number;
  trainingCount: number; remark?: string; createdAt: string; updatedAt: string;
}

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const refreshKey = ref(0)
const refreshFields = () => { refreshKey.value++ }
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) { fieldDialogRef.value?.open() }
}

// 角色/状态/性别/学历/编制类型标签
const roleLabels: Record<PersonnelRole, string> = {
  'operator': '操作人员', 'supervisor': '主管', 'manager': '经理',
  'department_head': '部门负责人', 'company_head': '公司负责人'
}
const statusLabels: Record<PersonnelStatus, string> = {
  'active': '在职', 'inactive': '离职', 'on_leave': '休假中', 'probation': '试用期', 'resigned': '已辞职'
}
const genderLabels: Record<Gender, string> = { 'male': '男', 'female': '女', 'other': '其他' }
const educationLabels: Record<Education, string> = {
  'high_school': '高中', 'college': '大专', 'bachelor': '本科', 'master': '硕士', 'doctor': '博士'
}
const employmentTypeLabels: Record<EmploymentType, string> = {
  'full_time': '全职', 'part_time': '兼职', 'contract': '合同制', 'intern': '实习生'
}
const specialtyOptions = [
  '电梯维修', '水电维修', '消防设备', '门禁系统', '监控系统',
  '空调维修', '给排水', '强弱电', '日常保养', '设备巡检',
  '应急处理', '客户服务', '安全管理', '环境清洁', '绿化养护'
]
const departments: Department[] = [
  { id: 1, name: '工程部' }, { id: 2, name: '安保部' }, { id: 3, name: '客服部' },
  { id: 4, name: '财务部' }, { id: 5, name: '行政部' }, { id: 6, name: '保洁部' }, { id: 7, name: '绿化部' }
]

// 数据
const personnelList = ref<Personnel[]>([])
const loading = ref(false)
const stats = computed(() => ({
  total: personnelList.value.length,
  active: personnelList.value.filter(p => p.status === 'active').length,
  probation: personnelList.value.filter(p => p.status === 'probation').length,
  byRole: {
    supervisor: personnelList.value.filter(p => p.role === 'supervisor').length,
    manager: personnelList.value.filter(p => p.role === 'manager').length,
  }
}))

// 筛选
const filterRole = ref<PersonnelRole | ''>('')
const filterDepartment = ref<number | ''>('')
const filterStatus = ref<PersonnelStatus | ''>('')

// 筛选后的人员
const filteredPersonnel = computed(() => {
  let result = personnelList.value
  if (filterRole.value) result = result.filter(p => p.role === filterRole.value)
  if (filterDepartment.value) result = result.filter(p => p.departmentId === filterDepartment.value)
  if (filterStatus.value) result = result.filter(p => p.status === filterStatus.value)
  return result.sort((a, b) => {
    const levelA = getRoleLevel(a.role), levelB = getRoleLevel(b.role)
    if (levelA !== levelB) return levelA - levelB
    return a.name.localeCompare(b.name)
  })
})

const getRoleLevel = (role: PersonnelRole) => {
  const levels: Record<PersonnelRole, number> = {
    operator: 1, supervisor: 2, manager: 3, department_head: 4, company_head: 5
  }
  return levels[role]
}

// 对话框状态
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const backupDialogVisible = ref(false)
const dialogTitle = ref('新增人员')
const editingId = ref<number | null>(null)
const viewingPerson = ref<Personnel | null>(null)

// 表单数据
const form = ref({
  employeeNo: '', name: '', gender: 'male' as Gender, birthday: '', idCard: '', phone: '',
  email: '', address: '', education: 'bachelor' as Education, graduateSchool: '', major: '',
  role: 'operator' as PersonnelRole, departmentId: 1, position: '',
  employmentType: 'full_time' as EmploymentType, hireDate: '', contractStart: '', contractEnd: '',
  salary: 0, specialties: [] as string[], remark: ''
})

// 选项
const genderOptions = Object.entries(genderLabels).map(([value, label]) => ({ value, label }))
const educationOptions = Object.entries(educationLabels).map(([value, label]) => ({ value, label }))
const roleOptions = Object.entries(roleLabels).map(([value, label]) => ({ value, label }))
const statusOptions = Object.entries(statusLabels).map(([value, label]) => ({ value, label }))
const employmentTypeOptions = Object.entries(employmentTypeLabels).map(([value, label]) => ({ value, label }))

// 备份人员选项
const backupOptions = computed(() => {
  return personnelList.value
    .filter(p => p.id !== editingId.value && p.status !== 'resigned')
    .map(p => ({ value: p.id, label: `${p.name} (${roleLabels[p.role]})` }))
})

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const params: any = { page: 1, pageSize: 100 }
    if (filterRole.value) params.role = filterRole.value
    if (filterDepartment.value) params.departmentId = filterDepartment.value
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await masterApi.get('/personnel', { params })
    if (res.success) {
      personnelList.value = (res.data || []).map((p: any) => ({
        ...p,
        specialties: p.specialties ? (typeof p.specialties === 'string' ? JSON.parse(p.specialties) : p.specialties) : [],
        backups: p.backups ? (typeof p.backups === 'string' ? JSON.parse(p.backups) : p.backups) : []
      }))
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

// 获取启用的字段
const getPersonnelFields = () => getActiveFields('personnel')

// 打开新增
const handleAdd = () => {
  dialogTitle.value = '新增人员'
  editingId.value = null
  const today = new Date().toISOString().split('T')[0]
  form.value = {
    employeeNo: 'EMP' + String(Date.now()).slice(-5), name: '', gender: 'male', birthday: '',
    idCard: '', phone: '', email: '', address: '', education: 'bachelor', graduateSchool: '', major: '',
    role: 'operator', departmentId: 1, position: '', employmentType: 'full_time', hireDate: today,
    contractStart: '', contractEnd: '', salary: 0, specialties: [], remark: ''
  }
  dialogVisible.value = true
}

// 打开编辑
const handleEdit = (row: Personnel) => {
  dialogTitle.value = '编辑人员'
  editingId.value = row.id
  form.value = {
    employeeNo: row.employeeNo, name: row.name, gender: row.gender,
    birthday: row.birthday || '', idCard: row.idCard, phone: row.phone,
    email: row.email || '', address: row.address || '', education: row.education,
    graduateSchool: row.graduateSchool || '', major: row.major || '',
    role: row.role, departmentId: row.departmentId || 1, position: row.position,
    employmentType: row.employmentType, hireDate: row.hireDate,
    contractStart: row.contractStart || '', contractEnd: row.contractEnd || '',
    salary: row.salary || 0, specialties: [...row.specialties], remark: row.remark || ''
  }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!form.value.name.trim()) { ElMessage.warning('请输入姓名'); return }
  if (!form.value.phone.trim()) { ElMessage.warning('请输入联系电话'); return }
  if (!form.value.idCard.trim()) { ElMessage.warning('请输入身份证号'); return }

  const department = departments.find(d => d.id === form.value.departmentId)
  try {
    const payload = {
      ...form.value,
      departmentName: department?.name,
      specialties: JSON.stringify(form.value.specialties),
      Backups: JSON.stringify(viewingPerson.value?.backups || [])
    }

    if (editingId.value) {
      await masterApi.put(`/personnel/${editingId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/personnel', { ...payload, status: 'probation' })
      ElMessage.success('添加成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
}

// 删除
const handleDelete = async (row: Personnel) => {
  try {
    await ElMessageBox.confirm(`确定删除人员 "${row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning'
    })
    await masterApi.delete(`/personnel/${row.id}`)
    ElMessage.success('删除成功')
    loadData()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

// 查看详情
const handleView = (row: Personnel) => {
  viewingPerson.value = row
  detailDialogVisible.value = true
}

// 备份对话框
const openBackupDialog = (row: Personnel) => {
  viewingPerson.value = row
  backupDialogVisible.value = true
}

// 添加备份
const handleAddBackup = async (personId: number, person: Personnel) => {
  const selectEl = document.getElementById(`backup-select-${personId}`) as HTMLSelectElement
  if (selectEl && selectEl.value) {
    const backupId = parseInt(selectEl.value)
    const backupPerson = personnelList.value.find(p => p.id === backupId)
    if (backupPerson) {
      try {
        await masterApi.post(`/personnel/${personId}/backups`, { staffId: backupId, staffName: backupPerson.name })
        ElMessage.success('已添加备份人员')
        loadData()
        const updated = personnelList.value.find(p => p.id === personId)
        if (updated) viewingPerson.value = updated
      } catch (e: any) { ElMessage.error(e.message || '添加失败') }
    }
  }
}

// 移除备份
const handleRemoveBackup = async (personId: number, staffId: number) => {
  try {
    await masterApi.delete(`/personnel/${personId}/backups/${staffId}`)
    ElMessage.success('已移除备份人员')
    loadData()
    const updated = personnelList.value.find(p => p.id === personId)
    if (updated) viewingPerson.value = updated
  } catch (e: any) { ElMessage.error(e.message || '移除失败') }
}

const getDepartmentName = (id: number) => departments.find(d => d.id === id)?.name || '-'
const handleRefresh = () => { loadData(); ElMessage.success('已刷新') }
const handleReset = () => { filterRole.value = ''; filterDepartment.value = ''; filterStatus.value = '' }
const getStatusType = (status: PersonnelStatus) => {
  const map: Record<PersonnelStatus, string> = {
    active: 'success', inactive: 'info', on_leave: 'warning', probation: 'primary', resigned: 'danger'
  }
  return map[status]
}

onMounted(() => { loadData() })
</script>

<template>
  <div class="personnel-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>人员管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增人员
            </el-button>
          </div>
        </div>
      </template>

      <el-alert title="人员管理说明" description="管理所有员工信息，支持角色层级、备份人员、考勤统计等功能。" type="info" :closable="false" style="margin-bottom: 20px;" />

      <!-- 统计卡片 -->
      <div class="stats-grid">
        <el-card shadow="hover" class="stat-card"><div class="stat-content"><div class="stat-value">{{ stats.total }}</div><div class="stat-label">人员总数</div></div></el-card>
        <el-card shadow="hover" class="stat-card active"><div class="stat-content"><div class="stat-value">{{ stats.active }}</div><div class="stat-label">在职</div></div></el-card>
        <el-card shadow="hover" class="stat-card probation"><div class="stat-content"><div class="stat-value">{{ stats.probation }}</div><div class="stat-label">试用期</div></div></el-card>
        <el-card shadow="hover" class="stat-card supervisor"><div class="stat-content"><div class="stat-value">{{ stats.byRole.supervisor }}</div><div class="stat-label">主管</div></div></el-card>
        <el-card shadow="hover" class="stat-card manager"><div class="stat-content"><div class="stat-value">{{ stats.byRole.manager }}</div><div class="stat-label">经理</div></div></el-card>
      </div>

      <!-- 筛选工具栏 -->
      <div class="filter-toolbar">
        <el-select v-model="filterRole" placeholder="角色" clearable style="width: 130px;" @change="loadData">
          <el-option v-for="opt in roleOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterDepartment" placeholder="部门" clearable style="width: 130px;" @change="loadData">
          <el-option v-for="dept in departments" :key="dept.id" :label="dept.name" :value="dept.id" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 120px;" @change="loadData">
          <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-button @click="handleReset">重置</el-button>
      </div>

      <!-- 人员列表 -->
      <el-table :data="filteredPersonnel" stripe v-loading="loading" @row-click="handleView">
        <el-table-column prop="employeeNo" label="工号" width="90" />
        <el-table-column prop="name" label="姓名" width="100">
          <template #default="{ row }"><span style="font-weight: 600;">{{ row.name }}</span></template>
        </el-table-column>
        <el-table-column prop="role" label="角色" width="110" align="center">
          <template #default="{ row }"><el-tag size="small">{{ roleLabels[row.role] }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="departmentName" label="部门" width="100" align="center">
          <template #default="{ row }">{{ row.departmentName || '-' }}</template>
        </el-table-column>
        <el-table-column prop="position" label="职位" width="100" align="center" />
        <el-table-column prop="phone" label="联系电话" width="130" />
        <el-table-column prop="specialties" label="专业技能" min-width="180">
          <template #default="{ row }">
            <el-tag v-for="s in row.specialties.slice(0, 2)" :key="s" size="small" style="margin-right: 4px;">{{ s }}</el-tag>
            <span v-if="row.specialties.length > 2" style="color: #909399; font-size: 12px;">+{{ row.specialties.length - 2 }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ statusLabels[row.status] }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click.stop="handleEdit(row)">编辑</el-button>
            <el-button link type="success" size="small" @click.stop="openBackupDialog(row)"><el-icon><Link /></el-icon> 备份</el-button>
            <el-button link type="danger" size="small" @click.stop="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="800px">
      <el-form label-width="100px">
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="工号"><el-input v-model="form.employeeNo" disabled /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="姓名" required><el-input v-model="form.name" placeholder="请输入姓名" /></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="性别"><el-select v-model="form.gender" style="width: 100%"><el-option v-for="opt in genderOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="身份证号" required><el-input v-model="form.idCard" placeholder="请输入身份证号" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="联系电话" required><el-input v-model="form.phone" placeholder="请输入联系电话" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12"><el-form-item label="出生日期"><el-date-picker v-model="form.birthday" type="date" style="width: 100%" /></el-form-item></el-col>
          <el-col :span="12"><el-form-item label="邮箱"><el-input v-model="form.email" placeholder="请输入邮箱" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="角色"><el-select v-model="form.role" style="width: 100%"><el-option v-for="opt in roleOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="部门"><el-select v-model="form.departmentId" style="width: 100%"><el-option v-for="dept in departments" :key="dept.id" :label="dept.name" :value="dept.id" /></el-select></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="职位"><el-input v-model="form.position" placeholder="请输入职位" /></el-form-item></el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8"><el-form-item label="学历"><el-select v-model="form.education" style="width: 100%"><el-option v-for="opt in educationOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="编制类型"><el-select v-model="form.employmentType" style="width: 100%"><el-option v-for="opt in employmentTypeOptions" :key="opt.value" :label="opt.label" :value="opt.value" /></el-select></el-form-item></el-col>
          <el-col :span="8"><el-form-item label="入职日期"><el-date-picker v-model="form.hireDate" type="date" style="width: 100%" /></el-form-item></el-col>
        </el-row>
        <el-form-item label="专业技能">
          <el-select v-model="form.specialties" multiple placeholder="请选择专业技能" style="width: 100%">
            <el-option v-for="opt in specialtyOptions" :key="opt" :label="opt" :value="opt" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" placeholder="请输入备注" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 人员详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="人员详情" width="750px">
      <div v-if="viewingPerson" class="person-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="工号">{{ viewingPerson.employeeNo }}</el-descriptions-item>
          <el-descriptions-item label="姓名">{{ viewingPerson.name }}</el-descriptions-item>
          <el-descriptions-item label="性别">{{ genderLabels[viewingPerson.gender] }}</el-descriptions-item>
          <el-descriptions-item label="联系电话">{{ viewingPerson.phone }}</el-descriptions-item>
          <el-descriptions-item label="身份证号">{{ viewingPerson.idCard }}</el-descriptions-item>
          <el-descriptions-item label="角色"><el-tag>{{ roleLabels[viewingPerson.role] }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="部门">{{ viewingPerson.departmentName }}</el-descriptions-item>
          <el-descriptions-item label="职位">{{ viewingPerson.position }}</el-descriptions-item>
          <el-descriptions-item label="学历">{{ educationLabels[viewingPerson.education] }}</el-descriptions-item>
          <el-descriptions-item label="入职日期">{{ viewingPerson.hireDate }}</el-descriptions-item>
          <el-descriptions-item label="状态"><el-tag :type="getStatusType(viewingPerson.status)">{{ statusLabels[viewingPerson.status] }}</el-tag></el-descriptions-item>
          <el-descriptions-item label="月薪">{{ viewingPerson.salary ? '¥' + viewingPerson.salary.toLocaleString() : '-' }}</el-descriptions-item>
        </el-descriptions>
        <h4 style="margin-top: 20px;">专业技能</h4>
        <div style="display: flex; flex-wrap: wrap; gap: 8px;">
          <el-tag v-for="s in viewingPerson.specialties" :key="s" type="info">{{ s }}</el-tag>
          <span v-if="!viewingPerson.specialties.length" style="color: #909399;">暂无</span>
        </div>
        <h4 style="margin-top: 20px;">备份人员</h4>
        <div v-if="viewingPerson.backups.length" style="display: flex; flex-wrap: wrap; gap: 8px;">
          <el-tag v-for="b in viewingPerson.backups" :key="b.staffId" closable @close="handleRemoveBackup(viewingPerson.id, b.staffId)">{{ b.staffName }}</el-tag>
        </div>
        <span v-else style="color: #909399;">暂无备份人员</span>
      </div>
      <template #footer><el-button @click="detailDialogVisible = false">关闭</el-button></template>
    </el-dialog>

    <!-- 备份人员对话框 -->
    <el-dialog v-model="backupDialogVisible" title="管理备份人员" width="500px">
      <div v-if="viewingPerson">
        <p style="margin-bottom: 15px;">为 <strong>{{ viewingPerson.name }}</strong> 添加备份人员：</p>
        <div style="display: flex; gap: 10px; margin-bottom: 20px;">
          <el-select :id="`backup-select-${viewingPerson.id}`" placeholder="选择备份人员" style="flex: 1;">
            <el-option v-for="opt in backupOptions.filter(o => !viewingPerson.backups.some((b: any) => b.staffId === opt.value))" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
          <el-button type="primary" @click="handleAddBackup(viewingPerson.id, viewingPerson)">添加</el-button>
        </div>
        <h4>当前备份人员：</h4>
        <div style="display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px;">
          <el-tag v-for="b in viewingPerson.backups" :key="b.staffId" closable type="success" @close="handleRemoveBackup(viewingPerson.id, b.staffId)">{{ b.staffName }}</el-tag>
          <span v-if="!viewingPerson.backups.length" style="color: #909399;">暂无</span>
        </div>
      </div>
      <template #footer><el-button @click="backupDialogVisible = false">关闭</el-button></template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog ref="fieldDialogRef" module="personnel" module-name="人员管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.personnel-page { width: 100%; }
.header { display: flex; justify-content: space-between; align-items: center; }
.header-actions { display: flex; gap: 10px; }
.stats-grid { display: flex; gap: 15px; margin-bottom: 20px; }
.stat-card { flex: 1; cursor: pointer; transition: all 0.3s; }
.stat-card:hover { transform: translateY(-2px); }
.stat-content { text-align: center; }
.stat-value { font-size: 24px; font-weight: bold; color: #409EFF; }
.stat-card.active .stat-value { color: #67C23A; }
.stat-card.probation .stat-value { color: #E6A23C; }
.stat-card.supervisor .stat-value { color: #409EFF; }
.stat-card.manager .stat-value { color: #F56C6C; }
.stat-label { font-size: 13px; color: #909399; margin-top: 5px; }
.filter-toolbar { display: flex; gap: 10px; margin-bottom: 20px; }
.person-detail { padding: 10px; }
</style>