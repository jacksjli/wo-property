<template>
  <div class="not-found-view">
    <div class="not-found-container">
      <div class="not-found-content">
        <div class="error-code">404</div>
        <div class="error-title">页面未找到</div>
        <div class="error-description">
          抱歉，您访问的页面不存在或已被移除。
        </div>
        <div class="action-buttons">
          <el-button type="primary" @click="goHome">
            <el-icon><HomeFilled /></el-icon>
            返回首页
          </el-button>
          <el-button @click="goBack">
            <el-icon><ArrowLeft /></el-icon>
            返回上一页
          </el-button>
        </div>
      </div>
      
      <div class="not-found-illustration">
        <div class="illustration-container">
          <div class="illustration">
            <div class="search-icon">
              <el-icon><Search /></el-icon>
            </div>
            <div class="magnifying-glass"></div>
            <div class="page-icon">
              <el-icon><Document /></el-icon>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { HomeFilled, ArrowLeft, Search, Document } from '@element-plus/icons-vue';

const router = useRouter();

const goHome = () => {
  router.push('/');
};

const goBack = () => {
  router.back();
};
</script>

<style scoped>
.not-found-view {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.not-found-container {
  display: flex;
  max-width: 1000px;
  width: 100%;
  background: white;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.not-found-content {
  flex: 1;
  padding: 60px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.error-code {
  font-size: 120px;
  font-weight: 900;
  color: #667eea;
  line-height: 1;
  margin-bottom: 20px;
}

.error-title {
  font-size: 36px;
  font-weight: 700;
  color: #1f2937;
  margin-bottom: 16px;
}

.error-description {
  font-size: 18px;
  color: #6b7280;
  line-height: 1.6;
  margin-bottom: 40px;
  max-width: 400px;
}

.action-buttons {
  display: flex;
  gap: 16px;
}

.not-found-illustration {
  flex: 1;
  background: linear-gradient(135deg, #f3f4f6 0%, #e5e7eb 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px;
}

.illustration-container {
  position: relative;
  width: 300px;
  height: 300px;
}

.illustration {
  position: relative;
  width: 100%;
  height: 100%;
}

.search-icon {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 120px;
  height: 120px;
  background: #667eea;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2;
}

.search-icon .el-icon {
  font-size: 60px;
  color: white;
}

.magnifying-glass {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 200px;
  height: 200px;
  border: 20px solid rgba(102, 126, 234, 0.1);
  border-radius: 50%;
  z-index: 1;
  animation: pulse 2s infinite;
}

.page-icon {
  position: absolute;
  top: 30%;
  right: 20%;
  width: 80px;
  height: 80px;
  background: #10b981;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  transform: rotate(15deg);
  animation: float 3s ease-in-out infinite;
  box-shadow: 0 10px 20px rgba(16, 185, 129, 0.3);
}

.page-icon .el-icon {
  font-size: 40px;
  color: white;
}

@keyframes pulse {
  0% {
    transform: translate(-50%, -50%) scale(1);
    opacity: 0.1;
  }
  50% {
    transform: translate(-50%, -50%) scale(1.1);
    opacity: 0.2;
  }
  100% {
    transform: translate(-50%, -50%) scale(1);
    opacity: 0.1;
  }
}

@keyframes float {
  0%, 100% {
    transform: translateY(0) rotate(15deg);
  }
  50% {
    transform: translateY(-20px) rotate(15deg);
  }
}

/* 响应式设计 */
@media (max-width: 768px) {
  .not-found-container {
    flex-direction: column;
  }
  
  .not-found-content {
    padding: 40px;
    text-align: center;
  }
  
  .error-code {
    font-size: 80px;
  }
  
  .error-title {
    font-size: 28px;
  }
  
  .error-description {
    font-size: 16px;
  }
  
  .action-buttons {
    justify-content: center;
  }
  
  .illustration-container {
    width: 200px;
    height: 200px;
  }
  
  .search-icon {
    width: 80px;
    height: 80px;
  }
  
  .search-icon .el-icon {
    font-size: 40px;
  }
  
  .magnifying-glass {
    width: 140px;
    height: 140px;
    border-width: 15px;
  }
  
  .page-icon {
    width: 60px;
    height: 60px;
  }
  
  .page-icon .el-icon {
    font-size: 30px;
  }
}
</style>
