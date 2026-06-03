import { ref } from 'vue'

let socket: WebSocket | null = null
let reconnectTimer: number | null = null
const isConnected = ref(false)

// 事件监听器
const listeners: Map<string, Set<(data: any) => void>> = new Map()

const WS_URL = `ws://192.168.1.3:5000/ws`

function emit(eventType: string, data: any) {
  // 触发精确匹配
  const exactListeners = listeners.get(eventType)
  if (exactListeners) {
    exactListeners.forEach(callback => {
      try { callback(data) } catch (e) { console.error('[WebSocket] Callback error:', e) }
    })
  }

  // 触发通配符匹配 (module:*)
  listeners.forEach((callbacks, pattern) => {
    if (pattern.endsWith(':*') && eventType.startsWith(pattern.slice(0, -1))) {
      callbacks.forEach(callback => {
        try { callback(data) } catch (e) { console.error('[WebSocket] Callback error:', e) }
      })
    }
  })
}

function connect() {
  if (socket?.readyState === WebSocket.OPEN) return

  try {
    socket = new WebSocket(WS_URL)

    socket.onopen = () => {
      console.log('[WebSocket] Connected')
      isConnected.value = true
      if (reconnectTimer) {
        clearTimeout(reconnectTimer)
        reconnectTimer = null
      }
    }

    socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data)
        console.log('[WebSocket] Received:', message)
        
        if (message.type) {
          emit(message.type, message.data || message)
        } else if (message.eventType) {
          emit(`${message.module}:${message.eventType}`, message.data || message)
        }
      } catch (e) {
        console.error('[WebSocket] Parse error:', e)
      }
    }

    socket.onclose = () => {
      console.log('[WebSocket] Disconnected')
      isConnected.value = false
      socket = null
      reconnectTimer = window.setTimeout(() => {
        console.log('[WebSocket] Reconnecting...')
        connect()
      }, 5000)
    }

    socket.onerror = (error) => {
      console.error('[WebSocket] Error:', error)
      isConnected.value = false
    }
  } catch (e) {
    console.error('[WebSocket] Connection failed:', e)
  }
}

function disconnect() {
  if (reconnectTimer) {
    clearTimeout(reconnectTimer)
    reconnectTimer = null
  }
  if (socket) {
    socket.close()
    socket = null
  }
  isConnected.value = false
}

function on(eventType: string, callback: (data: any) => void) {
  if (!listeners.has(eventType)) {
    listeners.set(eventType, new Set())
  }
  listeners.get(eventType)!.add(callback)
}

function off(eventType: string, callback: (data: any) => void) {
  const typeListeners = listeners.get(eventType)
  if (typeListeners) {
    typeListeners.delete(callback)
  }
}

function send(message: object) {
  if (socket?.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message))
  }
}

export function useWebSocket() {
  return {
    isConnected,
    connect,
    disconnect,
    on,
    off,
    send
  }
}

let autoConnected = false

export function initWebSocket() {
  if (autoConnected) return
  autoConnected = true
  const { connect } = useWebSocket()
  connect()
}