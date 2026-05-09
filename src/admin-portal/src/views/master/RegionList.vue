<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Refresh, Setting } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { masterApi } from '@/api/http'

const loading = ref(false)
const treeData = ref<any[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const currentId = ref<number | null>(null)
const submitting = ref(false)

const form = ref({
  code: '',
  name: '',
  level: 1,
  parentCode: null as string | null,
  remark: '',
})

const levelOptions = [
  { label: '大区', value: 1 },
  { label: '省/直辖市', value: 2 },
  { label: '市/区', value: 3 },
  { label: '县/区', value: 4 },
]

const parentOptions = ref<any[]>([])

const loadRegions = async () => {
  loading.value = true
  try {
    const r: any = await masterApi.get('/regions')
    if (r.success) {
      treeData.value = buildTree(r.data || [])
    }
  } catch (e: any) { ElMessage.error(e.message || '加载失败') }
  finally { loading.value = false }
}

const loadParentOptions = async (level: number) => {
  if (level <= 1) { parentOptions.value = []; return }
  const r: any = await masterApi.get('/regions', { params: { level: level - 1 } })
  if (r.success) parentOptions.value = r.data || []
}

const buildTree = (list: any[]): any[] => {
  const map: Record<string, any> = {}
  const roots: any[] = []
  list.forEach((r: any) => { map[r.code] = { ...r, children: [] } })
  list.forEach((r: any) => {
    if (r.parentCode && map[r.parentCode]) {
      map[r.parentCode].children.push(map[r.code])
    } else {
      roots.push(map[r.code])
    }
  })
  return roots
}

const getLevelLabel = (level: number) => levelOptions.find(l => l.value === level)?.label || ''

const openCreate = (parent: any = null) => {
  isEdit.value = false
  form.value = { code: '', name: '', level: parent ? parent.level + 1 : 1, parentCode: parent?.code || null, remark: '' }
  loadParentOptions(form.value.level)
  dialogVisible.value = true
}

const openEdit = (row: any) => {
  isEdit.value = true
  currentId.value = row.id
  form.value = { code: row.code, name: row.name, level: row.level, parentCode: row.parentCode, remark: row.remark || '' }
  loadParentOptions(row.level)
  dialogVisible.value = true
}

const handleSave = async () => {
  if (!form.value.code || !form.value.name) { ElMessage.warning('请填写编码和名称'); return }
  if (!isEdit.value) {
    const exists = treeData.value.some(r => r.code === form.value.code || (r.children || []).some((c: any) => c.code === form.value.code))
    if (exists) { ElMessage.warning('编码已存在'); return }
  }
  submitting.value = true
  try {
    if (isEdit.value && currentId.value) {
      await masterApi.put(`/regions/${currentId.value}`, form.value)
      ElMessage.success('更新成功')
    } else {
      await masterApi.post('/regions', form.value)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadRegions()
  } catch (e: any) { ElMessage.error(e.message || '操作失败') }
  finally { submitting.value = false }
}

const handleDelete = async (row: any) => {
  if (row.children && row.children.length > 0) {
    ElMessage.warning('该区域有下级区域，请先删除下级')
    return
  }
  try {
    await ElMessageBox.confirm(`删除区域「${row.name}」（${row.code}）？`, '确认', { type: 'warning' })
    await masterApi.delete(`/regions/${row.id}`)
    ElMessage.success('已删除')
    loadRegions()
  } catch (e: any) { if (e !== 'cancel') ElMessage.error(e.message || '删除失败') }
}

const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()
const openFieldConfig = () => { fieldDialogRef.value?.open() }

onMounted(loadRegions)
</script>

<template>
  <div class="region-list">
    <div class="page-header">
      <h2>大区/省市区管理</h2>
      <div style="display:flex;gap:8px">
        <el-button type="primary" :icon="Plus" @click="openCreate()">新增区域</el-button>
        <el-button @click="loadRegions" :loading="loading"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button @click="openFieldConfig"><el-icon><Setting /></el-icon>配置字段</el-button>
      </div>
    </div>

    <el-card shadow="never">
      <el-alert type="info" :closable="false" style="margin-bottom:16px">
        <template #title>树形结构：支持4级（省→市→区→县），新增时下级区域会自动关联上级。</template>
      </el-alert>

      <el-table :data="treeData" v-loading="loading" stripe row-key="code" default-expand-all>
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="区域名称" min-width="200" />
        <el-table-column prop="level" label="级别" width="120" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="row.level === 1 ? '' : row.level === 2 ? 'success' : 'warning'" plain>
              {{ getLevelLabel(row.level) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="parentCode" label="上级编码" width="120" align="center">
          <template #default="{ row }">{{ row.parentCode || '-' }}</template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" />
        <el-table-column label="操作" width="200" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openCreate(row)">新增下级</el-button>
            <el-button link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑区域' : '新增区域'" width="500px" destroy-on-close>
      <el-form :model="form" label-width="90px">
        <el-form-item label="区域编码" required>
          <el-input v-model="form.code" placeholder="如：HD（华北）/ZJ（浙江）/GZ（广州）" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="区域名称" required>
          <el-input v-model="form.name" placeholder="如：华东区域/浙江省/杭州市" />
        </el-form-item>
        <el-form-item label="区域级别">
          <el-select v-model="form.level" style="width:100%" @change="loadParentOptions(form.level)">
            <el-option v-for="l in levelOptions" :key="l.value" :label="l.label" :value="l.value" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="form.level > 1" label="上级区域">
          <el-select v-model="form.parentCode" placeholder="选择上级区域" clearable style="width:100%">
            <el-option v-for="p in parentOptions" :key="p.code" :label="p.name + '（' + p.code + '）'" :value="p.code" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="submitting">确认</el-button>
      </template>
    </el-dialog>

    <FieldConfigDialog ref="fieldDialogRef" module="region" module-name="大区管理" @update="refreshFields" />
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
</style>
