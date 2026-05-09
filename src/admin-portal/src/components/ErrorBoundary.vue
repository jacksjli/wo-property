<script setup lang="ts">
import { onErrorCaptured, ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const hasError = ref(false)
const errorMessage = ref('')

onErrorCaptured((err: Error, instance, info: string) => {
  console.error('[ErrorBoundary] Caught error:', err)
  console.error('[ErrorBoundary] Component:', instance)
  console.error('[ErrorBoundary] Info:', info)

  hasError.value = true
  errorMessage.value = err?.message || '页面加载失败'

  return false
})

const handleRetry = () => {
  hasError.value = false
  errorMessage.value = ''
  router.go(0)
}

const handleGoHome = () => {
  hasError.value = false
  errorMessage.value = ''
  router.push('/')
}
</script>

<template>
  <div v-if="hasError" class="error-boundary">
    <div class="error-content">
      <div class="error-icon">⚠️</div>
      <h2 class="error-title">页面加载失败</h2>
      <p class="error-message">{{ errorMessage }}</p>
      <p class="error-tip">抱歉，页面在加载过程中出现了问题</p>
      <div class="error-actions">
        <el-button type="primary" @click="handleRetry">重新加载</el-button>
        <el-button @click="handleGoHome">返回首页</el-button>
      </div>
    </div>
  </div>
  <slot v-else />
</template>

<style scoped>
.error-boundary {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  padding: 40px;
  background: #fafafa;
}

.error-content {
  text-align: center;
  max-width: 400px;
}

.error-icon {
  font-size: 64px;
  margin-bottom: 16px;
}

.error-title {
  font-size: 24px;
  font-weight: 600;
  color: #303133;
  margin: 0 0 12px;
}

.error-message {
  font-size: 14px;
  color: #909399;
  margin: 0 0 8px;
  word-break: break-all;
}

.error-tip {
  font-size: 14px;
  color: #c0c4cc;
  margin: 0 0 24px;
}

.error-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
}
</style>