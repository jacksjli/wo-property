<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh, Document } from '@element-plus/icons-vue'
import FieldConfigDialog from '@/components/FieldConfigDialog.vue'
import { usePermission } from '@/composables/usePermission'
import { getActiveFields, type FieldConfig } from '@/stores/fieldConfig'
import {
  getAllTimeoutRules,
  updateTimeoutRule,
  resetTimeoutRules,
  timeoutColorLabels,
  timeoutColorTags,
  timeoutRoleLabels,
  formatTimeoutHours,
  type TimeoutRule,
  type TimeoutColor,
  type TimeoutRole
} from '@/stores/timeout'

// 权限验证
const { verifyAdminPassword } = usePermission()
const fieldDialogRef = ref<InstanceType<typeof FieldConfigDialog>>()

// 获取启用的字段
const getTimeoutFields = () => getActiveFields('timeout')

// 打开字段配置（需要管理员验证）
const openFieldConfig = async () => {
  const verified = await verifyAdminPassword()
  if (verified) {
    fieldDialogRef.value?.open()
  }
}

// 刷新字段
const refreshKey = ref(0)
const refreshFields = () => {
  refreshKey.value++
}

// 数据
const timeoutRules = ref<TimeoutRule[]>(getAllTimeoutRules())

// 颜色列表
const colors: TimeoutColor[] = ['green', 'blue', 'orange', 'red']

// 角色列表
const roles: TimeoutRole[] = ['operator', 'supervisor', 'manager', 'department_head', 'company_head']

// 获取规则
const getRule = (color: TimeoutColor, role: TimeoutRole) => {
  return timeoutRules.value.find(r => r.color === color && r.role === role)
}

// 更新规则
const handleUpdateHours = (rule: TimeoutRule, newHours: number) => {
  if (newHours < 1) {
    ElMessage.warning('超时时间不能小于1小时')
    return
  }
  updateTimeoutRule(rule.id, { hours: newHours })
  timeoutRules.value = getAllTimeoutRules()
  ElMessage.success('超时时间已更新')
}

// 切换启用状态
const handleToggle = (rule: TimeoutRule) => {
  updateTimeoutRule(rule.id, { enabled: !rule.enabled })
  timeoutRules.value = getAllTimeoutRules()
  ElMessage.success(`已${rule.enabled ? '禁用' : '启用'}`)
}

