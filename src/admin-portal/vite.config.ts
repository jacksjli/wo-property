import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import { compression } from 'vite-plugin-compression2'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    // Gzip 压缩
    compression({
      algorithm: 'gzip',
      threshold: 10240,
    }),
    // Brotli 压缩
    compression({
      algorithm: 'brotliCompress',
      threshold: 10240,
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      'vue': 'vue/dist/vue.esm-bundler.js'
    },
  },
  build: {
    // Rolldown/Vite 8: manualChunks 必须为函数
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes('node_modules')) {
            if (id.includes('element-plus')) return 'element-plus'
            if (id.includes('echarts') || id.includes('vue-echarts')) return 'echarts'
            if (id.includes('vue/dist') || id.includes('vue-esm')) return 'vue-runtime'
            if (id.includes('vue') || id.includes('pinia') || id.includes('vue-router')) return 'vue-vendor'
            if (id.includes('axios')) return 'axios'
            return 'vendor'
          }
          // 注意：Vite 8 / Rolldown 下 views 页面分割暂不生效
          // 动态 import() 的路由页面由 Vite 内联处理，webpackChunkName 注释暂不生效
        },
        // chunk 文件名带 hash，便于 CDN 长期缓存
        chunkFileNames: 'assets/js/[name]-[hash].js',
        entryFileNames: 'assets/js/[name]-[hash].js',
        assetFileNames: 'assets/[ext]/[name]-[hash].[ext]',
      },
    },
  },
  // 生产环境 CDN 地址（部署时替换）
  base: '/',
  server: {
    host: '0.0.0.0',
    proxy: {
      '/api/metrics': {
        target: 'http://localhost:5250',
        changeOrigin: true,
      },
      '/api/tenant/persons': {
        target: 'http://localhost:5018',
        changeOrigin: true,
      },
      '/project/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/project\/api/, '/api'),
      },
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})