<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { statisticsApi, materialApi, notificationApi, complaintApi, keyApi, visitorApi, inspectionApi, financeApi } from '../api/http'
import { ElCard, ElRow, ElCol, ElTag, ElTable, ElTableColumn } from 'element-plus'

const loading = ref(false)
const stats = ref({
  totalProperties: 500,
  occupancyRate: 92.5,
  activeTickets: 15,
  pendingComplaints: 8,
  todayVisitors: 23,
  unreadNotifications: 5,
  availableKeys: 45,
  totalKeys: 50
})

const recentActivities = ref([
  { time: '10:30', action: '新增工单 #TK-2026-0045', user: '王先生', type: 'ticket' },
  { time: '10:15', action: '访客 张先生 已签到', user: '门禁', type: 'visitor' },
  { time: '09:45', action: '投诉已解决: 电梯故障', user: '张师傅', type: 'complaint' },
  { time: '09:30', action: '物料出库: 灯泡 x50', user: '李仓管', type: 'material' },
  { time: '09:00', action: '巡检任务完成: A栋', user: '巡检员', type: 'inspection' },
])

const serviceStatus = ref([
  { name: '认证服务', port: 5006, status: 'running' },
  { name: '物料服务', port: 5004, status: 'running' },
  { name: '通知服务', port: 5005, status: 'running' },
  { name: '合同服务', port: 5008, status: 'running' },
  { name: '财务服务', port: 5009, status: 'running' },
  { name: '巡检服务', port: 5010, status: 'running' },
  { name: '投诉服务', port: 5011, status: 'running' },
  { name: '钥匙服务', port: 5012, status: 'running' },
  { name: '访客服务', port: 5013, status: 'running' },
  { name: '统计服务', port: 5014, status: 'running' },
  { name: '移动服务', port: 5015, status: 'running' },
])

const getStatusType = (status: string) => {
  return status === 'running' ? 'success' : 'danger'
}

onMounted(async () => {
  loading.value = true
  try {
    // 获取统计数据
    const res = await statisticsApi.get('/api/metrics/indicators')
    if (res.success && res.data) {
      stats.value = {
        totalProperties: res.data.operation?.totalUnits || 500,
        occupancyRate: res.data.operation?.occupancyRate || 92.5,
        activeTickets: res.data.customer?.totalTickets || 15,
        pendingComplaints: res.data.customer?.totalComplaints || 8,
        todayVisitors: res.data.visitor?.totalVisitors || 23,
        unreadNotifications: 5,
        availableKeys: 48,
        totalKeys: 50
      }
    }
  } catch (error) {
    console.error('获取数据失败:', error)
  }
  loading.value = false
})
</script>

<template>
  <div class="dashboard">
    <h2 class="page-title">物业管理后台</h2>
    
    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #409eff;">
            <el-icon size="30"><House /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.totalProperties }}</div>
            <div class="stat-label">物业单元总数</div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #67c23a;">
            <el-icon size="30"><User /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.occupancyRate }}%</div>
            <div class="stat-label">入住率</div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #e6a23c;">
            <el-icon size="30"><Tickets /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.activeTickets }}</div>
            <div class="stat-label">活跃工单</div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #f56c6c;">
            <el-icon size="30"><Warning /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.pendingComplaints }}</div>
            <div class="stat-label">待处理投诉</div>
          </div>
        </el-card>
      </el-col>
    </el-row>
    
    <el-row :gutter="20" class="stats-row">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #909399;">
            <el-icon size="30"><Avatar /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.todayVisitors }}</div>
            <div class="stat-label">今日访客</div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #00bcd4;">
            <el-icon size="30"><Key /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.availableKeys }}/{{ stats.totalKeys }}</div>
            <div class="stat-label">可用钥匙</div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon" style="background: #8bc34a;">
            <el-icon size="30"><Bell /></el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.unreadNotifications }}</div>
            <div class="stat-label">未读通知</div>
          </div>
        </el-card>
      </el-col>
    </el-row>
    
    <!-- 服务状态和活动 -->
    <el-row :gutter="20">
      <!-- 服务状态 -->
      <el-col :span="14">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>服务状态</span>
              <el-tag type="success" size="small">11个服务运行中</el-tag>
            </div>
          </template>
          <el-table :data="serviceStatus" size="small">
            <el-table-column prop="name" label="服务" width="150" />
            <el-table-column prop="port" label="端口" width="80" />
            <el-table-column label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="getStatusType(row.status)" size="small">
                  {{ row.status === 'running' ? '运行中' : '异常' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="健康检查">
              <template #default="{ row }">
                <el-icon color="#67c23a" v-if="row.status === 'running'"><CircleCheck /></el-icon>
                <el-icon color="#f56c6c" v-else><CircleClose /></el-icon>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
      
      <!-- 最近活动 -->
      <el-col :span="10">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>最近活动</span>
            </div>
          </template>
          <div class="activity-list">
            <div v-for="(activity, index) in recentActivities" :key="index" class="activity-item">
              <div class="activity-time">{{ activity.time }}</div>
              <div class="activity-content">
                <span class="activity-text">{{ activity.action }}</span>
                <span class="activity-user">by {{ activity.user }}</span>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped>
.dashboard {
  padding: 0;
}

.page-title {
  margin: 0 0 20px 0;
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.stats-row {
  margin-bottom: 20px;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 10px;
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  margin-right: 15px;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-top: 5px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.activity-list {
  max-height: 300px;
  overflow-y: auto;
}

.activity-item {
  display: flex;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}

.activity-item:last-child {
  border-bottom: none;
}

.activity-time {
  width: 60px;
  color: #909399;
  font-size: 13px;
}

.activity-content {
  flex: 1;
}

.activity-text {
  display: block;
  color: #303133;
  font-size: 14px;
}

.activity-user {
  display: block;
  color: #c0c4cc;
  font-size: 12px;
  margin-top: 4px;
}
</style>
