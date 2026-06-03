<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getDeliveryList } from '@/api/delivery'

const loading = ref(false)
const list = ref<any[]>([])

const loadData = async () => {
  loading.value = true
  try {
    const res: any = await getDeliveryList({ page: 1, pageSize: 100 })
    if (res.success !== false) {
      list.value = res.data || res
    }
  } catch (error) {
    console.error('加载失败:', error)
  }
  loading.value = false
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = {
    pending: 'warning',
    picked_up: 'primary',
    in_transit: 'info',
    delivered: 'success',
    cancelled: 'danger'
  }
  return map[status] || 'info'
}

const getStatusLabel = (status: string) => {
  const map: Record<string, string> = {
    pending: '待取货',
    picked_up: '已取货',
    in_transit: '配送中',
    delivered: '已送达',
    cancelled: '已取消'
  }
  return map[status] || status
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="delivery-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>配送订单管理</span>
          <el-button @click="loadData" :loading="loading">刷新</el-button>
        </div>
      </template>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="orderNumber" label="订单号" width="140" show-overflow-tooltip />
        <el-table-column prop="customerName" label="客户姓名" width="100" />
        <el-table-column prop="customerPhone" label="联系电话" width="120" />
        <el-table-column prop="roomNumber" label="房号" width="80" />
        <el-table-column prop="merchantName" label="商家" width="120" show-overflow-tooltip />
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="totalAmount" label="金额" width="90">
          <template #default="{ row }">
            ¥{{ (row.totalAmount || 0).toFixed(2) }}
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="下单时间" width="160" />
      </el-table>
    </el-card>
  </div>
</template>

<style scoped>
.delivery-page { padding: 20px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
</style>