// 重置为默认
const handleReset = async () => {
  try {
    await ElMessageBox.confirm('确定重置所有超时规则为默认值吗？', '重置确认', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    resetTimeoutRules()
    timeoutRules.value = getAllTimeoutRules()
    ElMessage.success('超时规则已重置为默认值')
  } catch {
    // 取消
  }
}

// 刷新
const handleRefresh = () => {
  timeoutRules.value = getAllTimeoutRules()
  ElMessage.success('已刷新')
}

// 应用标准配置
const applyStandardConfig = () => {
  colors.forEach(c => {
    roles.forEach(r => {
      const rule = getRule(c, r)
      if (rule) {
        const hours = c === 'red' ? 4 : c === 'orange' ? 24 : c === 'blue' ? 48 : 72
        updateTimeoutRule(rule.id, { hours })
      }
    })
  })
  timeoutRules.value = getAllTimeoutRules()
  ElMessage.success('已应用标准配置')
}

// 应用紧凑配置
const applyCompactConfig = () => {
  colors.forEach(c => {
    roles.forEach(r => {
      const rule = getRule(c, r)
      if (rule) {
        const hours = c === 'red' ? 2 : c === 'orange' ? 12 : c === 'blue' ? 24 : 48
        updateTimeoutRule(rule.id, { hours, enabled: true })
      }
    })
  })
  timeoutRules.value = getAllTimeoutRules()
  ElMessage.success('已应用紧凑配置')
}

// 应用宽松配置
const applyRelaxedConfig = () => {
  colors.forEach(c => {
    roles.forEach(r => {
      const rule = getRule(c, r)
      if (rule) {
        const hours = c === 'red' ? 8 : c === 'orange' ? 48 : c === 'blue' ? 72 : 120
        updateTimeoutRule(rule.id, { hours, enabled: true })
      }
    })
  })
  timeoutRules.value = getAllTimeoutRules()
  ElMessage.success('已应用宽松配置')
}

// 计算总计启用数
const getEnabledCount = (color: TimeoutColor) => {
  return timeoutRules.value.filter(r => r.color === color && r.enabled).length
}

// 计算颜色总计启用数
const getTotalEnabledCount = () => {
  return timeoutRules.value.filter(r => r.enabled).length
}

// 格式化小时显示
const formatHours = (hours: number) => {
  if (hours < 1) return `${Math.round(hours * 60)}分钟`
  if (hours < 24) return `${hours}小时`
  const days = Math.floor(hours / 24)
  const remaining = hours % 24
  if (remaining === 0) return `${days}天`
  return `${days}天${remaining}小时`
}
</script>

<template>
  <div class="timeout-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>超时规则配置</span>
          <div class="header-actions">
            <el-button @click="openFieldConfig">
              <el-icon><Document /></el-icon> 配置字段
            </el-button>
            <el-button @click="handleReset">
              <el-icon><Refresh /></el-icon> 重置为默认
            </el-button>
            <el-button @click="handleRefresh">
              <el-icon><Refresh /></el-icon> 刷新
            </el-button>
          </div>
        </div>
      </template>

      <el-alert
        title="超时规则配置说明"
        description="配置不同颜色工单在各角色层级的超时时间。启用后系统将按规则自动计算任务超时时间并触发升级。"
        type="info"
        :closable="false"
        style="margin-bottom: 20px;"
      />

      <!-- 统计 -->
      <el-row :gutter="20" style="margin-bottom: 20px;">
        <el-col :span="6" v-for="color in colors" :key="color">
          <el-card shadow="hover" class="color-stat">
            <div class="stat-content">
              <el-tag :type="timeoutColorTags[color]" style="margin-bottom: 10px;">
                {{ timeoutColorLabels[color] }}
              </el-tag>
              <div class="stat-value">
                {{ getEnabledCount(color) }}/{{ roles.length }}
              </div>
              <div class="stat-label">已启用规则数</div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 超时规则矩阵 -->
      <el-table :data="colors" border style="margin-bottom: 20px;">
        <el-table-column label="工单颜色" width="150" align="center">
          <template #default="{ row: color }">
            <el-tag :type="timeoutColorTags[color]" size="large" style="font-size: 14px;">
              {{ timeoutColorLabels[color] }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column v-for="role in roles" :key="role" :label="timeoutRoleLabels[role]" align="center">
          <template #default="{ row: color }">
            <div v-if="getRule(color, role)" class="rule-cell">
              <el-input-number
                :model-value="getRule(color, role)!.hours"
                :min="1"
                :max="720"
                size="small"
                @change="(val: number) => handleUpdateHours(getRule(color, role)!, val)"
                style="width: 100px;"
              />
              <span class="hours-label">小时</span>
              <el-switch
                :model-value="getRule(color, role)!.enabled"
                size="small"
                @change="() => handleToggle(getRule(color, role)!)"
                style="margin-left: 8px;"
              />
            </div>
          </template>
        </el-table-column>
      </el-table>

      <!-- 规则说明 -->
      <el-card shadow="never">
        <template #header>
          <span>超时规则说明</span>
        </template>
        <el-descriptions :column="2" border>
          <el-descriptions-item label="绿色工单">一般处理，优先级较低，可在{{ formatTimeoutHours(72) }}内完成</el-descriptions-item>
          <el-descriptions-item label="蓝色工单">普通处理，优先级一般，可在{{ formatTimeoutHours(48) }}内完成</el-descriptions-item>
          <el-descriptions-item label="橙色工单">较紧急，需要尽快处理，需在{{ formatTimeoutHours(24) }}内完成</el-descriptions-item>
          <el-descriptions-item label="红色工单">非常紧急，需立即处理，须在{{ formatTimeoutHours(4) }}内完成</el-descriptions-item>
        </el-descriptions>
        
        <el-divider />
        
        <h4>超时升级机制</h4>
        <ul class="explain-list">
          <li>当任务超过设定时间未完成，系统将自动升级任务</li>
          <li>升级顺序：操作人员 → 主管 → 经理 → 部门负责人 → 公司负责人</li>
          <li>每次升级会增加任务的 escalationLevel 字段值</li>
          <li>可以在派单规则中开启/关闭超时升级功能</li>
        </ul>
      </el-card>

      <!-- 快速配置 -->
      <el-card shadow="never" style="margin-top: 20px;">
        <template #header>
          <span>快速配置</span>
        </template>
        <el-space wrap>
          <el-button type="primary" plain @click="applyStandardConfig">
            应用标准配置
          </el-button>
          <el-button type="success" plain @click="applyCompactConfig">
            应用紧凑配置
          </el-button>
          <el-button type="warning" plain @click="applyRelaxedConfig">
            应用宽松配置
          </el-button>
          <el-button type="info" plain @click="handleReset">
            重置为默认
          </el-button>
        </el-space>
      </el-card>
    </el-card>

    <!-- 字段配置对话框 -->
    <FieldConfigDialog
      ref="fieldDialogRef"
      module="timeout"
      module-name="超时设置"
      @update="refreshFields"
    />
  </div>
</template>

<style scoped>
.timeout-page {
  width: 100%;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.header-actions {
  display: flex;
  gap: 10px;
}
.color-stat {
  text-align: center;
}
.stat-content {
  display: flex;
  flex-direction: column;
  align-items: center;
}
.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: #409EFF;
}
.stat-label {
  font-size: 13px;
  color: #909399;
  margin-top: 5px;
}
.rule-cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
}
.hours-label {
  font-size: 12px;
  color: #909399;
}
.explain-list {
  margin: 0;
  padding-left: 20px;
  line-height: 2;
  color: #606266;
}
</style>
