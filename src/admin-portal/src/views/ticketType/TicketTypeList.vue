<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete } from '@element-plus/icons-vue'
import {
  getAllTicketTypes,
  addTicketType,
  updateTicketType,
  deleteTicketType,
  toggleTicketTypeStatus,
  ticketTypeExists,
  resetTicketTypes,
  type TicketType
} from '@/stores/ticketType'

const typeList = ref<TicketType[]>(getAllTicketTypes())

const dialogVisible = ref(false)
const dialogTitle = ref('新增类型')
const editingId = ref<number | null>(null)

const form = ref({
  name: '',
  icon: 'Document',
  color: '#409EFF'
})

const colorOptions = [
  { value: '#409EFF', label: '蓝色' },
  { value: '#67C23A', label: '绿色' },
  { value: '#E6A23C', label: '橙色' },
  { value: '#F56C6C', label: '红色' },
  { value: '#909399', label: '灰色' },
  { value: '#9C27B0', label: '紫色' },
  { value: '#00BCD4', label: '青色' },
  { value: '#FF5722', label: '深橙' }
]

const iconOptions = [
  { value: 'Tools', label: '工具' },
  { value: 'Warning', label: '警告' },
  { value: 'QuestionFilled', label: '问号' },
  { value: 'Edit', label: '编辑' },
  { value: 'Document', label: '文档' },
  { value: 'Service', label: '服务' },
  { value: 'Phone', label: '电话' },
  { value: 'Message', label: '消息' }
]

const handleAdd = () => {
  dialogTitle.value = '新增类型'
  editingId.value = null
  form.value = { name: '', icon: 'Document', color: '#409EFF' }
  dialogVisible.value = true
}

const handleEdit = (row: TicketType) => {
  dialogTitle.value = '编辑类型'
  editingId.value = row.id
  form.value = { name: row.name, icon: row.icon, color: row.color }
  dialogVisible.value = true
}

const handleSubmit = () => {
  if (!form.value.name.trim()) {
    ElMessage.warning('请输入类型名称')
    return
  }

  // 检查名称是否重复
  const exists = ticketTypeExists(form.value.name)
  if (editingId.value) {
    // 编辑时检查是否与其他类型重名
    const current = typeList.value.find(t => t.id === editingId.value)
    if (exists && current?.name !== form.value.name) {
      ElMessage.warning('该类型名称已存在')
      return
    }
    updateTicketType(editingId.value, { ...form.value })
    ElMessage.success('更新成功')
  } else {
    if (exists) {
      ElMessage.warning('该类型名称已存在')
      return
    }
    addTicketType({ ...form.value, status: 'Active' })
    ElMessage.success('添加成功')
  }

  typeList.value = getAllTicketTypes()
  dialogVisible.value = false
}

const handleDelete = async (row: TicketType) => {
  try {
    await ElMessageBox.confirm(`确定删除类型 "${row.name}" 吗？`, '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    deleteTicketType(row.id)
    typeList.value = getAllTicketTypes()
    ElMessage.success('删除成功')
  } catch {
    // 取消
  }
}

const handleReset = async () => {
  try {
    await ElMessageBox.confirm('确定重置为默认类型吗？这将删除所有自定义类型。', '重置确认', {
      confirmButtonText: '重置',
      cancelButtonText: '取消',
      type: 'warning'
    })
    resetTicketTypes()
    typeList.value = getAllTicketTypes()
    ElMessage.success('已重置为默认类型')
  } catch {
    // 取消
  }
}

const handleToggle = (row: TicketType) => {
  toggleTicketTypeStatus(row.id)
  typeList.value = getAllTicketTypes()
  ElMessage.success(`已将 "${row.name}" 设为${row.status === 'Active' ? '启用' : '停用'}`)
}
</script>

<template>
  <div class="ticket-type-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>工单类型管理</span>
          <div class="header-actions">
            <el-button @click="handleReset">重置为默认</el-button>
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon> 新增类型
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="说明"
        description="配置工单的类型，如：报修、投诉、咨询、建议等。可以自由添加、编辑、删除类型。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <el-table :data="typeList" stripe>
        <el-table-column prop="name" label="类型名称" width="150" align="center">
          <template #default="{ row }">
            <el-tag :style="{ backgroundColor: row.color, borderColor: row.color, color: '#fff' }">
              {{ row.name }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="icon" label="图标" width="120" align="center">
          <template #default="{ row }">
            <span>{{ row.icon }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="color" label="颜色" width="100" align="center">
          <template #default="{ row }">
            <div class="color-box" :style="{ backgroundColor: row.color }"></div>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Active' ? 'success' : 'info'" size="small">
              {{ row.status === 'Active' ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button link type="warning" size="small" @click="handleToggle(row)">
              {{ row.status === 'Active' ? '停用' : '启用' }}
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="450px">
      <el-form label-width="80px">
        <el-form-item label="类型名称">
          <el-input v-model="form.name" placeholder="如：报修、投诉、咨询" />
        </el-form-item>
        <el-form-item label="颜色">
          <el-select v-model="form.color" style="width: 100%">
            <el-option
              v-for="c in colorOptions"
              :key="c.value"
              :label="c.label"
              :value="c.value"
            >
              <div style="display: flex; align-items: center; gap: 10px;">
                <div class="color-box" :style="{ backgroundColor: c.value }"></div>
                <span>{{ c.label }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="图标">
          <el-select v-model="form.icon" style="width: 100%">
            <el-option
              v-for="i in iconOptions"
              :key="i.value"
              :label="i.label"
              :value="i.value"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.ticket-type-page {
  width: 100%;
}
.header-actions {
  display: flex;
  gap: 10px;
}
.color-box {
  width: 20px;
  height: 20px;
  border-radius: 4px;
}
</style>
