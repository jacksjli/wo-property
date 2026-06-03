<template>
  <view class="login-container">
    <!-- 背景装饰 -->
    <view class="bg-decoration">
      <view class="circle circle-1"></view>
      <view class="circle circle-2"></view>
      <view class="circle circle-3"></view>
      <view class="light-ray ray-1"></view>
      <view class="light-ray ray-2"></view>
    </view>

    <!-- 顶部 slogan -->
    <view class="slogan-section" :class="{ 'fade-in-up': loaded }">
      <text class="slogan-sub">智慧物业</text>
      <text class="slogan-main">便捷生活</text>
    </view>

    <!-- Logo 区域 -->
    <view class="logo-section" :class="{ 'fade-in-up': loaded }">
      <view class="logo-wrapper">
        <view class="logo-glow"></view>
        <image class="logo" src="/static/logo.png" mode="aspectFit" />
        <view class="logo-ring"></view>
      </view>
    </view>

    <!-- 标题 -->
    <view class="title-section" :class="{ 'fade-in-up': loaded }">
      <text class="title">物业管理系统</text>
      <text class="subtitle">让物业服务更高效</text>
    </view>

    <!-- 登录方式切换（滑动指示条） -->
    <view class="login-tabs" :class="{ 'fade-in-up': loaded }">
      <view class="tab-indicator" :style="indicatorStyle"></view>
      <view 
        v-for="tab in tabs" 
        :key="tab.key"
        :class="['tab', { active: loginMode === tab.key }]"
        @click="switchTab(tab.key)"
      >
        {{ tab.name }}
      </view>
    </view>

    <!-- 员工登录 -->
    <view v-if="loginMode === 'namephone'" class="login-section" :class="{ 'fade-in-up': loaded }">
      <!-- 姓名输入 -->
      <view class="input-wrapper" :class="{ focused: nameFocused, shake: nameShake }">
        <view class="input-icon">
          <text class="iconfont">👤</text>
        </view>
        <input 
          class="input" 
          v-model="name" 
          placeholder="请输入姓名" 
          placeholder-class="placeholder"
          @focus="nameFocused = true"
          @blur="nameFocused = false; validateName()"
          @input="nameError = ''"
        />
        <view v-if="nameError" class="input-error-icon">!</view>
      </view>
      <view v-if="nameError" class="error-msg">{{ nameError }}</view>

      <!-- 手机号输入 -->
      <view class="input-wrapper" :class="{ focused: phoneFocused, shake: phoneShake }">
        <view class="input-icon">
          <text class="iconfont">📱</text>
        </view>
        <input 
          class="input" 
          v-model="phone" 
          type="number"
          maxlength="11"
          placeholder="请输入手机号" 
          placeholder-class="placeholder"
          @focus="phoneFocused = true"
          @blur="phoneFocused = false; validatePhone()"
          @input="onPhoneInput"
        />
        <view v-if="phoneError" class="input-error-icon">!</view>
      </view>
      <view v-if="phoneError" class="error-msg">{{ phoneError }}</view>

      <!-- 登录按钮 -->
      <button 
        type="primary" 
        class="login-btn" 
        :class="{ loading: logging, 'btn-pressed': btnPressed }"
        :loading="logging"
        @click="handlePersonLogin"
        @touchstart="btnPressed = true"
        @touchend="btnPressed = false"
        @touchcancel="btnPressed = false"
        :disabled="logging"
      >
        <view class="btn-bg"></view>
        <text class="btn-text" v-if="!logging">员工登录</text>
        <text class="btn-text" v-else>登录中...</text>
      </button>
    </view>

    <!-- 微信登录 -->
    <view v-if="loginMode === 'wechat'" class="login-section wechat-section" :class="{ 'fade-in-up': loaded }">
      <button 
        class="wechat-btn" 
        :class="{ 'btn-pressed': btnPressed, loading: logging }"
        :loading="logging"
        @click="handleWechatLogin"
        @touchstart="btnPressed = true"
        @touchend="btnPressed = false"
        @touchcancel="btnPressed = false"
        :disabled="logging"
      >
        <view class="wechat-icon">
          <text>微</text>
        </view>
        <text class="wechat-text" v-if="!logging">微信授权登录</text>
        <text class="wechat-text" v-else>登录中...</text>
      </button>
      
      <view class="tips">
        <text class="tip">登录即表示同意</text>
        <text class="link">《用户协议》</text>
        <text class="tip">和</text>
        <text class="link">《隐私政策》</text>
      </view>
    </view>

    <!-- 底部版本号 -->
    <view class="version" :class="{ 'fade-in-up': loaded }">
      <text>v1.0.0</text>
    </view>
  </view>
