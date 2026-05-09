<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Search } from '@element-plus/icons-vue'
import { masterApi } from '@/api/http'

// ============ 类型定义 ============
interface FieldDefinition {
  id?: number
  code: string
  name: string
  aliases?: string   // JSON string array
  dataType: string
  maxLength?: number | null
  isRequired: boolean
  category: string   // shared / private
  module?: string
  remark?: string
  isActive: boolean
  sort?: number
}

// ============ 数据状态 ============
const loading = ref(false)
const list = ref<FieldDefinition[]>([])
const dialogVisible = ref(false)
const dialogTitle = ref('新增字段')
const editingId = ref<number | null>(null)
const submitting = ref(false)
const filterCategory = ref('')
const filterKeyword = ref('')

// ============ 常量 ============
const dataTypeOptions = [
  { value: 'string', label: '字符串' },
  { value: 'number', label: '数字' },
  { value: 'date', label: '日期' },
  { value: 'bool', label: '布尔' }
]

const categoryOptions = [
  { value: 'shared', label: '共享字段' },
  { value: 'private', label: '私有字段' }
]

const moduleOptions = [
  { value: 'ticket', label: '工单' },
  { value: 'resident', label: '住户' },
  { value: 'visitor', label: '访客' },
  { value: 'device', label: '设备' },
  { value: 'inspection', label: '巡检' },
  { value: 'complaint', label: '投诉' },
  { value: 'contract', label: '合同' },
  { value: 'payment', label: '缴费' },
  { value: 'material', label: '物料' }
]

// ============ 计算属性 ============
const filteredList = computed(() => {
  return list.value.filter(f => {
    const matchCat = !filterCategory.value || f.category === filterCategory.value
    const matchKw = !filterKeyword.value ||
      f.code.includes(filterKeyword.value) ||
      f.name.includes(filterKeyword.value) ||
      (f.aliases || '').includes(filterKeyword.value)
    return matchCat && matchKw
  })
})

// 解析 aliases JSON 字符串为数组
const parseAliases = (aliasStr?: string): string[] => {
  if (!aliasStr) return []
  try {
    return JSON.parse(aliasStr)
  } catch {
    return []
  }
}

// 格式化 aliases 用于显示
const formatAliases = (aliasStr?: string): string => {
  const arr = parseAliases(aliasStr)
  return arr.length > 0 ? arr.join('、') : '—'
}

// ============ 加载数据 ============
const loadFields = async () => {
  loading.value = true
  try {
    const res: any = await masterApi.get('/field-definitions')
    if (res.success) {
      list.value = res.data || []
    }
  } catch {
    ElMessage.error('加载字段列表失败')
  } finally {
    loading.value = false
  }
}

// ============ 表单 ============
const defaultForm = (): FieldDefinition => ({
  code: '',
  name: '',
  aliases: '',
  dataType: 'string',
  maxLength: null,
  isRequired: false,
  category: 'shared',
  module: '',
  remark: '',
  isActive: true,
  sort: 0
})

const form = ref<FieldDefinition>(defaultForm())

// ============ 操作 ============
const handleAdd = () => {
  dialogTitle.value = '新增字段'
  editingId.value = null
  form.value = defaultForm()
  dialogVisible.value = true
}

