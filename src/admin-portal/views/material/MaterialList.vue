<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Setting } from '@element-plus/icons-vue'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'

// 字段配置对话框
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getMaterialFields = () => getActiveFields('material')

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
const materials = ref<any[]>([
  { id: 1, materialNo: 'MAT-001', name: '螺丝刀套装', spec: '6件套', unit: '套', quantity: 50, price: 45, status: '充足' },
  { id: 2, materialNo: 'MAT-002', name: '灯泡', spec: 'LED 9W', unit: '个', quantity: 200, price: 15, status: '充足' },
  { id: 3, materialNo: 'MAT-003', name: '水龙头', spec: 'DN15', unit: '个', quantity: 20, price: 85, status: '不足' },
  { id: 4, materialNo: 'MAT-004', name: '电线', spec: '2.5平方', unit: '米', quantity: 500, price: 3.5, status: '充足' },
  { id: 5, materialNo: 'MAT-005', name: '油漆', spec: '白色', unit: '桶', quantity: 10, price: 120, status: '缺货' },
])

const dialogVisible = ref(false)
const dialogTitle = ref('新增物料')
const formRef = ref<FormInstance>()
const submitting = ref(false)
const editingId = ref<number | null>(null)

// 初始化表单
const initForm = () => {
  const data: any = {}
  getMaterialFields().forEach(f => {
    data[f.key] = f.defaultValue ?? ''
  })
  return data
}

const form = ref<any>(initForm())

// 获取表单验证规则
const getRules = (): FormRules => {
  const rules: FormRules = {}
  getMaterialFields().forEach(f => {
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
const statusMap: Record<string, string> = {
  '充足': 'success',
  '不足': 'warning',
  '缺货': 'danger',
}

// 获取状态标签类型
const getStatusType = (status: string) => statusMap[status] || 'info'

// 打开新增对话框
const handleAdd = () => {
  dialogTitle.value = '新增物料'
  editingId.value = null
  form.value = initForm()
  dialogVisible.value = true
}

// 打开编辑对话框
const handleEdit = (row: any) => {
  dialogTitle.value = '编辑物料'
  editingId.value = row.id
  form.value = { ...row }
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    submitting.value = true
    
    if (editingId.value) {
      const index = materials.value.findIndex(m => m.id === editingId.value)
      if (index !== -1) {
        materials.value[index] = { ...form.value, id: editingId.value }
      }
      ElMessage.success('物料更新成功')
    } else {
      const newId = Math.max(...materials.value.map(m => m.id), 0) + 1
      materials.value.unshift({ ...form.value, id: newId })
      ElMessage.success('物料创建成功')
    }
    
    dialogVisible.value = false
  } catch (error) {
    // 表单验证失败
  } finally {
    submitting.value = false
  }
}

// 删除物料
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除物料"${row.name}"吗？此操作不可恢复！`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    const index = materials.value.findIndex(m => m.id === row.id)
    if (index !== -1) {
      materials.value.splice(index, 1)
    }
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 渲染单元格
const renderCell = (row: any, field: FieldConfig) => {
  const value = row[field.key]
  
  if (field.type === 'number' && value !== undefined) {
    return typeof field.defaultValue === 'number' ? value : `¥${value}`
  }
  
  if (field.key === 'status') {
    return value
  }
  
  return value ?? '-'
}
</script>

<template>
  <div class="material-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>物料管理</span>
          <div class="header-actions">
            <el-button type="default" @click="openFieldConfig">
              <el-icon><Setting /></el-icon>配置字段
            </el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增物料
            </el-button>
          </div>
        </div>
      </template>
      
      <el-table :data="materials" v-loading="loading" stripe>
        <el-table-column 
          v-for="field in getMaterialFields()" 
          :key="'col-' + refreshKey + '-' + field.id"
          :prop="field.key" 
          :label="field.name"
          :width="field.width"
          :align="field.align || 'left'"
        >
          <template #default="{ row }">
            <span v-if="field.key === 'status'">
              <el-tag :type="getStatusType(row[field.key])" size="small">
                {{ row[field.key] }}
              </el-tag>
            </span>
            <span v-else>{{ renderCell(row, field) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
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
      class="material-dialog"
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
            v-for="field in getMaterialFields()" 
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
      module="material" 
      module-name="物料管理" 
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.material-page {
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
.material-dialog .el-dialog__body {
  padding-top: 20px;
}
</style>
