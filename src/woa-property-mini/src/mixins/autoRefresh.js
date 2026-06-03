/**
 * 全局自动刷新工具（支持 <script setup>）
 * 
 * 使用方式：
 * import autoRefresh from '@/mixins/autoRefresh'
 * 
 * onMounted(() => {
 *   const { start, stop } = autoRefresh()
 *   start(async () => { await loadData() }, 30000)
 * })
 */

let refreshTimer = null
let failCount = 0
let currentDoRefresh = null
let currentInterval = 30000

export default function useAutoRefresh() {
  function start(doRefresh, intervalMs = 30000) {
    stop()
    currentDoRefresh = doRefresh
    currentInterval = intervalMs
    failCount = 0
    refreshTimer = setInterval(async () => {
      if (!currentDoRefresh) return
      try {
        await currentDoRefresh()
        failCount = 0
      } catch (e) {
        failCount++
      }
    }, intervalMs)
  }

  function stop() {
    if (refreshTimer) {
      clearInterval(refreshTimer)
      refreshTimer = null
    }
  }

  function getFailCount() {
    return failCount
  }

  function resume() {
    if (currentDoRefresh && !refreshTimer) {
      refreshTimer = setInterval(currentDoRefresh, currentInterval)
    }
  }

  return { start, stop, getFailCount, resume }
}