import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import App from './App.vue'
import router from './router'
import './assets/main.css'

const app = createApp(App)

// 注册所有图标（按需注册 vs 整体导入的差异已通过路由懒加载 + manualChunks 优化）
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.use(createPinia())
app.use(router)
app.use(ElementPlus)

// 全局错误处理
app.config.errorHandler = (err, instance, info) => {
  console.error('Global error:', err, 'Info:', info)
  ElMessage.error(`页面加载失败: ${(err as Error).message}`)
}

// 处理未捕获的 Promise 错误
window.addEventListener('unhandledrejection', (event) => {
  console.error('Unhandled promise rejection:', event.reason)
  ElMessage.error(`操作失败: ${(event.reason as Error).message || '未知错误'}`)
})

app.mount('#app')