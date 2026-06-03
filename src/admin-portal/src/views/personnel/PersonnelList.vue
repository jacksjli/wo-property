<script setup lang="ts">
import { ref, computed, onMounted, watch, getCurrentInstance } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting, User, UserFilled, Link, Phone, Postcard, Calendar, Upload, Download } from '@element-plus/icons-vue'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import * as XLSX from 'xlsx';
import { personApi } from '@/api/person'
import { masterApi } from '@/api/http'
import { ticketTypeApi } from '@/api/ticketType'
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
  areaIds: number[]; // 负责区域列表
  buildingIds: number[]; // 负责楼栋列表
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
const departments = ref<Department[]>([])
const loadDepartments = async () => {
  try {
    const r: any = await masterApi.get('/departments')
    if (r.success && r.data) departments.value = Array.isArray(r.data) ? r.data : (r.data.data || [])
  } catch {}
}

// 区域列表（用于人员负责区域配置）
const areas = ref<any[]>([])
const loadAreas = async () => {
  try {
    const r: any = await masterApi.get('/hierarchy/areas')
    if (r.success) areas.value = r.data || []
  } catch {}
}

// 楼栋列表（按区域存储，key=areaId）
const buildingsByArea = ref<Record<number, any[]>>({})

const loadBuildingsForArea = async (areaId: number) => {
  if (buildingsByArea.value[areaId]) return
  try {
    const r: any = await masterApi.get(`/hierarchy/area-buildings?areaId=${areaId}`)
    if (r.success && r.data?.buildings) {
      buildingsByArea.value[areaId] = r.data.buildings
    }
  } catch {}
}

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
    return (a.name || '').localeCompare(b.name || '')
  })
})

const getRoleLevel = (role?: PersonnelRole | string) => {
  const levels: Record<string, number> = {
    operator: 1, supervisor: 2, manager: 3, department_head: 4, company_head: 5
  }
  return levels[role || ''] ?? 0
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
  salary: 0, ticketTypeIds: [] as number[], specialtyIds: [] as number[], areaIds: [] as number[], buildingIds: [] as number[], remark: '',
    status: 'probation', isSupervisor: false, maxConcurrentTickets: 5, avatar: '',
    backups: '', emergencyContactName: '', emergencyContactPhone: '', emergencyContactRelationship: ''
})

// 监听区域变化，加载对应楼栋
watch(() => form.value.areaIds, async (newAreaIds) => {
  if (!newAreaIds || newAreaIds.length === 0) {
    form.value.buildingIds = []
    return
  }
  // 加载所有选中区域的楼栋（并行）
  await Promise.all(newAreaIds.map(areaId => loadBuildingsForArea(areaId)))
  // 过滤掉不属于已选区域的楼栋
  const validBuildingIds = form.value.buildingIds.filter(bId =>
    Object.values(buildingsByArea.value).some(bs => bs.some(b => b.id === bId))
  )
  form.value.buildingIds = validBuildingIds
})

// 选项
const genderOptions = Object.entries(genderLabels).map(([value, label]) => ({ value, label }))
const educationOptions = Object.entries(educationLabels).map(([value, label]) => ({ value, label }))
const roleOptions = Object.entries(roleLabels).map(([value, label]) => ({ value, label }))
const statusOptions = Object.entries(statusLabels).map(([value, label]) => ({ value, label }))
const employmentTypeOptions = Object.entries(employmentTypeLabels).map(([value, label]) => ({ value, label }))

// 工单类型和工种
const ticketTypes = ref<any[]>([])
const filteredJobTypes = ref<any[]>([]) // 当前选中工单类型的工种（用于编辑表单）
const allJobTypes = ref<any[]>([]) // 全量工种（用于表格渲染）
const selectedTicketTypeIds = ref<number[]>([])

const loadTicketTypes = async () => {
  try {
    const r: any = await ticketTypeApi.getAll()
    if (r.success) ticketTypes.value = r.data || []
  } catch {}
}

