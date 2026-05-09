<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Delete, Edit, Plus, Rank } from '@element-plus/icons-vue'
import { 
  getModuleFields, 
  addField, 
  updateField, 
  deleteField, 
  toggleFieldStatus,
  autoGenerateKey,
  fieldTypeOptions,
  fieldConfigs,
  type FieldConfig,
  type FieldType
} from '@/stores/fieldConfig'

const props = defineProps<{
  module: string        // 模块标识
  moduleName: string     // 模块名称（如：工单管理）
}>()

const emit = defineEmits<{
  (e: 'update'): void
}>()

const dialogVisible = ref(false)
const dialogTitle = computed(() => `${props.moduleName} - 字段配置`)
const fields = ref<FieldConfig[]>([])

// 拖拽相关
const dragIndex = ref<number | null>(null)
const dragOverIndex = ref<number | null>(null)

// 表单
const formRef = ref<FormInstance>()
const form = ref<Partial<FieldConfig>>({
  name: '',
  key: '',
  type: 'text',
  defaultValue: '',
  required: false,
  width: 100,
  align: 'left',
  options: []
})
const editingId = ref<number | null>(null)

// 验证规则
const rules: FormRules = {
  name: [{ required: true, message: '请输入字段名称', trigger: 'blur' }],
}

// 监听dialog打开
watch(dialogVisible, (val) => {
  if (val) {
    loadFields()
  }
})

// 加载字段
const loadFields = () => {
  console.log('🔍 loadFields called for module:', props.module)
  const loadedFields = getModuleFields(props.module)
  console.log('🔍 Loaded fields count:', loadedFields.length)
  console.log('🔍 Loaded fields:', loadedFields.map(f => `${f.name} (${f.status})`))
  fields.value = loadedFields
}

// 自动生成key
const handleNameInput = () => {
  if (form.value.name && !editingId.value) {
    form.value.key = autoGenerateKey(form.value.name)
  }
}

// 新增
const handleAdd = () => {
  editingId.value = null
  form.value = {
    name: '',
    key: '',
    type: 'text',
    defaultValue: '',
    required: false,
    width: 100,
    align: 'left',
    options: []
  }
}

// 编辑
const handleEdit = (row: FieldConfig) => {
  editingId.value = row.id
  form.value = { ...row, options: row.options ? [...row.options] : [] }
}

