import { lookupDict } from './dict'
import { pinyin } from 'pinyin-pro'

/**
 * 自动生成英文编码
 * 优先级：词典 → 拼音全拼大写 → 空
 */
export function toPinyinCode(name: string): string {
  if (!name) return ''

  // 1. 查词典
  const dictResult = lookupDict(name)
  if (dictResult) return dictResult

  // 2. 拼音全拼大写
  const py = pinyin(name, { toneType: 'none' }) as string
  return py.replace(/\s+/g, '').toUpperCase()
}