// 加载所有工种（初始化时加载，用于表格渲染）
const loadAllJobTypes = async () => {
  try {
    const r: any = await masterApi.get('/job-types', { params: { page: 1, pageSize: 500 } })
    if (r.success) allJobTypes.value = r.data?.items || r.data || []
  } catch { allJobTypes.value = [] }
}

// 加载当前选中工单类型的工种（用于编辑表单）
const loadFilteredJobTypes = async () => {
  filteredJobTypes.value = []
  if (!selectedTicketTypeIds.value.length) return
  const all: any[] = []
  for (const ttid of selectedTicketTypeIds.value) {
    try {
      const r: any = await masterApi.get('/job-types', { params: { ticketTypeId: ttid, status: 'Active', page: 1, pageSize: 500 } })
      if (r.success) all.push(...(r.data?.items || r.data || []))
    } catch {}
  }
  filteredJobTypes.value = all
}

// 按工单类型分组工种
const groupedJobTypes = computed(() => {
  const groups: Record<number, { name: string; items: any[] }> = {}
  for (const tt of ticketTypes.value) {
    if (selectedTicketTypeIds.value.includes(tt.id)) {
      const types = filteredJobTypes.value.filter(jt => jt.ticket_type_id === tt.id)
      if (types.length) groups[tt.id] = { name: tt.name, items: types }
    }
  }
  return groups
})

watch(selectedTicketTypeIds, async () => {
  await loadFilteredJobTypes()
  const allowed = new Set(filteredJobTypes.value.map(j => j.id))
  form.value.specialtyIds = form.value.specialtyIds.filter((id: number) => allowed.has(id))
})

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
    const res: any = await personApi.getList({ ...params })
    if (res.success) {
      const items = Array.isArray(res.data) ? res.data : (res.data?.items || [])
      personnelList.value = items.map((p: any) => ({
        id: p.id,
        employeeNo: p.employeeNo,
        name: p.name,
        avatar: p.avatar,
        gender: p.gender,
        birthday: p.birthday,
        idCard: p.idCard,
        phone: p.phone,
        email: p.email,
        address: p.address,
        education: p.education,
        role: p.role,
        departmentId: p.departmentId,
        departmentName: p.departmentName,
        position: p.position,
        employmentType: p.employmentType,
        hireDate: p.hireDate,
        status: p.status,
        // JSON 字段
        ticketTypeIds: p.ticketTypeIds ? JSON.parse(p.ticketTypeIds) : [],
        specialtyIds: p.specialtyIds ? JSON.parse(p.specialtyIds) : [],
        areaIds: p.areaIds ? JSON.parse(p.areaIds) : [],
        buildingIds: p.buildingIds ? JSON.parse(p.buildingIds) : [],
        backups: p.backups ? (typeof p.backups === 'string' ? JSON.parse(p.backups) : p.backups) : []
      }))
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

// 获取工种名称列表（优先从 filteredJobTypes 查找，回退到 allJobTypes）
const getJobTypeNames = (ids: number[]) => {
  if (!ids || !ids.length) return []
  return ids.map((id: number) => {
    const found = filteredJobTypes.value.find(j => j.id === id)
    if (found) return found.name
    const foundInAll = allJobTypes.value.find(j => j.id === id)
    return foundInAll?.name || null
  }).filter(Boolean)
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
    contractStart: '', contractEnd: '', salary: 0, ticketTypeIds: [], specialtyIds: [], areaIds: [], buildingIds: [], remark: '',
    status: 'probation', isSupervisor: false, maxConcurrentTickets: 5, avatar: '',
    backups: '', emergencyContactName: '', emergencyContactPhone: '', emergencyContactRelationship: ''
  }
  selectedTicketTypeIds.value = []
  filteredJobTypes.value = []
  dialogVisible.value = true
}

