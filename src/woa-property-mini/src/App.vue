<script>
import { connectWebSocket, disconnectWebSocket, isWebSocketConnected } from './utils/websocket.js'
import ENV from './config/env'

function getWebSocketManager() {
  return {
    connect: (options) => connectWebSocket(options),
    disconnect: () => disconnectWebSocket(),
    isConnected: () => isWebSocketConnected(),
    on: () => {},
    off: () => {}
  }
}

export default {
  onLaunch() {
    console.log('App Launch')
    const token = uni.getStorageSync('token')
    const userInfo = uni.getStorageSync('userInfo')
    const ws = getWebSocketManager()
    
    this.globalData = {
      token,
      userInfo,
      role: uni.getStorageSync('role'),
      personId: uni.getStorageSync('personId'),
      ws,
      TICKET_API: ENV.TICKET_API,
      DISPATCH_API: ENV.DISPATCH_API,
      PERSON_API: ENV.PERSON_API,
      MASTERDATA_API: ENV.MASTERDATA_API,
      AUTH_API: ENV.AUTH_API
    }
    
    console.log('[App] Global data initialized')
  },
  
  onShow() {
    console.log('App Show')
    const token = uni.getStorageSync('token')
    if (token) {
      const ws = this.globalData.ws
      if (ws && !ws.isConnected()) {
        console.log('[App] Reconnecting WebSocket on show')
        ws.connect({})
      }
    }
  },
  
  onHide() {
    console.log('App Hide')
  },
  
  globalData: {
    TICKET_API: ENV.TICKET_API,
    DISPATCH_API: ENV.DISPATCH_API,
    PERSON_API: ENV.PERSON_API,
    MASTERDATA_API: ENV.MASTERDATA_API,
    AUTH_API: ENV.AUTH_API
  }
}
</script>

<style>
page {
  background-color: #f5f5f5;
  font-size: 28rpx;
  color: #333;
  font-family: -apple-system, BlinkMacSystemFont, 'PingFang SC', 'Helvetica Neue', sans-serif;
  text-rendering: optimizeLegibility;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
.container {
  padding: 24rpx;
}
.btn-primary {
  background-color: #409EFF;
  color: #fff;
  border-radius: 8rpx;
  padding: 20rpx 40rpx;
  font-size: 28rpx;
}
.btn-default {
  background-color: #fff;
  color: #333;
  border: 1rpx solid #dcdfe6;
  border-radius: 8rpx;
  padding: 20rpx 40rpx;
  font-size: 28rpx;
}
.card {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;
  margin-bottom: 24rpx;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}
.status-new { color: #409EFF; }
.status-dispatched { color: #E6A23C; }
.status-accepted { color: #909399; }
.status-processing { color: #67C23A; }
.status-finished { color: #67C23A; }
.status-closed { color: #909399; }
</style>