<template>
  <div style="padding: 50px; text-align: center;">
    <h1 style="color: #409eff;">WO物业管理系统</h1>
    <p>登录页面测试</p>
    <div style="margin: 30px;">
      <input 
        type="text" 
        v-model="username" 
        placeholder="用户名"
        style="padding: 10px; margin: 5px; border: 1px solid #ddd; border-radius: 4px; width: 200px;"
      />
      <br/>
      <input 
        type="password" 
        v-model="password" 
        placeholder="密码"
        style="padding: 10px; margin: 5px; border: 1px solid #ddd; border-radius: 4px; width: 200px;"
      />
      <br/>
      <button 
        @click="login"
        style="padding: 10px 30px; margin: 10px; background: #409eff; color: white; border: none; border-radius: 4px; cursor: pointer;"
      >
        登录
      </button>
    </div>
    <div v-if="result" style="margin: 20px; padding: 15px; background: #f0f0f0; border-radius: 4px; text-align: left;">
      {{ result }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const username = ref('admin')
const password = ref('Admin@123')
const result = ref('')

const login = async () => {
  try {
    const response = await fetch('http://localhost:5006/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: username.value,
        password: password.value
      })
    })
    const data = await response.json()
    if (data.success) {
      result.value = `✅ 登录成功!\n用户名: ${data.data.user.username}\n角色: ${data.data.user.role}\n令牌: ${data.data.token.substring(0, 50)}...`
      localStorage.setItem('auth_token', data.data.token)
      localStorage.setItem('user_info', JSON.stringify(data.data.user))
    } else {
      result.value = `❌ 登录失败: ${data.message}`
    }
  } catch (error) {
    result.value = `❌ 错误: ${error}`
  }
}
</script>