// 打开编辑
const handleEdit = async (row: Personnel) => {
  dialogTitle.value = '编辑人员'
  editingId.value = row.id
  // 从 API 获取最新完整数据，避免列表数据字段缺失
  const res: any = await personApi.get(row.id)
  if (!res.success) { ElMessage.error('获取人员信息失败'); return }
  const p = res.data
  form.value = {
    employeeNo: p.employeeNo || '', name: p.name || '', gender: p.gender || 'male',
    birthday: p.birthday || '', idCard: p.idCard || '', phone: p.phone || '',
    email: p.email || '', address: p.address || '', education: p.education || 'bachelor',
    graduateSchool: p.graduateSchool || '', major: p.major || '',
    role: p.role || 'operator', departmentId: p.departmentId || 1, position: p.position || '',
    employmentType: p.employmentType || 'full_time', hireDate: p.hireDate || '',
    contractStart: p.contractStart || '', contractEnd: p.contractEnd || '',
    salary: p.salary || 0,
    ticketTypeIds: p.ticketTypeIds ? JSON.parse(p.ticketTypeIds) : [],
    specialtyIds: p.specialtyIds ? JSON.parse(p.specialtyIds) : [],
    areaIds: p.areaIds ? JSON.parse(p.areaIds) : [],
    buildingIds: p.buildingIds ? JSON.parse(p.buildingIds) : [],
    remark: p.remark || '',
    status: p.status || 'probation',
    isSupervisor: p.isSupervisor ?? false,
    maxConcurrentTickets: p.maxConcurrentTickets ?? 5,
    avatar: p.avatar || '',
    backups: p.backups ? (typeof p.backups === 'string' ? JSON.parse(p.backups) : p.backups) : [],
    emergencyContactName: p.emergencyContactName || '',
    emergencyContactPhone: p.emergencyContactPhone || '',
    emergencyContactRelationship: p.emergencyContactRelationship || ''
  }
  selectedTicketTypeIds.value = [...(form.value.ticketTypeIds || [])]
  
  // 若工单类型为空但专业技能有值，通过工种反向查找工单类型
  if (!selectedTicketTypeIds.value.length && form.value.specialtyIds.length) {
    try {
      const ids = form.value.specialtyIds.join(',')
      const r: any = await masterApi.get(`/job-types/by-ids?ids=${ids}`)
      if (r.success && r.data?.ticketTypeIds?.length) {
        selectedTicketTypeIds.value = r.data.ticketTypeIds
      }
    } catch {}
  }

  await loadFilteredJobTypes()

  // 过滤：只保留属于当前工单类型的工种
  const allowed = new Set(filteredJobTypes.value.map((j: any) => j.id))
  form.value.specialtyIds = form.value.specialtyIds.filter((id: number) => allowed.has(id))

  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  const instance = getCurrentInstance()
  const $msg = instance?.appContext.config.globalProperties.$message || ElMessage
  if (!form.value.name?.trim()) { $msg.warning('请输入姓名'); return }
  if (!form.value.phone?.trim()) { $msg.warning('请输入联系电话'); return }


  const department = departments.value.find(d => d.id === form.value.departmentId)
  try {
    // Build clean payload - only send fields that exist in Personnel table
    const payload: Record<string, any> = {
      Name: form.value.name,
      Phone: form.value.phone,
      Gender: form.value.gender,
      EmployeeNo: form.value.employeeNo,
      Birthday: form.value.birthday || null,
      IdCard: form.value.idCard || null,
      Email: form.value.email || null,
      Address: form.value.address || null,
      Education: form.value.education || null,
      GraduateSchool: form.value.graduateSchool || null,
      Major: form.value.major || null,
      Role: form.value.role,
      DepartmentId: form.value.departmentId,
      DepartmentName: department?.name || null,
      Position: form.value.position || null,
      EmploymentType: form.value.employmentType,
      HireDate: form.value.hireDate || null,
      ContractStart: form.value.contractStart || null,
      ContractEnd: form.value.contractEnd || null,
      Salary: form.value.salary || null,
      Remark: form.value.remark || null,
      TicketTypeIds: form.value.ticketTypeIds?.length ? JSON.stringify(form.value.ticketTypeIds) : null,
      SpecialtyIds: form.value.specialtyIds?.length ? JSON.stringify(form.value.specialtyIds) : null,
      AreaIds: form.value.areaIds?.length ? JSON.stringify(form.value.areaIds) : null,
      BuildingIds: form.value.buildingIds?.length ? JSON.stringify(form.value.buildingIds) : null,
      Status: form.value.status || 'probation',
      IsSupervisor: form.value.isSupervisor || false,
      MaxConcurrentTickets: form.value.maxConcurrentTickets || 5,
      Avatar: form.value.avatar || null,
      Backups: form.value.backups || null,
      EmergencyContactName: form.value.emergencyContactName || null,
      EmergencyContactPhone: form.value.emergencyContactPhone || null,
      EmergencyContactRelationship: form.value.emergencyContactRelationship || null,
    }

    if (editingId.value) {
      await personApi.update(editingId.value, payload)
      $msg.success('更新成功')
    } else {
      await personApi.create(payload)
      $msg.success('添加成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (e: any) { $msg.error(e.message || '操作失败') }
}

// 删除
const handleDelete = async (row: Personnel) => {
  if (!row?.id) {
    console.error('[handleDelete] row.id is invalid:', row?.id)
    ElMessage.error('删除失败：无效的人员ID')
    return
  }
  try {
    await ElMessageBox.confirm(`确定删除人员 "${row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning'
    })
    await personApi.delete(row.id)
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

const getDepartmentName = (id: number) => departments.value.find(d => d.id === id)?.name || '-'

// 批量导入
const importDialogVisible = ref(false)
const importLoading = ref(false)
const importResult = ref<{ success: number; failed: number; skipped: number; errors: string[] } | null>(null)

const handleDownloadTemplate = () => {
  // 生成导入模板
  const templateData = [
    {
      '姓名': '',
      '电话': '',
      '性别': 'male/female/other',
      '角色': 'operator/supervisor/manager',
      '工号': '',
      '部门名称': '',
      '职位': '',
      '入职日期': 'YYYY-MM-DD',
      '工单类型ID': '[1,2]',
      '专业技能ID': '[9,10]'
    }
  ]
  const ws = XLSX.utils.json_to_sheet(templateData)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, '人员导入模板')
  XLSX.writeFile(wb, '人员导入模板.xlsx')
}

const handleExport = async () => {
  try {
    ElMessage.info('正在导出...')
    const res: any = await personApi.exportPersons()
    if (res.success && res.data) {
      const exportData = res.data.map((p: any) => ({
        '姓名': p.姓名,
        '工号': p.工号,
        '性别': p.性别,
        '手机': p.手机,
        '邮箱': p.邮箱,
        '角色': p.角色,
        '部门': p.部门,
        '职位': p.职位,
        '状态': p.状态,
        '入职日期': p.入职日期,
        '专业技能': p.专业技能
      }))
      const ws = XLSX.utils.json_to_sheet(exportData)
      const wb = XLSX.utils.book_new()
      XLSX.utils.book_append_sheet(wb, ws, '人员列表')
      XLSX.writeFile(wb, `人员列表_${new Date().toISOString().split('T')[0]}.xlsx`)
      ElMessage.success('导出成功')
    } else {
      ElMessage.error(res.message || '导出失败')
    }
  } catch (e: any) { ElMessage.error(e.message || '导出失败') }
}

const handleFileChange = async (file: any) => {
  if (!file) return
  importLoading.value = true
  importResult.value = null
  
  try {
    const reader = new FileReader()
    reader.onload = async (e) => {
      try {
        const data = new Uint8Array(reader.result as ArrayBuffer)
        const workbook = XLSX.read(data, { type: 'array' })
        const sheetName = workbook.SheetNames[0]
        const worksheet = workbook.Sheets[sheetName]
        const jsonData = XLSX.utils.sheet_to_json(worksheet, { defval: '' })
        
        // 转换数据
        const rows = jsonData.map((row: any, index: number) => {
          // 跳过表头
          if (index === 0 && row['姓名'] === '姓名') return null
          
          // 校验必填字段
          if (!row['姓名'] || !row['电话']) {
            return { _error: `第${index + 1}行: 姓名和电话为必填字段`, _row: row }
          }
          
          return {
            name: row['姓名'] || '',
            phone: row['电话'] || '',
            gender: row['性别'] || 'male',
            role: row['角色'] || 'operator',
            employeeNo: row['工号'] || '',
            departmentName: row['部门名称'] || '',
            position: row['职位'] || '',
            hireDate: row['入职日期'] || null,
            ticketTypeIds: row['工单类型ID'] || null,
            specialtyIds: row['专业技能ID'] || null,
          }
        }).filter((r: any) => r !== null)
        
        // 调用后端导入
        const res: any = await personApi.importExcel({ rows })
        if (res.success) {
          importResult.value = res.data
          ElMessage.success(`导入完成: 成功${res.data.success}条, 跳过${res.data.skipped}条, 失败${res.data.failed}条`)
          if (res.data.failed > 0) {
            ElMessage.warning(`失败原因: ${res.data.errors?.join('; ')}`)
          }
          loadData()
        } else {
          ElMessage.error(res.message || '导入失败')
        }
      } catch (err: any) {
        ElMessage.error('解析Excel失败: ' + err.message)
      } finally {
        importLoading.value = false
      }
    }
    reader.readAsArrayBuffer(file.raw || file)
  } catch (err: any) {
    ElMessage.error('读取文件失败: ' + err.message)
    importLoading.value = false
  }
}

const openImportDialog = () => {
  importResult.value = null
  importDialogVisible.value = true
}


const handleRefresh = () => { loadData(); ElMessage.success('已刷新') }
const handleReset = () => { filterRole.value = ''; filterDepartment.value = ''; filterStatus.value = '' }
const getStatusType = (status: PersonnelStatus) => {
  const map: Record<PersonnelStatus, string> = {
    active: 'success', inactive: 'info', on_leave: 'warning', probation: 'primary', resigned: 'danger'
  }
  return map[status]
}

onMounted(() => { loadData(); loadTicketTypes(); loadDepartments(); loadAllJobTypes(); loadAreas() })
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
            <el-button @click="openImportDialog"><el-icon><Upload /></el-icon> 批量导入</el-button>
            <el-button type="success" @click="handleExport"><el-icon><Download /></el-icon> 导出
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
        <el-table-column prop="specialtyIds" label="专业技能" min-width="180">
          <template #default="{ row }">
            <el-tag v-for="name in getJobTypeNames(row.specialtyIds).slice(0, 2)" :key="name" size="small" style="margin-right: 4px;">{{ name }}</el-tag>
            <span v-if="getJobTypeNames(row.specialtyIds).length > 2" style="color: #909399; font-size: 12px;">+{{ getJobTypeNames(row.specialtyIds).length - 2 }}</span>
          </template>
        </el-table-column>
        <el-table-column label="负责区域" width="120" align="center">
          <template #default="{ row }">
            <span v-if="row.areaIds?.length">{{ row.areaIds.length }}个区域</span>
            <span v-else style="color: #909399">未配置</span>
          </template>
        </el-table-column>
        <el-table-column label="负责楼栋" width="120" align="center">
          <template #default="{ row }">
            <span v-if="row.buildingIds?.length">{{ row.buildingIds.length }}个楼栋</span>
            <span v-else style="color: #909399">全部</span>
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
          <el-col :span="12"><el-form-item label="身份证号"><el-input v-model="form.idCard" placeholder="请输入身份证号" /></el-form-item></el-col>
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
        <el-form-item label="工单类型">
          <el-select v-model="selectedTicketTypeIds" multiple placeholder="选择工单类型（可多选）" style="width: 100%" @change="loadFilteredJobTypes">
            <el-option v-for="tt in ticketTypes" :key="tt.id" :label="tt.name" :value="tt.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="专业技能">
          <div v-if="!selectedTicketTypeIds.length" style="color:#999;font-size:13px;">请先选择工单类型</div>
          <div v-else class="skill-groups">
            <div v-for="(group, ttId) in groupedJobTypes" :key="ttId" class="skill-group">
              <div class="skill-group-title">{{ group.name }}</div>
              <el-checkbox-group v-model="form.specialtyIds">
                <el-checkbox v-for="jt in group.items" :key="jt.id" :value="jt.id" style="margin-right:8px;margin-bottom:4px;">{{ jt.name }}</el-checkbox>
              </el-checkbox-group>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="负责区域">
          <el-select v-model="form.areaIds" multiple placeholder="选择负责区域（可多选）" style="width: 100%">
            <el-option v-for="a in areas" :key="a.id" :label="a.name" :value="a.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责楼栋" v-if="form.areaIds.length">
          <el-select v-model="form.buildingIds" multiple placeholder="选择负责楼栋（可多选，不选则该区域下所有楼栋）" style="width: 100%">
            <el-option-group v-for="areaId in form.areaIds" :key="areaId" :label="areas.find(a=>a.id===areaId)?.name">
              <el-option v-for="b in (buildingsByArea[areaId] || [])" :key="b.id" :label="b.name" :value="b.id" />
            </el-option-group>
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
          <span v-if="!(viewingPerson.specialties?.length)" style="color: #909399;">暂无</span>
        </div>
        <h4 style="margin-top: 20px;">备份人员</h4>
        <div v-if="(viewingPerson.backups?.length)" style="display: flex; flex-wrap: wrap; gap: 8px;">
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

    <!-- 批量导入对话框 -->
    <el-dialog v-model="importDialogVisible" title="批量导入人员" width="600px">
      <div style="margin-bottom: 20px;">
        <h4>导入说明：</h4>
        <ul style="color: #666; font-size: 13px; line-height: 1.8;">
          <li>请先下载导入模板，按模板格式填写数据</li>
          <li>姓名和电话为必填字段，其他为选填</li>
          <li>手机号重复的数据会自动覆盖</li>
          <li>必填字段为空的数据会自动跳过</li>
        </ul>
        <el-button type="primary" link @click="handleDownloadTemplate" style="margin: 10px 0;">
          <el-icon><Download /></el-icon> 下载导入模板
        </el-button>
      </div>
      
      <el-upload
        ref="uploadRef"
        :auto-upload="false"
        :limit="1"
        accept=".xlsx,.xls"
        :on-change="handleFileChange"
        style="margin-bottom: 20px;">
        <el-button type="default">选择Excel文件</el-button>
      </el-upload>
      
      <el-divider v-if="importResult" />
      
      <div v-if="importResult" style="background: #f5f7fa; padding: 15px; border-radius: 4px;">
        <h4>导入结果：</h4>
        <el-descriptions :column="1" border size="small">
          <el-descriptions-item label="成功">{{ importResult.success }} 条</el-descriptions-item>
          <el-descriptions-item label="跳过">{{ importResult.skipped }} 条</el-descriptions-item>
          <el-descriptions-item label="失败">{{ importResult.failed }} 条</el-descriptions-item>
        </el-descriptions>
        <div v-if="importResult.errors?.length" style="margin-top: 10px;">
          <el-alert type="error" :closable="false">
            <template #title>
              失败原因：{{ importResult.errors.join('; ') }}
            </template>
          </el-alert>
        </div>
      </div>
      
      <template #footer>
        <el-button @click="importDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
    
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