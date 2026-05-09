<template>
  <div class="auth-layout">
    <div class="auth-background">
      <div class="background-pattern"></div>
      <div class="background-overlay"></div>
    </div>
    
    <div class="auth-container">
      <div class="auth-header">
        <h1 class="auth-logo">WO物业管理</h1>
        <p class="auth-subtitle">智能管理平台</p>
      </div>
      
      <div class="auth-card">
        <router-view />
      </div>
      
      <div class="auth-footer">
        <p>© 2026 WO物业管理软件. 保留所有权利.</p>
        <p class="version">版本 1.0.0</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// 登录布局组件
</script>

<style scoped>
.auth-layout {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  background-color: #f8fafc;
}

.auth-background {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  overflow: hidden;
}

.background-pattern {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image: 
    radial-gradient(circle at 25% 25%, rgba(59, 130, 246, 0.1) 0%, transparent 55%),
    radial-gradient(circle at 75% 75%, rgba(139, 92, 246, 0.1) 0%, transparent 55%);
  animation: float 20s ease-in-out infinite;
}

.background-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.9) 0%, rgba(248, 250, 252, 0.9) 100%);
}

.auth-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 400px;
  padding: 20px;
}

.auth-header {
  text-align: center;
  margin-bottom: 32px;
}

.auth-logo {
  font-size: 32px;
  font-weight: 700;
  margin: 0 0 8px 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.auth-subtitle {
  font-size: 14px;
  color: #64748b;
  margin: 0;
}

.auth-card {
  background-color: white;
  border-radius: 12px;
  padding: 32px;
  box-shadow: 
    0 10px 25px rgba(0, 0, 0, 0.05),
    0 20px 48px rgba(0, 0, 0, 0.05),
    0 1px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #e2e8f0;
}

.auth-footer {
  text-align: center;
  margin-top: 32px;
  color: #64748b;
  font-size: 12px;
}

.auth-footer .version {
  margin-top: 4px;
  opacity: 0.7;
}

@keyframes float {
  0%, 100% {
    transform: translate(0, 0) rotate(0deg);
  }
  33% {
    transform: translate(30px, -30px) rotate(120deg);
  }
  66% {
    transform: translate(-20px, 20px) rotate(240deg);
  }
}
</style>
