<template>
  <div style="padding: 50px; text-align: center;">
    <h1>WO物业管理系统</h1>
    <p>登录页面加载中...</p>
    <div style="margin: 20px;">
      <button @click="testApi" style="padding: 10px 20px; background: #409eff; color: white; border: none; border-radius: 4px; cursor: pointer;">
        测试API
      </button>
    </div>
    <div v-if="message" style="margin: 20px; padding: 10px; background: #f0f0f0; border-radius: 4px;">
      {{ message }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const message = ref('')

const testApi = async () => {
  try {
    const response = await fetch('http://localhost:5006/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        username: 'admin',
        password: 'Admin@123'
      })
    })
    const data = await response.json()
    message.value = `登录成功! 用户: ${data.data?.user?.username || '未知'}`
  } catch (error) {
    message.value = `错误: ${error}`
  }
}
</script>
