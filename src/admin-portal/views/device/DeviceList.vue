<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Setting } from '@element-plus/icons-vue'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getDevices, createDevice, updateDevice, deleteDevice } from '@/api/device'

const { isAdmin, verifyAdminPassword } = usePermission()

// 打开字段配置（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 字段配置对话框
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getDeviceFields = () => getActiveFields('device')

// 刷新字段
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

// 设备数据
const loading = ref(false)
const devices = ref<any[]>([])

// 加载设备数据
const loadDevices = async () => {
  try {
    loading.value = true
    const data = await getDevices()
    // API 返回 { success, data: [...], total, page, pageSize }
    devices.value = data?.data || data?.devices || data?.list || data || []
  } catch (error) {
    console.error('加载设备失败:', error)
    ElMessage.error('加载设备数据失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadDevices()
})

// 对话框状态
const dialogVisible = ref(false)
const dialogTitle = ref('新增设备')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

// 表单数据 - 根据字段配置初始化
const initForm = () => {
  const data: any = {}
  getDeviceFields().forEach(f => {
    data[f.key] = f.defaultValue ?? ''
  })
  return data
}

const form = ref<any>(initForm())

// 获取表单验证规则
const getRules = (): FormRules => {
  const rules: FormRules = {}
  getDeviceFields().forEach(f => {
    if (f.required) {
      const label = f.name
      let message = `请输入${label}`
      if (f.type === 'select') message = `请选择${label}`
      else if (f.type === 'date') message = `请选择${label}`
      rules[f.key] = [{ required: true, message, trigger: f.type === 'select' ? 'change' : 'blur' }]
    }
  })
  return rules
}

// 状态映射
const statusMap: Record<string, { type: string; label: string }> = {
  Active: { type: 'success', label: '运行中' },
  Maintenance: { type: 'warning', label: '维修中' },
  Inactive: { type: 'info', label: '停用' },
  Scrapped: { type: 'danger', label: '已报废' },
  Normal: { type: 'success', label: '正常' },
  Warning: { type: 'warning', label: '预警' },
  Fault: { type: 'danger', label: '故障' },
}

// 获取状态显示
const getStatusInfo = (fieldKey: string, value: string) => {
  if (statusMap[value]) {
    return statusMap[value]
  }
  return { type: 'info', label: value || '-' }
}

// 打开新增对话框
const handleAdd = () => {
  dialogTitle.value = '新增设备'
  editingId.value = null
  form.value = initForm()
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑设备'
  editingId.value = row.id
  form.value = { ...row }
  dialogVisible.value = true
}

// 查看详情
const handleView = (row: any) => {
  handleEdit(row)
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    submitting.value = true
    
    // 构建 PascalCase payload（与后端 DeviceService API 一致）
    const payload = {
      Code: form.value.code,
      Name: form.value.name,
      DeviceTypeId: form.value.deviceTypeId ? parseInt(form.value.deviceTypeId) : null,
      BuildingId: form.value.buildingId ? parseInt(form.value.buildingId) : null,
      Floor: form.value.floor ? parseInt(form.value.floor) : null,
      Location: form.value.location || null,
      PurchaseDate: form.value.purchaseDate || null,
      Status: form.value.status || 'Active',
      Remarks: form.value.remarks || null,
    }
    
    if (editingId.value) {
      await updateDevice(editingId.value, payload)
      ElMessage.success('设备更新成功')
    } else {
      await createDevice(payload)
      ElMessage.success('设备创建成功')
    }
    
    dialogVisible.value = false
    loadDevices()
  } catch (error: any) {
    ElMessage.error(error?.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

// 删除设备
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除设备"${row.name}"吗？此操作不可恢复！`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    await deleteDevice(row.id)
    await loadDevices()
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error?.message || '删除失败')
    }
  }
}

// 格式化日期
const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('zh-CN')
}

// 渲染单元格内容
const renderCell = (row: any, field: FieldConfig) => {
  const value = row[field.key]
  
  if (field.type === 'date' && value) {
    return formatDate(value)
  }
  
  if (field.key === 'status' || field.key === 'currentStatus') {
    const info = getStatusInfo(field.key, value)
    return info.label
  }
  
  return value ?? '-'
}
</script>

<template>
  <div class="device-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>设备管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增设备
            </el-button>
          </div>
        </div>
      </template>
      
      <el-table :data="devices" v-loading="loading" stripe>
        <el-table-column 
          v-for="field in getDeviceFields()" 
          :key="'col-' + refreshKey + '-' + field.id"
          :prop="field.key" 
          :label="field.name"
          :width="field.width"
          :align="field.align || 'left'"
        >
          <template #default="{ row }">
            <span v-if="field.key === 'status' || field.key === 'currentStatus'">
              <el-tag :type="getStatusInfo(field.key, row[field.key]).type" size="small">
                {{ getStatusInfo(field.key, row[field.key]).label }}
              </el-tag>
            </span>
            <span v-else>{{ renderCell(row, field) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">查看</el-button>
            <el-button link type="warning" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog 
      v-model="dialogVisible" 
      :title="dialogTitle" 
      width="700px" 
      :close-on-click-modal="false"
      class="device-dialog"
    >
      <el-form 
        ref="formRef" 
        :model="form" 
        :rules="getRules()" 
        label-width="100px"
      >
        <el-row :gutter="20">
          <el-col 
            :span="field.type === 'textarea' ? 24 : 12" 
            v-for="field in getDeviceFields()" 
            :key="'form-' + refreshKey + '-' + field.id"
          >
            <el-form-item 
              :label="field.name" 
              :prop="field.key"
              :required="field.required"
            >
              <!-- 下拉选择 -->
              <el-select 
                v-if="field.type === 'select'" 
                v-model="form[field.key]" 
                :placeholder="`请选择${field.name}`"
                style="width: 100%"
              >
                <el-option 
                  v-for="opt in (field.options || [])" 
                  :key="opt" 
                  :label="opt" 
                  :value="opt" 
                />
              </el-select>
              
              <!-- 日期选择 -->
              <el-date-picker 
                v-else-if="field.type === 'date'" 
                v-model="form[field.key]" 
                type="date" 
                :placeholder="`选择${field.name}`"
                style="width: 100%"
              />
              
              <!-- 数字输入 -->
              <el-input-number 
                v-else-if="field.type === 'number'" 
                v-model="form[field.key]" 
                :min="0" 
                style="width: 100%"
              />
              
              <!-- 多行文本 -->
              <el-input 
                v-else-if="field.type === 'textarea'" 
                v-model="form[field.key]" 
                type="textarea" 
                :rows="3" 
                :placeholder="`请输入${field.name}`"
              />
              
              <!-- 文本输入 -->
              <el-input 
                v-else 
                v-model="form[field.key]" 
                :placeholder="`请输入${field.name}`"
              />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog 
      ref="fieldDialogRef" 
      module="device" 
      module-name="设备管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.device-page {
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
</style>

<style>
.device-dialog .el-dialog__body {
  padding-top: 20px;
}
</style>