const handleEdit = (row: FieldDefinition) => {
  dialogTitle.value = '编辑字段'
  editingId.value = row.id!
  // aliases 转为 JSON 字符串供编辑
  form.value = {
    ...row,
    aliases: row.aliases || ''
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  // 校验
  if (!form.value.code.trim()) {
    ElMessage.warning('请输入字段编码')
    return
  }
  if (!form.value.name.trim()) {
    ElMessage.warning('请输入字段名称')
    return
  }
  if (form.value.category === 'private' && !form.value.module) {
    ElMessage.warning('私有字段必须选择所属模块')
    return
  }

  submitting.value = true
  try {
    const payload = {
      ...form.value,
      // aliases 转为 JSON 数组
      aliases: form.value.aliases
        ? JSON.stringify(form.value.aliases.split(',').map(s => s.trim()).filter(Boolean))
        : null
    }

    if (editingId.value) {
      await masterApi.put(`/field-definitions/${editingId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/field-definitions', payload)
      ElMessage.success('添加成功')
    }
    dialogVisible.value = false
    loadFields()
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败')
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row: FieldDefinition) => {
  try {
    await ElMessageBox.confirm(
      `确定删除字段 "${row.name}"（${row.code}）吗？`,
      '删除确认',
      { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning' }
    )
    const res: any = await masterApi.delete(`/field-definitions/${row.id}`)
    if (res.success) {
      ElMessage.success('删除成功')
      loadFields()
    }
  } catch {
    // 用户取消
  }
}

const getCategoryLabel = (cat: string) => {
  return categoryOptions.find(o => o.value === cat)?.label || cat
}

const getDataTypeLabel = (dt: string) => {
  return dataTypeOptions.find(o => o.value === dt)?.label || dt
}

onMounted(() => {
  loadFields()
})
</script>

<template>
  <div class="page-container">
    <!-- 页面标题 -->
    <div class="page-header">
      <h2>字段管理</h2>
      <div class="header-actions">
        <el-button :icon="Refresh" @click="loadFields">刷新</el-button>
        <el-button type="primary" :icon="Plus" @click="handleAdd">新增字段</el-button>
      </div>
    </div>

    <!-- 筛选区 -->
    <div class="filter-bar">
      <el-select v-model="filterCategory" placeholder="字段类别" clearable style="width:140px">
        <el-option v-for="o in categoryOptions" :key="o.value" :label="o.label" :value="o.value" />
      </el-select>
      <el-input v-model="filterKeyword" placeholder="搜索编码/名称/别名" clearable style="width:200px">
        <template #prefix><el-icon><Search /></el-icon></template>
      </el-input>
    </div>

    <!-- 说明 -->
    <div class="info-tip">
      <strong>共享字段</strong>：所有模块通用，字段名统一管理，别名兼容不同场景<br />
      <strong>私有字段</strong>：仅某一模块内部使用，其他模块不可见<br />
      <span style="color:#909399;font-size:12px">字段编码全局唯一，新增前请确认是否已存在相同语义的字段。</span>
    </div>

    <!-- 列表 -->
    <el-table :data="filteredList" v-loading="loading" stripe>
      <el-table-column prop="code" label="字段编码" width="160">
        <template #default="{ row }">
          <code style="font-size:12px">{{ row.code }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="name" label="标准名称" width="120" />
      <el-table-column label="别名（各场景）" min-width="220">
        <template #default="{ row }">
          <span style="color:#67C23A;font-size:13px">{{ formatAliases(row.aliases) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="dataType" label="数据类型" width="100" align="center">
        <template #default="{ row }">
          {{ getDataTypeLabel(row.dataType) }}
        </template>
      </el-table-column>
      <el-table-column prop="category" label="类别" width="100" align="center">
        <template #default="{ row }">
          <el-tag :type="row.category === 'shared' ? 'success' : 'info'" size="small">
            {{ getCategoryLabel(row.category) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="module" label="所属模块" width="100" align="center">
        <template #default="{ row }">
          {{ row.module ? (moduleOptions.find(o => o.value === row.module)?.label || row.module) : '—' }}
        </template>
      </el-table-column>
      <el-table-column prop="isRequired" label="必填" width="60" align="center">
        <template #default="{ row }">
          <el-tag :type="row.isRequired ? 'danger' : 'info'" size="small">{{ row.isRequired ? '是' : '否' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
      <el-table-column label="状态" width="80" align="center">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'danger'" size="small">{{ row.isActive ? '启用' : '停用' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="120" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" size="small" :icon="Edit" @click="handleEdit(row)">编辑</el-button>
          <el-button link type="danger" size="small" :icon="Delete" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px" destroy-on-close>
      <el-form :model="form" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="字段编码" required>
              <el-input v-model="form.code" placeholder="如：person_name" :disabled="!!editingId" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="标准名称" required>
              <el-input v-model="form.name" placeholder="如：姓名" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="场景别名">
          <el-input v-model="form.aliases" placeholder="多个别名用逗号分隔，如：住户姓名,报修人,来访人" />
          <div style="font-size:12px;color:#909399;margin-top:4px">
            不同模块对此字段的称呼，用逗号分隔。例：住户姓名,报修人,来访人
          </div>
        </el-form-item>

        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="数据类型">
              <el-select v-model="form.dataType" style="width:100%">
                <el-option v-for="o in dataTypeOptions" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="最大长度">
              <el-input-number v-model="form.maxLength" :min="1" :max="1000" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="排序">
              <el-input-number v-model="form.sort" :min="0" :max="999" style="width:100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="字段类别">
              <el-select v-model="form.category" style="width:100%">
                <el-option v-for="o in categoryOptions" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="必填">
              <el-switch v-model="form.isRequired" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="状态">
              <el-switch v-model="form.isActive" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16" v-if="form.category === 'private'">
          <el-col :span="24">
            <el-form-item label="所属模块">
              <el-select v-model="form.module" placeholder="请选择所属模块" style="width:100%">
                <el-option v-for="o in moduleOptions" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-container { padding: 20px; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.header-actions { display: flex; gap: 8px; }
.filter-bar { display: flex; gap: 10px; margin-bottom: 12px; }
.info-tip {
  background: #f4f4f5;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  padding: 10px 14px;
  font-size: 13px;
  line-height: 1.8;
  margin-bottom: 12px;
  color: #606266;
}
</style>
