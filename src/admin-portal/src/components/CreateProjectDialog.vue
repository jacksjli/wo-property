<script setup lang="ts">
import { ref } from 'vue'
import { ElDialog, ElForm, ElFormItem, ElInput, ElButton, ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'success', project: any): void
}>()

const formRef = ref()
const loading = ref(false)

const form = ref({
  code: '',
  name: '',
  description: '',
  address: '',
  contactPhone: ''
})

const rules = {
  code: [
    { required: true, message: '请输入项目代码', trigger: 'blur' },
    { pattern: /^[a-z0-9]+$/, message: '项目代码只能包含小写字母和数字', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '请输入项目名称', trigger: 'blur' }
  ]
}

const handleClose = () => {
  form.value = { code: '', name: '', description: '', address: '', contactPhone: '' }
  emit('update:visible', false)
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid: boolean) => {
    if (!valid) return
    
    loading.value = true
    try {
      const response = await fetch('http://localhost:5016/api/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form.value)
      })
      
      const data = await response.json()
      
      if (data.success) {
        ElMessage.success(data.message || '项目创建成功')
        emit('success', data.project)
        handleClose()
      } else {
        ElMessage.error(data.message || '创建失败')
      }
    } catch (error: any) {
      ElMessage.error('创建失败: ' + (error.message || '网络错误'))
    } finally {
      loading.value = false
    }
  })
}
</script>

<template>
  <el-dialog
    title="创建新项目"
    :model-value="visible"
    width="500px"
    :close-on-click-modal="false"
    @update:model-value="emit('update:visible', $event)"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
    >
      <el-form-item label="项目代码" prop="code">
        <el-input
          v-model="form.code"
          placeholder="如: newproject"
          :disabled="loading"
        />
      </el-form-item>
      
      <el-form-item label="项目名称" prop="name">
        <el-input
          v-model="form.name"
          placeholder="如: 新建项目"
          :disabled="loading"
        />
      </el-form-item>
      
      <el-form-item label="项目描述" prop="description">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="3"
          placeholder="请输入项目描述"
          :disabled="loading"
        />
      </el-form-item>
      
      <el-form-item label="地址" prop="address">
        <el-input
          v-model="form.address"
          placeholder="请输入项目地址"
          :disabled="loading"
        />
      </el-form-item>
      
      <el-form-item label="联系电话" prop="contactPhone">
        <el-input
          v-model="form.contactPhone"
          placeholder="请输入联系电话"
          :disabled="loading"
        />
      </el-form-item>
    </el-form>
    
    <template #footer>
      <el-button @click="handleClose" :disabled="loading">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">
        创建
      </el-button>
    </template>
  </el-dialog>
</template>