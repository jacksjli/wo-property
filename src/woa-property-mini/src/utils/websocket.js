// WebSocket 管理器 - 工单实时刷新
let socket = null
let reconnectTimer = null
let reconnectInterval = 5000  // 5秒重连
let isConnected = false

// 事件处理函数
const eventHandlers = {
  onTicketCreated: null,
  onTicketStatusChanged: null,
  onConnected: null,
  onDisconnected: null
}

/**
 * 连接 WebSocket
 * @param {Function} onTicketCreated - 工单创建回调
 * @param {Function} onTicketStatusChanged - 工单状态变更回调
 */
export function connectWebSocket(options = {}) {
  eventHandlers.onTicketCreated = options.onTicketCreated || null
  eventHandlers.onTicketStatusChanged = options.onTicketStatusChanged || null
  eventHandlers.onConnected = options.onConnected || null
  eventHandlers.onDisconnected = options.onDisconnected || null

  // 如果已连接，先断开
  if (socket && socket.readyState === 1) {
    socket.close()
  }

  const env = require('../config/env.js').env
    // 去掉 /api 后缀和 http:// 前缀构造 WebSocket URL
  const wsUrl = `ws://${env.AUTH_API.replace('http://', '').replace('/api', '')}/ws`
  
  console.log('[WebSocket] Connecting to:', wsUrl)

  try {
    socket = wx.connectSocket({
      url: wsUrl,
      success: () => {
        console.log('[WebSocket] Connection attempt started')
      },
      fail: (err) => {
        console.error('[WebSocket] Connection failed:', err)
        scheduleReconnect()
      }
    })

    // 监听连接打开
    socket.onOpen(() => {
      console.log('[WebSocket] Connected')
      isConnected = true
      clearTimeout(reconnectTimer)
      if (eventHandlers.onConnected) {
        eventHandlers.onConnected()
      }
    })

    // 监听消息
    socket.onMessage((res) => {
      try {
        const message = JSON.parse(res.data)
        console.log('[WebSocket] Received:', message.type, message.data)
        
        handleMessage(message)
      } catch (e) {
        console.error('[WebSocket] Failed to parse message:', e)
      }
    })

    // 监听连接关闭
    socket.onClose(() => {
      console.log('[WebSocket] Disconnected')
      isConnected = false
      if (eventHandlers.onDisconnected) {
        eventHandlers.onDisconnected()
      }
      scheduleReconnect()
    })

    // 监听错误
    socket.onError((err) => {
      console.error('[WebSocket] Error:', err)
    })

  } catch (e) {
    console.error('[WebSocket] Failed to connect:', e)
    scheduleReconnect()
  }
}

/**
 * 处理收到的消息
 */
function handleMessage(message) {
  // Gateway sends: { type: "module:eventType", data: {...} }
  // Our API sends: { Module: "ticket", EventType: "created", Data: {...} }
  // Support both formats
  const type = message.type || (message.Module && message.EventType ? `${message.Module}:${message.EventType}` : null)
  const data = message.data || message.Data
  
  if (!type) {
    console.log('[WebSocket] Unknown message format:', message)
    return
  }
  
  switch (type) {
    case 'connected':
      console.log('[WebSocket] Server confirmed connection, clientId:', data?.clientId)
      break
      
    case 'ticket:created':
      console.log('[WebSocket] Ticket created:', data?.ticketCode)
      if (eventHandlers.onTicketCreated) {
        eventHandlers.onTicketCreated(data)
      }
      break
      
    case 'ticket:statusChanged':
      console.log('[WebSocket] Ticket status changed:', data?.ticketCode, '->', data?.currentStatus)
      if (eventHandlers.onTicketStatusChanged) {
        eventHandlers.onTicketStatusChanged(data)
      }
      break
      
    case 'dispatch:dispatched':
      console.log('[WebSocket] Ticket dispatched:', data?.ticketCode)
      if (eventHandlers.onTicketStatusChanged) {
        eventHandlers.onTicketStatusChanged(data)
      }
      break
      
    case 'ticketUpdated':
      // 兼容旧格式
      console.log('[WebSocket] Ticket updated (legacy):', data?.ticketCode)
      if (eventHandlers.onTicketStatusChanged) {
        eventHandlers.onTicketStatusChanged(data)
      }
      break
      
    default:
      console.log('[WebSocket] Unknown message type:', type)
  }
}

/**
 * 定时重连
 */
function scheduleReconnect() {
  clearTimeout(reconnectTimer)
  reconnectTimer = setTimeout(() => {
    console.log('[WebSocket] Attempting to reconnect...')
    connectWebSocket({
      onTicketCreated: eventHandlers.onTicketCreated,
      onTicketStatusChanged: eventHandlers.onTicketStatusChanged,
      onConnected: eventHandlers.onConnected,
      onDisconnected: eventHandlers.onDisconnected
    })
  }, reconnectInterval)
}

/**
 * 断开连接
 */
export function disconnectWebSocket() {
  clearTimeout(reconnectTimer)
  if (socket) {
    socket.close()
    socket = null
  }
  isConnected = false
  console.log('[WebSocket] Disconnected manually')
}

/**
 * 获取连接状态
 */
export function isWebSocketConnected() {
  return isConnected && socket && socket.readyState === 1
}

/**
 * 发送消息到服务器
 */
export function sendMessage(data) {
  if (socket && socket.readyState === 1) {
    socket.send({
      data: JSON.stringify(data)
    })
  } else {
    console.warn('[WebSocket] Cannot send, not connected')
  }
}