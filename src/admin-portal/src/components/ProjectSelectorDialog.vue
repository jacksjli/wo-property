<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElDialog, ElButton, ElEmpty } from 'element-plus'

interface Project {
  code: string
  name: string
  displayName?: string
}

const props = defineProps<{
  visible: boolean
  projects: Project[]
}>()

const emit = defineEmits<{
  (e: 'select', project: Project): void
  (e: 'update:visible', value: boolean): void
}>()

const selectedProject = ref<string>('')

const displayProjects = computed(() => {
  return props.projects.map(p => ({
    code: p.code,
    name: p.displayName || p.name
  }))
})

const handleSelect = () => {
  const project = props.projects.find(p => p.code === selectedProject.value)
  if (project) {
    emit('select', project)
  }
}

const handleCancel = () => {
  emit('update:visible', false)
}

onMounted(() => {
  if (props.projects.length > 0) {
    selectedProject.value = props.projects[0].code
  }
})
</script>

<template>
  <el-dialog
    title="选择项目"
    :model-value="visible"
    :close-on-click-modal="false"
    :close-on-press-escape="false"
    width="400px"
    @update:model-value="emit('update:visible', $event)"
  >
    <div class="project-selector">
      <p class="info-text">您有权限访问以下项目，请选择一个：</p>
      
      <div class="project-list">
        <div
          v-for="project in displayProjects"
          :key="project.code"
          class="project-item"
          :class="{ selected: selectedProject === project.code }"
          @click="selectedProject = project.code"
        >
          <div class="project-icon">🏢</div>
          <div class="project-info">
            <div class="project-name">{{ project.name }}</div>
            <div class="project-code">{{ project.code }}</div>
          </div>
          <div class="check-icon" v-if="selectedProject === project.code">✓</div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleCancel">取消</el-button>
        <el-button 
          type="primary" 
          :disabled="!selectedProject"
          @click="handleSelect"
        >
          进入项目
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<style scoped>
.project-selector {
  padding: 10px 0;
}

.info-text {
  color: #909399;
  margin-bottom: 20px;
  text-align: center;
}

.project-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.project-item {
  display: flex;
  align-items: center;
  padding: 15px;
  border: 2px solid #e4e7ed;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.project-item:hover {
  border-color: #409eff;
  background: #f5f7fa;
}

.project-item.selected {
  border-color: #409eff;
  background: #ecf5ff;
}

.project-icon {
  font-size: 28px;
  margin-right: 15px;
}

.project-info {
  flex: 1;
}

.project-name {
  font-size: 16px;
  font-weight: 500;
  color: #303133;
}

.project-code {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}

.check-icon {
  width: 24px;
  height: 24px;
  background: #409eff;
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
</style>