<template>
  <transition name="slide-down">
    <div v-if="visible && hasUnhealthyServices" class="service-health-alert">
      <div class="alert-content">
        <div class="alert-icon">
          <el-icon size="24"><WarningFilled /></el-icon>
        </div>
        <div class="alert-body">
          <div class="alert-title">部分服务不可用</div>
          <div class="alert-message">
            以下服务可能影响功能正常使用：
          </div>
          <div class="unhealthy-list">
            <span
              v-for="service in unhealthyServices"
              :key="service.key"
              class="unhealthy-tag"
            >
              {{ service.name }}
              <span class="unhealthy-reason">({{ service.message }})</span>
            </span>
          </div>
        </div>
        <div class="alert-actions">
          <el-button size="small" @click="handleRetry" :loading="isChecking">
            <el-icon><Refresh /></el-icon>
            重试
          </el-button>
          <el-button size="small" @click="handleDismiss">
            知道了
          </el-button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import { WarningFilled, Refresh } from '@element-plus/icons-vue'
import { useServiceHealth, type ServiceHealth } from '@/stores/serviceHealth'

const props = defineProps<{
  services?: string[]
}>()

const emit = defineEmits<{
  dismiss: []
}>()

const {
  isChecking,
  checkAllServices,
  checkProjectServices
} = useServiceHealth()

const visible = defineModel<boolean>('visible', { default: true })

const unhealthyServices = computed(() => {
  return Array.from(serviceHealthMap.value.values()).filter((s: ServiceHealth) => !s.healthy)
})

const hasUnhealthyServices = computed(() => unhealthyServices.value.length > 0)

const handleRetry = async () => {
  await checkAllServices()
  if (!hasUnhealthyServices.value) {
    emit('dismiss')
  }
}

const handleDismiss = () => {
  visible.value = false
  emit('dismiss')
}

// Auto-check on mount when used as standalone component
watch(() => props.services, async (moduleNames) => {
  if (moduleNames && moduleNames.length > 0) {
    await checkProjectServices(moduleNames)
  }
}, { immediate: true })
</script>

<script lang="ts">
// Import serviceHealthMap for use in computed
import { serviceHealthMap } from '@/stores/serviceHealth'
export default { name: 'ServiceHealthAlert' }
</script>

<style scoped>
.service-health-alert {
  position: fixed;
  top: 60px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 9999;
  min-width: 400px;
  max-width: 600px;
  background: linear-gradient(135deg, #fff8e6 0%, #fff3cd 100%);
  border: 1px solid #ffc107;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  padding: 16px 20px;
}

.alert-content {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.alert-icon {
  color: #f56c6c;
  flex-shrink: 0;
  padding-top: 2px;
}

.alert-body {
  flex: 1;
  min-width: 0;
}

.alert-title {
  font-size: 16px;
  font-weight: 600;
  color: #856404;
  margin-bottom: 4px;
}

.alert-message {
  font-size: 14px;
  color: #856404;
  margin-bottom: 8px;
}

.unhealthy-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.unhealthy-tag {
  display: inline-block;
  background: rgba(255, 193, 7, 0.3);
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 13px;
  color: #664d03;
}

.unhealthy-reason {
  font-size: 12px;
  color: #856404;
  margin-left: 4px;
}

.alert-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex-shrink: 0;
}

/* Transition */
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}

.slide-down-enter-from,
.slide-down-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(-20px);
}
</style>