</template>

<script>
import { personLogin } from '@/api/personAuth.js'
import { wechatLogin } from '@/api/auth.js'

export default {
  data() {
    return {
      logging: false,
      loginMode: 'namephone',
      name: wx.getStorageSync('userInfo')?.name || '',
      phone: wx.getStorageSync('userInfo')?.phone || '',
      nameFocused: false,
      phoneFocused: false,
      nameShake: false,
      phoneShake: false,
      nameError: '',
      phoneError: '',
      btnPressed: false,
      loaded: false,
      tabs: [
        { key: 'namephone', name: '员工登录' },
        { key: 'wechat', name: '微信授权' }
      ]
    }
  },
  computed: {
    indicatorStyle() {
      const idx = this.loginMode === 'namephone' ? 0 : 1
      return {
        transform: `translateX(${idx * 100}%)`
      }
    }
  },
  onLoad() {
    setTimeout(() => { this.loaded = true }, 50)
  },
  methods: {
    switchTab(key) {
      if (this.loginMode === key) return
      // Animate tab switch
      this.loginMode = key
    },

    validateName() {
      if (!this.name.trim()) {
        this.nameError = '请输入姓名'
        this.triggerShake('name')
        return false
      }
      this.nameError = ''
      return true
    },

    validatePhone() {
      if (!this.phone.trim()) {
        this.phoneError = '请输入手机号'
        this.triggerShake('phone')
        return false
      }
      if (!/^1[3-9]\d{9}$/.test(this.phone)) {
        this.phoneError = '手机号格式不正确'
        this.triggerShake('phone')
        return false
      }
      this.phoneError = ''
      return true
    },

    onPhoneInput() {
      this.phoneError = ''
      // Auto-format: add space after 3, 7 digits for readability
    },

    triggerShake(field) {
      if (field === 'name') {
        this.nameShake = true
        setTimeout(() => { this.nameShake = false }, 500)
      } else {
        this.phoneShake = true
        setTimeout(() => { this.phoneShake = false }, 500)
      }
    },

    async handlePersonLogin() {
      if (this.logging) return
      if (!this.validateName()) return
      if (!this.validatePhone()) return
      
      this.logging = true
      
      try {
        const result = await personLogin(this.name.trim(), this.phone.trim())
        console.log('员工登录成功:', result)
        
        const role = result.data.role
        if (role === 'operator') {
          uni.reLaunch({ url: '/pages/mine/index' })
        } else if (role === 'admin' || role === 'manager') {
          uni.reLaunch({ url: '/pages/admin/dashboard' })
        } else {
          uni.reLaunch({ url: '/pages/owner/index' })
        }
      } catch (error) {
        console.error('登录失败:', error)
        wx.showToast({
          title: error.message || '登录失败',
          icon: 'none'
        })
      } finally {
        this.logging = false
      }
    },

    async handleWechatLogin() {
      if (this.logging) return
      
      this.logging = true
      
      try {
        const loginResult = await new Promise((resolve, reject) => {
          wx.login({
            success: resolve,
            fail: reject
          })
        })
        
        console.log('wx.login result:', loginResult)
        const result = await wechatLogin(loginResult.code)
        console.log('登录成功:', result)
        
        const userInfo = result.data.userInfo
        if (userInfo.role === 'admin' || userInfo.role === 'manager') {
          uni.reLaunch({ url: '/pages/admin/dashboard' })
        } else if (userInfo.role === 'engineer') {
          uni.reLaunch({ url: '/pages/ticket/list' })
        } else {
          uni.reLaunch({ url: '/pages/owner/index' })
        }
        
      } catch (error) {
        console.error('登录失败:', error)
        wx.showToast({
          title: error.message || '登录失败',
          icon: 'none'
        })
      } finally {
        this.logging = false
      }
    }
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-start;
  min-height: 100vh;
  background: linear-gradient(160deg, #0c2a4d 0%, #1a5276 40%, #2e86c1 100%);
  padding: 0 60rpx;
  padding-top: 120rpx;
  position: relative;
  overflow: hidden;
}

/* ========== 背景装饰 ========== */
.bg-decoration {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  pointer-events: none;
  overflow: hidden;
}

.circle {
  position: absolute;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
}

.circle-1 {
  width: 600rpx;
  height: 600rpx;
  top: -200rpx;
  right: -150rpx;
  background: radial-gradient(circle, rgba(46,204,113,0.15) 0%, transparent 70%);
  animation: float 8s ease-in-out infinite;
}

.circle-2 {
  width: 400rpx;
  height: 400rpx;
  bottom: 100rpx;
  left: -150rpx;
  background: radial-gradient(circle, rgba(52,152,219,0.2) 0%, transparent 70%);
  animation: float 10s ease-in-out infinite reverse;
}

.circle-3 {
  width: 300rpx;
  height: 300rpx;
  top: 400rpx;
  right: -100rpx;
  background: radial-gradient(circle, rgba(241,196,15,0.1) 0%, transparent 70%);
  animation: float 12s ease-in-out infinite;
}

.light-ray {
  position: absolute;
  width: 2rpx;
  background: linear-gradient(180deg, rgba(255,255,255,0.2) 0%, transparent 100%);
}

.ray-1 {
  height: 300rpx;
  top: 50rpx;
  right: 150rpx;
  transform: rotate(15deg);
}

.ray-2 {
  height: 200rpx;
  top: 100rpx;
  right: 250rpx;
  transform: rotate(-10deg);
}

@keyframes float {
  0%, 100% { transform: translateY(0rpx); }
  50% { transform: translateY(-20rpx); }
}

/* ========== 入场动画 ========== */
.fade-in-up {
  animation: fadeInUp 0.8s cubic-bezier(0.4, 0, 0.2, 1) both;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(40rpx);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ========== Slogan ========== */
.slogan-section {
  text-align: center;
  margin-bottom: 40rpx;
  animation-delay: 0.1s;
}

.slogan-sub {
  display: block;
  font-size: 28rpx;
  color: rgba(255,255,255,0.6);
  letter-spacing: 8rpx;
  margin-bottom: 8rpx;
}

.slogan-main {
  display: block;
  font-size: 36rpx;
  font-weight: 300;
  color: rgba(255,255,255,0.8);
  letter-spacing: 12rpx;
}

/* ========== Logo ========== */
.logo-section {
  margin-bottom: 40rpx;
  animation-delay: 0.2s;
}

.logo-wrapper {
  position: relative;
  width: 180rpx;
  height: 180rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.logo-glow {
  position: absolute;
  width: 200rpx;
  height: 200rpx;
  background: radial-gradient(circle, rgba(46,204,113,0.4) 0%, transparent 70%);
  border-radius: 50%;
  animation: pulse-glow 3s ease-in-out infinite;
}

.logo-ring {
  position: absolute;
  width: 180rpx;
  height: 180rpx;
  border: 2rpx solid rgba(46,204,113,0.3);
  border-radius: 50%;
  animation: ring-expand 3s ease-in-out infinite;
}

.logo {
  width: 140rpx;
  height: 140rpx;
  border-radius: 30rpx;
  position: relative;
  z-index: 1;
  animation: logo-breathe 4s ease-in-out infinite;
}

@keyframes logo-breathe {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.03); }
}

@keyframes pulse-glow {
  0%, 100% { opacity: 0.6; transform: scale(1); }
  50% { opacity: 1; transform: scale(1.1); }
}

@keyframes ring-expand {
  0%, 100% { transform: scale(1); opacity: 0.3; }
  50% { transform: scale(1.15); opacity: 0; }
}

/* ========== 标题 ========== */
.title-section {
  text-align: center;
  margin-bottom: 60rpx;
  animation-delay: 0.3s;
}

.title {
  display: block;
  font-size: 44rpx;
  font-weight: 600;
  color: #fff;
  letter-spacing: 4rpx;
  margin-bottom: 12rpx;
  text-shadow: 0 4rpx 20rpx rgba(0,0,0,0.2);
}

.subtitle {
  display: block;
  font-size: 26rpx;
  color: rgba(255,255,255,0.5);
  letter-spacing: 2rpx;
}

/* ========== Tab 切换 ========== */
.login-tabs {
  position: relative;
  display: flex;
  background: rgba(255,255,255,0.1);
  border-radius: 28rpx;
  padding: 6rpx;
  width: 440rpx;
  margin-bottom: 60rpx;
  animation-delay: 0.4s;
  overflow: hidden;
}

.tab-indicator {
  position: absolute;
  top: 6rpx;
  left: 6rpx;
  width: calc(50% - 6rpx);
  height: calc(100% - 12rpx);
  background: #fff;
  border-radius: 22rpx;
  transition: transform 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.15);
}

.tab {
  flex: 1;
  text-align: center;
  padding: 18rpx 0;
  font-size: 28rpx;
  color: rgba(255,255,255,0.7);
  position: relative;
  z-index: 1;
  transition: color 0.3s ease;
}

.tab.active {
  color: #1a5276;
}

/* ========== 登录表单 ========== */
.login-section {
  width: 80%;
  display: flex;
  flex-direction: column;
  align-items: center;
  animation-delay: 0.5s;
}

.input-wrapper {
  width: 100%;
  height: 80rpx;
  background: rgba(255,255,255,0.95);
  border-radius: 50rpx;
  display: flex;
  align-items: center;
  padding: 0 32rpx;
  margin-bottom: 28rpx;
  border: 3rpx solid transparent;
  transition: all 0.3s ease;
  position: relative;
}

.input-wrapper.focused {
  border-color: #2ecc71;
  box-shadow: 0 0 30rpx rgba(46,204,113,0.2), 0 8rpx 30rpx rgba(0,0,0,0.1);
  transform: scale(1.02);
}

.input-wrapper.shake {
  animation: shake 0.5s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes shake {
  0%, 100% { transform: translateX(0); }
  20% { transform: translateX(-10rpx); }
  40% { transform: translateX(10rpx); }
  60% { transform: translateX(-8rpx); }
  80% { transform: translateX(8rpx); }
}

.input-icon {
  width: 48rpx;
  font-size: 36rpx;
  margin-right: 16rpx;
  opacity: 0.5;
}

.input {
  flex: 1;
  height: 100%;
  font-size: 30rpx;
  color: #333;
}

.placeholder {
  color: #aaa;
}

.input-error-icon {
  width: 36rpx;
  height: 36rpx;
  background: #e74c3c;
  color: #fff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24rpx;
  font-weight: bold;
}

.error-msg {
  width: 100%;
  font-size: 24rpx;
  color: #ff6b6b;
  text-align: left;
  padding-left: 36rpx;
  margin-top: -16rpx;
  margin-bottom: 12rpx;
}

/* ========== 登录按钮 ========== */
.login-btn {
  position: relative;
  width: 100%;
  height: 100rpx;
  border-radius: 50rpx;
  overflow: hidden;
  margin-top: 20rpx;
  border: none;
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.login-btn.btn-pressed {
  transform: scale(0.97);
}

.login-btn[disabled] {
  opacity: 0.8;
}

.btn-bg {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(135deg, #2ecc71 0%, #27ae60 100%);
  border-radius: 50rpx;
  box-shadow: 0 8rpx 30rpx rgba(46,204,113,0.4);
}

.login-btn.btn-pressed .btn-bg {
  box-shadow: 0 4rpx 16rpx rgba(46,204,113,0.4);
}

.btn-text {
  position: relative;
  z-index: 1;
  font-size: 34rpx;
  font-weight: 600;
  color: #fff;
  letter-spacing: 4rpx;
}

/* ========== 微信登录 ========== */
.wechat-section {
  margin-top: 40rpx;
}

.wechat-btn {
  width: 100%;
  height: 100rpx;
  background: #07c160;
  border-radius: 50rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  box-shadow: 0 8rpx 30rpx rgba(7,193,96,0.3);
}

.wechat-btn.btn-pressed {
  transform: scale(0.97);
}

.wechat-btn[disabled] {
  opacity: 0.7;
}

.wechat-icon {
  width: 48rpx;
  height: 48rpx;
  background: #fff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 16rpx;
}

.wechat-icon text {
  font-size: 24rpx;
  color: #07c160;
  font-weight: bold;
}

.wechat-text {
  font-size: 32rpx;
  color: #fff;
  font-weight: 500;
  letter-spacing: 2rpx;
}

.tips {
  display: flex;
  justify-content: center;
  margin-top: 60rpx;
  font-size: 24rpx;
  color: rgba(255,255,255,0.5);
}

.link {
  color: #fff;
  text-decoration: underline;
  margin: 0 4rpx;
}

/* ========== 版本号 ========== */
.version {
  position: absolute;
  bottom: 60rpx;
  text-align: center;
  animation-delay: 0.6s;
}

.version text {
  font-size: 22rpx;
  color: rgba(255,255,255,0.3);
  letter-spacing: 2rpx;
}
</style>