// 删除
const handleDelete = async (row: FieldConfig) => {
  try {
    // 即使是必填字段也允许删除，但给出更强警告
    const message = row.required 
      ? `确定要删除必填字段「${row.name}」吗？\n\n⚠️ 警告：这是必填字段，删除后可能影响数据完整性！`
      : `确定要删除字段「${row.name}」吗？`
    
    await ElMessageBox.confirm(
      message,
      '删除确认',
      { 
        confirmButtonText: '确定删除', 
        cancelButtonText: '取消', 
        type: row.required ? 'error' : 'warning',
        confirmButtonClass: row.required ? 'el-button--danger' : ''
      }
    )
    deleteField(props.module, row.id)
    loadFields()
    emit('update')
    ElMessage.success('删除成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 切换启用状态
const handleToggle = (row: FieldConfig) => {
  toggleFieldStatus(props.module, row.id)
  loadFields()
  emit('update')
}

// 拖拽排序
const handleDragStart = (index: number) => {
  dragIndex.value = index
}

const handleDragOver = (index: number, event: DragEvent) => {
  event.preventDefault()
  dragOverIndex.value = index
}

const handleDrop = (index: number) => {
  if (dragIndex.value === null || dragIndex.value === index) {
    dragIndex.value = null
    dragOverIndex.value = null
    return
  }

  // 获取当前模块的字段配置
  const moduleFields = fieldConfigs.value[props.module]
  if (!moduleFields) return

  // 移动字段
  const movedField = moduleFields.splice(dragIndex.value, 1)[0]
  moduleFields.splice(index, 0, movedField)

  // 重新编号
  moduleFields.forEach((f, i) => {
    f.id = i + 1
  })

  dragIndex.value = null
  dragOverIndex.value = null
  loadFields()
  emit('update')
  ElMessage.success('字段顺序已更新')
}

const handleDragEnd = () => {
  dragIndex.value = null
  dragOverIndex.value = null
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
    
    // 二次确认
    const confirmed = await ElMessageBox.confirm(
      `确定要${editingId.value ? '更新' : '新增'}字段「${form.value.name}」吗？\n\n配置将立即生效。`,
      '确认保存',
      {
        confirmButtonText: '确定保存',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    // 处理options（如果是select类型）
    if (form.value.type !== 'select') {
      form.value.options = []
    }
    
    if (editingId.value) {
      updateField(props.module, editingId.value, form.value)
      ElMessage.success('字段更新成功')
    } else {
      addField(props.module, form.value as any)
      ElMessage.success('字段新增成功')
    }
    
    loadFields()
    emit('update')
    handleAdd() // 重置表单
  } catch (error: any) {
    if (error !== 'cancel') {
      // 可能是验证失败
    }
  }
}

// 打开对话框
const open = () => {
  dialogVisible.value = true
}

// 暴露方法
defineExpose({ open })

// 获取字段类型标签
const getFieldTypeLabel = (type: string) => {
  return fieldTypeOptions.find(t => t.value === type)?.label || type
}
</script>

<template>
  <el-dialog
    v-model="dialogVisible"
    :title="dialogTitle"
    width="850px"
    :close-on-click-modal="false"
    class="field-config-dialog"
  >
    <!-- 说明 -->
    <el-alert
      title="拖动字段可调整显示顺序，勾选可启用/停用字段"
      type="info"
      :closable="false"
      show-icon
      style="margin-bottom: 15px;"
    />
    
    <!-- 字段列表 -->
    <div class="field-list">
      <div class="field-list-header">
        <span class="col-drag"></span>
        <span class="col-name">字段名称</span>
        <span class="col-key">标识</span>
        <span class="col-type">类型</span>
        <span class="col-width">宽度</span>
        <span class="col-required">必填</span>
        <span class="col-status">显示</span>
        <span class="col-actions">操作</span>
      </div>
      
      <div 
        v-for="(field, index) in fields" 
        :key="field.id"
        class="field-item"
        :class="{ 
          'is-inactive': field.status === 'Inactive',
          'is-drag-over': dragOverIndex === index,
          'is-dragging': dragIndex === index
        }"
        draggable="true"
        @dragstart="handleDragStart(index)"
        @dragover="handleDragOver(index, $event)"
        @drop="handleDrop(index)"
        @dragend="handleDragEnd"
      >
        <span class="col-drag">
          <el-icon class="drag-handle"><Rank /></el-icon>
        </span>
        <span class="col-name">
          {{ field.name }}
          <el-tag v-if="field.required" type="danger" size="small" style="margin-left: 5px;">必填</el-tag>
        </span>
        <span class="col-key">{{ field.key }}</span>
        <span class="col-type">
          <el-tag size="small">{{ getFieldTypeLabel(field.type) }}</el-tag>
        </span>
        <span class="col-width">{{ field.width }}px</span>
        <span class="col-required">
          <el-tag :type="field.required ? 'success' : 'info'" size="small">
            {{ field.required ? '是' : '否' }}
          </el-tag>
        </span>
        <span class="col-status">
          <el-checkbox 
            :model-value="field.status === 'Active'" 
            @change="handleToggle(field)"
          />
        </span>
        <span class="col-actions">
          <el-button link type="primary" size="small" @click="handleEdit(field)">
            <el-icon><Edit /></el-icon>
          </el-button>
          <el-button link type="danger" size="small" @click="handleDelete(field)">
            <el-icon><Delete /></el-icon>
          </el-button>
        </span>
      </div>
      
      <div v-if="fields.length === 0" class="empty-tip">
        暂无字段配置，请点击下方"新增字段"添加
      </div>
    </div>
    
    <!-- 新增/编辑表单 -->
    <el-divider content-position="left">
      <span v-if="editingId">编辑字段</span>
      <span v-else>新增字段</span>
    </el-divider>
    
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" class="field-form">
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="字段名称" prop="name">
            <el-input v-model="form.name" placeholder="如：设备编号" @input="handleNameInput" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="字段标识">
            <el-input v-model="form.key" placeholder="自动生成" disabled />
          </el-form-item>
        </el-col>
      </el-row>
      
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="字段类型">
            <el-select v-model="form.type" placeholder="请选择" style="width: 100%">
              <el-option v-for="opt in fieldTypeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="默认值">
            <el-input v-model="form.defaultValue" placeholder="请输入默认值" />
          </el-form-item>
        </el-col>
      </el-row>
      
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="表格列宽">
            <el-input-number v-model="form.width" :min="60" :max="300" style="width: 100%" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="对齐方式">
            <el-select v-model="form.align" placeholder="请选择" style="width: 100%">
              <el-option label="左对齐" value="left" />
              <el-option label="居中" value="center" />
              <el-option label="右对齐" value="right" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
      
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="必填字段">
            <el-switch v-model="form.required" />
          </el-form-item>
        </el-col>
        <el-col :span="12" v-if="form.type === 'select'">
          <el-form-item label="选项">
            <el-input 
              :value="form.options?.join(',')" 
              placeholder="用逗号分隔，如: 选项1,选项2,选项3" 
              @input="form.options = $event.split(',')" 
            />
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    
    <template #footer>
      <el-button @click="dialogVisible = false">关闭</el-button>
      <el-button @click="handleAdd">重置</el-button>
      <el-button type="primary" @click="handleSubmit">
        {{ editingId ? '保存修改' : '新增字段' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.field-list {
  border: 1px solid #ebeef5;
  border-radius: 8px;
  overflow: hidden;
  margin-bottom: 20px;
}

.field-list-header {
  display: flex;
  background: #f5f7fa;
  padding: 12px 16px;
  font-weight: 600;
  color: #606266;
  font-size: 13px;
}

.field-item {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  border-top: 1px solid #ebeef5;
  transition: background 0.2s, transform 0.1s;
  cursor: grab;
  user-select: none;
}

.field-item:hover {
  background: #f5f7fa;
}

.field-item:active {
  cursor: grabbing;
}

.field-item.is-inactive {
  opacity: 0.6;
  background: #fafafa;
}

.field-item.is-drag-over {
  background: #e8f4ff;
  border-top: 2px solid #409eff;
}

.field-item.is-dragging {
  opacity: 0.5;
  background: #f0f9eb;
}

.col-drag {
  width: 40px;
  text-align: center;
}

.drag-handle {
  color: #c0c4cc;
  font-size: 16px;
  transition: color 0.2s;
}

.field-item:hover .drag-handle {
  color: #409eff;
}

.col-name {
  flex: 1.5;
  min-width: 120px;
}

.col-key {
  flex: 1.2;
  min-width: 100px;
  font-family: Monaco, Menlo, monospace;
  color: #409eff;
  font-size: 12px;
}

.col-type {
  width: 80px;
}

.col-width {
  width: 70px;
  text-align: center;
}

.col-required {
  width: 60px;
  text-align: center;
}

.col-status {
  width: 60px;
  text-align: center;
}

.col-actions {
  width: 100px;
  text-align: center;
}

.empty-tip {
  padding: 40px;
  text-align: center;
  color: #909399;
}

.field-form {
  margin-top: 10px;
}
</style>

<style>
.field-config-dialog .el-dialog__body {
  padding-top: 15px;
}
</style>
