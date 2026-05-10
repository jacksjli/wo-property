/**
 * useFieldConfig - 获取模块字段配置（alias 优先的 label）
 * 用法：const { getLabel, fieldConfig } = useFieldConfig('ticket')
 */
import { ref } from 'vue'
import { masterDataService } from '@/api/masterDataService'

// 按模块缓存 field-config
const fieldConfigCache = ref<Record<string, Record<string, any>>>({})
const loadingCache = ref<Record<string, boolean>>({})

export function useFieldConfig() {
  /**
   * 获取某模块的字段配置（带缓存）
   */
  const fetchFieldConfig = async (module: string) => {
    if (fieldConfigCache.value[module]) {
      return fieldConfigCache.value[module]
    }
    if (loadingCache.value[module]) {
      return null
    }
    loadingCache.value[module] = true
    try {
      const res = await masterDataService.getModuleFieldConfig(module)
      if (res.data?.success) {
        fieldConfigCache.value[module] = res.data.data
        return fieldConfigCache.value[module]
      }
    } catch (e) {
      console.error(`[useFieldConfig] ${module} 加载失败`, e)
    } finally {
      loadingCache.value[module] = false
    }
    return null
  }

  /**
   * 获取字段的显示标签（alias 优先）
   */
  const getLabel = (module: string, fieldKey: string, fallback: string) => {
    const config = fieldConfigCache.value[module]
    if (config && config[fieldKey]) {
      return config[fieldKey].label || fallback
    }
    return fallback
  }

  /**
   * 强制刷新某模块配置
   */
  const refresh = async (module: string) => {
    delete fieldConfigCache.value[module]
    delete loadingCache.value[module]
    return fetchFieldConfig(module)
  }

  return {
    fieldConfig: fieldConfigCache,
    fetchFieldConfig,
    getLabel,
    refresh
  }
}
